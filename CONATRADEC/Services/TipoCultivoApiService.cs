using CONATRADEC.Models;
using System.Collections.ObjectModel;
using System.Net.Http.Json;
using System.Text.Json;

namespace CONATRADEC.Services
{
    public sealed class TipoCultivoApiService
    {
        private readonly HttpClient httpClient;

        private static readonly SemaphoreSlim CacheLock =
            new(1, 1);

        private static List<TipoCultivoResponse>?
            cacheFormulario;

        private static DateTime cacheCreadoUtc;

        private static readonly TimeSpan DuracionCache =
            TimeSpan.FromMinutes(20);

        public TipoCultivoApiService()
            : this(ApiClientService.Client)
        {
        }

        public TipoCultivoApiService(
            HttpClient httpClient)
        {
            this.httpClient =
                httpClient
                ?? throw new ArgumentNullException(
                    nameof(httpClient));
        }

        /// <summary>
        /// Listado completo conservado para análisis, rangos
        /// y otros selectores existentes.
        /// </summary>
        public async Task<ApiResult<ObservableCollection<TipoCultivoResponse>>>
            GetAsync(
                CancellationToken cancellationToken = default)
        {
            if (CacheVigente())
            {
                return ApiResult<ObservableCollection<TipoCultivoResponse>>
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
                        ObservableCollection<TipoCultivoResponse>>
                        .Ok(
                            CrearColeccionCache());
                }

                ApiResult<ObservableCollection<TipoCultivoResponse>>
                    result =
                        await ConfiguracionApiServiceHelper
                            .GetCollectionAsync<TipoCultivoResponse>(
                                httpClient,
                                "api/configuracion/tipos-cultivo",
                                "los tipos de cultivo",
                                cancellationToken);

                if (!result.Success ||
                    result.Data == null)
                {
                    return result;
                }

                cacheFormulario =
                    result.Data
                        .Where(item =>
                            item.TipoCultivoId > 0)
                        .OrderBy(item =>
                            item.NombreMostrar)
                        .ToList();

                cacheCreadoUtc =
                    DateTime.UtcNow;

                return ApiResult<
                    ObservableCollection<TipoCultivoResponse>>
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
        public async Task<ApiResult<TipoCultivoPaginaResponse>>
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
                "api/configuracion/tipos-cultivo/buscar" +
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
                    return ApiResult<TipoCultivoPaginaResponse>
                        .Fail(
                            await ApiServiceHelper
                                .ReadResponseMessageAsync(
                                    response,
                                    "No fue posible cargar los tipos de cultivo.",
                                    cancellationToken),
                            (int)response.StatusCode);
                }

                TipoCultivoPaginaResponse? data =
                    await response.Content
                        .ReadFromJsonAsync<TipoCultivoPaginaResponse>(
                            cancellationToken:
                                cancellationToken);

                return ApiResult<TipoCultivoPaginaResponse>
                    .Ok(
                        data ??
                        new TipoCultivoPaginaResponse());
            }
            catch (TaskCanceledException)
                when (!cancellationToken.IsCancellationRequested)
            {
                return ApiResult<TipoCultivoPaginaResponse>
                    .Fail(
                        "La carga de tipos de cultivo tardó demasiado. Intente nuevamente.");
            }
            catch (OperationCanceledException)
            {
                return ApiResult<TipoCultivoPaginaResponse>
                    .Fail(
                        "La operación fue cancelada.");
            }
            catch (HttpRequestException)
            {
                return ApiResult<TipoCultivoPaginaResponse>
                    .Fail(
                        "No fue posible comunicarse con el servidor para cargar los tipos de cultivo.");
            }
            catch (JsonException)
            {
                return ApiResult<TipoCultivoPaginaResponse>
                    .Fail(
                        "El servidor respondió, pero el listado de tipos de cultivo no tiene el formato esperado.");
            }
            catch
            {
                return ApiResult<TipoCultivoPaginaResponse>
                    .Fail(
                        "Ocurrió un error inesperado al cargar los tipos de cultivo.");
            }
        }

        /// <summary>
        /// Obtiene el registro activo actual antes de abrir Ver o Editar.
        /// Evita navegar con una copia potencialmente antigua del listado.
        /// </summary>
        public async Task<ApiResult<TipoCultivoResponse>>
            GetByIdAsync(
                int id,
                CancellationToken cancellationToken = default)
        {
            if (id <= 0)
            {
                return ApiResult<TipoCultivoResponse>
                    .Fail(
                        "El identificador del tipo de cultivo no es válido.");
            }

            try
            {
                using HttpResponseMessage response =
                    await httpClient.GetAsync(
                        $"api/configuracion/tipos-cultivo/{id}",
                        cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    return ApiResult<TipoCultivoResponse>
                        .Fail(
                            await ApiServiceHelper
                                .ReadResponseMessageAsync(
                                    response,
                                    "No fue posible obtener el tipo de cultivo.",
                                    cancellationToken),
                            (int)response.StatusCode);
                }

                TipoCultivoResponse? data =
                    await response.Content
                        .ReadFromJsonAsync<TipoCultivoResponse>(
                            cancellationToken:
                                cancellationToken);

                if (data == null ||
                    data.TipoCultivoId <= 0)
                {
                    return ApiResult<TipoCultivoResponse>
                        .Fail(
                            "El servidor no devolvió un tipo de cultivo válido.");
                }

                return ApiResult<TipoCultivoResponse>
                    .Ok(data);
            }
            catch (TaskCanceledException)
                when (!cancellationToken.IsCancellationRequested)
            {
                return ApiResult<TipoCultivoResponse>
                    .Fail(
                        "La consulta del tipo de cultivo tardó demasiado. Intente nuevamente.");
            }
            catch (OperationCanceledException)
            {
                return ApiResult<TipoCultivoResponse>
                    .Fail(
                        "La operación fue cancelada.");
            }
            catch (HttpRequestException)
            {
                return ApiResult<TipoCultivoResponse>
                    .Fail(
                        "No fue posible comunicarse con el servidor para obtener el tipo de cultivo.");
            }
            catch (JsonException)
            {
                return ApiResult<TipoCultivoResponse>
                    .Fail(
                        "El servidor respondió, pero el tipo de cultivo no tiene el formato esperado.");
            }
            catch
            {
                return ApiResult<TipoCultivoResponse>
                    .Fail(
                        "Ocurrió un error inesperado al obtener el tipo de cultivo.");
            }
        }

        public async Task<ApiResult<bool>>
            CreateAsync(
                TipoCultivoRequest request,
                CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(
                request);

            ApiResult<bool> result =
                await ConfiguracionApiServiceHelper.SendAsync(
                    httpClient,
                    HttpMethod.Post,
                    "api/configuracion/tipos-cultivo",
                    request,
                    "No fue posible crear el tipo de cultivo.",
                    "Tipo de cultivo creado correctamente.",
                    cancellationToken);

            if (result.Success)
                LimpiarCaches();

            return result;
        }

        public async Task<ApiResult<bool>>
            UpdateAsync(
                TipoCultivoRequest request,
                CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(
                request);

            if (request.TipoCultivoId <= 0)
            {
                return ApiResult<bool>.Fail(
                    "El identificador del tipo de cultivo no es válido.");
            }

            ApiResult<bool> result =
                await ConfiguracionApiServiceHelper.SendAsync(
                    httpClient,
                    HttpMethod.Put,
                    $"api/configuracion/tipos-cultivo/{request.TipoCultivoId}",
                    request,
                    "No fue posible actualizar el tipo de cultivo.",
                    "Tipo de cultivo actualizado correctamente.",
                    cancellationToken);

            if (result.Success)
                LimpiarCaches();

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
                    "El identificador del tipo de cultivo no es válido.");
            }

            ApiResult<bool> result =
                await ConfiguracionApiServiceHelper
                    .SendAsync<object>(
                        httpClient,
                        HttpMethod.Put,
                        $"api/configuracion/tipos-cultivo/{id}/eliminar",
                        null,
                        "No fue posible eliminar el tipo de cultivo.",
                        "Tipo de cultivo desactivado correctamente.",
                        cancellationToken);

            if (result.Success)
                LimpiarCaches();

            return result;
        }

        /// <summary>
        /// CRUD administrativo usado exclusivamente por Rangos nutricionales.
        /// Mantiene intactos los endpoints históricos de Tipos de cultivo y
        /// utiliza la API protegida por permisos del módulo actual.
        /// </summary>
        public async Task<ApiResult<bool>>
            CreateDesdeRangosAsync(
                TipoCultivoRequest request,
                CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            ApiResult<bool> result =
                await ConfiguracionApiServiceHelper.SendAsync(
                    httpClient,
                    HttpMethod.Post,
                    "api/configuracion/rangos-nutrientes/cultivos",
                    request,
                    "No fue posible crear el tipo de cultivo.",
                    "Tipo de cultivo creado correctamente.",
                    cancellationToken);

            if (result.Success)
                LimpiarCaches();

            return result;
        }

        public async Task<ApiResult<bool>>
            UpdateDesdeRangosAsync(
                TipoCultivoRequest request,
                CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            if (request.TipoCultivoId <= 0)
            {
                return ApiResult<bool>.Fail(
                    "El identificador del tipo de cultivo no es válido.");
            }

            ApiResult<bool> result =
                await ConfiguracionApiServiceHelper.SendAsync(
                    httpClient,
                    HttpMethod.Put,
                    $"api/configuracion/rangos-nutrientes/cultivos/{request.TipoCultivoId}",
                    request,
                    "No fue posible actualizar el tipo de cultivo.",
                    "Tipo de cultivo actualizado correctamente.",
                    cancellationToken);

            if (result.Success)
                LimpiarCaches();

            return result;
        }

        public async Task<ApiResult<bool>>
            DeleteDesdeRangosAsync(
                int id,
                CancellationToken cancellationToken = default)
        {
            if (id <= 0)
            {
                return ApiResult<bool>.Fail(
                    "El identificador del tipo de cultivo no es válido.");
            }

            ApiResult<bool> result =
                await ConfiguracionApiServiceHelper
                    .SendAsync<object>(
                        httpClient,
                        HttpMethod.Put,
                        $"api/configuracion/rangos-nutrientes/cultivos/{id}/eliminar",
                        null,
                        "No fue posible eliminar el tipo de cultivo.",
                        "Tipo de cultivo desactivado correctamente.",
                        cancellationToken);

            if (result.Success)
                LimpiarCaches();

            return result;
        }

        private static bool CacheVigente() =>
            cacheFormulario != null &&
            DateTime.UtcNow -
                cacheCreadoUtc <
                DuracionCache;

        private static ObservableCollection<TipoCultivoResponse>
            CrearColeccionCache() =>
            new(
                cacheFormulario ??
                Enumerable.Empty<TipoCultivoResponse>());

        private static void LimpiarCaches()
        {
            cacheFormulario =
                null;

            cacheCreadoUtc =
                default;

            /*
             * El formulario de análisis mantiene su propio caché.
             */
            AnalisisSueloApiService
                .LimpiarCacheTiposCultivo();
        }
    }
}
