using CONATRADEC.Models;
using System.Net.Http.Json;
using System.Text.Json;

namespace CONATRADEC.Services
{
    /// <summary>
    /// Mantiene la reserva exclusiva de una inspección mientras el analizador
    /// o aprobador permanece trabajando en ella.
    /// </summary>
    public sealed class InspeccionRevisionBloqueoApiService
    {
        private static readonly JsonSerializerOptions JsonOptions =
            new(JsonSerializerDefaults.Web)
            {
                PropertyNameCaseInsensitive = true
            };

        private readonly HttpClient client = ApiClientService.Client;

        public Task<InspeccionRevisionBloqueo> AdquirirAsync(
            int inspeccionId,
            string modo,
            CancellationToken cancellationToken = default) =>
            EnviarAsync(
                new HttpRequestMessage(
                    HttpMethod.Post,
                    $"api/revision-fitosanitaria/{inspeccionId}/bloqueo/adquirir?modo={Uri.EscapeDataString(NormalizarModo(modo))}"),
                cancellationToken);

        public Task<InspeccionRevisionBloqueo> RenovarAsync(
            int inspeccionId,
            string modo,
            string token,
            CancellationToken cancellationToken = default)
        {
            var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"api/revision-fitosanitaria/{inspeccionId}/bloqueo/renovar?modo={Uri.EscapeDataString(NormalizarModo(modo))}")
            {
                Content = JsonContent.Create(new { token })
            };

            return EnviarAsync(request, cancellationToken);
        }

        public async Task LiberarAsync(
            int inspeccionId,
            string modo,
            string token,
            CancellationToken cancellationToken = default)
        {
            if (inspeccionId <= 0 || string.IsNullOrWhiteSpace(token))
                return;

            var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"api/revision-fitosanitaria/{inspeccionId}/bloqueo/liberar?modo={Uri.EscapeDataString(NormalizarModo(modo))}")
            {
                Content = JsonContent.Create(new { token })
            };

            SesionInactividadService.Instance.RegistrarActividad();

            using (request)
            using (HttpResponseMessage response = await client.SendAsync(
                       request,
                       HttpCompletionOption.ResponseHeadersRead,
                       cancellationToken))
            {
                /*
                 * La liberación es de mejor esfuerzo. Si la red se corta, el
                 * backend libera automáticamente el bloqueo cuando vence.
                 */
            }
        }

        private async Task<InspeccionRevisionBloqueo> EnviarAsync(
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

                RespuestaApi<InspeccionRevisionBloqueo>? envelope = null;
                if (!string.IsNullOrWhiteSpace(contenido))
                {
                    try
                    {
                        envelope = JsonSerializer.Deserialize<
                            RespuestaApi<InspeccionRevisionBloqueo>>(
                            contenido,
                            JsonOptions);
                    }
                    catch (JsonException)
                    {
                    }
                }

                if (!response.IsSuccessStatusCode)
                {
                    throw new InspeccionRevisionBloqueoException(
                        string.IsNullOrWhiteSpace(envelope?.Message)
                            ? "No fue posible reservar la inspección para esta sesión."
                            : envelope.Message,
                        envelope?.Data);
                }

                if (envelope?.Data != null)
                    return envelope.Data;

                throw new InspeccionRevisionBloqueoException(
                    "El servidor devolvió un bloqueo de inspección incompleto.",
                    null);
            }
        }

        private static string NormalizarModo(string? modo)
        {
            string valor = (modo ?? string.Empty)
                .Trim()
                .ToLowerInvariant();

            return valor switch
            {
                "analizador" => "analizador",
                "aprobador" => "aprobador",
                _ => valor
            };
        }

        private sealed class RespuestaApi<T>
        {
            public bool Success { get; set; }
            public string Message { get; set; } = string.Empty;
            public T? Data { get; set; }
        }
    }

    public sealed class InspeccionRevisionBloqueoException : Exception
    {
        public InspeccionRevisionBloqueoException(
            string mensaje,
            InspeccionRevisionBloqueo? bloqueo)
            : base(mensaje)
        {
            Bloqueo = bloqueo;
        }

        public InspeccionRevisionBloqueo? Bloqueo { get; }
    }
}
