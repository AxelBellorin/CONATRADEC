using CONATRADEC.Models;
using System.Collections.ObjectModel;
using System.Net.Http.Json;
using System.Text.Json;

namespace CONATRADEC.Services
{
    public sealed class ElementoQuimicoApiService
    {
        private readonly HttpClient httpClient;

        private static readonly SemaphoreSlim CacheLock =
            new(1, 1);

        private static List<ElementoQuimicoResponse>?
            cacheFormulario;

        private static DateTime cacheCreadoUtc;

        private static readonly TimeSpan DuracionCache =
            TimeSpan.FromMinutes(20);

        public ElementoQuimicoApiService()
            : this(ApiClientService.Client)
        {
        }

        public ElementoQuimicoApiService(
            HttpClient httpClient)
        {
            this.httpClient =
                httpClient
                ?? throw new ArgumentNullException(
                    nameof(httpClient));
        }

        /// <summary>
        /// Listado completo conservado para análisis, fuentes de nutrientes
        /// y demás formularios que utilizan este catálogo como selector.
        /// </summary>
        public async Task<ApiResult<ObservableCollection<ElementoQuimicoResponse>>>
            GetElementoQuimicoResultAsync(
                CancellationToken cancellationToken = default)
        {
            if (CacheVigente())
            {
                return ApiResult<ObservableCollection<ElementoQuimicoResponse>>
                    .Ok(
                        CrearColeccionCache());
            }

            await CacheLock.WaitAsync(
                cancellationToken);

            try
            {
                if (CacheVigente())
                {
                    return ApiResult<
                        ObservableCollection<ElementoQuimicoResponse>>
                        .Ok(
                            CrearColeccionCache());
                }

                ApiResult<
                    ObservableCollection<ElementoQuimicoResponse>>
                    result =
                        await ApiServiceHelper
                            .GetCollectionAsync<ElementoQuimicoResponse>(
                                httpClient,
                                "api/elemento-quimico/listar",
                                "los elementos químicos",
                                cancellationToken);

                if (!result.Success ||
                    result.Data == null)
                {
                    return result;
                }

                cacheFormulario =
                    result.Data
                        .Where(elemento =>
                            elemento.ElementoQuimicosId is > 0)
                        .OrderBy(elemento =>
                            elemento.NombreElementoQuimico)
                        .ToList();

                cacheCreadoUtc =
                    DateTime.UtcNow;

                return ApiResult<
                    ObservableCollection<ElementoQuimicoResponse>>
                    .Ok(
                        CrearColeccionCache(),
                        result.Message);
            }
            finally
            {
                CacheLock.Release();
            }
        }

        /// <summary>
        /// Consulta paginada utilizada únicamente por la pantalla
        /// administrativa del catálogo.
        /// </summary>
        public async Task<ApiResult<ElementoQuimicoPaginaResponse>>
            BuscarElementosAsync(
                string? buscar,
                int pagina,
                int tamanoPagina,
                CancellationToken cancellationToken = default)
        {
            pagina =
                Math.Max(
                    1,
                    pagina);

            tamanoPagina =
                Math.Clamp(
                    tamanoPagina,
                    5,
                    100);

            string ruta =
                "api/elemento-quimico/buscar" +
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
                    return ApiResult<ElementoQuimicoPaginaResponse>
                        .Fail(
                            await ApiServiceHelper
                                .ReadResponseMessageAsync(
                                    response,
                                    "No fue posible cargar los elementos químicos.",
                                    cancellationToken),
                            (int)response.StatusCode);
                }

                ElementoQuimicoPaginaResponse? data =
                    await response.Content
                        .ReadFromJsonAsync<ElementoQuimicoPaginaResponse>(
                            cancellationToken:
                                cancellationToken);

                return ApiResult<ElementoQuimicoPaginaResponse>
                    .Ok(
                        data ??
                        new ElementoQuimicoPaginaResponse());
            }
            catch (TaskCanceledException)
                when (!cancellationToken.IsCancellationRequested)
            {
                return ApiResult<ElementoQuimicoPaginaResponse>
                    .Fail(
                        "La carga de elementos químicos tardó demasiado. Intente nuevamente.");
            }
            catch (OperationCanceledException)
            {
                return ApiResult<ElementoQuimicoPaginaResponse>
                    .Fail(
                        "La operación fue cancelada.");
            }
            catch (HttpRequestException)
            {
                return ApiResult<ElementoQuimicoPaginaResponse>
                    .Fail(
                        "No fue posible comunicarse con el servidor para cargar los elementos químicos.");
            }
            catch (JsonException)
            {
                return ApiResult<ElementoQuimicoPaginaResponse>
                    .Fail(
                        "El servidor respondió, pero el listado de elementos químicos no tiene el formato esperado.");
            }
            catch
            {
                return ApiResult<ElementoQuimicoPaginaResponse>
                    .Fail(
                        "Ocurrió un error inesperado al cargar los elementos químicos.");
            }
        }

        public async Task<ApiResult<bool>>
            CreateElementoQuimicoResultAsync(
                ElementoQuimicoRequest elemento,
                CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(
                elemento);

            ApiResult<bool> result =
                await ApiServiceHelper.SendAsync(
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

        public async Task<ApiResult<bool>>
            UpdateElementoQuimicoResultAsync(
                ElementoQuimicoRequest elemento,
                CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(
                elemento);

            if (!elemento.ElementoQuimicosId.HasValue ||
                elemento.ElementoQuimicosId.Value <= 0)
            {
                return ApiResult<bool>.Fail(
                    "No se recibió un identificador de elemento químico válido.");
            }

            ApiResult<bool> result =
                await ApiServiceHelper.SendAsync(
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

        public async Task<ApiResult<bool>>
            DeleteElementoQuimicoResultAsync(
                ElementoQuimicoRequest elemento,
                CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(
                elemento);

            if (!elemento.ElementoQuimicosId.HasValue ||
                elemento.ElementoQuimicosId.Value <= 0)
            {
                return ApiResult<bool>.Fail(
                    "No se recibió un identificador de elemento químico válido.");
            }

            ApiResult<bool> result =
                await ApiServiceHelper
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

        public async Task<ObservableCollection<ElementoQuimicoResponse>>
            GetElementoQuimicoAsync()
        {
            ApiResult<ObservableCollection<ElementoQuimicoResponse>>
                result =
                    await GetElementoQuimicoResultAsync();

            return result.Data ??
                new ObservableCollection<ElementoQuimicoResponse>();
        }

        // Métodos conservados para no afectar código existente.
        public async Task<bool> CreateElementoQuimicoAsync(
            ElementoQuimicoRequest elemento)
        {
            ApiResult<bool> result =
                await CreateElementoQuimicoResultAsync(
                    elemento);

            return
                result.Success &&
                result.Data == true;
        }

        public async Task<bool> UpdateElementoQuimicoAsync(
            ElementoQuimicoRequest elemento)
        {
            ApiResult<bool> result =
                await UpdateElementoQuimicoResultAsync(
                    elemento);

            return
                result.Success &&
                result.Data == true;
        }

        public async Task<bool> DeleteElementoQuimicoAsync(
            ElementoQuimicoRequest elemento)
        {
            ApiResult<bool> result =
                await DeleteElementoQuimicoResultAsync(
                    elemento);

            return
                result.Success &&
                result.Data == true;
        }

        private static bool CacheVigente() =>
            cacheFormulario != null &&
            DateTime.UtcNow -
                cacheCreadoUtc <
                DuracionCache;

        private static ObservableCollection<ElementoQuimicoResponse>
            CrearColeccionCache() =>
            new(
                cacheFormulario ??
                Enumerable.Empty<ElementoQuimicoResponse>());

        private static void LimpiarCache()
        {
            cacheFormulario =
                null;

            cacheCreadoUtc =
                default;
        }
    }
}
