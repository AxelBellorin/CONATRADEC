using CONATRADEC.Models;
using System.Net;
using System.Text.Json;

namespace CONATRADEC.Services
{
    /// <summary>
    /// Cliente liviano para las bandejas del analizador y del aprobador. El
    /// filtro se envía por identificador del técnico, nunca por texto.
    /// </summary>
    public sealed class InspeccionFitosanitariaBandejaOperativaApiService
    {
        private static readonly JsonSerializerOptions JsonOptions =
            new(JsonSerializerDefaults.Web)
            {
                PropertyNameCaseInsensitive = true
            };

        private readonly HttpClient client = ApiClientService.Client;

        public async Task<List<InspeccionFitosanitariaListaItemV2>> ObtenerAsync(
            string modo,
            int? tecnicoId,
            CancellationToken cancellationToken = default)
        {
            string ruta =
                "api/inspecciones-fitosanitarias-flujo/bandeja?modo=" +
                Uri.EscapeDataString(
                    string.IsNullOrWhiteSpace(modo)
                        ? DiagnosticoIARoutes.ModoAnalizador
                        : modo.Trim().ToLowerInvariant());

            if (tecnicoId is > 0)
            {
                ruta += "&tecnicoId=" +
                        Uri.EscapeDataString(tecnicoId.Value.ToString());
            }

            SesionInactividadService.Instance.RegistrarActividad();

            using HttpRequestMessage request = new(HttpMethod.Get, ruta);
            using HttpResponseMessage response = await client.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            string contenido = await response.Content.ReadAsStringAsync(
                cancellationToken);

            RespuestaApi<List<InspeccionFitosanitariaListaItemV2>>? envelope =
                null;

            if (!string.IsNullOrWhiteSpace(contenido))
            {
                try
                {
                    envelope = JsonSerializer.Deserialize<
                        RespuestaApi<List<InspeccionFitosanitariaListaItemV2>>>(
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
                        ? "No fue posible cargar la bandeja de inspecciones."
                        : envelope.Message);
            }

            if (envelope?.Data != null)
                return envelope.Data;

            throw new InspeccionFitosanitariaApiException(
                HttpStatusCode.BadGateway,
                "El servidor devolvió una bandeja incompleta.");
        }

        private sealed class RespuestaApi<T>
        {
            public bool Success { get; set; }
            public string Message { get; set; } = string.Empty;
            public T? Data { get; set; }
        }
    }
}
