using System.Net;
using System.Text;
using System.Text.Json;

namespace CONATRADEC.Services
{
    public sealed class SesionOfflineHandler : DelegatingHandler
    {
        private static readonly TimeSpan TiempoMaximoLogin =
            TimeSpan.FromSeconds(8);

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (!EsSolicitudLogin(request))
            {
                return await base.SendAsync(request, cancellationToken);
            }

            CredencialesLogin credenciales =
                await LeerCredencialesAsync(request, cancellationToken);

            using var timeout =
                CancellationTokenSource.CreateLinkedTokenSource(
                    cancellationToken);

            timeout.CancelAfter(TiempoMaximoLogin);

            try
            {
                HttpResponseMessage response =
                    await base.SendAsync(request, timeout.Token);

                EstadoConexionService.Instance
                    .ReportarServidorDisponible();

                if (!response.IsSuccessStatusCode)
                    return response;

                string json = await response.Content.ReadAsStringAsync(
                    cancellationToken);

                ReemplazarContenido(response, json);

                await SesionOfflineService.Instance.GuardarSesionOnlineAsync(
                    credenciales.Usuario,
                    credenciales.Clave,
                    json);

                return response;
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (OperationCanceledException)
            {
                return await CrearRespuestaOfflineAsync(
                    request,
                    credenciales);
            }
            catch (HttpRequestException)
            {
                return await CrearRespuestaOfflineAsync(
                    request,
                    credenciales);
            }
            catch (IOException)
            {
                return await CrearRespuestaOfflineAsync(
                    request,
                    credenciales);
            }
        }

        private static bool EsSolicitudLogin(HttpRequestMessage request)
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

        private static async Task<CredencialesLogin> LeerCredencialesAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (request.Content == null)
                return new CredencialesLogin();

            byte[] bytes =
                await request.Content.ReadAsByteArrayAsync(
                    cancellationToken);

            string json = Encoding.UTF8.GetString(bytes);

            var restored = new ByteArrayContent(bytes);

            foreach (var header in request.Content.Headers)
            {
                restored.Headers.TryAddWithoutValidation(
                    header.Key,
                    header.Value);
            }

            request.Content = restored;

            try
            {
                using JsonDocument document = JsonDocument.Parse(json);
                JsonElement root = document.RootElement;

                return new CredencialesLogin
                {
                    Usuario = ObtenerTexto(
                        root,
                        "UsuarioOEmail",
                        "usuarioOEmail",
                        "NombreUsuario"),
                    Clave = ObtenerTexto(
                        root,
                        "Clave",
                        "clave",
                        "ClaveUsuario")
                };
            }
            catch
            {
                return new CredencialesLogin();
            }
        }

        private static async Task<HttpResponseMessage>
            CrearRespuestaOfflineAsync(
                HttpRequestMessage request,
                CredencialesLogin credenciales)
        {
            EstadoConexionService.Instance.ReportarServidorNoDisponible();

            SesionOfflineResultado resultado =
                await SesionOfflineService.Instance.ValidarAsync(
                    credenciales.Usuario,
                    credenciales.Clave);

            if (resultado.Success)
            {
                var response = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    RequestMessage = request,
                    Content = new StringContent(
                        resultado.JsonLogin,
                        Encoding.UTF8,
                        "application/json")
                };

                response.Headers.TryAddWithoutValidation(
                    "X-Sesion-Origen",
                    "datos-sincronizados");

                return response;
            }

            HttpStatusCode status = resultado.CredencialesIncorrectas
                ? HttpStatusCode.Unauthorized
                : HttpStatusCode.ServiceUnavailable;

            return new HttpResponseMessage(status)
            {
                RequestMessage = request,
                Content = new StringContent(
                    JsonSerializer.Serialize(new
                    {
                        success = false,
                        message = resultado.Message
                    }),
                    Encoding.UTF8,
                    "application/json")
            };
        }

        private static void ReemplazarContenido(
            HttpResponseMessage response,
            string json)
        {
            response.Content.Dispose();
            response.Content = new StringContent(
                json,
                Encoding.UTF8,
                "application/json");
        }

        private static string ObtenerTexto(
            JsonElement root,
            params string[] nombres)
        {
            foreach (JsonProperty property in root.EnumerateObject())
            {
                if (nombres.Any(nombre =>
                        string.Equals(
                            property.Name,
                            nombre,
                            StringComparison.OrdinalIgnoreCase)) &&
                    property.Value.ValueKind == JsonValueKind.String)
                {
                    return property.Value.GetString() ?? string.Empty;
                }
            }

            return string.Empty;
        }

        private sealed class CredencialesLogin
        {
            public string Usuario { get; set; } = string.Empty;
            public string Clave { get; set; } = string.Empty;
        }
    }
}
