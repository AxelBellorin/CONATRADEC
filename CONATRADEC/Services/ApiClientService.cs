using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices;
using Microsoft.Maui.Storage;
using System.Net;
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
                    "La URL configurada para la API no es válida: " +
                    urlApiService.BaseUrlApi);
            }

            var handler = new ContextoBitacoraHandler
            {
                InnerHandler = new SesionOfflineHandler
                {
                    InnerHandler = new ModoSesionRoutingHandler
                    {
                        InnerHandler = new RespuestaLocalGeneralHttpHandler
                        {
                            InnerHandler =
                                new AnalisisHistorialCacheHttpHandler
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
                                                                                        new ModoSesionHttpHandler
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

            return client;
        }

        private sealed class ContextoBitacoraHandler : DelegatingHandler
        {
            protected override async Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken)
            {
                bool esLogin =
                    EsSolicitudLogin(request);

                long actividadVersionEnviada = 0;

                if (!esLogin &&
                    ModoSesionService.EsEnLinea)
                {
                    string? token =
                        await SessionTokenService.Instance
                            .ObtenerAsync();

                    request.Headers.Authorization =
                        string.IsNullOrWhiteSpace(token)
                            ? null
                            : new AuthenticationHeaderValue(
                                "Bearer",
                                token);

                    actividadVersionEnviada =
                        SesionInactividadService.Instance
                            .ObtenerVersionActividadPendienteServidor();

                    if (actividadVersionEnviada > 0)
                    {
                        AgregarEncabezado(
                            request,
                            "X-Actividad-Usuario",
                            "true");
                    }
                }
                else
                {
                    request.Headers.Authorization = null;
                }

                AgregarEncabezado(
                    request,
                    "X-Usuario-Id",
                    Preferences.Get(
                        SessionKeys.KeyUserId,
                        string.Empty));

                int versionSesion = Preferences.Get(
                    SessionKeys.KeySessionVersion,
                    0);

                if (versionSesion > 0)
                {
                    AgregarEncabezado(
                        request,
                        "X-Version-Sesion",
                        versionSesion.ToString());
                }

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

                AgregarEncabezado(
                    request,
                    "X-Modo-Sesion",
                    ModoSesionService.Instance
                        .ModoActual
                        .ToString());

                AnalisisOfflineSincronizacionService.Instance
                    .SolicitarUnaVezPorSesionOnline();

                HttpResponseMessage response =
                    await base.SendAsync(
                        request,
                        cancellationToken);

                if (actividadVersionEnviada > 0)
                {
                    /*
                     * Solo se confirma al recibir una respuesta HTTP. Si hubo una
                     * falla de red, la actividad permanece pendiente y se enviará
                     * nuevamente en la próxima solicitud.
                     */
                    SesionInactividadService.Instance
                        .ConfirmarActividadEnviada(
                            actividadVersionEnviada);
                }

                bool invalidadaPorEncabezado =
                    response.Headers.TryGetValues(
                        "X-Sesion-Invalidada",
                        out IEnumerable<string>? valores) &&
                    valores.Any(value =>
                        value.Equals(
                            "true",
                            StringComparison.OrdinalIgnoreCase));

                string content = string.Empty;

                if (!response.IsSuccessStatusCode)
                {
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

                bool invalidadaPorRespuesta =
                    response.StatusCode ==
                        HttpStatusCode.Unauthorized &&
                    ContieneCodigoSesion(
                        content);

                if (invalidadaPorEncabezado ||
                    invalidadaPorRespuesta)
                {
                    SessionValidationService.Instance
                        .NotificarSesionRechazada(
                            content);
                }

                return response;
            }

            private static bool EsSolicitudLogin(
                HttpRequestMessage request)
            {
                if (request.Method != HttpMethod.Post)
                    return false;

                string path =
                    request.RequestUri?.AbsolutePath ??
                    request.RequestUri?.OriginalString ??
                    string.Empty;

                return path.EndsWith(
                    "/api/auth/login",
                    StringComparison.OrdinalIgnoreCase);
            }

            private static bool ContieneCodigoSesion(
                string content)
            {
                if (string.IsNullOrWhiteSpace(content))
                    return false;

                string[] codigos =
                [
                    "SESSION_INVALIDATED",
                    "SESSION_INACTIVITY_TIMEOUT",
                    "SESSION_TOKEN_EXPIRED",
                    "SESSION_NOT_ACTIVE",
                    "AUTH_TOKEN_REQUIRED",
                    "AUTH_TOKEN_INVALID"
                ];

                return codigos.Any(
                    codigo =>
                        content.Contains(
                            codigo,
                            StringComparison.OrdinalIgnoreCase));
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
