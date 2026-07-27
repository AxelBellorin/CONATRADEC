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

            string json = JsonSerializer.Serialize(new
            {
                success = false,
                message =
                    "La sesión está trabajando sin conexión y esta información no existe en la copia local. Inicie una sesión en línea y utilice Descargar todo."
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
                "X-CONATRADEC-Origen",
                "LOCAL-SIN-DATOS");

            return Task.FromResult(response);
        }
    }
}
