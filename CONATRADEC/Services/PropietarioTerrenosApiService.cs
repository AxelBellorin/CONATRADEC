using CONATRADEC.Models;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace CONATRADEC.Services
{
    /// <summary>
    /// Consulta los terrenos de un propietario y permite reasignarlos a otro
    /// propietario activo sin salir del módulo de Propietarios.
    /// </summary>
    public sealed class PropietarioTerrenosApiService
    {
        private static readonly JsonSerializerOptions JsonOptions =
            new(JsonSerializerDefaults.Web)
            {
                PropertyNameCaseInsensitive = true
            };

        private readonly HttpClient httpClient;

        public PropietarioTerrenosApiService()
            : this(ApiClientService.Client)
        {
        }

        internal PropietarioTerrenosApiService(
            HttpClient httpClient)
        {
            this.httpClient =
                httpClient ??
                throw new ArgumentNullException(
                    nameof(httpClient));
        }

        public async Task<ApiResult<PropietarioDetalleResponse>>
            ObtenerDetalleAsync(
                int propietarioId,
                CancellationToken cancellationToken = default)
        {
            if (propietarioId <= 0)
            {
                return ApiResult<PropietarioDetalleResponse>.Fail(
                    "No se recibió un propietario válido.");
            }

            try
            {
                using HttpResponseMessage response =
                    await httpClient.GetAsync(
                        "api/parametrizacion-acceso/" +
                        $"propietarios/{propietarioId}",
                        cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    return ApiResult<PropietarioDetalleResponse>.Fail(
                        await ObtenerMensajeAsync(
                            response,
                            "No fue posible cargar los terrenos del propietario.",
                            cancellationToken),
                        (int)response.StatusCode);
                }

                PropietarioDetalleResponse? detalle =
                    await response.Content
                        .ReadFromJsonAsync<PropietarioDetalleResponse>(
                            JsonOptions,
                            cancellationToken);

                if (detalle?.Propietario == null)
                {
                    return ApiResult<PropietarioDetalleResponse>.Fail(
                        "El servidor respondió, pero no devolvió el propietario.");
                }

                detalle.Terrenos ??= [];

                return ApiResult<PropietarioDetalleResponse>.Ok(
                    detalle);
            }
            catch (TaskCanceledException)
                when (!cancellationToken.IsCancellationRequested)
            {
                return ApiResult<PropietarioDetalleResponse>.Fail(
                    "La consulta tardó demasiado.");
            }
            catch (OperationCanceledException)
            {
                return ApiResult<PropietarioDetalleResponse>.Fail(
                    "La operación fue cancelada.");
            }
            catch (HttpRequestException)
            {
                return ApiResult<PropietarioDetalleResponse>.Fail(
                    "No fue posible conectarse con el servidor.");
            }
            catch (JsonException)
            {
                return ApiResult<PropietarioDetalleResponse>.Fail(
                    "El servidor devolvió un detalle con formato no reconocido.");
            }
            catch
            {
                return ApiResult<PropietarioDetalleResponse>.Fail(
                    "Ocurrió un error inesperado al cargar los terrenos.");
            }
        }

        public async Task<ApiResult<bool>>
            ReasignarTerrenoAsync(
                int propietarioDestinoId,
                int terrenoId,
                CancellationToken cancellationToken = default)
        {
            if (propietarioDestinoId <= 0)
            {
                return ApiResult<bool>.Fail(
                    "Debe seleccionar un propietario de destino.");
            }

            if (terrenoId <= 0)
            {
                return ApiResult<bool>.Fail(
                    "No se recibió un terreno válido.");
            }

            try
            {
                var request =
                    new VincularTerrenoPropietarioRequest
                    {
                        TerrenoId = terrenoId
                    };

                using HttpResponseMessage response =
                    await httpClient.PostAsJsonAsync(
                        "api/parametrizacion-acceso/" +
                        $"propietarios/{propietarioDestinoId}/terrenos",
                        request,
                        cancellationToken);

                string mensaje =
                    await ObtenerMensajeAsync(
                        response,
                        response.IsSuccessStatusCode
                            ? "Terreno reasignado correctamente."
                            : "No fue posible reasignar el terreno.",
                        cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    return ApiResult<bool>.Fail(
                        mensaje,
                        (int)response.StatusCode);
                }

                return ApiResult<bool>.Ok(
                    true,
                    mensaje);
            }
            catch (TaskCanceledException)
                when (!cancellationToken.IsCancellationRequested)
            {
                return ApiResult<bool>.Fail(
                    "La solicitud tardó demasiado.");
            }
            catch (OperationCanceledException)
            {
                return ApiResult<bool>.Fail(
                    "La operación fue cancelada.");
            }
            catch (HttpRequestException)
            {
                return ApiResult<bool>.Fail(
                    "No fue posible conectarse con el servidor.");
            }
            catch
            {
                return ApiResult<bool>.Fail(
                    "Ocurrió un error inesperado al reasignar el terreno.");
            }
        }

        private static async Task<string> ObtenerMensajeAsync(
            HttpResponseMessage response,
            string predeterminado,
            CancellationToken cancellationToken)
        {
            try
            {
                string contenido =
                    await response.Content.ReadAsStringAsync(
                        cancellationToken);

                if (!string.IsNullOrWhiteSpace(contenido))
                {
                    using JsonDocument documento =
                        JsonDocument.Parse(contenido);

                    JsonElement raiz =
                        documento.RootElement;

                    foreach (string nombre in new[]
                    {
                        "message",
                        "mensaje",
                        "error"
                    })
                    {
                        if (TryGetPropertyIgnoreCase(
                                raiz,
                                nombre,
                                out JsonElement valor) &&
                            valor.ValueKind ==
                                JsonValueKind.String)
                        {
                            string? texto =
                                valor.GetString();

                            if (!string.IsNullOrWhiteSpace(texto))
                                return texto;
                        }
                    }
                }
            }
            catch
            {
            }

            return response.StatusCode switch
            {
                HttpStatusCode.Unauthorized =>
                    "La sesión no es válida o ha expirado.",

                HttpStatusCode.Forbidden =>
                    "No tiene permiso para administrar los terrenos del propietario.",

                HttpStatusCode.NotFound =>
                    "No se encontró el propietario o el terreno.",

                HttpStatusCode.Conflict =>
                    "No fue posible cambiar la relación del terreno.",

                _ => predeterminado
            };
        }

        private static bool TryGetPropertyIgnoreCase(
            JsonElement elemento,
            string nombre,
            out JsonElement valor)
        {
            if (elemento.ValueKind != JsonValueKind.Object)
            {
                valor = default;
                return false;
            }

            foreach (JsonProperty propiedad
                     in elemento.EnumerateObject())
            {
                if (string.Equals(
                        propiedad.Name,
                        nombre,
                        StringComparison.OrdinalIgnoreCase))
                {
                    valor = propiedad.Value;
                    return true;
                }
            }

            valor = default;
            return false;
        }
    }
}
