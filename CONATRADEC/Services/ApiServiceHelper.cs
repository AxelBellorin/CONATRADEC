using CONATRADEC.Models;
using System.Collections.ObjectModel;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace CONATRADEC.Services
{
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
                using var response = await httpClient.GetAsync(route, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    return ApiResult<ObservableCollection<T>>.Fail(
                        await GetResponseMessageAsync(
                            response,
                            $"No fue posible cargar {entityName}."),
                        (int)response.StatusCode);
                }

                var data = await response.Content.ReadFromJsonAsync<ObservableCollection<T>>(
                    cancellationToken: cancellationToken);

                return ApiResult<ObservableCollection<T>>.Ok(
                    data ?? new ObservableCollection<T>());
            }
            catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
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
            try
            {
                using var message = new HttpRequestMessage(method, route);

                if (request is not null)
                    message.Content = JsonContent.Create(request);

                using var response = await httpClient.SendAsync(message, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    return ApiResult<bool>.Fail(
                        await GetResponseMessageAsync(
                            response,
                            $"No fue posible {action}."),
                        (int)response.StatusCode);
                }

                return ApiResult<bool>.Ok(true, successMessage);
            }
            catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                return ApiResult<bool>.Fail(
                    "La solicitud tardó demasiado. Intente nuevamente.");
            }
            catch (OperationCanceledException)
            {
                return ApiResult<bool>.Fail("La operación fue cancelada.");
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

                using var response = await httpClient.SendAsync(message, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    return ApiResult<TResponse>.Fail(
                        await GetResponseMessageAsync(
                            response,
                            $"No fue posible {action}."),
                        (int)response.StatusCode);
                }

                var data = await response.Content.ReadFromJsonAsync<TResponse>(
                    cancellationToken: cancellationToken);

                if (data == null)
                {
                    return ApiResult<TResponse>.Fail(
                        "La operación se procesó, pero el servidor no devolvió los datos esperados.");
                }

                return ApiResult<TResponse>.Ok(data, successMessage);
            }
            catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                return ApiResult<TResponse>.Fail(
                    "La solicitud tardó demasiado. Intente nuevamente.");
            }
            catch (OperationCanceledException)
            {
                return ApiResult<TResponse>.Fail("La operación fue cancelada.");
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

        private static async Task<string> GetResponseMessageAsync(
            HttpResponseMessage response,
            string fallback)
        {
            try
            {
                string content = await response.Content.ReadAsStringAsync();

                if (string.IsNullOrWhiteSpace(content))
                    return GetHttpMessage(response.StatusCode, fallback);

                content = content.Trim();

                if (!content.StartsWith("{") && !content.StartsWith("["))
                    return content.Trim('"');

                using JsonDocument document = JsonDocument.Parse(content);
                JsonElement root = document.RootElement;

                foreach (string propertyName in new[] { "message", "mensaje", "title", "detail" })
                {
                    if (root.ValueKind == JsonValueKind.Object &&
                        root.TryGetProperty(propertyName, out JsonElement value) &&
                        value.ValueKind == JsonValueKind.String &&
                        !string.IsNullOrWhiteSpace(value.GetString()))
                    {
                        return value.GetString()!;
                    }
                }

                if (root.ValueKind == JsonValueKind.Object &&
                    root.TryGetProperty("errors", out JsonElement errors) &&
                    errors.ValueKind == JsonValueKind.Object)
                {
                    foreach (JsonProperty property in errors.EnumerateObject())
                    {
                        if (property.Value.ValueKind == JsonValueKind.Array)
                        {
                            string? first = property.Value
                                .EnumerateArray()
                                .FirstOrDefault(x => x.ValueKind == JsonValueKind.String)
                                .GetString();

                            if (!string.IsNullOrWhiteSpace(first))
                                return first;
                        }
                    }
                }
            }
            catch
            {
                // Se usa el mensaje alternativo.
            }

            return GetHttpMessage(response.StatusCode, fallback);
        }

        public static string GetHttpMessage(
            HttpStatusCode statusCode,
            string fallback)
        {
            return statusCode switch
            {
                HttpStatusCode.BadRequest => "Revise los datos ingresados.",
                HttpStatusCode.Unauthorized => "La sesión no está autorizada. Inicie sesión nuevamente.",
                HttpStatusCode.Forbidden => "No tiene permisos para realizar esta operación.",
                HttpStatusCode.NotFound => "No se encontró el registro solicitado.",
                HttpStatusCode.Conflict => "La información ingresada ya está siendo utilizada.",
                HttpStatusCode.InternalServerError or
                HttpStatusCode.BadGateway or
                HttpStatusCode.ServiceUnavailable or
                HttpStatusCode.GatewayTimeout =>
                    "El servidor presentó un problema. Intente nuevamente.",
                _ => fallback
            };
        }
    }
}
