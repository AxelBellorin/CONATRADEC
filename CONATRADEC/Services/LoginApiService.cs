using CONATRADEC.Models;
using Microsoft.Maui.Storage;
using System.Net;
using System.Net.Http.Json;

namespace CONATRADEC.Services
{
    /// <summary>
    /// Maneja la autenticación de usuarios contra la API.
    /// </summary>
    public sealed class LoginApiService
    {
        private readonly HttpClient httpClient;

        public LoginApiService()
            : this(ApiClientService.Client)
        {
        }

        public LoginApiService(HttpClient httpClient)
        {
            this.httpClient = httpClient
                ?? throw new ArgumentNullException(nameof(httpClient));
        }

        public async Task<LoginResponse> LoginAsync(
            LoginRequest request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            using HttpResponseMessage response =
                await httpClient.PostAsJsonAsync(
                    "api/auth/login",
                    request,
                    cancellationToken);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                throw new UnauthorizedAccessException(
                    "El usuario o la contraseña son incorrectos.");
            }

            if (!response.IsSuccessStatusCode)
            {
                string contenidoError =
                    await response.Content.ReadAsStringAsync(
                        cancellationToken);

                throw new HttpRequestException(
                    $"La API respondió con el código " +
                    $"{(int)response.StatusCode} ({response.StatusCode}). " +
                    contenidoError,
                    null,
                    response.StatusCode);
            }

            LoginResponse? loginResponse =
                await response.Content.ReadFromJsonAsync<LoginResponse>(
                    cancellationToken: cancellationToken);

            if (loginResponse == null)
            {
                throw new InvalidOperationException(
                    "La API respondió correctamente, pero no devolvió " +
                    "los datos del usuario.");
            }

            if (ModoSesionService.EsEnLinea)
            {
                if (string.IsNullOrWhiteSpace(
                        loginResponse.AccessToken))
                {
                    throw new InvalidOperationException(
                        "La API no devolvió el token de seguridad. " +
                        "Publique primero el backend actualizado.");
                }

                await SessionTokenService.Instance
                    .GuardarAsync(
                        loginResponse.AccessToken);
            }
            else
            {
                /*
                 * El JSON local puede conservar un token antiguo del último
                 * login en línea. Una sesión offline nunca debe reutilizarlo.
                 */
                SessionTokenService.Instance
                    .Limpiar();
            }

            if (loginResponse.UsuarioId is > 0)
            {
                Preferences.Set(
                    SessionKeys.KeyUserId,
                    loginResponse.UsuarioId.Value.ToString());

                Preferences.Set(
                    SessionKeys.KeySessionVersion,
                    Math.Max(
                        1,
                        loginResponse.VersionSesion));

                /*
                 * Cada inicio de sesión debe invalidar los catálogos estáticos.
                 * De lo contrario, una lista vacía o antigua de otro modo de
                 * sesión puede permanecer durante veinte minutos.
                 */
                AnalisisSueloApiService
                    .LimpiarCacheTiposCultivo();

                UnidadMedidaApiService
                    .InvalidarCache();

                ElementoQuimicoApiService
                    .InvalidarCache();

                /*
                 * Si la aplicación se cerró durante una descarga anterior,
                 * se elimina el falso estado de descarga activa del usuario.
                 */
                SincronizacionOfflineEstadoRecuperacionService
                    .RecuperarSiInterrumpida();

                /*
                 * Comprueba que el motor físico siga siendo compatible con
                 * esta versión de la aplicación. Si el archivo pertenece a
                 * un esquema anterior, invalida únicamente la preparación
                 * global para evitar que la pantalla indique "Listo".
                 */
                await MotorCalculoCompatibilidadPreparacionService
                    .ValidarAsync(cancellationToken);

                int minutosInactividad =
                    Math.Clamp(
                        loginResponse.MinutosInactividad,
                        1,
                        1440);

                Preferences.Set(
                    SessionKeys.KeyInactivityMinutes,
                    minutosInactividad);

                SesionInactividadService.Instance
                    .IniciarNuevaSesion(
                        minutosInactividad);

                if (ModoSesionService.EsEnLinea)
                {
                    SessionValidationService.Instance
                        .Iniciar();
                }
                else
                {
                    SessionValidationService.Instance
                        .Detener();
                }
            }

            return loginResponse;
        }
    }
}
