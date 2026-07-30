using CONATRADEC.Models;
using System.Collections.ObjectModel;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace CONATRADEC.Services
{
    internal static class ApiServiceHelper
    {
        public static async Task<ApiResult<ObservableCollection<T>>>
            GetCollectionAsync<T>(
                HttpClient httpClient,
                string route,
                string entityName,
                CancellationToken cancellationToken = default)
        {
            try
            {
                using HttpResponseMessage response =
                    await httpClient.GetAsync(
                        route,
                        cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    /*
                     * Una colección auxiliar que no existe dentro del paquete
                     * offline se representa como una colección vacía.
                     *
                     * No se utiliza este comportamiento en línea ni para
                     * operaciones de escritura.
                     */
                    if (OfflineReadResponseService
                        .EsLecturaSinDatosLocales(response))
                    {
                        return ApiResult<ObservableCollection<T>>.Ok(
                            new ObservableCollection<T>());
                    }

                    return ApiResult<ObservableCollection<T>>.Fail(
                        await ReadResponseMessageAsync(
                            response,
                            $"No fue posible cargar {entityName}.",
                            cancellationToken),
                        (int)response.StatusCode);
                }

                ObservableCollection<T>? data =
                    await response.Content
                        .ReadFromJsonAsync<ObservableCollection<T>>(
                            cancellationToken:
                                cancellationToken);

                return ApiResult<ObservableCollection<T>>.Ok(
                    data ??
                    new ObservableCollection<T>());
            }
            catch (TaskCanceledException)
                when (!cancellationToken.IsCancellationRequested)
            {
                return ApiResult<ObservableCollection<T>>.Fail(
                    "La solicitud tardó demasiado. Intente nuevamente.");
            }
            catch (OperationCanceledException)
            {
                return ApiResult<ObservableCollection<T>>.Fail(
                    "La operación fue cancelada.");
            }
            catch (HttpRequestException)
            {
                return ApiResult<ObservableCollection<T>>.Fail(
                    "No fue posible comunicarse con el servidor. Verifique que la API esté disponible.");
            }
            catch (JsonException)
            {
                return ApiResult<ObservableCollection<T>>.Fail(
                    $"El servidor respondió, pero los datos de {entityName} no tienen el formato esperado.");
            }
            catch
            {
                return ApiResult<ObservableCollection<T>>.Fail(
                    $"Ocurrió un error inesperado al cargar {entityName}.");
            }
        }

        public static async Task<ApiResult<bool>> SendAsync<TRequest>(
            HttpClient httpClient,
            HttpMethod method,
            string route,
            TRequest? request,
            string action,
            string successMessage,
            CancellationToken cancellationToken = default)
        {
            /*
             * Las creaciones de catálogos compatibles pasan por un único
             * flujo. Esto permite detectar coincidencias inactivas sin
             * modificar cada ViewModel o cada servicio individual.
             */
            if (request is not null &&
                CatalogoEliminadoCodigos.TryGetCodigoCreacion(
                    method,
                    route,
                    out string catalogo))
            {
                var reactivacionService =
                    new CatalogosEliminadosApiService(
                        httpClient);

                return await reactivacionService
                    .CrearConResolucionAsync(
                        catalogo,
                        request,
                        $"No fue posible {action}.",
                        successMessage,
                        cancellationToken);
            }

            try
            {
                using var message =
                    new HttpRequestMessage(
                        method,
                        route);

                if (request is not null)
                {
                    message.Content =
                        JsonContent.Create(
                            request);
                }

                using HttpResponseMessage response =
                    await httpClient.SendAsync(
                        message,
                        cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    return ApiResult<bool>.Fail(
                        await ReadResponseMessageAsync(
                            response,
                            $"No fue posible {action}.",
                            cancellationToken),
                        (int)response.StatusCode);
                }

                return ApiResult<bool>.Ok(
                    true,
                    successMessage);
            }
            catch (TaskCanceledException)
                when (!cancellationToken.IsCancellationRequested)
            {
                return ApiResult<bool>.Fail(
                    "La solicitud tardó demasiado. Intente nuevamente.");
            }
            catch (OperationCanceledException)
            {
                return ApiResult<bool>.Fail(
                    "La operación fue cancelada.");
            }
            catch (HttpRequestException)
            {
                return ApiResult<bool>.Fail(
                    "No fue posible comunicarse con el servidor. Verifique que la API esté disponible.");
            }
            catch
            {
                return ApiResult<bool>.Fail(
                    $"Ocurrió un error inesperado al {action}.");
            }
        }

        public static async Task<ApiResult<TResponse>>
            SendAndReadAsync<TRequest, TResponse>(
                HttpClient httpClient,
                HttpMethod method,
                string route,
                TRequest request,
                string action,
                string successMessage,
                CancellationToken cancellationToken = default)
        {
            string rutaLimpia =
                (route ?? string.Empty)
                    .Split('?', 2)[0]
                    .Trim()
                    .TrimStart('/')
                    .TrimEnd('/');

            if (method == HttpMethod.Post &&
                string.Equals(
                    rutaLimpia,
                    "api/usuarios/crear",
                    StringComparison.OrdinalIgnoreCase))
            {
                var reactivacionService =
                    new CatalogosEliminadosApiService(
                        httpClient);

                ApiResult<TResponse>? resuelto =
                    await reactivacionService
                        .IntentarResolverUsuarioInactivoAsync<
                            TRequest,
                            TResponse>(
                                request,
                                cancellationToken);

                if (resuelto != null)
                    return resuelto;
            }

            try
            {
                using var message =
                    new HttpRequestMessage(
                        method,
                        route)
                    {
                        Content =
                            JsonContent.Create(
                                request)
                    };

                using HttpResponseMessage response =
                    await httpClient.SendAsync(
                        message,
                        cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    return ApiResult<TResponse>.Fail(
                        await ReadResponseMessageAsync(
                            response,
                            $"No fue posible {action}.",
                            cancellationToken),
                        (int)response.StatusCode);
                }

                TResponse? data =
                    await response.Content
                        .ReadFromJsonAsync<TResponse>(
                            cancellationToken:
                                cancellationToken);

                if (data == null)
                {
                    return ApiResult<TResponse>.Fail(
                        "La operación se procesó, pero el servidor no devolvió los datos esperados.");
                }

                return ApiResult<TResponse>.Ok(
                    data,
                    successMessage);
            }
            catch (TaskCanceledException)
                when (!cancellationToken.IsCancellationRequested)
            {
                return ApiResult<TResponse>.Fail(
                    "La solicitud tardó demasiado. Intente nuevamente.");
            }
            catch (OperationCanceledException)
            {
                return ApiResult<TResponse>.Fail(
                    "La operación fue cancelada.");
            }
            catch (HttpRequestException)
            {
                return ApiResult<TResponse>.Fail(
                    "No fue posible comunicarse con el servidor. Verifique que la API esté disponible.");
            }
            catch (JsonException)
            {
                return ApiResult<TResponse>.Fail(
                    "El servidor respondió, pero los datos no tienen el formato esperado.");
            }
            catch
            {
                return ApiResult<TResponse>.Fail(
                    $"Ocurrió un error inesperado al {action}.");
            }
        }

        internal static async Task<string>
            ReadResponseMessageAsync(
                HttpResponseMessage response,
                string fallback,
                CancellationToken cancellationToken = default)
        {
            if (OfflineReadResponseService
                .EsLecturaSinDatosLocales(response))
            {
                return OfflineReadResponseService
                    .MensajeSinDatosLocales;
            }

            string content;

            try
            {
                content =
                    await response.Content.ReadAsStringAsync(
                        cancellationToken);
            }
            catch
            {
                content =
                    string.Empty;
            }

            return ApiErrorMessageParser.Parse(
                response.StatusCode,
                content,
                fallback);
        }

        public static string GetHttpMessage(
            HttpStatusCode statusCode,
            string fallback) =>
            ApiErrorMessageParser.GetDefaultMessage(
                statusCode,
                fallback);
    }
}
