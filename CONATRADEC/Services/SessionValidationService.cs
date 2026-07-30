using CONATRADEC.Models;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Storage;
using System.Net;

namespace CONATRADEC.Services
{
    /// <summary>
    /// Comprueba periódicamente que el token, el usuario, el rol y los permisos
    /// continúen vigentes. Una falla de red nunca cierra la sesión.
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

            int version = Preferences.Get(
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

            _ = FinalizarSesionAsync(
                razon);
        }

        private async Task EjecutarAsync(
            CancellationToken cancellationToken)
        {
            // Máximo aproximado de treinta segundos estando la app abierta.
            using var timer = new PeriodicTimer(
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

            await MainThread.InvokeOnMainThreadAsync(
                async () =>
                {
                    if (Shell.Current != null)
                    {
                        await Shell.Current.GoToAsync(
                            AppRoutes.Login,
                            false);
                    }
                });
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
                 * Evita mostrar una advertencia atrasada después de que el usuario
                 * ya cerró sesión manualmente.
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
                 * La inactividad y la expiración conservan "Recordarme" y permiten
                 * volver a autenticar con contraseña guardada o biometría.
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

                await MainThread.InvokeOnMainThreadAsync(
                    async () =>
                    {
                        await GlobalService.MostrarToastAsync(
                            mensaje);

                        if (Shell.Current != null)
                        {
                            await Shell.Current.GoToAsync(
                                AppRoutes.Login,
                                false);
                        }
                    });
            }
            finally
            {
                Interlocked.Exchange(
                    ref invalidando,
                    0);
            }
        }

        private static bool TryGetUsuarioId(
            out int usuarioId)
        {
            string texto = Preferences.Get(
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
