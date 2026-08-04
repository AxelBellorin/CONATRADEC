using CONATRADEC.Models;
using System.Net.Http.Json;
using System.Text.Json;

namespace CONATRADEC.Services
{
    public sealed class DiagnosticoIAConfiguracionApiService
    {
        private static readonly JsonSerializerOptions JsonOptions =
            new(JsonSerializerDefaults.Web)
            {
                PropertyNameCaseInsensitive = true
            };

        private readonly HttpClient client;

        public DiagnosticoIAConfiguracionApiService()
            : this(ApiClientService.Client)
        {
        }

        public DiagnosticoIAConfiguracionApiService(
            HttpClient client)
        {
            this.client = client ??
                throw new ArgumentNullException(nameof(client));
        }

        public async Task<DiagnosticoIAConfiguracion> ObtenerAsync(
            CancellationToken cancellationToken = default)
        {
            SesionInactividadService.Instance.RegistrarActividad();

            using HttpResponseMessage response =
                await client.GetAsync(
                    "api/diagnostico-ia/configuracion",
                    cancellationToken);

            return await LeerRespuestaAsync(
                response,
                cancellationToken);
        }

        public async Task<DiagnosticoIAConfiguracion> ActualizarAsync(
            DiagnosticoIAConfiguracionActualizarRequest request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            SesionInactividadService.Instance.RegistrarActividad();

            using HttpResponseMessage response =
                await client.PutAsJsonAsync(
                    "api/diagnostico-ia/configuracion",
                    request,
                    JsonOptions,
                    cancellationToken);

            return await LeerRespuestaAsync(
                response,
                cancellationToken);
        }

        private static async Task<DiagnosticoIAConfiguracion>
            LeerRespuestaAsync(
                HttpResponseMessage response,
                CancellationToken cancellationToken)
        {
            string json = await response.Content.ReadAsStringAsync(
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                string mensaje = ApiErrorMessageParser.Parse(
                    response.StatusCode,
                    json,
                    "No fue posible administrar la configuración del diagnóstico IA.");

                throw new DiagnosticoIAApiException(
                    (int)response.StatusCode,
                    mensaje);
            }

            DiagnosticoIAApiEnvelope<DiagnosticoIAConfiguracion>? envelope;

            try
            {
                envelope = JsonSerializer.Deserialize<
                    DiagnosticoIAApiEnvelope<DiagnosticoIAConfiguracion>>(
                        json,
                        JsonOptions);
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException(
                    "La API respondió con una configuración no válida.",
                    ex);
            }

            if (envelope?.Success != true || envelope.Data == null)
            {
                throw new InvalidOperationException(
                    envelope?.Message ??
                    "La API no devolvió la configuración esperada.");
            }

            return envelope.Data;
        }
    }
}
