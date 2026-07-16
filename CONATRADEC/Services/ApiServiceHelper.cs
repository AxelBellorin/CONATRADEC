using CONATRADEC.Models;
using System.Collections.ObjectModel;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace CONATRADEC.Services
{
    /// <summary>
    /// Centraliza el manejo común de respuestas y excepciones HTTP.
    /// Evita repetir la misma lógica en cada servicio de catálogo.
    /// </summary>
    internal static class ApiServiceHelper
    {
        public static async Task<ApiResult<ObservableCollection<T>>> GetCollectionAsync<T>(
            HttpClient httpClient,
            string route,
            string entityName,
            CancellationToken cancellationToken = default)
        {
            try
            {
                using var response = await httpClient.GetAsync(
                    route,
                    cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    return ApiResult<ObservableCollection<T>>.Fail(
                        GetHttpMessage(
                            response.StatusCode,
                            $"cargar {entityName}"),
                        (int)response.StatusCode);
                }

                var data = await response.Content
                    .ReadFromJsonAsync<ObservableCollection<T>>(
                        cancellationToken: cancellationToken);

                return ApiResult<ObservableCollection<T>>.Ok(
                    data ?? new ObservableCollection<T>());
            }
            catch (TaskCanceledException)
                when (!cancellationToken.IsCancellationRequested)
            {
                return ApiResult<ObservableCollection<T>>.Fail(
                    "La solicitud tardó demasiado. Verifique su conexión e intente nuevamente.");
            }
            catch (OperationCanceledException)
            {
                return ApiResult<ObservableCollection<T>>.Fail(
                    "La operación fue cancelada.");
            }
            catch (HttpRequestException)
            {
                return ApiResult<ObservableCollection<T>>.Fail(
                    "No fue posible conectarse con el servidor. Verifique su conexión.");
            }
            catch (JsonException)
            {
                return ApiResult<ObservableCollection<T>>.Fail(
                    $"El servidor respondió, pero los datos de {entityName} no tienen el formato esperado.");
            }
            catch (Exception)
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
            try
            {
                using var message = new HttpRequestMessage(method, route);

                if (request is not null)
                    message.Content = JsonContent.Create(request);

                using var response = await httpClient.SendAsync(
                    message,
                    cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    return ApiResult<bool>.Fail(
                        GetHttpMessage(response.StatusCode, action),
                        (int)response.StatusCode);
                }

                return ApiResult<bool>.Ok(true, successMessage);
            }
            catch (TaskCanceledException)
                when (!cancellationToken.IsCancellationRequested)
            {
                return ApiResult<bool>.Fail(
                    "La solicitud tardó demasiado. Verifique su conexión e intente nuevamente.");
            }
            catch (OperationCanceledException)
            {
                return ApiResult<bool>.Fail("La operación fue cancelada.");
            }
            catch (HttpRequestException)
            {
                return ApiResult<bool>.Fail(
                    "No fue posible conectarse con el servidor. Verifique su conexión.");
            }
            catch (Exception)
            {
                return ApiResult<bool>.Fail(
                    $"Ocurrió un error inesperado al {action}.");
            }
        }

        public static async Task<ApiResult<TResponse>> SendAndReadAsync<TRequest, TResponse>(
            HttpClient httpClient,
            HttpMethod method,
            string route,
            TRequest request,
            string action,
            string successMessage,
            CancellationToken cancellationToken = default)
        {
            try
            {
                using var message = new HttpRequestMessage(method, route)
                {
                    Content = JsonContent.Create(request)
                };

                using var response = await httpClient.SendAsync(
                    message,
                    cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    return ApiResult<TResponse>.Fail(
                        GetHttpMessage(response.StatusCode, action),
                        (int)response.StatusCode);
                }

                var data = await response.Content.ReadFromJsonAsync<TResponse>(
                    cancellationToken: cancellationToken);

                if (data == null)
                {
                    return ApiResult<TResponse>.Fail(
                        $"La operación fue procesada, pero el servidor no devolvió los datos esperados.");
                }

                return ApiResult<TResponse>.Ok(data, successMessage);
            }
            catch (TaskCanceledException)
                when (!cancellationToken.IsCancellationRequested)
            {
                return ApiResult<TResponse>.Fail(
                    "La solicitud tardó demasiado. Verifique su conexión e intente nuevamente.");
            }
            catch (OperationCanceledException)
            {
                return ApiResult<TResponse>.Fail("La operación fue cancelada.");
            }
            catch (HttpRequestException)
            {
                return ApiResult<TResponse>.Fail(
                    "No fue posible conectarse con el servidor. Verifique su conexión.");
            }
            catch (JsonException)
            {
                return ApiResult<TResponse>.Fail(
                    "El servidor respondió, pero los datos no tienen el formato esperado.");
            }
            catch (Exception)
            {
                return ApiResult<TResponse>.Fail(
                    $"Ocurrió un error inesperado al {action}.");
            }
        }

        public static string GetHttpMessage(
            HttpStatusCode statusCode,
            string action)
        {
            return statusCode switch
            {
                HttpStatusCode.BadRequest =>
                    $"No fue posible {action}. Revise los datos enviados.",

                HttpStatusCode.Unauthorized =>
                    "La sesión no está autorizada. Inicie sesión nuevamente.",

                HttpStatusCode.Forbidden =>
                    $"No tiene permisos para {action}.",

                HttpStatusCode.NotFound =>
                    $"No se encontró el recurso solicitado para {action}.",

                HttpStatusCode.Conflict =>
                    $"No fue posible {action} porque el registro está relacionado con otros datos.",

                HttpStatusCode.InternalServerError or
                HttpStatusCode.BadGateway or
                HttpStatusCode.ServiceUnavailable or
                HttpStatusCode.GatewayTimeout =>
                    "El servidor presentó un problema. Intente nuevamente.",

                _ =>
                    $"No fue posible {action}. Código HTTP {(int)statusCode}."
            };
        }
    }
}
