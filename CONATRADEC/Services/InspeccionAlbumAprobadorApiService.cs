using CONATRADEC.Models;
using System.Text.Json;

namespace CONATRADEC.Services
{
    /// <summary>
    /// Operaciones posteriores a la decisión técnica del aprobador.
    /// Una vez confirmada la clasificación oficial, la administración del
    /// Álbum Botánico se reduce a consultar el estado o retirar una publicación.
    /// La publicación se realiza mediante el servicio fitosanitario existente.
    /// </summary>
    public sealed class InspeccionAlbumAprobadorApiService
    {
        private static readonly JsonSerializerOptions JsonOptions =
            new(JsonSerializerDefaults.Web)
            {
                PropertyNameCaseInsensitive = true
            };

        private readonly HttpClient client = ApiClientService.Client;

        public Task<EstadoAlbumAprobador> ObtenerEstadoAsync(
            int inspeccionId,
            int fotografiaId,
            CancellationToken cancellationToken = default) =>
            EnviarAsync(
                new HttpRequestMessage(
                    HttpMethod.Get,
                    $"api/publicaciones-album-fitosanitarias/{inspeccionId}/fotografias/{fotografiaId}/estado"),
                cancellationToken);

        /// <summary>
        /// Retira únicamente la copia activa del Álbum Botánico. La decisión
        /// técnica y la clasificación oficial del expediente permanecen
        /// inalterables y la fotografía puede volver a publicarse después.
        /// </summary>
        public Task<EstadoAlbumAprobador> RetirarPublicacionAsync(
            int inspeccionId,
            int fotografiaId,
            CancellationToken cancellationToken = default) =>
            EnviarAsync(
                new HttpRequestMessage(
                    HttpMethod.Patch,
                    $"api/publicaciones-album-fitosanitarias/{inspeccionId}/fotografias/{fotografiaId}/publicacion/retirar"),
                cancellationToken);

        private async Task<EstadoAlbumAprobador> EnviarAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            SesionInactividadService.Instance.RegistrarActividad();

            using (request)
            using (HttpResponseMessage response = await client.SendAsync(
                       request,
                       HttpCompletionOption.ResponseHeadersRead,
                       cancellationToken))
            {
                string contenido = await response.Content.ReadAsStringAsync(
                    cancellationToken);

                RespuestaApi<EstadoAlbumAprobador>? envelope = null;
                if (!string.IsNullOrWhiteSpace(contenido))
                {
                    try
                    {
                        envelope = JsonSerializer.Deserialize<
                            RespuestaApi<EstadoAlbumAprobador>>(
                            contenido,
                            JsonOptions);
                    }
                    catch (JsonException)
                    {
                    }
                }

                if (!response.IsSuccessStatusCode)
                {
                    throw new InvalidOperationException(
                        string.IsNullOrWhiteSpace(envelope?.Message)
                            ? "No fue posible actualizar el estado del Álbum Botánico."
                            : envelope.Message);
                }

                if (envelope?.Data != null)
                    return envelope.Data;

                throw new InvalidOperationException(
                    "El servidor devolvió un estado incompleto del Álbum Botánico.");
            }
        }

        private sealed class RespuestaApi<T>
        {
            public bool Success { get; set; }
            public string Message { get; set; } = string.Empty;
            public T? Data { get; set; }
        }
    }
}
