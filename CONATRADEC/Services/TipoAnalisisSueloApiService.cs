using CONATRADEC.Models;
using System.Collections.ObjectModel;
using System.Net.Http.Json;
using System.Text.Json;

namespace CONATRADEC.Services
{
    public sealed class TipoAnalisisSueloApiService
    {
        private readonly HttpClient httpClient;

        private static readonly SemaphoreSlim CacheLock =
            new(1, 1);

        private static List<TipoAnalisisSueloResponse>?
            cacheFormulario;

        private static DateTime cacheCreadoUtc;

        private static readonly TimeSpan DuracionCache =
            TimeSpan.FromMinutes(20);

        public TipoAnalisisSueloApiService()
            : this(ApiClientService.Client)
        {
        }

        public TipoAnalisisSueloApiService(
            HttpClient httpClient)
        {
            this.httpClient =
                httpClient
                ?? throw new ArgumentNullException(
                    nameof(httpClient));
        }

        /// <summary>
        /// Listado completo conservado para formularios y futuros
        /// selectores del flujo de análisis.
        /// </summary>
        public async Task<ApiResult<ObservableCollection<TipoAnalisisSueloResponse>>>
            GetAsync(
                CancellationToken cancellationToken = default)
        {
            if (CacheVigente())
            {
                return ApiResult<
                    ObservableCollection<TipoAnalisisSueloResponse>>
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
                        ObservableCollection<TipoAnalisisSueloResponse>>
                        .Ok(
                            CrearColeccionCache());
                }

                ApiResult<
                    ObservableCollection<TipoAnalisisSueloResponse>>
                    result =
                        await ConfiguracionApiServiceHelper
                            .GetCollectionAsync<TipoAnalisisSueloResponse>(
                                httpClient,
                                "api/configuracion/tipos-analisis-suelo",
                                "los tipos de análisis de suelo",
                                cancellationToken);

                if (!result.Success ||
                    result.Data == null)
                {
                    return result;
                }

                cacheFormulario =
                    result.Data
                        .Where(item =>
                            item.TipoAnalisisSueloId > 0)
                        .OrderBy(item =>
                            item.NombreMostrar)
                        .ToList();

                cacheCreadoUtc =
                    DateTime.UtcNow;

                return ApiResult<
                    ObservableCollection<TipoAnalisisSueloResponse>>
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
        /// Búsqueda paginada para la pantalla administrativa.
        /// </summary>
        public async Task<ApiResult<TipoAnalisisSueloPaginaResponse>>
            BuscarAsync(
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
                "api/configuracion/tipos-analisis-suelo/buscar" +
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
                    return ApiResult<TipoAnalisisSueloPaginaResponse>
                        .Fail(
                            await ApiServiceHelper
                                .ReadResponseMessageAsync(
                                    response,
                                    "No fue posible cargar los tipos de análisis de suelo.",
                                    cancellationToken),
                            (int)response.StatusCode);
                }

                TipoAnalisisSueloPaginaResponse? data =
                    await response.Content
                        .ReadFromJsonAsync<TipoAnalisisSueloPaginaResponse>(
                            cancellationToken:
                                cancellationToken);

                return ApiResult<TipoAnalisisSueloPaginaResponse>
                    .Ok(
                        data ??
                        new TipoAnalisisSueloPaginaResponse());
            }
            catch (TaskCanceledException)
                when (!cancellationToken.IsCancellationRequested)
            {
                return ApiResult<TipoAnalisisSueloPaginaResponse>
                    .Fail(
                        "La carga de tipos de análisis de suelo tardó demasiado. Intente nuevamente.");
            }
            catch (OperationCanceledException)
            {
                return ApiResult<TipoAnalisisSueloPaginaResponse>
                    .Fail(
                        "La operación fue cancelada.");
            }
            catch (HttpRequestException)
            {
                return ApiResult<TipoAnalisisSueloPaginaResponse>
                    .Fail(
                        "No fue posible comunicarse con el servidor para cargar los tipos de análisis de suelo.");
            }
            catch (JsonException)
            {
                return ApiResult<TipoAnalisisSueloPaginaResponse>
                    .Fail(
                        "El servidor respondió, pero el listado de tipos de análisis de suelo no tiene el formato esperado.");
            }
            catch
            {
                return ApiResult<TipoAnalisisSueloPaginaResponse>
                    .Fail(
                        "Ocurrió un error inesperado al cargar los tipos de análisis de suelo.");
            }
        }

        public async Task<ApiResult<bool>>
            CreateAsync(
                TipoAnalisisSueloRequest request,
                CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(
                request);

            ApiResult<bool> result =
                await ConfiguracionApiServiceHelper.SendAsync(
                    httpClient,
                    HttpMethod.Post,
                    "api/configuracion/tipos-analisis-suelo",
                    request,
                    "No fue posible crear el tipo de análisis de suelo.",
                    "Tipo de análisis de suelo creado correctamente.",
                    cancellationToken);

            if (result.Success)
                LimpiarCache();

            return result;
        }

        public async Task<ApiResult<bool>>
            UpdateAsync(
                TipoAnalisisSueloRequest request,
                CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(
                request);

            if (request.TipoAnalisisSueloId <= 0)
            {
                return ApiResult<bool>.Fail(
                    "El identificador del tipo de análisis no es válido.");
            }

            ApiResult<bool> result =
                await ConfiguracionApiServiceHelper.SendAsync(
                    httpClient,
                    HttpMethod.Put,
                    $"api/configuracion/tipos-analisis-suelo/{request.TipoAnalisisSueloId}",
                    request,
                    "No fue posible actualizar el tipo de análisis de suelo.",
                    "Tipo de análisis de suelo actualizado correctamente.",
                    cancellationToken);

            if (result.Success)
                LimpiarCache();

            return result;
        }

        public async Task<ApiResult<bool>>
            DeleteAsync(
                int id,
                CancellationToken cancellationToken = default)
        {
            if (id <= 0)
            {
                return ApiResult<bool>.Fail(
                    "El identificador del tipo de análisis no es válido.");
            }

            ApiResult<bool> result =
                await ConfiguracionApiServiceHelper
                    .SendAsync<object>(
                        httpClient,
                        HttpMethod.Put,
                        $"api/configuracion/tipos-analisis-suelo/{id}/eliminar",
                        null,
                        "No fue posible eliminar el tipo de análisis de suelo.",
                        "Tipo de análisis de suelo desactivado correctamente.",
                        cancellationToken);

            if (result.Success)
                LimpiarCache();

            return result;
        }

        private static bool CacheVigente() =>
            cacheFormulario != null &&
            DateTime.UtcNow -
                cacheCreadoUtc <
                DuracionCache;

        private static ObservableCollection<TipoAnalisisSueloResponse>
            CrearColeccionCache() =>
            new(
                cacheFormulario ??
                Enumerable.Empty<TipoAnalisisSueloResponse>());

        private static void LimpiarCache()
        {
            cacheFormulario =
                null;

            cacheCreadoUtc =
                default;
        }
    }
}
