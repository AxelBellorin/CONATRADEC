using CONATRADEC.Models;
using System.Net;
using System.Text;
using System.Text.Json;

namespace CONATRADEC.Services
{
    /// <summary>
    /// Cálculo inicial del análisis según el modo global de la sesión.
    /// No existe cambio automático ni fallback entre API y motor local.
    /// </summary>
    public sealed class AnalisisCalculoLocalHttpHandler :
        DelegatingHandler
    {
        private const string RutaCalcular =
            "/api/analisis-suelo/calcular";

        private static readonly JsonSerializerOptions JsonOptions =
            new(JsonSerializerDefaults.Web)
            {
                PropertyNameCaseInsensitive = true
            };

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (!EsCalculoAnalisis(request))
            {
                return await base.SendAsync(
                    request,
                    cancellationToken);
            }

            if (ModoSesionService.EsEnLinea)
            {
                return await base.SendAsync(
                    request,
                    cancellationToken);
            }

            if (!DatosSinConexionPermisos.TienePermiso)
            {
                return CrearError(
                    request,
                    HttpStatusCode.Forbidden,
                    "Su usuario no tiene habilitado el cálculo sin conexión.");
            }

            string cuerpo = request.Content == null
                ? string.Empty
                : await request.Content.ReadAsStringAsync(
                    cancellationToken);

            return await CrearRespuestaLocalAsync(
                request,
                cuerpo,
                cancellationToken);
        }

        private static bool EsCalculoAnalisis(
            HttpRequestMessage request)
        {
            if (request.Method != HttpMethod.Post)
                return false;

            Uri? uri = request.RequestUri;
            string path = uri == null
                ? string.Empty
                : uri.IsAbsoluteUri
                    ? uri.AbsolutePath
                    : "/" + uri.OriginalString
                        .Split('?')[0]
                        .TrimStart('/');

            return string.Equals(
                path,
                RutaCalcular,
                StringComparison.OrdinalIgnoreCase);
        }

        private static async Task<HttpResponseMessage>
            CrearRespuestaLocalAsync(
                HttpRequestMessage request,
                string cuerpo,
                CancellationToken cancellationToken)
        {
            AnalisisSueloCalcularRequest? solicitud;

            try
            {
                solicitud = JsonSerializer.Deserialize<
                    AnalisisSueloCalcularRequest>(
                    cuerpo,
                    JsonOptions);
            }
            catch (JsonException)
            {
                solicitud = null;
            }

            AnalisisSueloCalculoResponse resultado =
                solicitud == null
                    ? new AnalisisSueloCalculoResponse
                    {
                        Success = false,
                        Message =
                            "No fue posible interpretar los datos del análisis para calcularlos localmente."
                    }
                    : await MotorCalculoLocalService.Instance
                        .CalcularRequerimientoAnualAsync(
                            solicitud,
                            cancellationToken);

            string json = JsonSerializer.Serialize(
                resultado,
                JsonOptions);

            var response = new HttpResponseMessage(
                HttpStatusCode.OK)
            {
                RequestMessage = request,
                Content = new StringContent(
                    json,
                    Encoding.UTF8,
                    "application/json")
            };

            response.Headers.TryAddWithoutValidation(
                "X-CONATRADEC-Calculo-Origen",
                "LOCAL");

            return response;
        }

        private static HttpResponseMessage CrearError(
            HttpRequestMessage request,
            HttpStatusCode status,
            string message) =>
            new(status)
            {
                RequestMessage = request,
                Content = new StringContent(
                    JsonSerializer.Serialize(new
                    {
                        success = false,
                        message
                    }),
                    Encoding.UTF8,
                    "application/json")
            };
    }
}
