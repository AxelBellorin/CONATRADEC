using CONATRADEC.Models;
using System.Collections.ObjectModel;

namespace CONATRADEC.Services
{
    public class PaisApiService
    {
        private readonly HttpClient httpClient;

        private static readonly SemaphoreSlim CacheLock = new(1, 1);
        private static List<PaisResponse>? cacheFormulario;
        private static DateTime cacheCreadoUtc;
        private static readonly TimeSpan DuracionCache = TimeSpan.FromMinutes(30);

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

            ApiResult<bool> result = await ApiServiceHelper
                .SendAsync<PaisRequest>(
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

        public async Task<ObservableCollection<PaisResponse>> GetPaisAsync()
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
                    .Where(x => x != null && x.PaisId is > 0)
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

        public async Task<bool> CreatePaisAsync(PaisRequest pais)
        {
            ApiResult<bool> result = await CreatePaisResultAsync(pais);
            return result.Success && result.Data == true;
        }

        public async Task<bool> UpdatePaisAsync(PaisRequest pais)
        {
            ApiResult<bool> result = await UpdatePaisResultAsync(pais);
            return result.Success && result.Data == true;
        }

        public async Task<bool> DeletePaisAsync(PaisRequest pais)
        {
            ApiResult<bool> result = await DeletePaisResultAsync(pais);
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
