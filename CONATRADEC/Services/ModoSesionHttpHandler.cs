using System.Net;
using System.Text;
using System.Text.Json;

namespace CONATRADEC.Services
{
    /// <summary>
    /// Última barrera antes de HttpClientHandler.
    ///
    /// Durante una sesión offline ninguna solicitud puede alcanzar la red,
    /// incluso si un servicio antiguo intenta llamar a base.SendAsync.
    /// La respuesta se genera inmediatamente, sin DNS, socket, timeout ni
    /// comprobación de conectividad.
    /// </summary>
    public sealed class ModoSesionHttpHandler : DelegatingHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (ModoSesionService.EsEnLinea)
            {
                return base.SendAsync(
                    request,
                    cancellationToken);
            }

            bool esOperacionEscritura =
                request.Method != HttpMethod.Get &&
                request.Method != HttpMethod.Head &&
                request.Method != HttpMethod.Options;

            string mensaje =
                esOperacionEscritura
                    ? OfflineWriteAccessService.Mensaje
                    : OfflineReadResponseService
                        .MensajeSinDatosLocales;

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

            return Task.FromResult(response);
        }
    }
}
