using CONATRADEC.Models;
using System.Collections.ObjectModel;

namespace CONATRADEC.Services
{
    public class ElementoQuimicoApiService
    {
        private readonly HttpClient httpClient;

        private static readonly SemaphoreSlim CacheLock = new(1, 1);
        private static List<ElementoQuimicoResponse>? cacheFormulario;
        private static DateTime cacheCreadoUtc;
        private static readonly TimeSpan DuracionCache = TimeSpan.FromMinutes(20);

        public ElementoQuimicoApiService()
            : this(ApiClientService.Client)
        {
        }

        public ElementoQuimicoApiService(HttpClient httpClient)
        {
            this.httpClient = httpClient
                ?? throw new ArgumentNullException(nameof(httpClient));
        }

        public async Task<ApiResult<ObservableCollection<ElementoQuimicoResponse>>>
            GetElementoQuimicoResultAsync(
                CancellationToken cancellationToken = default)
        {
            if (CacheVigente())
            {
                return ApiResult<ObservableCollection<ElementoQuimicoResponse>>
                    .Ok(CrearColeccionCache());
            }

            await CacheLock.WaitAsync(cancellationToken);

            try
            {
                if (CacheVigente())
                {
                    return ApiResult<ObservableCollection<ElementoQuimicoResponse>>
                        .Ok(CrearColeccionCache());
                }

                ApiResult<ObservableCollection<ElementoQuimicoResponse>> result =
                    await ApiServiceHelper
                        .GetCollectionAsync<ElementoQuimicoResponse>(
                            httpClient,
                            "api/elemento-quimico/listar",
                            "los elementos químicos",
                            cancellationToken);

                if (!result.Success || result.Data == null)
                    return result;

                cacheFormulario = result.Data
                    .Where(x => x != null &&
                                x.ElementoQuimicosId is > 0)
                    .ToList();

                cacheCreadoUtc = DateTime.UtcNow;

                return ApiResult<ObservableCollection<ElementoQuimicoResponse>>
                    .Ok(
                        CrearColeccionCache(),
                        result.Message);
            }
            finally
            {
                CacheLock.Release();
            }
        }

        public async Task<ApiResult<bool>> CreateElementoQuimicoResultAsync(
            ElementoQuimicoRequest elemento,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(elemento);

            ApiResult<bool> result = await ApiServiceHelper.SendAsync(
                httpClient,
                HttpMethod.Post,
                "api/elemento-quimico/crear",
                elemento,
                "crear el elemento químico",
                "Elemento químico creado correctamente.",
                cancellationToken);

            if (result.Success)
                LimpiarCache();

            return result;
        }

        public async Task<ApiResult<bool>> UpdateElementoQuimicoResultAsync(
            ElementoQuimicoRequest elemento,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(elemento);

            if (!elemento.ElementoQuimicosId.HasValue ||
                elemento.ElementoQuimicosId.Value <= 0)
            {
                return ApiResult<bool>.Fail(
                    "No se recibió un identificador de elemento químico válido.");
            }

            ApiResult<bool> result = await ApiServiceHelper.SendAsync(
                httpClient,
                HttpMethod.Put,
                $"api/elemento-quimico/editar/{elemento.ElementoQuimicosId.Value}",
                elemento,
                "actualizar el elemento químico",
                "Elemento químico actualizado correctamente.",
                cancellationToken);

            if (result.Success)
                LimpiarCache();

            return result;
        }

        public async Task<ApiResult<bool>> DeleteElementoQuimicoResultAsync(
            ElementoQuimicoRequest elemento,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(elemento);

            if (!elemento.ElementoQuimicosId.HasValue ||
                elemento.ElementoQuimicosId.Value <= 0)
            {
                return ApiResult<bool>.Fail(
                    "No se recibió un identificador de elemento químico válido.");
            }

            ApiResult<bool> result = await ApiServiceHelper
                .SendAsync<ElementoQuimicoRequest>(
                    httpClient,
                    HttpMethod.Delete,
                    $"api/elemento-quimico/eliminar/{elemento.ElementoQuimicosId.Value}",
                    null,
                    "eliminar el elemento químico",
                    "Elemento químico eliminado correctamente.",
                    cancellationToken);

            if (result.Success)
                LimpiarCache();

            return result;
        }

        /// <summary>
        /// Versión utilizada por el formulario de análisis. Conserva el
        /// catálogo activo durante la sesión y devuelve una colección nueva
        /// para que cada pantalla pueda trabajar sin compartir la colección.
        /// </summary>
        public async Task<ObservableCollection<ElementoQuimicoResponse>>
            GetElementoQuimicoAsync()
        {
            ApiResult<ObservableCollection<ElementoQuimicoResponse>> result =
                await GetElementoQuimicoResultAsync();

            return result.Data ??
                new ObservableCollection<ElementoQuimicoResponse>();
        }

        public async Task<bool> CreateElementoQuimicoAsync(
            ElementoQuimicoRequest elemento)
        {
            ApiResult<bool> result =
                await CreateElementoQuimicoResultAsync(elemento);

            return result.Success && result.Data == true;
        }

        public async Task<bool> UpdateElementoQuimicoAsync(
            ElementoQuimicoRequest elemento)
        {
            ApiResult<bool> result =
                await UpdateElementoQuimicoResultAsync(elemento);

            return result.Success && result.Data == true;
        }

        public async Task<bool> DeleteElementoQuimicoAsync(
            ElementoQuimicoRequest elemento)
        {
            ApiResult<bool> result =
                await DeleteElementoQuimicoResultAsync(elemento);

            return result.Success && result.Data == true;
        }

        private static bool CacheVigente() =>
            cacheFormulario != null &&
            DateTime.UtcNow - cacheCreadoUtc < DuracionCache;

        private static ObservableCollection<ElementoQuimicoResponse>
            CrearColeccionCache() =>
            new(cacheFormulario ?? Enumerable.Empty<ElementoQuimicoResponse>());

        private static void LimpiarCache()
        {
            cacheFormulario = null;
            cacheCreadoUtc = default;
        }
    }
}
