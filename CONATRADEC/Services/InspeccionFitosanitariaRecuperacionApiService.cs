using CONATRADEC.Models;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace CONATRADEC.Services
{
    /// <summary>
    /// Cliente exclusivo para recuperar análisis IA interrumpidos. La operación
    /// no ejecuta nuevamente el proveedor: consolida un resultado ya guardado o
    /// convierte un intento abandonado en ERROR_IA para permitir un reintento.
    /// </summary>
    public sealed class InspeccionFitosanitariaRecuperacionApiService
    {
        private static readonly JsonSerializerOptions JsonOptions =
            new(JsonSerializerDefaults.Web)
            {
                PropertyNameCaseInsensitive = true
            };

        private readonly HttpClient client;

        public InspeccionFitosanitariaRecuperacionApiService()
            : this(ApiClientService.Client)
        {
        }

        public InspeccionFitosanitariaRecuperacionApiService(HttpClient client)
        {
            this.client = client ?? throw new ArgumentNullException(nameof(client));
        }

        public async Task<InspeccionOperacionMasivaV2> RecuperarAsync(
            int inspeccionId,
            IReadOnlyCollection<int> fotografiaIds,
            CancellationToken cancellationToken = default)
        {
            if (inspeccionId <= 0)
                throw new ArgumentOutOfRangeException(nameof(inspeccionId));

            int[] ids = (fotografiaIds ?? [])
                .Where(item => item > 0)
                .Distinct()
                .ToArray();

            if (ids.Length == 0)
            {
                throw new ArgumentException(
                    "Debe indicar al menos una fotografía para recuperar.",
                    nameof(fotografiaIds));
            }

            SesionInactividadService.Instance.RegistrarActividad();

            using HttpRequestMessage request = new(
                HttpMethod.Post,
                $"api/inspecciones-fitosanitarias/{inspeccionId}/recuperar-analisis-ia")
            {
                Content = JsonContent.Create(
                    new { fotografiaIds = ids },
                    options: JsonOptions)
            };

            using HttpResponseMessage response = await client.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            string contenido = await response.Content.ReadAsStringAsync(
                cancellationToken);

            ApiEnvelopeRecuperacion<InspeccionOperacionMasivaV2>? envelope = null;
            if (!string.IsNullOrWhiteSpace(contenido))
            {
                try
                {
                    envelope = JsonSerializer.Deserialize<
                        ApiEnvelopeRecuperacion<InspeccionOperacionMasivaV2>>(
                        contenido,
                        JsonOptions);
                }
                catch (JsonException)
                {
                }
            }

            if (!response.IsSuccessStatusCode)
            {
                throw new InspeccionFitosanitariaApiException(
                    response.StatusCode,
                    string.IsNullOrWhiteSpace(envelope?.Message)
                        ? ExtraerMensaje(contenido)
                        : envelope.Message);
            }

            if (envelope?.Data != null)
                return envelope.Data;

            throw new InspeccionFitosanitariaApiException(
                HttpStatusCode.BadGateway,
                "El servidor devolvió una respuesta incompleta al recuperar el análisis IA.");
        }

        private static string ExtraerMensaje(string contenido)
        {
            if (string.IsNullOrWhiteSpace(contenido))
                return "No fue posible recuperar el análisis IA.";

            try
            {
                using JsonDocument document = JsonDocument.Parse(contenido);
                if (document.RootElement.TryGetProperty(
                        "message",
                        out JsonElement message))
                {
                    string? valor = message.GetString();
                    if (!string.IsNullOrWhiteSpace(valor))
                        return valor;
                }
            }
            catch (JsonException)
            {
            }

            return contenido.Length <= 600
                ? contenido
                : contenido[..600];
        }

        private sealed class ApiEnvelopeRecuperacion<T>
        {
            public bool Success { get; set; }
            public string Message { get; set; } = string.Empty;
            public T? Data { get; set; }
        }
    }
}
