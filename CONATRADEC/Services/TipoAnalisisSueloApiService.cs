using CONATRADEC.Models;
using System.Collections.ObjectModel;
using System.Net.Http.Json;
using System.Text.Json;

namespace CONATRADEC.Services
{
    public sealed class TipoAnalisisSueloApiService
    {
        private readonly HttpClient httpClient;

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
        /// Listado completo para formularios y selectores.
        /// Se consulta siempre al servidor para evitar reutilizar datos de una
        /// visita anterior del módulo.
        /// </summary>
        public async Task<ApiResult<ObservableCollection<TipoAnalisisSueloResponse>>>
            GetAsync(
                CancellationToken cancellationToken = default)
        {
            ApiResult<ObservableCollection<TipoAnalisisSueloResponse>> result =
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

            ObservableCollection<TipoAnalisisSueloResponse> ordenados =
                new(
                    result.Data
                        .Where(item =>
                            item.TipoAnalisisSueloId > 0)
                        .OrderBy(item =>
                            item.NombreMostrar)
                        .ThenBy(item =>
                            item.TipoAnalisisSueloId));

            return ApiResult<ObservableCollection<TipoAnalisisSueloResponse>>
                .Ok(
                    ordenados,
                    result.Message);
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

        /// <summary>
        /// Obtiene el registro activo directamente del servidor antes de abrir
        /// Ver o Editar, evitando utilizar una tarjeta desactualizada.
        /// </summary>
        public async Task<ApiResult<TipoAnalisisSueloResponse>>
            GetByIdAsync(
                int id,
                CancellationToken cancellationToken = default)
        {
            if (id <= 0)
            {
                return ApiResult<TipoAnalisisSueloResponse>.Fail(
                    "El identificador del tipo de análisis no es válido.");
            }

            try
            {
                using HttpResponseMessage response =
                    await httpClient.GetAsync(
                        $"api/configuracion/tipos-analisis-suelo/{id}",
                        cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    return ApiResult<TipoAnalisisSueloResponse>
                        .Fail(
                            await ApiServiceHelper
                                .ReadResponseMessageAsync(
                                    response,
                                    "No fue posible obtener el tipo de análisis de suelo.",
                                    cancellationToken),
                            (int)response.StatusCode);
                }

                TipoAnalisisSueloResponse? data =
                    await response.Content
                        .ReadFromJsonAsync<TipoAnalisisSueloResponse>(
                            cancellationToken:
                                cancellationToken);

                if (data == null ||
                    data.TipoAnalisisSueloId <= 0)
                {
                    return ApiResult<TipoAnalisisSueloResponse>.Fail(
                        "El servidor respondió, pero no devolvió un tipo de análisis válido.");
                }

                return ApiResult<TipoAnalisisSueloResponse>.Ok(data);
            }
            catch (TaskCanceledException)
                when (!cancellationToken.IsCancellationRequested)
            {
                return ApiResult<TipoAnalisisSueloResponse>.Fail(
                    "La consulta del tipo de análisis tardó demasiado. Intente nuevamente.");
            }
            catch (OperationCanceledException)
            {
                return ApiResult<TipoAnalisisSueloResponse>.Fail(
                    "La operación fue cancelada.");
            }
            catch (HttpRequestException)
            {
                return ApiResult<TipoAnalisisSueloResponse>.Fail(
                    "No fue posible comunicarse con el servidor para obtener el tipo de análisis de suelo.");
            }
            catch (JsonException)
            {
                return ApiResult<TipoAnalisisSueloResponse>.Fail(
                    "El servidor respondió, pero el tipo de análisis de suelo no tiene el formato esperado.");
            }
            catch
            {
                return ApiResult<TipoAnalisisSueloResponse>.Fail(
                    "Ocurrió un error inesperado al obtener el tipo de análisis de suelo.");
            }
        }

        public async Task<ApiResult<bool>>
            CreateAsync(
                TipoAnalisisSueloRequest request,
                CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(
                request);

            return await ConfiguracionApiServiceHelper.SendAsync(
                httpClient,
                HttpMethod.Post,
                "api/configuracion/tipos-analisis-suelo",
                request,
                "No fue posible crear el tipo de análisis de suelo.",
                "Tipo de análisis de suelo creado correctamente.",
                cancellationToken);
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

            return await ConfiguracionApiServiceHelper.SendAsync(
                httpClient,
                HttpMethod.Put,
                $"api/configuracion/tipos-analisis-suelo/{request.TipoAnalisisSueloId}",
                request,
                "No fue posible actualizar el tipo de análisis de suelo.",
                "Tipo de análisis de suelo actualizado correctamente.",
                cancellationToken);
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

            return await ConfiguracionApiServiceHelper
                .SendAsync<object>(
                    httpClient,
                    HttpMethod.Put,
                    $"api/configuracion/tipos-analisis-suelo/{id}/eliminar",
                    null,
                    "No fue posible eliminar el tipo de análisis de suelo.",
                    "Tipo de análisis de suelo desactivado correctamente.",
                    cancellationToken);
        }
    }
}
