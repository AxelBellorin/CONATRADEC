using CONATRADEC.Models;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace CONATRADEC.Services
{
    /// <summary>
    /// Comprueba periódicamente que el token, el usuario, el rol y los permisos
    /// continúen vigentes. Una falla de red nunca cierra la sesión.
    ///
    /// El cierre automático también se encarga de recuperar visualmente la
    /// ventana y reconstruir el Shell cuando una navegación al Login falla.
    /// </summary>
    public sealed class SessionValidationService
    {
        private static readonly Lazy<SessionValidationService> instancia =
            new(() => new SessionValidationService());

        private CancellationTokenSource? cancellationTokenSource;
        private int invalidando;

        private SessionValidationService()
        {
        }

        public static SessionValidationService Instance =>
            instancia.Value;

        public void Iniciar()
        {
            Detener();

            if (!ModoSesionService.EsEnLinea)
                return;

            if (!TryGetUsuarioId(out int usuarioId))
                return;

            int version =
                Preferences.Get(
                    SessionKeys.KeySessionVersion,
                    0);

            /*
             * Una sesión creada antes de implementar el control de versión
             * puede conservar el UsuarioId, pero no tener VersionSesion.
             */
            if (version <= 0)
            {
                _ = LimpiarSesionIncompletaAsync(
                    usuarioId);

                return;
            }

            cancellationTokenSource =
                new CancellationTokenSource();

            _ = EjecutarAsync(
                cancellationTokenSource.Token);
        }

        public void Detener()
        {
            CancellationTokenSource? anterior =
                Interlocked.Exchange(
                    ref cancellationTokenSource,
                    null);

            if (anterior == null)
                return;

            try
            {
                anterior.Cancel();
            }
            catch (ObjectDisposedException)
            {
            }
            finally
            {
                anterior.Dispose();
            }
        }

        public void NotificarSesionInvalidada()
        {
            _ = FinalizarSesionAsync(
                RazonCierreSesion.CambioPermisos);
        }

        public void NotificarSesionInactiva()
        {
            _ = FinalizarSesionAsync(
                RazonCierreSesion.Inactividad);
        }

        public void NotificarSesionRechazada(
            string? contenido)
        {
            string value =
                contenido ??
                string.Empty;

            RazonCierreSesion razon =
                value.Contains(
                    "SESSION_INACTIVITY_TIMEOUT",
                    StringComparison.OrdinalIgnoreCase)
                    ? RazonCierreSesion.Inactividad
                    : value.Contains(
                            "SESSION_INVALIDATED",
                            StringComparison.OrdinalIgnoreCase)
                        ? RazonCierreSesion.CambioPermisos
                        : RazonCierreSesion.SeguridadVencida;

            _ = FinalizarSesionAsync(razon);
        }

        private async Task EjecutarAsync(
            CancellationToken cancellationToken)
        {
            // Máximo aproximado de treinta segundos estando la app abierta.
            using var timer =
                new PeriodicTimer(
                    TimeSpan.FromSeconds(30));

            try
            {
                while (await timer.WaitForNextTickAsync(
                           cancellationToken))
                {
                    if (!TryGetUsuarioId(out _))
                    {
                        Detener();
                        return;
                    }

                    if (!ModoSesionService.EsEnLinea)
                    {
                        Detener();
                        return;
                    }

                    try
                    {
                        using HttpResponseMessage response =
                            await ApiClientService.Client.GetAsync(
                                "api/sesion/validar",
                                cancellationToken);

                        if (response.StatusCode ==
                            HttpStatusCode.Unauthorized)
                        {
                            /*
                             * ApiClientService ya leyó el código de la API y
                             * notificó el motivo correcto del cierre.
                             */
                            return;
                        }
                    }
                    catch (OperationCanceledException)
                        when (cancellationToken.IsCancellationRequested)
                    {
                        return;
                    }
                    catch
                    {
                        // No cerrar sesión por una pérdida temporal de red.
                    }
                }
            }
            catch (OperationCanceledException)
            {
            }
        }

        /// <summary>
        /// Limpia silenciosamente una sesión local antigua o incompleta.
        /// </summary>
        private async Task LimpiarSesionIncompletaAsync(
            int usuarioIdEsperado)
        {
            if (!TryGetUsuarioId(out int usuarioIdActual) ||
                usuarioIdActual != usuarioIdEsperado)
            {
                return;
            }

            Detener();

            SesionInactividadService.Instance
                .Limpiar();

            SessionTokenService.Instance
                .Limpiar();

            LimpiarDatosLocales(
                limpiarCredencialesRecordadas: true);

            PermissionService.Instance.Load(
                new List<UserPermissionDTO>());

            await NavegarAlLoginConRecuperacionAsync(
                mensaje: null);
        }

        private async Task FinalizarSesionAsync(
            RazonCierreSesion razon)
        {
            if (Interlocked.Exchange(
                    ref invalidando,
                    1) == 1)
            {
                return;
            }

            try
            {
                /*
                 * Evita mostrar una advertencia atrasada después de que el
                 * usuario ya cerró sesión manualmente.
                 */
                if (!TryGetUsuarioId(out _))
                    return;

                Detener();

                SesionInactividadService.Instance
                    .Limpiar();

                SessionTokenService.Instance
                    .Limpiar();

                /*
                 * Los cambios de rol o permisos exigen credenciales reales.
                 * La inactividad y la expiración conservan "Recordarme" y
                 * permiten volver a autenticar con contraseña o biometría.
                 */
                LimpiarDatosLocales(
                    limpiarCredencialesRecordadas:
                        razon ==
                        RazonCierreSesion.CambioPermisos);

                PermissionService.Instance.Load(
                    new List<UserPermissionDTO>());

                string mensaje =
                    razon switch
                    {
                        RazonCierreSesion.Inactividad =>
                            "La sesión se cerró por inactividad. Inicie sesión nuevamente.",

                        RazonCierreSesion.SeguridadVencida =>
                            "La sesión de seguridad venció. Inicie sesión nuevamente.",

                        _ =>
                            "Su rol o sus permisos cambiaron. Inicie sesión nuevamente."
                    };

                /*
                 * Primero se navega al Login y después se muestra el mensaje.
                 * Snackbar.Show espera varios segundos; mostrarlo antes podía
                 * dejar la ventana todavía en la pantalla anterior mientras la
                 * sesión ya había sido eliminada.
                 */
                await NavegarAlLoginConRecuperacionAsync(
                    mensaje);
            }
            catch
            {
                /*
                 * La sesión ya quedó invalidada. Se realiza un último intento
                 * visual para no dejar el proceso vivo con una ventana perdida.
                 */
                await IntentarReconstruirShellAsync();
            }
            finally
            {
                Interlocked.Exchange(
                    ref invalidando,
                    0);
            }
        }

        private static async Task
            NavegarAlLoginConRecuperacionAsync(
                string? mensaje)
        {
            await MainThread.InvokeOnMainThreadAsync(
                async () =>
                {
                    bool navego = false;

                    Shell? shellActual =
                        Shell.Current;

                    if (shellActual != null)
                    {
                        navego =
                            await IntentarNavegarAsync(
                                shellActual);
                    }

                    if (!navego)
                    {
                        await ReconstruirShellEnVentanaAsync();
                    }

                    ActivarVentanaPrincipal();

                    if (!string.IsNullOrWhiteSpace(mensaje))
                    {
                        /*
                         * No se espera la duración completa del Snackbar.
                         * El cierre ya terminó y la pantalla de Login está lista.
                         */
                        _ = GlobalService.MostrarToastAsync(
                            mensaje);
                    }
                });
        }

        private static async Task<bool>
            IntentarNavegarAsync(
                Shell shell)
        {
            for (int intento = 0;
                 intento < 2;
                 intento++)
            {
                try
                {
                    await shell.GoToAsync(
                        AppRoutes.Login,
                        false);

                    return true;
                }
                catch when (intento == 0)
                {
                    /*
                     * Shell puede estar terminando una navegación anterior.
                     * Se concede un instante y se intenta una vez más.
                     */
                    await Task.Delay(150);
                }
                catch
                {
                    return false;
                }
            }

            return false;
        }

        private static async Task
            ReconstruirShellEnVentanaAsync()
        {
            var nuevoShell =
                new global::CONATRADEC.AppShell();

            Application? aplicacion =
                Application.Current;

            Window? ventana =
                aplicacion?
                    .Windows
                    .FirstOrDefault();

            if (ventana != null)
            {
                ventana.Page = nuevoShell;
            }
            else if (aplicacion != null)
            {
#pragma warning disable CS0618
                aplicacion.MainPage = nuevoShell;
#pragma warning restore CS0618
            }

            /*
             * LoginPage es el primer ShellContent, pero se navega de forma
             * explícita para limpiar cualquier ruta residual.
             */
            await nuevoShell.GoToAsync(
                AppRoutes.Login,
                false);
        }

        private static async Task
            IntentarReconstruirShellAsync()
        {
            try
            {
                await MainThread.InvokeOnMainThreadAsync(
                    async () =>
                    {
                        await ReconstruirShellEnVentanaAsync();
                        ActivarVentanaPrincipal();
                    });
            }
            catch
            {
                /*
                 * Nunca se propaga una segunda excepción desde el proceso de
                 * recuperación del cierre de sesión.
                 */
            }
        }

        private static void ActivarVentanaPrincipal()
        {
#if WINDOWS
            try
            {
                Window? ventanaMaui =
                    Application.Current?
                        .Windows
                        .FirstOrDefault();

                if (ventanaMaui?
                        .Handler?
                        .PlatformView
                    is Microsoft.UI.Xaml.Window ventanaNativa)
                {
                    ventanaNativa.Activate();
                }
            }
            catch
            {
                // La activación visual no debe impedir el cierre de sesión.
            }
#endif
        }

        private static bool TryGetUsuarioId(
            out int usuarioId)
        {
            string texto =
                Preferences.Get(
                    SessionKeys.KeyUserId,
                    string.Empty);

            return int.TryParse(
                       texto,
                       out usuarioId) &&
                   usuarioId > 0;
        }

        private static void LimpiarDatosLocales(
            bool limpiarCredencialesRecordadas)
        {
            Preferences.Remove(SessionKeys.KeyUserId);
            Preferences.Remove(SessionKeys.KeyNombreCompletoUsuario);
            Preferences.Remove(SessionKeys.KeyCorreoUsuario);
            Preferences.Remove(SessionKeys.KeyUrlImagenUsuario);
            Preferences.Remove(SessionKeys.KeyRolId);
            Preferences.Remove(SessionKeys.KeyRolNombre);
            Preferences.Remove(SessionKeys.KeySessionVersion);
            Preferences.Remove(SessionKeys.KeyInactivityMinutes);
            Preferences.Remove(SessionKeys.KeyLastActivityUtcTicks);
            Preferences.Remove(SessionKeys.KeyAccessToken);

            if (!limpiarCredencialesRecordadas)
                return;

            Preferences.Remove("login.remember");
            Preferences.Remove("login.username");
            Preferences.Remove("login.use_biometrics");
            Preferences.Remove("login.require_pwd_relogin");
            SecureStorage.Remove("login.password");
        }

        private enum RazonCierreSesion
        {
            CambioPermisos,
            Inactividad,
            SeguridadVencida
        }
    }
}
