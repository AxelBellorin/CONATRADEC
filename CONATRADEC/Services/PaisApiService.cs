using CONATRADEC.Models;
using Microsoft.Maui.ApplicationModel;
using System.Collections.ObjectModel;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace CONATRADEC.Services
{
    public sealed class PaisApiService
    {
        private const string CodigoPaisInactivo =
            "PAIS_INACTIVO_EXISTENTE";

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

        private static readonly SemaphoreSlim CacheLock = new(1, 1);
        private static List<PaisResponse>? cacheFormulario;
        private static DateTime cacheCreadoUtc;
        private static readonly TimeSpan DuracionCache =
            TimeSpan.FromMinutes(30);

        public PaisApiService()
            : this(ApiClientService.Client)
        {
        }

        public PaisApiService(HttpClient httpClient)
        {
            this.httpClient = httpClient
                ?? throw new ArgumentNullException(nameof(httpClient));
        }

        /// <summary>
        /// Endpoint histórico completo conservado exclusivamente para
        /// formularios y selectores que requieren el catálogo completo.
        /// Las operaciones de administración de la aplicación actual utilizan
        /// únicamente api/administracion/ubicaciones.
        /// </summary>
        public Task<ApiResult<ObservableCollection<PaisResponse>>>
            GetPaisResultAsync(
                CancellationToken cancellationToken = default)
        {
            return ApiServiceHelper.GetCollectionAsync<PaisResponse>(
                httpClient,
                "api/pais",
                "los países",
                cancellationToken);
        }

        /// <summary>
        /// Consulta administrativa paginada. Utiliza el controlador actual de
        /// ubicaciones y conserva únicamente la página solicitada.
        /// </summary>
        public async Task<ApiResult<PaisPaginaResponse>>
            BuscarPaisesAsync(
                string? buscar,
                int pagina,
                int tamanoPagina,
                CancellationToken cancellationToken = default)
        {
            pagina = Math.Max(1, pagina);
            tamanoPagina = Math.Clamp(tamanoPagina, 5, 100);

            string ruta =
                "api/administracion/ubicaciones/paises" +
                $"?pagina={pagina}" +
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
                    return ApiResult<PaisPaginaResponse>.Fail(
                        await ApiServiceHelper.ReadResponseMessageAsync(
                            response,
                            "No fue posible cargar los países.",
                            cancellationToken),
                        (int)response.StatusCode);
                }

                PaginaAdminResponse<PaisAdminItem>? data =
                    await response.Content
                        .ReadFromJsonAsync<PaginaAdminResponse<PaisAdminItem>>(
                            jsonOptions,
                            cancellationToken);

                if (data == null)
                {
                    return ApiResult<PaisPaginaResponse>.Fail(
                        "El servidor no devolvió la página de países esperada.");
                }

                return ApiResult<PaisPaginaResponse>.Ok(
                    new PaisPaginaResponse
                    {
                        Items = data.Items
                            .Where(item =>
                                item.PaisId > 0 &&
                                item.Activo)
                            .Select(MapearPais)
                            .ToList(),
                        PaginaActual = data.PaginaActual,
                        TamanoPagina = data.TamanoPagina,
                        TotalRegistros = data.TotalRegistros,
                        TotalPaginas = data.TotalPaginas
                    });
            }
            catch (TaskCanceledException)
                when (!cancellationToken.IsCancellationRequested)
            {
                return ApiResult<PaisPaginaResponse>.Fail(
                    "La carga de países tardó demasiado. Intente nuevamente.");
            }
            catch (OperationCanceledException)
            {
                return ApiResult<PaisPaginaResponse>.Fail(
                    "La operación fue cancelada.");
            }
            catch (HttpRequestException)
            {
                return ApiResult<PaisPaginaResponse>.Fail(
                    "No fue posible comunicarse con el servidor para cargar los países.");
            }
            catch (JsonException)
            {
                return ApiResult<PaisPaginaResponse>.Fail(
                    "El servidor respondió, pero el listado de países no tiene el formato esperado.");
            }
            catch
            {
                return ApiResult<PaisPaginaResponse>.Fail(
                    "Ocurrió un error inesperado al cargar los países.");
            }
        }

        /// <summary>
        /// Crea un país mediante la API administrativa actual. Si existe una
        /// coincidencia inactiva permite reactivarla conservando su historial.
        /// </summary>
        public async Task<ApiResult<PaisResponse>> CreatePaisResultAsync(
            PaisRequest pais,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(pais);

            ApiResult<PaisResponse> result =
                await EnviarPaisAdministracionAsync(
                    HttpMethod.Post,
                    "api/administracion/ubicaciones/paises",
                    pais,
                    "crear el país",
                    "País creado correctamente.",
                    cancellationToken,
                    manejarInactivo: true);

            if (result.Success)
                LimpiarCache();

            return result;
        }

        public async Task<ApiResult<PaisResponse>> UpdatePaisResultAsync(
            PaisRequest pais,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(pais);

            if (pais.PaisId <= 0)
            {
                return ApiResult<PaisResponse>.Fail(
                    "No se recibió un identificador de país válido.");
            }

            ApiResult<PaisResponse> result =
                await EnviarPaisAdministracionAsync(
                    HttpMethod.Put,
                    $"api/administracion/ubicaciones/paises/{pais.PaisId}",
                    pais,
                    "actualizar el país",
                    "País actualizado correctamente.",
                    cancellationToken);

            if (result.Success)
                LimpiarCache();

            return result;
        }

        public async Task<ApiResult<bool>> DeletePaisResultAsync(
            PaisRequest pais,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(pais);

            if (pais.PaisId <= 0)
            {
                return ApiResult<bool>.Fail(
                    "No se recibió un identificador de país válido.");
            }

            ApiResult<bool> result =
                await ApiServiceHelper.SendAsync<PaisAdministracionRequest>(
                    httpClient,
                    HttpMethod.Delete,
                    $"api/administracion/ubicaciones/paises/{pais.PaisId}",
                    null,
                    "eliminar el país",
                    "País eliminado correctamente.",
                    cancellationToken);

            if (result.Success)
                LimpiarCache();

            return result;
        }

        public async Task<ObservableCollection<PaisResponse>>
            GetPaisAsync()
        {
            if (CacheVigente())
                return CrearColeccionCache();

            await CacheLock.WaitAsync();

            try
            {
                if (CacheVigente())
                    return CrearColeccionCache();

                ApiResult<ObservableCollection<PaisResponse>> result =
                    await GetPaisResultAsync();

                cacheFormulario = result.Data?
                    .Where(pais => pais.PaisId > 0)
                    .ToList()
                    ?? new List<PaisResponse>();

                cacheCreadoUtc = DateTime.UtcNow;
                return CrearColeccionCache();
            }
            finally
            {
                CacheLock.Release();
            }
        }

        // Métodos de compatibilidad conservados para consumidores existentes.
        public async Task<bool> CreatePaisAsync(PaisRequest pais)
        {
            ApiResult<PaisResponse> result =
                await CreatePaisResultAsync(pais);

            return result.Success &&
                   result.Data?.PaisId > 0;
        }

        public async Task<bool> UpdatePaisAsync(PaisRequest pais)
        {
            ApiResult<PaisResponse> result =
                await UpdatePaisResultAsync(pais);

            return result.Success &&
                   result.Data?.PaisId > 0;
        }

        public async Task<bool> DeletePaisAsync(PaisRequest pais)
        {
            ApiResult<bool> result =
                await DeletePaisResultAsync(pais);

            return result.Success && result.Data == true;
        }

        private async Task<ApiResult<PaisResponse>>
            EnviarPaisAdministracionAsync(
                HttpMethod method,
                string route,
                PaisRequest pais,
                string accion,
                string mensajeExito,
                CancellationToken cancellationToken,
                bool manejarInactivo = false)
        {
            var request = new PaisAdministracionRequest
            {
                Nombre = pais.NombrePais ?? string.Empty,
                CodigoIso = pais.CodigoISOPais ?? string.Empty
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

                UbicacionOperacionEnvelope<PaisAdminItem>? envelope =
                    await LeerEnvelopeAsync<PaisAdminItem>(
                        response,
                        cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    if (envelope?.Data == null ||
                        envelope.Data.PaisId <= 0)
                    {
                        return ApiResult<PaisResponse>.Fail(
                            "La operación se procesó, pero el servidor no devolvió el país actualizado.");
                    }

                    return ApiResult<PaisResponse>.Ok(
                        MapearPais(envelope.Data),
                        string.IsNullOrWhiteSpace(envelope.Message)
                            ? mensajeExito
                            : envelope.Message);
                }

                if (manejarInactivo &&
                    response.StatusCode == HttpStatusCode.Conflict &&
                    string.Equals(
                        envelope?.Code,
                        CodigoPaisInactivo,
                        StringComparison.OrdinalIgnoreCase) &&
                    envelope?.Data?.PaisId > 0)
                {
                    return await ResolverPaisInactivoAsync(
                        pais,
                        envelope.Data,
                        cancellationToken);
                }

                return ApiResult<PaisResponse>.Fail(
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
                return ApiResult<PaisResponse>.Fail(
                    "La solicitud tardó demasiado. Intente nuevamente.");
            }
            catch (OperationCanceledException)
            {
                return ApiResult<PaisResponse>.Fail(
                    "La operación fue cancelada.");
            }
            catch (HttpRequestException)
            {
                return ApiResult<PaisResponse>.Fail(
                    "No fue posible comunicarse con el servidor.");
            }
            catch (JsonException)
            {
                return ApiResult<PaisResponse>.Fail(
                    "El servidor respondió con un formato inesperado.");
            }
            catch
            {
                return ApiResult<PaisResponse>.Fail(
                    $"Ocurrió un error inesperado al {accion}.");
            }
        }

        private async Task<ApiResult<PaisResponse>>
            ResolverPaisInactivoAsync(
                PaisRequest nuevoPais,
                PaisAdminItem inactivo,
                CancellationToken cancellationToken)
        {
            string? decision =
                await MostrarOpcionesPaisInactivoAsync(
                    inactivo);

            if (decision == OpcionReactivar)
            {
                var request = new PaisRequest
                {
                    PaisId = inactivo.PaisId,
                    NombrePais = nuevoPais.NombrePais,
                    CodigoISOPais = nuevoPais.CodigoISOPais
                };

                return await EnviarPaisAdministracionAsync(
                    HttpMethod.Put,
                    $"api/administracion/ubicaciones/paises/{inactivo.PaisId}/reactivar",
                    request,
                    "reactivar el país",
                    "País reactivado correctamente.",
                    cancellationToken);
            }

            if (decision == OpcionCrearNuevo)
            {
                return await EnviarPaisAdministracionAsync(
                    HttpMethod.Post,
                    "api/administracion/ubicaciones/paises?crearNuevoSiExisteInactivo=true",
                    nuevoPais,
                    "crear el país",
                    "País creado correctamente.",
                    cancellationToken);
            }

            return ApiResult<PaisResponse>.Fail(
                "La creación fue cancelada.");
        }

        private static async Task<string?>
            MostrarOpcionesPaisInactivoAsync(
                PaisAdminItem pais)
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
                        string.IsNullOrWhiteSpace(pais.Nombre)
                            ? "el país eliminado"
                            : $"'{pais.Nombre}'";

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

        private static PaisResponse MapearPais(
            PaisAdminItem item) =>
            new()
            {
                PaisId = item.PaisId,
                NombrePais = item.Nombre ?? string.Empty,
                CodigoISOPais = item.CodigoIso ?? string.Empty,
                Activo = item.Activo,
                CantidadDepartamentos =
                    item.CantidadDependencias
            };

        private static bool CacheVigente() =>
            cacheFormulario != null &&
            DateTime.UtcNow - cacheCreadoUtc < DuracionCache;

        private static ObservableCollection<PaisResponse>
            CrearColeccionCache() =>
            new(cacheFormulario ?? Enumerable.Empty<PaisResponse>());

        private static void LimpiarCache()
        {
            cacheFormulario = null;
            cacheCreadoUtc = default;
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

        private sealed class PaisAdminItem
        {
            public int PaisId { get; set; }
            public string Nombre { get; set; } = string.Empty;
            public string CodigoIso { get; set; } = string.Empty;
            public bool Activo { get; set; }
            public int CantidadDependencias { get; set; }
        }

        private sealed class PaisAdministracionRequest
        {
            public string Nombre { get; set; } = string.Empty;
            public string CodigoIso { get; set; } = string.Empty;
        }
    }
}
