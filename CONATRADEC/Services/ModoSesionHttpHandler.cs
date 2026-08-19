using System.Net;
using System.Text;
using System.Text.Json;

namespace CONATRADEC.Services
{
    /// <summary>
    /// Última barrera antes de HttpClientHandler.
    ///
    /// Durante una sesión offline ninguna solicitud puede alcanzar la red,
    /// incluso si un servicio antiguo intenta llamar a base.SendAsync. La
    /// captura fitosanitaria constituye una excepción controlada y se resuelve
    /// íntegramente en el dispositivo.
    /// </summary>
    public sealed class ModoSesionHttpHandler : DelegatingHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (ModoSesionService.EsEnLinea)
            {
                HttpResponseMessage respuesta = await base.SendAsync(
                    request,
                    cancellationToken);
                RegistrarMutacionFitosanitaria(request, respuesta);
                return respuesta;
            }

            HttpResponseMessage? respuestaFitosanitaria =
                await FitosanitariaOfflineService.Instance
                    .IntentarProcesarSolicitudAsync(
                        request,
                        cancellationToken);

            if (respuestaFitosanitaria != null)
            {
                RegistrarMutacionFitosanitaria(
                    request,
                    respuestaFitosanitaria);
                return respuestaFitosanitaria;
            }

            bool esOperacionEscritura =
                request.Method != HttpMethod.Get &&
                request.Method != HttpMethod.Head &&
                request.Method != HttpMethod.Options;

            string mensaje = esOperacionEscritura
                ? OfflineWriteAccessService.Mensaje
                : OfflineReadResponseService.MensajeSinDatosLocales;

            string json = JsonSerializer.Serialize(new
            {
                success = false,
                message = mensaje
            });

            var response = new HttpResponseMessage(
                HttpStatusCode.ServiceUnavailable)
            {
                RequestMessage = request,
                Content = new StringContent(
                    json,
                    Encoding.UTF8,
                    "application/json")
            };

            response.Headers.TryAddWithoutValidation(
                OfflineReadResponseService.HeaderOrigen,
                esOperacionEscritura
                    ? "LOCAL-ESCRITURA-BLOQUEADA"
                    : OfflineReadResponseService.OrigenSinDatos);

            return response;
        }

        /// <summary>
        /// Una escritura funcional dentro del expediente invalida las bandejas
        /// que pueden reflejar ese cambio. Los POST usados únicamente para
        /// adquirir, renovar o liberar el bloqueo temporal de edición no son
        /// cambios de negocio y por eso no fuerzan una recarga al regresar.
        /// </summary>
        private static void RegistrarMutacionFitosanitaria(
            HttpRequestMessage request,
            HttpResponseMessage response)
        {
            if (!response.IsSuccessStatusCode ||
                request.Method == HttpMethod.Get ||
                request.Method == HttpMethod.Head ||
                request.Method == HttpMethod.Options)
            {
                return;
            }

            string path = request.RequestUri?.AbsolutePath ?? string.Empty;
            bool esFlujoFitosanitario =
                path.StartsWith(
                    "/api/inspecciones-fitosanitarias",
                    StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith(
                    "/api/revision-fitosanitaria",
                    StringComparison.OrdinalIgnoreCase);

            if (!esFlujoFitosanitario)
                return;

            bool esSoloGestionBloqueo =
                path.EndsWith(
                    "/bloqueo/adquirir",
                    StringComparison.OrdinalIgnoreCase) ||
                path.EndsWith(
                    "/bloqueo/renovar",
                    StringComparison.OrdinalIgnoreCase) ||
                path.EndsWith(
                    "/bloqueo/liberar",
                    StringComparison.OrdinalIgnoreCase);

            if (esSoloGestionBloqueo)
                return;

            DiagnosticoIASolicitudVisitaService.MarcarMutacion();
            DiagnosticoIAAnalizadorVisitaService.MarcarMutacion();
        }
    }
}
