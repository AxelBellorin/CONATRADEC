using CONATRADEC.Models;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Storage;
using System.Net;

namespace CONATRADEC.Services
{
    /// <summary>
    /// Comprueba periódicamente que el rol y los permisos de la sesión sigan
    /// vigentes. Los errores de conexión no cierran la sesión: solamente una
    /// respuesta explícita de invalidación emitida por la API.
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

        public static SessionValidationService Instance => instancia.Value;

        public void Iniciar()
        {
            Detener();

            if (!TryGetUsuarioId(out int usuarioId))
                return;

            int version = Preferences.Get(
                SessionKeys.KeySessionVersion,
                0);

            /*
             * Una sesión creada antes de implementar el control de versión
             * puede conservar el UsuarioId, pero no tener VersionSesion.
             *
             * Ese caso no significa que el administrador haya cambiado el rol
             * o los permisos en este momento. Se limpia la sesión antigua y se
             * vuelve al login sin mostrar la advertencia de permisos cambiados.
             *
             * La advertencia solamente se mostrará cuando la API responda de
             * forma explícita que una sesión activa fue invalidada.
             */
            if (version <= 0)
            {
                _ = LimpiarSesionIncompletaAsync(usuarioId);
                return;
            }

            cancellationTokenSource = new CancellationTokenSource();
            _ = EjecutarAsync(cancellationTokenSource.Token);
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
            _ = InvalidarSesionAsync();
        }

        private async Task EjecutarAsync(
            CancellationToken cancellationToken)
        {
            // Máximo aproximado de treinta segundos estando la app abierta.
            using var timer = new PeriodicTimer(
                TimeSpan.FromSeconds(30));

            try
            {
                while (await timer.WaitForNextTickAsync(cancellationToken))
                {
                    if (!TryGetUsuarioId(out _))
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
                            await InvalidarSesionAsync();
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
        /// No debe mostrar el mensaje de cambio de rol/permisos porque todavía
        /// no existe una respuesta de invalidación proveniente de la API.
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
            LimpiarDatosLocales();

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

        private async Task InvalidarSesionAsync()
        {
            if (Interlocked.Exchange(ref invalidando, 1) == 1)
                return;

            try
            {
                /*
                 * Si ya no existe un usuario activo, no hay una sesión vigente
                 * que invalidar ni debe mostrarse una advertencia atrasada.
                 */
                if (!TryGetUsuarioId(out _))
                    return;

                Detener();
                LimpiarDatosLocales();

                PermissionService.Instance.Load(
                    new List<UserPermissionDTO>());

                await MainThread.InvokeOnMainThreadAsync(
                    async () =>
                    {
                        await GlobalService.MostrarToastAsync(
                            "Su rol o sus permisos cambiaron. Inicie sesión nuevamente.");

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
                Interlocked.Exchange(ref invalidando, 0);
            }
        }

        private static bool TryGetUsuarioId(out int usuarioId)
        {
            string texto = Preferences.Get(
                SessionKeys.KeyUserId,
                string.Empty);

            return int.TryParse(texto, out usuarioId) &&
                   usuarioId > 0;
        }

        private static void LimpiarDatosLocales()
        {
            Preferences.Remove(SessionKeys.KeyUserId);
            Preferences.Remove(SessionKeys.KeyNombreCompletoUsuario);
            Preferences.Remove(SessionKeys.KeyCorreoUsuario);
            Preferences.Remove(SessionKeys.KeyUrlImagenUsuario);
            Preferences.Remove(SessionKeys.KeyRolId);
            Preferences.Remove(SessionKeys.KeyRolNombre);
            Preferences.Remove(SessionKeys.KeySessionVersion);

            // Se eliminan las credenciales recordadas para exigir un login real.
            Preferences.Remove("login.remember");
            Preferences.Remove("login.username");
            Preferences.Remove("login.use_biometrics");
            Preferences.Remove("login.require_pwd_relogin");
            SecureStorage.Remove("login.password");
        }
    }
}
