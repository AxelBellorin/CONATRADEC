using CONATRADEC.Models;
using System.Text.Json;

namespace CONATRADEC.Services
{
    /// <summary>
    /// Operaciones posteriores a la decisión técnica del aprobador.
    /// El estado agregado se obtiene ahora desde las publicaciones por
    /// diagnóstico. La forma del modelo se conserva para no romper las tarjetas
    /// históricas que todavía muestran un resumen por fotografía.
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
                    $"api/inspecciones-fitosanitarias/{inspeccionId}/" +
                    $"fotografias/{fotografiaId}/" +
                    "estado-publicaciones-diagnosticos"),
                cancellationToken);

        /// <summary>
        /// Compatibilidad con el botón anterior: si se invoca esta operación se
        /// retiran todas las publicaciones activas de la fotografía. La pantalla
        /// nueva permite decidirlo de forma individual por diagnóstico.
        /// </summary>
        public Task<EstadoAlbumAprobador> RetirarPublicacionAsync(
            int inspeccionId,
            int fotografiaId,
            CancellationToken cancellationToken = default) =>
            EnviarAsync(
                new HttpRequestMessage(
                    HttpMethod.Patch,
                    $"api/inspecciones-fitosanitarias/{inspeccionId}/" +
                    $"fotografias/{fotografiaId}/" +
                    "publicaciones-diagnosticos/retirar-todas"),
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
