using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices;
using Microsoft.Maui.Storage;
using System.Net.Http.Headers;

namespace CONATRADEC.Services
{
    /// <summary>
    /// Proporciona una única instancia de HttpClient para los servicios.
    /// Cada solicitud incluye automáticamente el contexto de la sesión y
    /// del dispositivo para que la API pueda generar la bitácora.
    /// </summary>
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
                InnerHandler = new HttpClientHandler()
            };

            var client = new HttpClient(handler)
            {
                BaseAddress = baseAddress,
                Timeout = TimeSpan.FromMinutes(2)
            };

            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            return client;
        }

        private sealed class ContextoBitacoraHandler : DelegatingHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken)
            {
                AgregarEncabezado(
                    request,
                    "X-Usuario-Id",
                    Preferences.Get(SessionKeys.KeyUserId, string.Empty));

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
                    Shell.Current?.CurrentState?.Location?.OriginalString ??
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
                    AppInfo.Current.VersionString ?? string.Empty);

                return base.SendAsync(request, cancellationToken);
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
                string fabricante = DeviceInfo.Current.Manufacturer ??
                    string.Empty;
                string modelo = DeviceInfo.Current.Model ?? string.Empty;
                string nombre = DeviceInfo.Current.Name ?? string.Empty;

                return string.Join(
                    " ",
                    new[] { fabricante, modelo, nombre }
                        .Where(x => !string.IsNullOrWhiteSpace(x))
                        .Distinct(StringComparer.OrdinalIgnoreCase));
            }
        }
    }
}
