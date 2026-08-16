using CONATRADEC.Models;
using Microsoft.Maui.ApplicationModel;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace CONATRADEC.Services
{
    public sealed class DepartamentoApiService
    {
        private const string CodigoDepartamentoInactivo =
            "DEPARTAMENTO_INACTIVO_EXISTENTE";

        private const string OpcionReactivar =
            "Reactivar y usar estos datos";

        private const string OpcionCrearNuevo =
            "Crear un registro diferente";

        private readonly HttpClient httpClient;

        private readonly JsonSerializerOptions jsonOptions =
            new(JsonSerializerDefaults.Web)
            {
                PropertyNameCaseInsensitive = true
            };

        private sealed record CacheEntry(
            List<DepartamentoResponse> Items,
            DateTime CreadoUtc);

        private static readonly ConcurrentDictionary<int, CacheEntry>
            CachePorPais = new();

        private static readonly ConcurrentDictionary<int, SemaphoreSlim>
            BloqueosPorPais = new();

        private static readonly TimeSpan DuracionCache =
            TimeSpan.FromMinutes(30);

        public DepartamentoApiService()
            : this(ApiClientService.Client)
        {
        }

        public DepartamentoApiService(HttpClient httpClient)
        {
            this.httpClient = httpClient
                ?? throw new ArgumentNullException(nameof(httpClient));
        }

        /// <summary>
        /// Endpoint completo conservado para pickers y formularios históricos.
        /// La administración actual utiliza api/administracion/ubicaciones.
        /// </summary>
        public Task<ApiResult<ObservableCollection<DepartamentoResponse>>>
            GetDepartamentosResultAsync(
                int? paisId,
                CancellationToken cancellationToken = default)
        {
            if (!paisId.HasValue || paisId.Value <= 0)
            {
                return Task.FromResult(
                    ApiResult<ObservableCollection<DepartamentoResponse>>.Fail(
                        "No se recibió un país válido para cargar sus departamentos."));
            }

            return ApiServiceHelper.GetCollectionAsync<DepartamentoResponse>(
                httpClient,
                $"api/departamento/por-pais/{paisId.Value}",
                "los departamentos",
                cancellationToken);
        }

        public async Task<ApiResult<DepartamentoPaginaResponse>>
            BuscarDepartamentosAsync(
                int paisId,
                string? buscar,
                int pagina,
                int tamanoPagina,
                CancellationToken cancellationToken = default)
        {
            if (paisId <= 0)
            {
                return ApiResult<DepartamentoPaginaResponse>.Fail(
                    "No se recibió un país válido.");
            }

            pagina = Math.Max(1, pagina);
            tamanoPagina = Math.Clamp(tamanoPagina, 5, 100);

            string ruta =
                "api/administracion/ubicaciones/departamentos" +
                $"?paisId={paisId}" +
                $"&pagina={pagina}" +
                $"&tamanoPagina={tamanoPagina}" +
                "&incluirInactivos=false";

            if (!string.IsNullOrWhiteSpace(buscar))
            {
                ruta +=
                    $"&buscar={Uri.EscapeDataString(buscar.Trim())}";
            }

            try
            {
                using HttpResponseMessage response =
                    await httpClient.GetAsync(
                        ruta,
                        cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    return ApiResult<DepartamentoPaginaResponse>.Fail(
                        await ApiServiceHelper.ReadResponseMessageAsync(
                            response,
                            "No fue posible cargar los departamentos.",
                            cancellationToken),
                        (int)response.StatusCode);
                }

                PaginaAdminResponse<DepartamentoAdminItem>? data =
                    await response.Content
                        .ReadFromJsonAsync<PaginaAdminResponse<DepartamentoAdminItem>>(
                            jsonOptions,
                            cancellationToken);

                if (data == null)
                {
                    return ApiResult<DepartamentoPaginaResponse>.Fail(
                        "El servidor no devolvió la página de departamentos esperada.");
                }

                string nombrePais = data.Items
                    .Select(item => item.NombrePais)
                    .FirstOrDefault(nombre =>
                        !string.IsNullOrWhiteSpace(nombre))
                    ?? string.Empty;

                return ApiResult<DepartamentoPaginaResponse>.Ok(
                    new DepartamentoPaginaResponse
                    {
                        Items = data.Items
                            .Where(item =>
                                item.DepartamentoId > 0 &&
                                item.Activo)
                            .Select(MapearDepartamento)
                            .ToList(),
                        PaginaActual = data.PaginaActual,
                        TamanoPagina = data.TamanoPagina,
                        TotalRegistros = data.TotalRegistros,
                        TotalPaginas = data.TotalPaginas,
                        PaisId = paisId,
                        NombrePais = nombrePais
                    });
            }
            catch (TaskCanceledException)
                when (!cancellationToken.IsCancellationRequested)
            {
                return ApiResult<DepartamentoPaginaResponse>.Fail(
                    "La carga de departamentos tardó demasiado. Intente nuevamente.");
            }
            catch (OperationCanceledException)
            {
                return ApiResult<DepartamentoPaginaResponse>.Fail(
                    "La operación fue cancelada.");
            }
            catch (HttpRequestException)
            {
                return ApiResult<DepartamentoPaginaResponse>.Fail(
                    "No fue posible comunicarse con el servidor para cargar los departamentos.");
            }
            catch (JsonException)
            {
                return ApiResult<DepartamentoPaginaResponse>.Fail(
                    "El servidor respondió, pero el listado de departamentos no tiene el formato esperado.");
            }
            catch
            {
                return ApiResult<DepartamentoPaginaResponse>.Fail(
                    "Ocurrió un error inesperado al cargar los departamentos.");
            }
        }

        public async Task<ApiResult<DepartamentoResponse>>
            CreateDepartamentoResultAsync(
                DepartamentoRequest departamento,
                CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(departamento);

            if (!departamento.PaisId.HasValue ||
                departamento.PaisId.Value <= 0)
            {
                return ApiResult<DepartamentoResponse>.Fail(
                    "No se recibió un país válido.");
            }

            ApiResult<DepartamentoResponse> result =
                await EnviarDepartamentoAdministracionAsync(
                    HttpMethod.Post,
                    "api/administracion/ubicaciones/departamentos",
                    departamento,
                    "crear el departamento",
                    "Departamento creado correctamente.",
                    cancellationToken,
                    manejarInactivo: true);

            if (result.Success)
                LimpiarCache(departamento.PaisId.Value);

            return result;
        }

        public async Task<ApiResult<DepartamentoResponse>>
            UpdateDepartamentoResultAsync(
                DepartamentoRequest departamento,
                CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(departamento);

            if (!departamento.DepartamentoId.HasValue ||
                departamento.DepartamentoId.Value <= 0)
            {
                return ApiResult<DepartamentoResponse>.Fail(
                    "No se recibió un identificador de departamento válido.");
            }

            if (!departamento.PaisId.HasValue ||
                departamento.PaisId.Value <= 0)
            {
                return ApiResult<DepartamentoResponse>.Fail(
                    "No se recibió un país válido.");
            }

            ApiResult<DepartamentoResponse> result =
                await EnviarDepartamentoAdministracionAsync(
                    HttpMethod.Put,
                    $"api/administracion/ubicaciones/departamentos/{departamento.DepartamentoId.Value}",
                    departamento,
                    "actualizar el departamento",
                    "Departamento actualizado correctamente.",
                    cancellationToken);

            if (result.Success)
                LimpiarCache(departamento.PaisId.Value);

            return result;
        }

        public async Task<ApiResult<bool>> DeleteDepartamentoResultAsync(
            DepartamentoRequest departamento,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(departamento);

            if (!departamento.DepartamentoId.HasValue ||
                departamento.DepartamentoId.Value <= 0)
            {
                return ApiResult<bool>.Fail(
                    "No se recibió un identificador de departamento válido.");
            }

            ApiResult<bool> result =
                await ApiServiceHelper.SendAsync<DepartamentoAdministracionRequest>(
                    httpClient,
                    HttpMethod.Delete,
                    $"api/administracion/ubicaciones/departamentos/{departamento.DepartamentoId.Value}",
                    null,
                    "eliminar el departamento",
                    "Departamento eliminado correctamente.",
                    cancellationToken);

            if (result.Success && departamento.PaisId.HasValue)
                LimpiarCache(departamento.PaisId.Value);

            return result;
        }

        public async Task<ObservableCollection<DepartamentoResponse>>
            GetDepartamentosAsync(int? paisId)
        {
            if (!paisId.HasValue || paisId.Value <= 0)
                return new ObservableCollection<DepartamentoResponse>();

            int id = paisId.Value;

            if (ObtenerCacheVigente(id) is List<DepartamentoResponse> cache)
                return new ObservableCollection<DepartamentoResponse>(cache);

            SemaphoreSlim bloqueo = BloqueosPorPais.GetOrAdd(
                id,
                _ => new SemaphoreSlim(1, 1));

            await bloqueo.WaitAsync();

            try
            {
                if (ObtenerCacheVigente(id) is List<DepartamentoResponse> vigente)
                {
                    return new ObservableCollection<DepartamentoResponse>(
                        vigente);
                }

                ApiResult<ObservableCollection<DepartamentoResponse>> result =
                    await GetDepartamentosResultAsync(id);

                List<DepartamentoResponse> items = result.Data?
                    .Where(item => item.DepartamentoId is > 0)
                    .ToList()
                    ?? new List<DepartamentoResponse>();

                CachePorPais[id] =
                    new CacheEntry(items, DateTime.UtcNow);

                return new ObservableCollection<DepartamentoResponse>(items);
            }
            finally
            {
                bloqueo.Release();
            }
        }

        public async Task<bool> CreateDepartamentoAsync(
            DepartamentoRequest departamento)
        {
            ApiResult<DepartamentoResponse> result =
                await CreateDepartamentoResultAsync(departamento);

            return result.Success &&
                   result.Data?.DepartamentoId is > 0;
        }

        public async Task<bool> UpdateDepartamentoAsync(
            DepartamentoRequest departamento)
        {
            ApiResult<DepartamentoResponse> result =
                await UpdateDepartamentoResultAsync(departamento);

            return result.Success &&
                   result.Data?.DepartamentoId is > 0;
        }

        public async Task<bool> DeleteDepartamentoAsync(
            DepartamentoRequest departamento)
        {
            ApiResult<bool> result =
                await DeleteDepartamentoResultAsync(departamento);

            return result.Success && result.Data == true;
        }

        private async Task<ApiResult<DepartamentoResponse>>
            EnviarDepartamentoAdministracionAsync(
                HttpMethod method,
                string route,
                DepartamentoRequest departamento,
                string accion,
                string mensajeExito,
                CancellationToken cancellationToken,
                bool manejarInactivo = false)
        {
            var request = new DepartamentoAdministracionRequest
            {
                PaisId = departamento.PaisId ?? 0,
                Nombre = departamento.NombreDepartamento ?? string.Empty
            };

            try
            {
                using var message =
                    new HttpRequestMessage(method, route)
                    {
                        Content = JsonContent.Create(
                            request,
                            options: jsonOptions)
                    };

                using HttpResponseMessage response =
                    await httpClient.SendAsync(
                        message,
                        cancellationToken);

                UbicacionOperacionEnvelope<DepartamentoAdminItem>? envelope =
                    await LeerEnvelopeAsync<DepartamentoAdminItem>(
                        response,
                        cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    if (envelope?.Data == null ||
                        envelope.Data.DepartamentoId <= 0)
                    {
                        return ApiResult<DepartamentoResponse>.Fail(
                            "La operación se procesó, pero el servidor no devolvió el departamento actualizado.");
                    }

                    return ApiResult<DepartamentoResponse>.Ok(
                        MapearDepartamento(envelope.Data),
                        string.IsNullOrWhiteSpace(envelope.Message)
                            ? mensajeExito
                            : envelope.Message);
                }

                if (manejarInactivo &&
                    response.StatusCode == HttpStatusCode.Conflict &&
                    string.Equals(
                        envelope?.Code,
                        CodigoDepartamentoInactivo,
                        StringComparison.OrdinalIgnoreCase) &&
                    envelope?.Data?.DepartamentoId > 0)
                {
                    return await ResolverDepartamentoInactivoAsync(
                        departamento,
                        envelope.Data,
                        cancellationToken);
                }

                return ApiResult<DepartamentoResponse>.Fail(
                    string.IsNullOrWhiteSpace(envelope?.Message)
                        ? await ApiServiceHelper.ReadResponseMessageAsync(
                            response,
                            $"No fue posible {accion}.",
                            cancellationToken)
                        : envelope.Message,
                    (int)response.StatusCode);
            }
            catch (TaskCanceledException)
                when (!cancellationToken.IsCancellationRequested)
            {
                return ApiResult<DepartamentoResponse>.Fail(
                    "La solicitud tardó demasiado. Intente nuevamente.");
            }
            catch (OperationCanceledException)
            {
                return ApiResult<DepartamentoResponse>.Fail(
                    "La operación fue cancelada.");
            }
            catch (HttpRequestException)
            {
                return ApiResult<DepartamentoResponse>.Fail(
                    "No fue posible comunicarse con el servidor.");
            }
            catch (JsonException)
            {
                return ApiResult<DepartamentoResponse>.Fail(
                    "El servidor respondió con un formato inesperado.");
            }
            catch
            {
                return ApiResult<DepartamentoResponse>.Fail(
                    $"Ocurrió un error inesperado al {accion}.");
            }
        }

        private async Task<ApiResult<DepartamentoResponse>>
            ResolverDepartamentoInactivoAsync(
                DepartamentoRequest nuevoDepartamento,
                DepartamentoAdminItem inactivo,
                CancellationToken cancellationToken)
        {
            string? decision =
                await MostrarOpcionesDepartamentoInactivoAsync(
                    inactivo);

            if (decision == OpcionReactivar)
            {
                var request = new DepartamentoRequest
                {
                    DepartamentoId = inactivo.DepartamentoId,
                    PaisId = nuevoDepartamento.PaisId,
                    NombreDepartamento =
                        nuevoDepartamento.NombreDepartamento
                };

                return await EnviarDepartamentoAdministracionAsync(
                    HttpMethod.Put,
                    $"api/administracion/ubicaciones/departamentos/{inactivo.DepartamentoId}/reactivar",
                    request,
                    "reactivar el departamento",
                    "Departamento reactivado correctamente.",
                    cancellationToken);
            }

            if (decision == OpcionCrearNuevo)
            {
                return await EnviarDepartamentoAdministracionAsync(
                    HttpMethod.Post,
                    "api/administracion/ubicaciones/departamentos?crearNuevoSiExisteInactivo=true",
                    nuevoDepartamento,
                    "crear el departamento",
                    "Departamento creado correctamente.",
                    cancellationToken);
            }

            return ApiResult<DepartamentoResponse>.Fail(
                "La creación fue cancelada.");
        }

        private static async Task<string?>
            MostrarOpcionesDepartamentoInactivoAsync(
                DepartamentoAdminItem departamento)
        {
            return await MainThread.InvokeOnMainThreadAsync(
                async () =>
                {
                    Page? pagina =
                        Application.Current?
                            .Windows
                            .FirstOrDefault()?
                            .Page;

                    if (pagina == null)
                        return null;

                    string nombre =
                        string.IsNullOrWhiteSpace(departamento.Nombre)
                            ? "el departamento eliminado"
                            : $"'{departamento.Nombre}'";

                    return await pagina.DisplayActionSheet(
                        $"Ya existe {nombre}. Puede reactivarlo conservando su identificador e historial.",
                        "Cancelar",
                        null,
                        OpcionReactivar,
                        OpcionCrearNuevo);
                });
        }

        private async Task<UbicacionOperacionEnvelope<T>?>
            LeerEnvelopeAsync<T>(
                HttpResponseMessage response,
                CancellationToken cancellationToken)
        {
            string contenido =
                await response.Content.ReadAsStringAsync(
                    cancellationToken);

            if (string.IsNullOrWhiteSpace(contenido))
                return null;

            return JsonSerializer.Deserialize<
                UbicacionOperacionEnvelope<T>>(
                    contenido,
                    jsonOptions);
        }

        private static DepartamentoResponse MapearDepartamento(
            DepartamentoAdminItem item) =>
            new()
            {
                DepartamentoId = item.DepartamentoId,
                NombreDepartamento = item.Nombre ?? string.Empty,
                PaisId = item.PaisId,
                NombrePais = item.NombrePais ?? string.Empty,
                Activo = item.Activo,
                CantidadMunicipios =
                    item.CantidadDependencias
            };

        private static List<DepartamentoResponse>? ObtenerCacheVigente(
            int paisId)
        {
            if (!CachePorPais.TryGetValue(
                    paisId,
                    out CacheEntry? entry))
            {
                return null;
            }

            if (DateTime.UtcNow - entry.CreadoUtc >= DuracionCache)
            {
                CachePorPais.TryRemove(paisId, out _);
                return null;
            }

            return entry.Items;
        }

        private static void LimpiarCache(int paisId)
        {
            if (paisId > 0)
                CachePorPais.TryRemove(paisId, out _);
        }

        private sealed class PaginaAdminResponse<T>
        {
            public List<T> Items { get; set; } = new();
            public int PaginaActual { get; set; }
            public int TamanoPagina { get; set; }
            public int TotalRegistros { get; set; }
            public int TotalPaginas { get; set; }
        }

        private sealed class UbicacionOperacionEnvelope<T>
        {
            public bool Success { get; set; }
            public string? Code { get; set; }
            public string? Message { get; set; }
            public T? Data { get; set; }
        }

        private sealed class DepartamentoAdminItem
        {
            public int DepartamentoId { get; set; }
            public int PaisId { get; set; }
            public string NombrePais { get; set; } = string.Empty;
            public string Nombre { get; set; } = string.Empty;
            public bool Activo { get; set; }
            public int CantidadDependencias { get; set; }
        }

        private sealed class DepartamentoAdministracionRequest
        {
            public int PaisId { get; set; }
            public string Nombre { get; set; } = string.Empty;
        }
    }
}
