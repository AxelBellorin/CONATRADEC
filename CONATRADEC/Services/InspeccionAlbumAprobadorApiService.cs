using CONATRADEC.Models;
using System.Net.Http.Json;
using System.Text.Json;

namespace CONATRADEC.Services
{
    /// <summary>
    /// Operaciones posteriores a la decisión técnica del aprobador. La
    /// autorización para el Álbum Botánico se mantiene separada de la
    /// aprobación de la evidencia y puede cambiarse posteriormente.
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

        public Task<EstadoAlbumAprobador> CambiarAutorizacionAsync(
            int inspeccionId,
            int fotografiaId,
            bool autorizar,
            CancellationToken cancellationToken = default)
        {
            var request = new HttpRequestMessage(
                HttpMethod.Patch,
                $"api/publicaciones-album-fitosanitarias/{inspeccionId}/fotografias/{fotografiaId}/autorizacion")
            {
                Content = JsonContent.Create(new { autorizar })
            };

            return EnviarAsync(request, cancellationToken);
        }

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
