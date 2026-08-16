using CONATRADEC.Models;
using Microsoft.Maui.ApplicationModel;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace CONATRADEC.Services
{
    public sealed class MunicipioApiService
    {
        private const string CodigoMunicipioInactivo =
            "MUNICIPIO_INACTIVO_EXISTENTE";

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
            List<MunicipioResponse> Items,
            DateTime CreadoUtc);

        private static readonly ConcurrentDictionary<int, CacheEntry>
            CachePorDepartamento = new();

        private static readonly ConcurrentDictionary<int, SemaphoreSlim>
            BloqueosPorDepartamento = new();

        private static readonly TimeSpan DuracionCache =
            TimeSpan.FromMinutes(30);

        public MunicipioApiService()
            : this(ApiClientService.Client)
        {
        }

        public MunicipioApiService(HttpClient httpClient)
        {
            this.httpClient = httpClient
                ?? throw new ArgumentNullException(nameof(httpClient));
        }

        /// <summary>
        /// Listado completo histórico conservado para formularios y selectores.
        /// </summary>
        public Task<ApiResult<ObservableCollection<MunicipioResponse>>>
            GetMunicipiosResultAsync(
                int? departamentoId,
                CancellationToken cancellationToken = default)
        {
            if (!departamentoId.HasValue || departamentoId.Value <= 0)
            {
                return Task.FromResult(
                    ApiResult<ObservableCollection<MunicipioResponse>>.Fail(
                        "Seleccione un departamento válido."));
            }

            return ApiServiceHelper.GetCollectionAsync<MunicipioResponse>(
                httpClient,
                $"api/municipio/por-departamento/{departamentoId.Value}",
                "los municipios",
                cancellationToken);
        }

        /// <summary>
        /// Listado geográfico histórico utilizado por Terreno y Usuario.
        /// </summary>
        public Task<ApiResult<ObservableCollection<MunicipioResponse>>>
            GetMunicipiosConUbicacionResultAsync(
                CancellationToken cancellationToken = default)
        {
            return ApiServiceHelper.GetCollectionAsync<MunicipioResponse>(
                httpClient,
                "api/municipio/listarTodos-por-departamento-por-pais",
                "los municipios",
                cancellationToken);
        }

        public async Task<ApiResult<MunicipioPaginaResponse>>
            BuscarMunicipiosAsync(
                int departamentoId,
                string? buscar,
                int pagina,
                int tamanoPagina,
                CancellationToken cancellationToken = default)
        {
            if (departamentoId <= 0)
            {
                return ApiResult<MunicipioPaginaResponse>.Fail(
                    "No se recibió un departamento válido.");
            }

            pagina = Math.Max(1, pagina);
            tamanoPagina = Math.Clamp(tamanoPagina, 5, 100);

            string ruta =
                "api/administracion/ubicaciones/municipios" +
                $"?departamentoId={departamentoId}" +
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
                    return ApiResult<MunicipioPaginaResponse>.Fail(
                        await ApiServiceHelper.ReadResponseMessageAsync(
                            response,
                            "No fue posible cargar los municipios.",
                            cancellationToken),
                        (int)response.StatusCode);
                }

                PaginaAdminResponse<MunicipioAdminItem>? data =
                    await response.Content
                        .ReadFromJsonAsync<PaginaAdminResponse<MunicipioAdminItem>>(
                            jsonOptions,
                            cancellationToken);

                if (data == null)
                {
                    return ApiResult<MunicipioPaginaResponse>.Fail(
                        "El servidor no devolvió la página de municipios esperada.");
                }

                MunicipioAdminItem? ubicacion = data.Items
                    .FirstOrDefault();

                return ApiResult<MunicipioPaginaResponse>.Ok(
                    new MunicipioPaginaResponse
                    {
                        Items = data.Items
                            .Where(item =>
                                item.MunicipioId > 0 &&
                                item.Activo)
                            .Select(MapearMunicipio)
                            .ToList(),
                        PaginaActual = data.PaginaActual,
                        TamanoPagina = data.TamanoPagina,
                        TotalRegistros = data.TotalRegistros,
                        TotalPaginas = data.TotalPaginas,
                        DepartamentoId = departamentoId,
                        NombreDepartamento =
                            ubicacion?.NombreDepartamento ?? string.Empty,
                        PaisId = ubicacion?.PaisId ?? 0,
                        NombrePais =
                            ubicacion?.NombrePais ?? string.Empty
                    });
            }
            catch (TaskCanceledException)
                when (!cancellationToken.IsCancellationRequested)
            {
                return ApiResult<MunicipioPaginaResponse>.Fail(
                    "La carga de municipios tardó demasiado. Intente nuevamente.");
            }
            catch (OperationCanceledException)
            {
                return ApiResult<MunicipioPaginaResponse>.Fail(
                    "La operación fue cancelada.");
            }
            catch (HttpRequestException)
            {
                return ApiResult<MunicipioPaginaResponse>.Fail(
                    "No fue posible comunicarse con el servidor para cargar los municipios.");
            }
            catch (JsonException)
            {
                return ApiResult<MunicipioPaginaResponse>.Fail(
                    "El servidor respondió, pero el listado de municipios no tiene el formato esperado.");
            }
            catch
            {
                return ApiResult<MunicipioPaginaResponse>.Fail(
                    "Ocurrió un error inesperado al cargar los municipios.");
            }
        }

        public async Task<ApiResult<MunicipioResponse>>
            CreateMunicipioResultAsync(
                MunicipioRequest municipio,
                CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(municipio);

            if (!municipio.DepartamentoId.HasValue ||
                municipio.DepartamentoId.Value <= 0)
            {
                return ApiResult<MunicipioResponse>.Fail(
                    "No se recibió un departamento válido.");
            }

            ApiResult<MunicipioResponse> result =
                await EnviarMunicipioAdministracionAsync(
                    HttpMethod.Post,
                    "api/administracion/ubicaciones/municipios",
                    municipio,
                    "crear el municipio",
                    "Municipio creado correctamente.",
                    cancellationToken,
                    manejarInactivo: true);

            if (result.Success)
                LimpiarCache(municipio.DepartamentoId.Value);

            return result;
        }

        public async Task<ApiResult<MunicipioResponse>>
            UpdateMunicipioResultAsync(
                MunicipioRequest municipio,
                CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(municipio);

            if (!municipio.MunicipioId.HasValue ||
                municipio.MunicipioId.Value <= 0)
            {
                return ApiResult<MunicipioResponse>.Fail(
                    "No se recibió un identificador de municipio válido.");
            }

            if (!municipio.DepartamentoId.HasValue ||
                municipio.DepartamentoId.Value <= 0)
            {
                return ApiResult<MunicipioResponse>.Fail(
                    "No se recibió un departamento válido.");
            }

            ApiResult<MunicipioResponse> result =
                await EnviarMunicipioAdministracionAsync(
                    HttpMethod.Put,
                    $"api/administracion/ubicaciones/municipios/{municipio.MunicipioId.Value}",
                    municipio,
                    "actualizar el municipio",
                    "Municipio actualizado correctamente.",
                    cancellationToken);

            if (result.Success)
                LimpiarCache(municipio.DepartamentoId.Value);

            return result;
        }

        public async Task<ApiResult<bool>> DeleteMunicipioResultAsync(
            MunicipioRequest municipio,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(municipio);

            if (!municipio.MunicipioId.HasValue ||
                municipio.MunicipioId.Value <= 0)
            {
                return ApiResult<bool>.Fail(
                    "No se recibió un identificador de municipio válido.");
            }

            ApiResult<bool> result =
                await ApiServiceHelper.SendAsync<MunicipioAdministracionRequest>(
                    httpClient,
                    HttpMethod.Delete,
                    $"api/administracion/ubicaciones/municipios/{municipio.MunicipioId.Value}",
                    null,
                    "eliminar el municipio",
                    "Municipio eliminado correctamente.",
                    cancellationToken);

            if (result.Success && municipio.DepartamentoId.HasValue)
                LimpiarCache(municipio.DepartamentoId.Value);

            return result;
        }

        public async Task<ObservableCollection<MunicipioResponse>>
            GetMunicipiosAsync(int? departamentoId)
        {
            if (!departamentoId.HasValue || departamentoId.Value <= 0)
                return new ObservableCollection<MunicipioResponse>();

            int id = departamentoId.Value;

            if (ObtenerCacheVigente(id) is List<MunicipioResponse> cache)
                return new ObservableCollection<MunicipioResponse>(cache);

            SemaphoreSlim bloqueo = BloqueosPorDepartamento.GetOrAdd(
                id,
                _ => new SemaphoreSlim(1, 1));

            await bloqueo.WaitAsync();

            try
            {
                if (ObtenerCacheVigente(id) is List<MunicipioResponse> vigente)
                    return new ObservableCollection<MunicipioResponse>(vigente);

                ApiResult<ObservableCollection<MunicipioResponse>> result =
                    await GetMunicipiosResultAsync(id);

                List<MunicipioResponse> items = result.Data?
                    .Where(item => item.MunicipioId is > 0)
                    .ToList()
                    ?? new List<MunicipioResponse>();

                CachePorDepartamento[id] =
                    new CacheEntry(items, DateTime.UtcNow);

                return new ObservableCollection<MunicipioResponse>(items);
            }
            finally
            {
                bloqueo.Release();
            }
        }

        public async Task<bool> CreateMunicipioAsync(
            MunicipioRequest municipio)
        {
            ApiResult<MunicipioResponse> result =
                await CreateMunicipioResultAsync(municipio);

            return result.Success &&
                   result.Data?.MunicipioId is > 0;
        }

        public async Task<bool> UpdateMunicipioAsync(
            MunicipioRequest municipio)
        {
            ApiResult<MunicipioResponse> result =
                await UpdateMunicipioResultAsync(municipio);

            return result.Success &&
                   result.Data?.MunicipioId is > 0;
        }

        public async Task<bool> DeleteMunicipioAsync(
            MunicipioRequest municipio)
        {
            ApiResult<bool> result =
                await DeleteMunicipioResultAsync(municipio);

            return result.Success && result.Data == true;
        }

        private async Task<ApiResult<MunicipioResponse>>
            EnviarMunicipioAdministracionAsync(
                HttpMethod method,
                string route,
                MunicipioRequest municipio,
                string accion,
                string mensajeExito,
                CancellationToken cancellationToken,
                bool manejarInactivo = false)
        {
            var request = new MunicipioAdministracionRequest
            {
                DepartamentoId = municipio.DepartamentoId ?? 0,
                Nombre = municipio.NombreMunicipio ?? string.Empty
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

                UbicacionOperacionEnvelope<MunicipioAdminItem>? envelope =
                    await LeerEnvelopeAsync<MunicipioAdminItem>(
                        response,
                        cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    if (envelope?.Data == null ||
                        envelope.Data.MunicipioId <= 0)
                    {
                        return ApiResult<MunicipioResponse>.Fail(
                            "La operación se procesó, pero el servidor no devolvió el municipio actualizado.");
                    }

                    return ApiResult<MunicipioResponse>.Ok(
                        MapearMunicipio(envelope.Data),
                        string.IsNullOrWhiteSpace(envelope.Message)
                            ? mensajeExito
                            : envelope.Message);
                }

                if (manejarInactivo &&
                    response.StatusCode == HttpStatusCode.Conflict &&
                    string.Equals(
                        envelope?.Code,
                        CodigoMunicipioInactivo,
                        StringComparison.OrdinalIgnoreCase) &&
                    envelope?.Data?.MunicipioId > 0)
                {
                    return await ResolverMunicipioInactivoAsync(
                        municipio,
                        envelope.Data,
                        cancellationToken);
                }

                return ApiResult<MunicipioResponse>.Fail(
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
                return ApiResult<MunicipioResponse>.Fail(
                    "La solicitud tardó demasiado. Intente nuevamente.");
            }
            catch (OperationCanceledException)
            {
                return ApiResult<MunicipioResponse>.Fail(
                    "La operación fue cancelada.");
            }
            catch (HttpRequestException)
            {
                return ApiResult<MunicipioResponse>.Fail(
                    "No fue posible comunicarse con el servidor.");
            }
            catch (JsonException)
            {
                return ApiResult<MunicipioResponse>.Fail(
                    "El servidor respondió con un formato inesperado.");
            }
            catch
            {
                return ApiResult<MunicipioResponse>.Fail(
                    $"Ocurrió un error inesperado al {accion}.");
            }
        }

        private async Task<ApiResult<MunicipioResponse>>
            ResolverMunicipioInactivoAsync(
                MunicipioRequest nuevoMunicipio,
                MunicipioAdminItem inactivo,
                CancellationToken cancellationToken)
        {
            string? decision =
                await MostrarOpcionesMunicipioInactivoAsync(
                    inactivo);

            if (decision == OpcionReactivar)
            {
                var request = new MunicipioRequest
                {
                    MunicipioId = inactivo.MunicipioId,
                    DepartamentoId = nuevoMunicipio.DepartamentoId,
                    NombreMunicipio = nuevoMunicipio.NombreMunicipio
                };

                return await EnviarMunicipioAdministracionAsync(
                    HttpMethod.Put,
                    $"api/administracion/ubicaciones/municipios/{inactivo.MunicipioId}/reactivar",
                    request,
                    "reactivar el municipio",
                    "Municipio reactivado correctamente.",
                    cancellationToken);
            }

            if (decision == OpcionCrearNuevo)
            {
                return await EnviarMunicipioAdministracionAsync(
                    HttpMethod.Post,
                    "api/administracion/ubicaciones/municipios?crearNuevoSiExisteInactivo=true",
                    nuevoMunicipio,
                    "crear el municipio",
                    "Municipio creado correctamente.",
                    cancellationToken);
            }

            return ApiResult<MunicipioResponse>.Fail(
                "La creación fue cancelada.");
        }

        private static async Task<string?>
            MostrarOpcionesMunicipioInactivoAsync(
                MunicipioAdminItem municipio)
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
                        string.IsNullOrWhiteSpace(municipio.Nombre)
                            ? "el municipio eliminado"
                            : $"'{municipio.Nombre}'";

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

        private static MunicipioResponse MapearMunicipio(
            MunicipioAdminItem item) =>
            new()
            {
                MunicipioId = item.MunicipioId,
                NombreMunicipio = item.Nombre ?? string.Empty,
                DepartamentoId = item.DepartamentoId,
                NombreDepartamento =
                    item.NombreDepartamento ?? string.Empty,
                PaisId = item.PaisId,
                NombrePais = item.NombrePais ?? string.Empty,
                Activo = item.Activo,
                CantidadTerrenos = item.CantidadTerrenos,
                CantidadUsuarios = item.CantidadUsuarios
            };

        private static List<MunicipioResponse>? ObtenerCacheVigente(
            int departamentoId)
        {
            if (!CachePorDepartamento.TryGetValue(
                    departamentoId,
                    out CacheEntry? entry))
            {
                return null;
            }

            if (DateTime.UtcNow - entry.CreadoUtc >= DuracionCache)
            {
                CachePorDepartamento.TryRemove(departamentoId, out _);
                return null;
            }

            return entry.Items;
        }

        private static void LimpiarCache(int departamentoId)
        {
            if (departamentoId > 0)
                CachePorDepartamento.TryRemove(departamentoId, out _);
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

        private sealed class MunicipioAdminItem
        {
            public int MunicipioId { get; set; }
            public int DepartamentoId { get; set; }
            public string NombreDepartamento { get; set; } = string.Empty;
            public int PaisId { get; set; }
            public string NombrePais { get; set; } = string.Empty;
            public string Nombre { get; set; } = string.Empty;
            public bool Activo { get; set; }
            public int CantidadTerrenos { get; set; }
            public int CantidadUsuarios { get; set; }
        }

        private sealed class MunicipioAdministracionRequest
        {
            public int DepartamentoId { get; set; }
            public string Nombre { get; set; } = string.Empty;
        }
    }
}
