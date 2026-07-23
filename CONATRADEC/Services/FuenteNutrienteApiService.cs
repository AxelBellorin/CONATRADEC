using CONATRADEC.Models;
using System.Collections.ObjectModel;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CONATRADEC.Services
{
    public class FuenteNutrienteApiService
    {
        private readonly HttpClient httpClient;

        private readonly JsonSerializerOptions jsonOptions =
            new(JsonSerializerDefaults.Web)
            {
                PropertyNameCaseInsensitive = true
            };

        public FuenteNutrienteApiService()
            : this(ApiClientService.Client)
        {
        }

        public FuenteNutrienteApiService(HttpClient httpClient)
        {
            this.httpClient = httpClient
                ?? throw new ArgumentNullException(nameof(httpClient));
        }

        // =========================================================
        // MÉTODOS CON RESULTADO DETALLADO
        // =========================================================

        public async Task<ApiResult<ObservableCollection<FuenteNutrienteResponse>>>
            GetFuenteNutrienteResultAsync(
                CancellationToken cancellationToken = default)
        {
            try
            {
                using var response = await httpClient.GetAsync(
                    "api/fuente-nutriente/listar",
                    cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    string mensaje =
                        await ApiServiceHelper.ReadResponseMessageAsync(
                            response,
                            ObtenerMensajeHttp(
                                response.StatusCode,
                                "cargar las fuentes de nutrientes"),
                            cancellationToken);

                    return ApiResult<ObservableCollection<FuenteNutrienteResponse>>
                        .Fail(
                            mensaje,
                            (int)response.StatusCode);
                }

                var fuentes = await response.Content
                    .ReadFromJsonAsync<
                        ObservableCollection<FuenteNutrienteResponse>>(
                        jsonOptions,
                        cancellationToken);

                return ApiResult<
                    ObservableCollection<FuenteNutrienteResponse>>.Ok(
                    fuentes ??
                    new ObservableCollection<FuenteNutrienteResponse>());
            }
            catch (TaskCanceledException)
                when (!cancellationToken.IsCancellationRequested)
            {
                return ApiResult<
                    ObservableCollection<FuenteNutrienteResponse>>.Fail(
                    "La solicitud tardó demasiado. Verifique su conexión e intente nuevamente.");
            }
            catch (OperationCanceledException)
            {
                return ApiResult<
                    ObservableCollection<FuenteNutrienteResponse>>.Fail(
                    "La operación fue cancelada.");
            }
            catch (HttpRequestException)
            {
                return ApiResult<
                    ObservableCollection<FuenteNutrienteResponse>>.Fail(
                    "No fue posible conectarse con el servidor. Verifique su conexión.");
            }
            catch (JsonException)
            {
                return ApiResult<
                    ObservableCollection<FuenteNutrienteResponse>>.Fail(
                    "El servidor respondió, pero los datos de fuentes de nutrientes no tienen el formato esperado.");
            }
            catch
            {
                return ApiResult<
                    ObservableCollection<FuenteNutrienteResponse>>.Fail(
                    "Ocurrió un error inesperado al cargar las fuentes de nutrientes.");
            }
        }

        public async Task<ApiResult<FuenteNutrienteResponse>>
            CreateFuenteNutrienteResultAsync(
                FuenteNutrienteRequest fuente,
                CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(fuente);

            try
            {
                using var response = await httpClient.PostAsJsonAsync(
                    "api/fuente-nutriente/crear-con-elementos",
                    fuente,
                    jsonOptions,
                    cancellationToken);

                string contenido = await response.Content.ReadAsStringAsync(
                    cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    string mensaje =
                        ApiErrorMessageParser.Parse(
                            response.StatusCode,
                            contenido,
                            ObtenerMensajeHttp(
                                response.StatusCode,
                                "crear la fuente de nutriente"));

                    return ApiResult<FuenteNutrienteResponse>.Fail(
                        mensaje,
                        (int)response.StatusCode);
                }

                FuenteNutrienteResponse? creada =
                    DeserializarRespuestaFuente(contenido);

                if (creada?.FuenteNutrientesId == null ||
                    creada.FuenteNutrientesId <= 0)
                {
                    return ApiResult<FuenteNutrienteResponse>.Fail(
                        "La fuente fue procesada, pero el servidor no devolvió su identificador.");
                }

                return ApiResult<FuenteNutrienteResponse>.Ok(
                    creada,
                    "Fuente de nutriente creada correctamente.");
            }
            catch (TaskCanceledException)
                when (!cancellationToken.IsCancellationRequested)
            {
                return ApiResult<FuenteNutrienteResponse>.Fail(
                    "La solicitud tardó demasiado. Verifique su conexión e intente nuevamente.");
            }
            catch (OperationCanceledException)
            {
                return ApiResult<FuenteNutrienteResponse>.Fail(
                    "La operación fue cancelada.");
            }
            catch (HttpRequestException)
            {
                return ApiResult<FuenteNutrienteResponse>.Fail(
                    "No fue posible conectarse con el servidor. Verifique su conexión.");
            }
            catch (JsonException)
            {
                return ApiResult<FuenteNutrienteResponse>.Fail(
                    "El servidor respondió, pero no fue posible interpretar la fuente creada.");
            }
            catch
            {
                return ApiResult<FuenteNutrienteResponse>.Fail(
                    "Ocurrió un error inesperado al crear la fuente de nutriente.");
            }
        }

        public async Task<ApiResult<bool>>
            UpdateFuenteNutrienteResultAsync(
                FuenteNutrienteRequest fuente,
                CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(fuente);

            if (!fuente.FuenteNutrientesId.HasValue ||
                fuente.FuenteNutrientesId.Value <= 0)
            {
                return ApiResult<bool>.Fail(
                    "No se recibió un identificador de fuente de nutriente válido.");
            }

            try
            {
                using var response = await httpClient.PutAsJsonAsync(
                    $"api/fuente-nutriente/editar-con-elementos/{fuente.FuenteNutrientesId.Value}",
                    fuente,
                    jsonOptions,
                    cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    string mensaje =
                        await ApiServiceHelper.ReadResponseMessageAsync(
                            response,
                            ObtenerMensajeHttp(
                                response.StatusCode,
                                "actualizar la fuente de nutriente"),
                            cancellationToken);

                    return ApiResult<bool>.Fail(
                        mensaje,
                        (int)response.StatusCode);
                }

                return ApiResult<bool>.Ok(
                    true,
                    "Fuente de nutriente actualizada correctamente.");
            }
            catch (TaskCanceledException)
                when (!cancellationToken.IsCancellationRequested)
            {
                return ApiResult<bool>.Fail(
                    "La solicitud tardó demasiado. Verifique su conexión e intente nuevamente.");
            }
            catch (OperationCanceledException)
            {
                return ApiResult<bool>.Fail(
                    "La operación fue cancelada.");
            }
            catch (HttpRequestException)
            {
                return ApiResult<bool>.Fail(
                    "No fue posible conectarse con el servidor. Verifique su conexión.");
            }
            catch
            {
                return ApiResult<bool>.Fail(
                    "Ocurrió un error inesperado al actualizar la fuente de nutriente.");
            }
        }

        public async Task<ApiResult<bool>>
            DeleteFuenteNutrienteResultAsync(
                FuenteNutrienteRequest fuente,
                CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(fuente);

            if (!fuente.FuenteNutrientesId.HasValue ||
                fuente.FuenteNutrientesId.Value <= 0)
            {
                return ApiResult<bool>.Fail(
                    "No se recibió un identificador de fuente de nutriente válido.");
            }

            try
            {
                using var response = await httpClient.DeleteAsync(
                    $"api/fuente-nutriente/eliminar/{fuente.FuenteNutrientesId.Value}",
                    cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    string mensaje =
                        await ApiServiceHelper.ReadResponseMessageAsync(
                            response,
                            ObtenerMensajeHttp(
                                response.StatusCode,
                                "eliminar la fuente de nutriente"),
                            cancellationToken);

                    return ApiResult<bool>.Fail(
                        mensaje,
                        (int)response.StatusCode);
                }

                return ApiResult<bool>.Ok(
                    true,
                    "Fuente de nutriente eliminada correctamente.");
            }
            catch (TaskCanceledException)
                when (!cancellationToken.IsCancellationRequested)
            {
                return ApiResult<bool>.Fail(
                    "La solicitud tardó demasiado. Verifique su conexión e intente nuevamente.");
            }
            catch (OperationCanceledException)
            {
                return ApiResult<bool>.Fail(
                    "La operación fue cancelada.");
            }
            catch (HttpRequestException)
            {
                return ApiResult<bool>.Fail(
                    "No fue posible conectarse con el servidor. Verifique su conexión.");
            }
            catch
            {
                return ApiResult<bool>.Fail(
                    "Ocurrió un error inesperado al eliminar la fuente de nutriente.");
            }
        }

        // =========================================================
        // MÉTODOS COMPATIBLES CON EL RESTO DEL PROYECTO
        // =========================================================

        public async Task<ObservableCollection<FuenteNutrienteResponse>>
            GetFuenteNutrienteAsync()
        {
            var resultado = await GetFuenteNutrienteResultAsync();

            return resultado.Success && resultado.Data != null
                ? resultado.Data
                : new ObservableCollection<FuenteNutrienteResponse>();
        }

        public async Task<
            ObservableCollection<FuenteNutrienteAporteTablaResponse>>
            GetAportesTablaAsync()
        {
            try
            {
                var response = await httpClient
                    .GetFromJsonAsync<
                        ObservableCollection<
                            FuenteNutrienteAporteTablaResponse>>(
                        "api/fuente-nutriente/aportes-tabla",
                        jsonOptions);

                return response ??
                    new ObservableCollection<
                        FuenteNutrienteAporteTablaResponse>();
            }
            catch
            {
                return new ObservableCollection<
                    FuenteNutrienteAporteTablaResponse>();
            }
        }

        public async Task<bool> CreateFuenteNutrienteAsync(
            FuenteNutrienteRequest fuente)
        {
            var resultado =
                await CreateFuenteNutrienteResultAsync(fuente);

            return resultado.Success &&
                   resultado.Data?.FuenteNutrientesId > 0;
        }

        public async Task<FuenteNutrienteResponse?>
            CreateFuenteNutrienteConRespuestaAsync(
                FuenteNutrienteRequest fuente)
        {
            var resultado =
                await CreateFuenteNutrienteResultAsync(fuente);

            return resultado.Success
                ? resultado.Data
                : null;
        }

        public async Task<bool> UpdateFuenteNutrienteAsync(
            FuenteNutrienteRequest fuente)
        {
            var resultado =
                await UpdateFuenteNutrienteResultAsync(fuente);

            return resultado.Success;
        }

        public async Task<bool> DeleteFuenteNutrienteAsync(
            FuenteNutrienteRequest fuente)
        {
            var resultado =
                await DeleteFuenteNutrienteResultAsync(fuente);

            return resultado.Success;
        }

        public async Task<bool> HabilitarEnmiendaCalcareaAsync(
            int fuenteNutrientesId,
            HabilitarEnmiendaCalcareaRequest request)
        {
            try
            {
                using var response = await httpClient.PostAsJsonAsync(
                    $"api/fuente-nutriente/{fuenteNutrientesId}/habilitar-enmienda-calcarea",
                    request,
                    jsonOptions);

                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> DeshabilitarEnmiendaCalcareaAsync(
            int fuenteNutrientesId)
        {
            try
            {
                using var response = await httpClient.PutAsync(
                    $"api/fuente-nutriente/deshabilitar-enmienda-calcarea/{fuenteNutrientesId}",
                    null);

                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> HabilitarFertilizacionMixtaAsync(
            int fuenteNutrientesId)
        {
            try
            {
                using var response = await httpClient.PostAsync(
                    $"api/fuente-nutriente/habilitar-fertilizacion-mixta/{fuenteNutrientesId}",
                    null);

                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> DeshabilitarFertilizacionMixtaAsync(
            int fuenteNutrientesId)
        {
            try
            {
                using var response = await httpClient.PutAsync(
                    $"api/fuente-nutriente/deshabilitar-fertilizacion-mixta/{fuenteNutrientesId}",
                    null);

                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        // =========================================================
        // AUXILIARES
        // =========================================================

        private FuenteNutrienteResponse?
            DeserializarRespuestaFuente(string contenido)
        {
            if (string.IsNullOrWhiteSpace(contenido))
                return null;

            ApiDataResponse<FuenteNutrienteResponse>? respuesta =
                JsonSerializer.Deserialize<
                    ApiDataResponse<FuenteNutrienteResponse>>(
                    contenido,
                    jsonOptions);

            if (respuesta?.Data != null)
                return respuesta.Data;

            return JsonSerializer.Deserialize<FuenteNutrienteResponse>(
                contenido,
                jsonOptions);
        }

        private static string ObtenerMensajeHttp(
            HttpStatusCode statusCode,
            string operacion)
        {
            return statusCode switch
            {
                HttpStatusCode.BadRequest =>
                    $"No fue posible {operacion} porque los datos enviados no son válidos.",

                HttpStatusCode.Unauthorized =>
                    "La sesión no está autorizada. Inicie sesión nuevamente.",

                HttpStatusCode.Forbidden =>
                    $"No tiene permiso para {operacion}.",

                HttpStatusCode.NotFound =>
                    "No se encontró el recurso solicitado.",

                HttpStatusCode.Conflict =>
                    $"No fue posible {operacion} porque existe un conflicto con los datos actuales.",

                >= HttpStatusCode.InternalServerError =>
                    "El servidor presentó un problema. Intente nuevamente.",

                _ =>
                    $"No fue posible {operacion}. Código HTTP: {(int)statusCode}."
            };
        }

        private sealed class ApiDataResponse<T>
        {
            [JsonPropertyName("success")]
            public bool? Success { get; set; }

            [JsonPropertyName("message")]
            public string? Message { get; set; }

            [JsonPropertyName("mensaje")]
            public string? Mensaje { get; set; }

            [JsonPropertyName("data")]
            public T? Data { get; set; }
        }
    }
}
