using CONATRADEC.Models;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace CONATRADEC.Services
{
    /// <summary>
    /// Obtiene la versión actual del terreno directamente desde la API.
    ///
    /// Se utiliza al abrir Ver o Editar para no depender de una tarjeta
    /// cargada antes de que el terreno fuera reasignado desde la Web u otro
    /// dispositivo.
    /// </summary>
    public sealed class TerrenoDetalleActualApiService
    {
        private static readonly JsonSerializerOptions JsonOptions =
            new(JsonSerializerDefaults.Web)
            {
                PropertyNameCaseInsensitive = true
            };

        private readonly HttpClient httpClient;

        public TerrenoDetalleActualApiService()
            : this(ApiClientService.Client)
        {
        }

        internal TerrenoDetalleActualApiService(
            HttpClient httpClient)
        {
            this.httpClient =
                httpClient ??
                throw new ArgumentNullException(
                    nameof(httpClient));
        }

        public async Task<ApiResult<TerrenoResponse>>
            ObtenerAsync(
                int terrenoId,
                CancellationToken cancellationToken = default)
        {
            if (terrenoId <= 0)
            {
                return ApiResult<TerrenoResponse>.Fail(
                    "El identificador del terreno no es válido.");
            }

            try
            {
                using HttpResponseMessage response =
                    await httpClient.GetAsync(
                        $"api/terreno/{terrenoId}",
                        HttpCompletionOption.ResponseHeadersRead,
                        cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    return ApiResult<TerrenoResponse>.Fail(
                        ObtenerMensaje(
                            response.StatusCode),
                        (int)response.StatusCode);
                }

                TerrenoResponse? terreno =
                    await response.Content
                        .ReadFromJsonAsync<TerrenoResponse>(
                            JsonOptions,
                            cancellationToken);

                if (terreno?.TerrenoId is null or <= 0)
                {
                    return ApiResult<TerrenoResponse>.Fail(
                        "El servidor respondió, pero no devolvió el terreno solicitado.");
                }

                return ApiResult<TerrenoResponse>.Ok(
                    terreno);
            }
            catch (TaskCanceledException)
                when (!cancellationToken.IsCancellationRequested)
            {
                return ApiResult<TerrenoResponse>.Fail(
                    "La consulta del terreno tardó demasiado.");
            }
            catch (OperationCanceledException)
            {
                return ApiResult<TerrenoResponse>.Fail(
                    "La consulta del terreno fue cancelada.");
            }
            catch (HttpRequestException)
            {
                return ApiResult<TerrenoResponse>.Fail(
                    "No fue posible conectarse con el servidor.");
            }
            catch (JsonException)
            {
                return ApiResult<TerrenoResponse>.Fail(
                    "El servidor devolvió un terreno con un formato no reconocido.");
            }
            catch
            {
                return ApiResult<TerrenoResponse>.Fail(
                    "No fue posible actualizar los datos del terreno.");
            }
        }

        private static string ObtenerMensaje(
            HttpStatusCode statusCode) =>
            statusCode switch
            {
                HttpStatusCode.Unauthorized =>
                    "La sesión no es válida o ha expirado.",

                HttpStatusCode.Forbidden =>
                    "No tiene permiso para consultar el terreno.",

                HttpStatusCode.NotFound =>
                    "El terreno ya no existe o fue desactivado.",

                HttpStatusCode.BadGateway or
                HttpStatusCode.ServiceUnavailable or
                HttpStatusCode.GatewayTimeout =>
                    "El servidor no está disponible temporalmente.",

                _ =>
                    "No fue posible obtener la información actual del terreno."
            };
    }
}
