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
                "api/pais/buscar" +
                $"?pagina={pagina}" +
                $"&tamanoPagina={tamanoPagina}" +
                "&orden=nombre" +
                "&direccion=asc";

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

                PaisPaginaResponse? data =
                    await response.Content
                        .ReadFromJsonAsync<PaisPaginaResponse>(
                            cancellationToken: cancellationToken);

                return ApiResult<PaisPaginaResponse>.Ok(
                    data ?? new PaisPaginaResponse());
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
                $"api/pais/actualizarPais/{pais.PaisId}",
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
                await ApiServiceHelper.SendAsync<PaisRequest>(
                    httpClient,
                    HttpMethod.Delete,
                    $"api/pais/eliminarPais/{pais.PaisId}",
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
    }
}
