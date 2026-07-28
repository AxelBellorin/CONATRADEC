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

            if (!TryGetUsuarioId(out _))
                return;

            int version = Preferences.Get(
                SessionKeys.KeySessionVersion,
                0);

            /*
             * Una sesión creada antes de esta mejora no posee versión.
             * No se debe inventar una versión local porque conservaría la
             * matriz antigua. Se obliga a iniciar sesión nuevamente.
             */
            if (version <= 0)
            {
                _ = InvalidarSesionAsync();
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

        private async Task InvalidarSesionAsync()
        {
            if (Interlocked.Exchange(ref invalidando, 1) == 1)
                return;

            try
            {
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
