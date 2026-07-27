using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices;
using Microsoft.Maui.Storage;
using System.Net.Http.Headers;

namespace CONATRADEC.Services
{
    public static class ApiClientService
    {
        private static readonly Lazy<HttpClient> lazyClient =
            new(CrearCliente);

        public static HttpClient Client => lazyClient.Value;

        private static HttpClient CrearCliente()
        {
            var urlApiService = new UrlApiService();

            if (!Uri.TryCreate(
                    urlApiService.BaseUrlApi,
                    UriKind.Absolute,
                    out Uri? baseAddress))
            {
                throw new InvalidOperationException(
                    $"La URL configurada para la API no es válida: " +
                    urlApiService.BaseUrlApi);
            }

            var handler = new ContextoBitacoraHandler
            {
                InnerHandler = new SesionOfflineHandler
                {
                    InnerHandler =
                        new AnalisisOfflineGuardarHttpHandler
                        {
                            InnerHandler =
                                new AnalisisCalculoLocalHttpHandler
                                {
                                    InnerHandler =
                                        new AnalisisComplementariosLocalHttpHandler
                                        {
                                            /*
                                             * Atiende las rutas históricas
                                             * utilizadas por Nuevo análisis
                                             * para cultivos y unidades.
                                             */
                                            InnerHandler =
                                                new AnalisisCatalogosLocalHttpHandler
                                                {
                                                    InnerHandler =
                                                        new CatalogosLocalHttpHandler
                                                        {
                                                            InnerHandler =
                                                                new ContenidoSincronizacionHandler
                                                                {
                                                                    InnerHandler =
                                                                        new HttpClientHandler()
                                                                }
                                                        }
                                                }
                                        }
                                }
                        }
                }
            };

            var client = new HttpClient(handler)
            {
                BaseAddress = baseAddress,
                Timeout = TimeSpan.FromMinutes(2)
            };

            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue(
                    "application/json"));

            /*
             * Iniciar no realiza solicitudes en este punto. Únicamente deja
             * registrada la cola para detectar una futura reconexión.
             */
            AnalisisOfflineSincronizacionService.Instance
                .Iniciar();

            return client;
        }

        private sealed class ContextoBitacoraHandler : DelegatingHandler
        {
            protected override async Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken)
            {
                /*
                 * En este punto el HttpClient ya terminó de construirse. Es
                 * seguro iniciar la cola sin provocar acceso recursivo al Lazy.
                 */
                AnalisisOfflineSincronizacionService.Instance
                    .Iniciar();

                /*
                 * Iniciar es idempotente. La solicitud adicional permite que
                 * una cola creada en una sesión anterior se procese apenas el
                 * usuario autenticado vuelva a realizar una operación online.
                 */
                AnalisisOfflineSincronizacionService.Instance
                    .SolicitarSincronizacion();

                AgregarEncabezado(
                    request,
                    "X-Usuario-Id",
                    Preferences.Get(
                        SessionKeys.KeyUserId,
                        string.Empty));

                AgregarEncabezado(
                    request,
                    "X-Usuario-Nombre",
                    Preferences.Get(
                        SessionKeys.KeyNombreCompletoUsuario,
                        string.Empty));

                AgregarEncabezado(
                    request,
                    "X-Rol-Nombre",
                    Preferences.Get(
                        SessionKeys.KeyRolNombre,
                        string.Empty));

                AgregarEncabezado(
                    request,
                    "X-Pagina-Origen",
                    Shell.Current?
                        .CurrentState?
                        .Location?
                        .OriginalString ??
                    string.Empty);

                AgregarEncabezado(
                    request,
                    "X-Dispositivo",
                    ObtenerDispositivo());

                AgregarEncabezado(
                    request,
                    "X-Plataforma",
                    DeviceInfo.Current.Platform.ToString());

                AgregarEncabezado(
                    request,
                    "X-Version-App",
                    AppInfo.Current.VersionString ??
                    string.Empty);

                HttpResponseMessage response =
                    await base.SendAsync(
                        request,
                        cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    string content;

                    try
                    {
                        content = await response.Content
                            .ReadAsStringAsync(cancellationToken);
                    }
                    catch
                    {
                        content = string.Empty;
                    }

                    string message = ApiErrorMessageParser.Parse(
                        response.StatusCode,
                        content,
                        ApiErrorMessageParser.GetDefaultMessage(
                            response.StatusCode,
                            "No fue posible completar la operación."));

                    ApiErrorContext.Set(
                        message,
                        (int)response.StatusCode);
                }

                return response;
            }

            private static void AgregarEncabezado(
                HttpRequestMessage request,
                string nombre,
                string? valor)
            {
                if (string.IsNullOrWhiteSpace(valor))
                    return;

                request.Headers.Remove(nombre);
                request.Headers.TryAddWithoutValidation(
                    nombre,
                    Uri.EscapeDataString(valor.Trim()));
            }

            private static string ObtenerDispositivo()
            {
                string fabricante =
                    DeviceInfo.Current.Manufacturer ??
                    string.Empty;

                string modelo =
                    DeviceInfo.Current.Model ??
                    string.Empty;

                string nombre =
                    DeviceInfo.Current.Name ??
                    string.Empty;

                return string.Join(
                    " ",
                    new[]
                    {
                        fabricante,
                        modelo,
                        nombre
                    }
                    .Where(value =>
                        !string.IsNullOrWhiteSpace(value))
                    .Distinct(
                        StringComparer.OrdinalIgnoreCase));
            }
        }
    }
}
