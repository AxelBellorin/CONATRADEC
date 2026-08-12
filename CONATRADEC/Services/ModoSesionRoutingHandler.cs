namespace CONATRADEC.Services
{
    /// <summary>
    /// Indica a los manejadores históricos de catálogos que una sesión online
    /// debe consultar siempre el backend. En modo offline no agrega el bypass,
    /// por lo que dichos manejadores entregan SQLite.
    /// </summary>
    public sealed class ModoSesionRoutingHandler : DelegatingHandler
    {
        public const string HeaderBypass = "X-Offline-Bypass";

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (ModoSesionService.EsEnLinea)
            {
                request.Headers.Remove(HeaderBypass);

                /*
                 * CatalogosLocalHttpHandler reconoce explícitamente el valor
                 * "true" para permitir que la solicitud continúe al backend.
                 * De esta forma una sesión ONLINE no queda atendida por una
                 * copia antigua del paquete SQLite.
                 */
                request.Headers.TryAddWithoutValidation(
                    HeaderBypass,
                    "true");
            }
            else
            {
                request.Headers.Remove(HeaderBypass);
            }

            return base.SendAsync(request, cancellationToken);
        }
    }
}
