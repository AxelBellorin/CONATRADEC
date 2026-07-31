using CONATRADEC.Models;
using System.Net;
using System.Text.Json;

namespace CONATRADEC.Services
{
    /// <summary>
    /// Operaciones administrativas adicionales del CRUD de propietarios.
    /// Se mantiene separado para no alterar las llamadas existentes de
    /// creación, edición y selección.
    /// </summary>
    public sealed class PropietarioCrudApiService
    {
        private readonly HttpClient httpClient;

        public PropietarioCrudApiService()
            : this(ApiClientService.Client)
        {
        }

        public PropietarioCrudApiService(
            HttpClient httpClient)
        {
            this.httpClient =
                httpClient ??
                throw new ArgumentNullException(
                    nameof(httpClient));
        }

        public async Task<ApiResult<bool>>
            EliminarPropietarioResultAsync(
                int propietarioId,
                CancellationToken cancellationToken = default)
        {
            if (propietarioId <= 0)
            {
                return ApiResult<bool>.Fail(
                    "No se recibió un propietario válido.");
            }

            try
            {
                using HttpResponseMessage response =
                    await httpClient.DeleteAsync(
                        "api/parametrizacion-acceso/" +
                        $"propietarios/{propietarioId}",
                        cancellationToken);

                string mensaje =
                    await ObtenerMensajeAsync(
                        response,
                        response.IsSuccessStatusCode
                            ? "Propietario eliminado correctamente."
                            : "No fue posible eliminar el propietario.",
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
                    "Ocurrió un error inesperado al eliminar el propietario.");
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
                HttpStatusCode.Conflict =>
                    "No se puede eliminar el propietario porque tiene terrenos vinculados. Reasigne los terrenos a otro propietario antes de continuar.",

                HttpStatusCode.Forbidden =>
                    "No tiene permiso para eliminar propietarios.",

                HttpStatusCode.NotFound =>
                    "No se encontró el propietario.",

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
