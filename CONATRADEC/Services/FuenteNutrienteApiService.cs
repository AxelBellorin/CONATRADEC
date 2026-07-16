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

        private readonly JsonSerializerOptions jsonOptions = new(JsonSerializerDefaults.Web)
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
                    return ApiResult<ObservableCollection<FuenteNutrienteResponse>>.Fail(
                        ObtenerMensajeHttp(
                            response.StatusCode,
                            "cargar las fuentes de nutrientes"),
                        (int)response.StatusCode);
                }

                var fuentes = await response.Content
                    .ReadFromJsonAsync<ObservableCollection<FuenteNutrienteResponse>>(
                        jsonOptions,
                        cancellationToken);

                return ApiResult<ObservableCollection<FuenteNutrienteResponse>>.Ok(
                    fuentes ?? new ObservableCollection<FuenteNutrienteResponse>());
            }
            catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                return ApiResult<ObservableCollection<FuenteNutrienteResponse>>.Fail(
                    "La solicitud tardó demasiado. Verifique su conexión e intente nuevamente.");
            }
            catch (OperationCanceledException)
            {
                return ApiResult<ObservableCollection<FuenteNutrienteResponse>>.Fail(
                    "La operación fue cancelada.");
            }
            catch (HttpRequestException)
            {
                return ApiResult<ObservableCollection<FuenteNutrienteResponse>>.Fail(
                    "No fue posible conectarse con el servidor. Verifique su conexión.");
            }
            catch (JsonException)
            {
                return ApiResult<ObservableCollection<FuenteNutrienteResponse>>.Fail(
                    "El servidor respondió, pero los datos de fuentes de nutrientes no tienen el formato esperado.");
            }
            catch (Exception)
            {
                return ApiResult<ObservableCollection<FuenteNutrienteResponse>>.Fail(
                    "Ocurrió un error inesperado al cargar las fuentes de nutrientes.");
            }
        }

        public async Task<ApiResult<bool>> DeleteFuenteNutrienteResultAsync(
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
                    return ApiResult<bool>.Fail(
                        ObtenerMensajeHttp(
                            response.StatusCode,
                            "eliminar la fuente de nutriente"),
                        (int)response.StatusCode);
                }

                return ApiResult<bool>.Ok(
                    true,
                    "Fuente de nutriente eliminada correctamente.");
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
            catch (Exception)
            {
                return ApiResult<bool>.Fail(
                    "Ocurrió un error inesperado al eliminar la fuente de nutriente.");
            }
        }

        // =========================================================
        // MÉTODOS ANTERIORES
        // Se conservan para no romper formularios ni otros módulos.
        // =========================================================

        public async Task<ObservableCollection<FuenteNutrienteResponse>>
            GetFuenteNutrienteAsync()
        {
            var resultado = await GetFuenteNutrienteResultAsync();

            return resultado.Success && resultado.Data != null
                ? resultado.Data
                : new ObservableCollection<FuenteNutrienteResponse>();
        }

        public async Task<ObservableCollection<FuenteNutrienteAporteTablaResponse>>
            GetAportesTablaAsync()
        {
            try
            {
                var response = await httpClient
                    .GetFromJsonAsync<ObservableCollection<FuenteNutrienteAporteTablaResponse>>(
                        "api/fuente-nutriente/aportes-tabla",
                        jsonOptions);

                return response
                    ?? new ObservableCollection<FuenteNutrienteAporteTablaResponse>();
            }
            catch
            {
                return new ObservableCollection<FuenteNutrienteAporteTablaResponse>();
            }
        }

        public async Task<bool> CreateFuenteNutrienteAsync(
            FuenteNutrienteRequest fuente)
        {
            FuenteNutrienteResponse? creada =
                await CreateFuenteNutrienteConRespuestaAsync(fuente);

            return creada?.FuenteNutrientesId != null &&
                   creada.FuenteNutrientesId > 0;
        }

        public async Task<FuenteNutrienteResponse?>
            CreateFuenteNutrienteConRespuestaAsync(
                FuenteNutrienteRequest fuente)
        {
            try
            {
                using HttpResponseMessage response = await httpClient.PostAsJsonAsync(
                    "api/fuente-nutriente/crear-con-elementos",
                    fuente,
                    jsonOptions);

                string jsonRespuesta = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode ||
                    string.IsNullOrWhiteSpace(jsonRespuesta))
                {
                    return null;
                }

                ApiDataResponse<FuenteNutrienteResponse>? respuesta =
                    JsonSerializer.Deserialize<ApiDataResponse<FuenteNutrienteResponse>>(
                        jsonRespuesta,
                        jsonOptions);

                if (respuesta?.Data != null)
                    return respuesta.Data;

                return JsonSerializer.Deserialize<FuenteNutrienteResponse>(
                    jsonRespuesta,
                    jsonOptions);
            }
            catch
            {
                return null;
            }
        }

        public async Task<bool> UpdateFuenteNutrienteAsync(
            FuenteNutrienteRequest fuente)
        {
            try
            {
                if (!fuente.FuenteNutrientesId.HasValue)
                    return false;

                using var response = await httpClient.PutAsJsonAsync(
                    $"api/fuente-nutriente/editar-con-elementos/{fuente.FuenteNutrientesId.Value}",
                    fuente,
                    jsonOptions);

                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> DeleteFuenteNutrienteAsync(
            FuenteNutrienteRequest fuente)
        {
            var resultado = await DeleteFuenteNutrienteResultAsync(fuente);
            return resultado.Success;
        }

        public async Task<bool> HabilitarEnmiendaCalcareaAsync(
            int fuenteNutrientesId,
            HabilitarEnmiendaCalcareaRequest request)
        {
            try
            {
                using HttpResponseMessage response = await httpClient.PostAsJsonAsync(
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
                using HttpResponseMessage response = await httpClient.PutAsync(
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
                using HttpResponseMessage response = await httpClient.PostAsync(
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
                using HttpResponseMessage response = await httpClient.PutAsync(
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
        // MÉTODOS AUXILIARES
        // =========================================================

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
