using CONATRADEC.Models;
using System.Collections.ObjectModel;
using System.Net.Http.Json;
using System.Text.Json;

namespace CONATRADEC.Services
{
    public sealed class PaisApiService
    {
        private readonly HttpClient httpClient;

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
        /// Consulta administrativa paginada. Utiliza el controlador de
        /// administración de ubicaciones, que valida permisos en backend.
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
                            cancellationToken: cancellationToken);

                if (data == null)
                {
                    return ApiResult<PaisPaginaResponse>.Fail(
                        "El servidor no devolvió la página de países esperada.");
                }

                return ApiResult<PaisPaginaResponse>.Ok(
                    new PaisPaginaResponse
                    {
                        Items = data.Items
                            .Where(item => item.PaisId > 0 && item.Activo)
                            .Select(item => new PaisResponse
                            {
                                PaisId = item.PaisId,
                                NombrePais = item.Nombre,
                                CodigoISOPais = item.CodigoIso,
                                Activo = item.Activo,
                                CantidadDepartamentos =
                                    item.CantidadDependencias
                            })
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

        public async Task<ApiResult<bool>> CreatePaisResultAsync(
            PaisRequest pais,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(pais);

            ApiResult<bool> result = await ApiServiceHelper.SendAsync(
                httpClient,
                HttpMethod.Post,
                "api/pais/crearPais",
                pais,
                "crear el país",
                "País creado correctamente.",
                cancellationToken);

            if (result.Success)
                LimpiarCache();

            return result;
        }

        public async Task<ApiResult<bool>> UpdatePaisResultAsync(
            PaisRequest pais,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(pais);

            if (pais.PaisId <= 0)
            {
                return ApiResult<bool>.Fail(
                    "No se recibió un identificador de país válido.");
            }

            ApiResult<bool> result = await ApiServiceHelper.SendAsync(
                httpClient,
                HttpMethod.Put,
                $"api/administracion/ubicaciones/paises/{pais.PaisId}",
                new PaisAdministracionRequest
                {
                    Nombre = pais.NombrePais ?? string.Empty,
                    CodigoIso = pais.CodigoISOPais ?? string.Empty
                },
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

        // Métodos conservados para no afectar código existente.
        public async Task<bool> CreatePaisAsync(PaisRequest pais)
        {
            ApiResult<bool> result =
                await CreatePaisResultAsync(pais);

            return result.Success && result.Data == true;
        }

        public async Task<bool> UpdatePaisAsync(PaisRequest pais)
        {
            ApiResult<bool> result =
                await UpdatePaisResultAsync(pais);

            return result.Success && result.Data == true;
        }

        public async Task<bool> DeletePaisAsync(PaisRequest pais)
        {
            ApiResult<bool> result =
                await DeletePaisResultAsync(pais);

            return result.Success && result.Data == true;
        }

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
