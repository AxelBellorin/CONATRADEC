using CONATRADEC.Models;
using System.Collections.ObjectModel;
using System.Net.Http.Json;
using System.Text.Json;

namespace CONATRADEC.Services
{
    internal static class ConfiguracionApiServiceHelper
    {
        private static readonly JsonSerializerOptions JsonOptions =
            new(JsonSerializerDefaults.Web)
            {
                PropertyNameCaseInsensitive = true
            };

        public static async Task<ApiResult<ObservableCollection<T>>> GetCollectionAsync<T>(
            HttpClient httpClient,
            string route,
            string entityName,
            CancellationToken cancellationToken = default)
        {
            try
            {
                using HttpResponseMessage response = await httpClient.GetAsync(
                    route,
                    cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    string message = await ReadMessageAsync(
                        response,
                        $"No fue posible cargar {entityName}.",
                        cancellationToken);

                    return ApiResult<ObservableCollection<T>>.Fail(
                        message,
                        (int)response.StatusCode);
                }

                ObservableCollection<T>? data = await response.Content
                    .ReadFromJsonAsync<ObservableCollection<T>>(
                        JsonOptions,
                        cancellationToken);

                return ApiResult<ObservableCollection<T>>.Ok(
                    data ?? new ObservableCollection<T>());
            }
            catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
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
            string errorMessage,
            string successMessage,
            CancellationToken cancellationToken = default)
        {
            try
            {
                using var message = new HttpRequestMessage(method, route);

                if (request is not null)
                {
                    message.Content = JsonContent.Create(
                        request,
                        options: JsonOptions);
                }

                using HttpResponseMessage response = await httpClient.SendAsync(
                    message,
                    cancellationToken);

                string apiMessage = await ReadMessageAsync(
                    response,
                    response.IsSuccessStatusCode ? successMessage : errorMessage,
                    cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    return ApiResult<bool>.Fail(
                        apiMessage,
                        (int)response.StatusCode);
                }

                return ApiResult<bool>.Ok(true, apiMessage);
            }
            catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
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
            catch
            {
                return ApiResult<bool>.Fail(errorMessage);
            }
        }

        private static async Task<string> ReadMessageAsync(
            HttpResponseMessage response,
            string fallback,
            CancellationToken cancellationToken)
        {
            string content = await response.Content.ReadAsStringAsync(cancellationToken);

            if (string.IsNullOrWhiteSpace(content))
                return fallback;

            try
            {
                ApiMessagePayload? payload = JsonSerializer.Deserialize<ApiMessagePayload>(
                    content,
                    JsonOptions);

                string message = payload?.Mensaje
                    ?? payload?.Message
                    ?? fallback;

                if (payload?.UsadoEn is { Count: > 0 })
                {
                    message += Environment.NewLine
                        + Environment.NewLine
                        + "Usado en:"
                        + Environment.NewLine
                        + string.Join(
                            Environment.NewLine,
                            payload.UsadoEn.Select(x => $"• {x}"));
                }

                return message;
            }
            catch (JsonException)
            {
                string clean = content.Trim().Trim('"');
                return string.IsNullOrWhiteSpace(clean) ? fallback : clean;
            }
        }

        private sealed class ApiMessagePayload
        {
            public string? Mensaje { get; set; }
            public string? Message { get; set; }
            public List<string>? UsadoEn { get; set; }
        }
    }
}
