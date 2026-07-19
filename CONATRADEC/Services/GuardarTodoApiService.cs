using CONATRADEC.Models;
using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace CONATRADEC.Services
{
    public sealed class GuardarTodoApiService
    {
        private const string EndpointGuardar = "api/guardar-todo";
        private const string EndpointListado = "api/guardar-todo";

        private readonly HttpClient httpClient;

        private readonly JsonSerializerOptions jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.Never
        };

        public GuardarTodoApiService()
            : this(ApiClientService.Client)
        {
        }

        public GuardarTodoApiService(HttpClient httpClient)
        {
            this.httpClient = httpClient
                ?? throw new ArgumentNullException(nameof(httpClient));
        }

        public Task<GuardarTodoResponse> GuardarAsync(
            GuardarTodoRequest request,
            CancellationToken cancellationToken = default)
        {
            return EnviarSolicitudAsync(
                HttpMethod.Post,
                EndpointGuardar,
                request,
                "guardar",
                cancellationToken);
        }

        public Task<GuardarTodoResponse> EditarAsync(
            int analisisSueloCalculoId,
            GuardarTodoRequest request,
            CancellationToken cancellationToken = default)
        {
            if (analisisSueloCalculoId <= 0)
            {
                return Task.FromResult(new GuardarTodoResponse
                {
                    Success = false,
                    Message = "El identificador del cálculo que se debe editar no es válido."
                });
            }

            return EnviarSolicitudAsync(
                HttpMethod.Put,
                $"{EndpointGuardar}/editar/{analisisSueloCalculoId}",
                request,
                "actualizar",
                cancellationToken);
        }

        public async Task<AnalisisGuardadoListaResponse> ListarAsync(
            CancellationToken cancellationToken = default)
        {
            try
            {
                using HttpResponseMessage response = await httpClient.GetAsync(
                    EndpointListado,
                    cancellationToken);

                string jsonResponse = await response.Content.ReadAsStringAsync(
                    cancellationToken);

                AnalisisGuardadoListaResponse? resultado =
                    await DeserializarSeguroAsync<
                        AnalisisGuardadoListaResponse>(jsonResponse);

                if (!response.IsSuccessStatusCode)
                {
                    return new AnalisisGuardadoListaResponse
                    {
                        Success = false,
                        Message = ExtraerMensajeError(
                            jsonResponse,
                            $"No fue posible cargar los análisis. Código HTTP {(int)response.StatusCode}.")
                    };
                }

                if (resultado == null)
                {
                    return new AnalisisGuardadoListaResponse
                    {
                        Success = false,
                        Message = "La API respondió, pero no se pudo interpretar la lista de análisis."
                    };
                }

                resultado.Data ??= new();
                return resultado;
            }
            catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                return new AnalisisGuardadoListaResponse
                {
                    Success = false,
                    Message = "La carga tardó demasiado. Revise la conexión e intente nuevamente."
                };
            }
            catch (HttpRequestException)
            {
                return new AnalisisGuardadoListaResponse
                {
                    Success = false,
                    Message = "No fue posible conectarse con el servidor para cargar los análisis."
                };
            }
            catch (Exception ex)
            {
                return new AnalisisGuardadoListaResponse
                {
                    Success = false,
                    Message = $"Ocurrió un error al cargar los análisis: {ex.Message}"
                };
            }
        }

        public async Task<AnalisisGuardadoDetalleResponse> ObtenerDetalleAsync(
            int analisisSueloCalculoId,
            CancellationToken cancellationToken = default)
        {
            if (analisisSueloCalculoId <= 0)
            {
                return new AnalisisGuardadoDetalleResponse
                {
                    Success = false,
                    Message = "El identificador del cálculo no es válido."
                };
            }

            try
            {
                using HttpResponseMessage response = await httpClient.GetAsync(
                    $"{EndpointGuardar}/listardetalle/{analisisSueloCalculoId}",
                    cancellationToken);

                string jsonResponse = await response.Content.ReadAsStringAsync(
                    cancellationToken);

                AnalisisGuardadoDetalleResponse? resultado =
                    await DeserializarSeguroAsync<
                        AnalisisGuardadoDetalleResponse>(jsonResponse);

                if (!response.IsSuccessStatusCode)
                {
                    return new AnalisisGuardadoDetalleResponse
                    {
                        Success = false,
                        Message = ExtraerMensajeError(
                            jsonResponse,
                            $"No fue posible cargar el detalle. Código HTTP {(int)response.StatusCode}.")
                    };
                }

                if (resultado?.Data == null)
                {
                    return new AnalisisGuardadoDetalleResponse
                    {
                        Success = false,
                        Message = "La API respondió, pero no devolvió el detalle del análisis."
                    };
                }

                return resultado;
            }
            catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                return new AnalisisGuardadoDetalleResponse
                {
                    Success = false,
                    Message = "La consulta tardó demasiado. Revise la conexión e intente nuevamente."
                };
            }
            catch (HttpRequestException)
            {
                return new AnalisisGuardadoDetalleResponse
                {
                    Success = false,
                    Message = "No fue posible conectarse con el servidor para cargar el detalle."
                };
            }
            catch (Exception ex)
            {
                return new AnalisisGuardadoDetalleResponse
                {
                    Success = false,
                    Message = $"Ocurrió un error al cargar el detalle: {ex.Message}"
                };
            }
        }

        public async Task<EliminarAnalisisResponse> EliminarAsync(
            int analisisSueloId,
            CancellationToken cancellationToken = default)
        {
            if (analisisSueloId <= 0)
            {
                return new EliminarAnalisisResponse
                {
                    Success = false,
                    Message = "El identificador del análisis no es válido."
                };
            }

            try
            {
                using HttpResponseMessage response = await httpClient.DeleteAsync(
                    $"{EndpointGuardar}/{analisisSueloId}",
                    cancellationToken);

                string jsonResponse = await response.Content.ReadAsStringAsync(
                    cancellationToken);

                EliminarAnalisisResponse? resultado =
                    await DeserializarSeguroAsync<
                        EliminarAnalisisResponse>(jsonResponse);

                if (!response.IsSuccessStatusCode)
                {
                    string mensajeError = ExtraerMensajeError(
                        jsonResponse,
                        $"No fue posible eliminar el análisis. Código HTTP {(int)response.StatusCode}.");

                    if (resultado != null)
                    {
                        resultado.Success = false;

                        if (string.IsNullOrWhiteSpace(resultado.Message))
                            resultado.Message = mensajeError;

                        return resultado;
                    }

                    return new EliminarAnalisisResponse
                    {
                        Success = false,
                        Message = mensajeError
                    };
                }

                return resultado ?? new EliminarAnalisisResponse
                {
                    Success = false,
                    Message = "La API procesó la eliminación, pero no se pudo interpretar su respuesta."
                };
            }
            catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                return new EliminarAnalisisResponse
                {
                    Success = false,
                    Message = "La eliminación tardó demasiado. Revise la conexión e intente nuevamente."
                };
            }
            catch (HttpRequestException)
            {
                return new EliminarAnalisisResponse
                {
                    Success = false,
                    Message = "No fue posible conectarse con el servidor para eliminar el análisis."
                };
            }
            catch (Exception ex)
            {
                return new EliminarAnalisisResponse
                {
                    Success = false,
                    Message = $"Ocurrió un error al eliminar el análisis: {ex.Message}"
                };
            }
        }

        private async Task<GuardarTodoResponse> EnviarSolicitudAsync(
            HttpMethod method,
            string endpoint,
            GuardarTodoRequest request,
            string accion,
            CancellationToken cancellationToken)
        {
            if (request == null)
            {
                return new GuardarTodoResponse
                {
                    Success = false,
                    Message = "No se recibieron los datos que se deben procesar."
                };
            }

            try
            {
                string jsonRequest = await Task.Run(() =>
                    JsonSerializer.Serialize(
                        request,
                        jsonOptions));

                using HttpRequestMessage mensaje = new(method, endpoint)
                {
                    Content = new StringContent(
                        jsonRequest,
                        Encoding.UTF8,
                        "application/json")
                };

                using HttpResponseMessage response = await httpClient.SendAsync(
                    mensaje,
                    cancellationToken);

                string jsonResponse = await response.Content.ReadAsStringAsync(
                    cancellationToken);

                GuardarTodoResponse? resultado =
                    await DeserializarSeguroAsync<
                        GuardarTodoResponse>(jsonResponse);

                if (!response.IsSuccessStatusCode)
                {
                    string mensajeError = ExtraerMensajeError(
                        jsonResponse,
                        $"No fue posible {accion} el análisis. Código HTTP {(int)response.StatusCode}.");

                    if (resultado != null)
                    {
                        resultado.Success = false;

                        if (string.IsNullOrWhiteSpace(resultado.Message))
                            resultado.Message = mensajeError;

                        return resultado;
                    }

                    return new GuardarTodoResponse
                    {
                        Success = false,
                        Message = mensajeError
                    };
                }

                return resultado ?? new GuardarTodoResponse
                {
                    Success = false,
                    Message = $"La API procesó la solicitud, pero no se pudo interpretar la respuesta al {accion}."
                };
            }
            catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                return new GuardarTodoResponse
                {
                    Success = false,
                    Message = "La solicitud tardó demasiado. Revise la conexión e intente nuevamente."
                };
            }
            catch (HttpRequestException)
            {
                return new GuardarTodoResponse
                {
                    Success = false,
                    Message = "No fue posible conectarse con el servidor. Verifique su conexión."
                };
            }
            catch (Exception ex)
            {
                return new GuardarTodoResponse
                {
                    Success = false,
                    Message = $"Ocurrió un error al {accion} el análisis: {ex.Message}"
                };
            }
        }

        private Task<T?> DeserializarSeguroAsync<T>(string json)
            where T : class
        {
            if (string.IsNullOrWhiteSpace(json))
                return Task.FromResult<T?>(null);

            return Task.Run<T?>(() =>
            {
                try
                {
                    return JsonSerializer.Deserialize<T>(
                        json,
                        jsonOptions);
                }
                catch
                {
                    return null;
                }
            });
        }

        private static string ExtraerMensajeError(
            string json,
            string mensajePredeterminado)
        {
            if (string.IsNullOrWhiteSpace(json))
                return mensajePredeterminado;

            try
            {
                using JsonDocument document = JsonDocument.Parse(json);
                JsonElement root = document.RootElement;

                if (TryGetPropertyIgnoreCase(root, "message", out JsonElement message) &&
                    message.ValueKind == JsonValueKind.String &&
                    !string.IsNullOrWhiteSpace(message.GetString()))
                {
                    return message.GetString()!;
                }

                if (TryGetPropertyIgnoreCase(root, "title", out JsonElement title) &&
                    title.ValueKind == JsonValueKind.String &&
                    !string.IsNullOrWhiteSpace(title.GetString()))
                {
                    return title.GetString()!;
                }

                if (TryGetPropertyIgnoreCase(root, "errors", out JsonElement errors) &&
                    errors.ValueKind == JsonValueKind.Object)
                {
                    foreach (JsonProperty property in errors.EnumerateObject())
                    {
                        if (property.Value.ValueKind != JsonValueKind.Array)
                            continue;

                        string? firstError = property.Value
                            .EnumerateArray()
                            .Where(x => x.ValueKind == JsonValueKind.String)
                            .Select(x => x.GetString())
                            .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));

                        if (!string.IsNullOrWhiteSpace(firstError))
                            return firstError;
                    }
                }
            }
            catch
            {
            }

            return mensajePredeterminado;
        }

        private static bool TryGetPropertyIgnoreCase(
            JsonElement element,
            string propertyName,
            out JsonElement value)
        {
            foreach (JsonProperty property in element.EnumerateObject())
            {
                if (string.Equals(
                        property.Name,
                        propertyName,
                        StringComparison.OrdinalIgnoreCase))
                {
                    value = property.Value;
                    return true;
                }
            }

            value = default;
            return false;
        }
    }
}
