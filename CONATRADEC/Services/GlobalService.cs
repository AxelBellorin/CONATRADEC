using Microsoft.Maui.Networking;
using Microsoft.Maui.Storage;
using System.ComponentModel;
using System.Diagnostics;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;

namespace CONATRADEC.Services
{
    public class GlobalService : INotifyPropertyChanged
    {
        /*
         * Shell no admite dos navegaciones simultáneas. Este bloqueo es
         * estático porque cada ViewModel hereda una instancia diferente de
         * GlobalService, pero todos comparten el mismo Shell de la aplicación.
         */
        private static readonly SemaphoreSlim NavigationSemaphore =
            new(initialCount: 1, maxCount: 1);

        private static string? rutaNavegacionEnCurso;

        private const int MaximoReintentosNavegacion = 20;

        public bool CanAdd { get; protected set; }
        public bool CanEdit { get; protected set; }
        public bool CanDelete { get; protected set; }
        public bool CanView { get; protected set; }

        public Command goToMainPageCommand { get; }
        public Command goToConfiguracionPageCommand { get; }
        public Command goToAlbumFotosPageCommand { get; }

        public Command goToUserPageButtonCommand { get; }
        public Command goToRolPageButtonCommand { get; }
        public Command goToMatrizPermisosPageButtonCommad { get; }
        public Command goToPaisPageButtonCommand { get; }
        public Command goToElementoQuimicoPageButtonCommand { get; }
        public Command goToTerrenoPageButtonCommand { get; }
        public Command goToFuenteNutrientePageButtonCommand { get; }
        public Command goToTipoCultivoPageButtonCommand { get; }
        public Command goToTipoAnalisisSueloPageButtonCommand { get; }
        public Command goToExtraccionNutrientePageButtonCommand { get; }
        public Command goToRangoNutrientePageButtonCommand { get; }

        public Command goToBack { get; }
        public Command CerrarSesionCommand { get; }

        private bool isBusy;

        public event PropertyChangedEventHandler? PropertyChanged;

        public bool IsBusy
        {
            get => isBusy;
            set
            {
                if (isBusy == value)
                    return;

                isBusy = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(NotIsBusy));
                UpdateNavigationCommands();
            }
        }

        public bool NotIsBusy => !IsBusy;

        public GlobalService()
        {
            goToMainPageCommand = new Command(
                async () => await GoToMainPage(),
                () => !IsBusy);

            goToConfiguracionPageCommand = new Command(
                async () => await GoToConfiguracionPage(),
                () => !IsBusy);

            goToAlbumFotosPageCommand = new Command(
                async () => await GoToAlbumFotosPage(),
                () => !IsBusy);

            goToUserPageButtonCommand = new Command(
                async () => await GoToUserPage(),
                () => !IsBusy);

            goToRolPageButtonCommand = new Command(
                async () => await GoToRolPage(),
                () => !IsBusy);

            goToMatrizPermisosPageButtonCommad = new Command(
                async () => await GoToMatrizPermisosPage(),
                () => !IsBusy);

            goToPaisPageButtonCommand = new Command(
                async () => await GoToPaisPage(),
                () => !IsBusy);

            goToElementoQuimicoPageButtonCommand = new Command(
                async () => await GoToElementoQuimicoPage(),
                () => !IsBusy);

            goToTerrenoPageButtonCommand = new Command(
                async () => await GoToTerrenoPage(),
                () => !IsBusy);

            goToFuenteNutrientePageButtonCommand = new Command(
                async () => await GoToFuenteNutrientePage(),
                () => !IsBusy);

            goToTipoCultivoPageButtonCommand = new Command(
                async () => await GoToTipoCultivoPage(),
                () => !IsBusy);

            goToTipoAnalisisSueloPageButtonCommand = new Command(
                async () => await GoToTipoAnalisisSueloPage(),
                () => !IsBusy);

            goToExtraccionNutrientePageButtonCommand = new Command(
                async () => await GoToExtraccionNutrientePage(),
                () => !IsBusy);

            goToRangoNutrientePageButtonCommand = new Command(
                async () => await GoToRangoNutrientePage(),
                () => !IsBusy);

            goToBack = new Command(
                async () => await GoToAsyncParameters(AppRoutes.Regresar),
                () => !IsBusy);

            CerrarSesionCommand = new Command(
                async () => await CerrarSesionAsync(),
                () => !IsBusy);
        }

        private void UpdateNavigationCommands()
        {
            goToMainPageCommand.ChangeCanExecute();
            goToConfiguracionPageCommand.ChangeCanExecute();
            goToAlbumFotosPageCommand.ChangeCanExecute();

            goToUserPageButtonCommand.ChangeCanExecute();
            goToRolPageButtonCommand.ChangeCanExecute();
            goToMatrizPermisosPageButtonCommad.ChangeCanExecute();
            goToPaisPageButtonCommand.ChangeCanExecute();
            goToElementoQuimicoPageButtonCommand.ChangeCanExecute();
            goToTerrenoPageButtonCommand.ChangeCanExecute();
            goToFuenteNutrientePageButtonCommand.ChangeCanExecute();
            goToTipoCultivoPageButtonCommand.ChangeCanExecute();
            goToTipoAnalisisSueloPageButtonCommand.ChangeCanExecute();
            goToExtraccionNutrientePageButtonCommand.ChangeCanExecute();
            goToRangoNutrientePageButtonCommand.ChangeCanExecute();

            goToBack.ChangeCanExecute();
            CerrarSesionCommand.ChangeCanExecute();
        }

        private async Task CerrarSesionAsync()
        {
            if (IsBusy)
                return;

            bool confirm = await ConfirmarAsync(
                "Cerrar sesión",
                "¿Está seguro de que desea cerrar la sesión actual?",
                "Cerrar sesión",
                "Cancelar");

            if (!confirm)
                return;

            IsBusy = true;

            try
            {
                /*
                 * Se elimina primero el usuario visible para que el servicio de
                 * conectividad marque el dispositivo como desconectado mientras
                 * el JWT todavía continúa disponible.
                 */
                Preferences.Remove(SessionKeys.KeyUserId);

                try
                {
                    await DispositivoConexionService.Instance
                        .ActualizarEstadoActualAsync();
                }
                catch
                {
                    // La telemetría no debe impedir el cierre de la sesión.
                }

                try
                {
                    if (ModoSesionService.EsEnLinea)
                    {
                        using var request =
                            new HttpRequestMessage(
                                HttpMethod.Post,
                                "api/sesion/cerrar");

                        using HttpResponseMessage response =
                            await ApiClientService.Client.SendAsync(
                                request);
                    }
                }
                catch
                {
                    /*
                     * Aunque no haya red, se elimina la sesión del dispositivo.
                     * El backend la cerrará después por inactividad o expiración.
                     */
                }

                SessionValidationService.Instance.Detener();
                SesionInactividadService.Instance.Limpiar();
                SessionTokenService.Instance.Limpiar();

                Preferences.Remove(SessionKeys.KeyNombreCompletoUsuario);
                Preferences.Remove(SessionKeys.KeyCorreoUsuario);
                Preferences.Remove(SessionKeys.KeyUrlImagenUsuario);
                Preferences.Remove(SessionKeys.KeyRolId);
                Preferences.Remove(SessionKeys.KeyRolNombre);
                Preferences.Remove(SessionKeys.KeySessionVersion);
                Preferences.Remove(SessionKeys.KeyInactivityMinutes);
                Preferences.Remove(SessionKeys.KeyLastActivityUtcTicks);
                Preferences.Remove(SessionKeys.KeyAccessToken);

                PermissionService.Instance.ClearPermissions();

                await GoToAsyncParameters(AppRoutes.Login);
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task GoToAsyncParameters(
            string route,
            IDictionary<string, object>? parameters = null)
        {
            if (string.IsNullOrWhiteSpace(route))
                return;

            string rutaDestino = ResolverRutaDestino(route);

            /*
             * Un doble clic puede entrar aquí desde dos ViewModels diferentes.
             * Si el mismo destino ya está procesándose, la segunda llamada se
             * descarta antes de esperar el bloqueo global.
             */
            if (string.Equals(
                    rutaNavegacionEnCurso,
                    rutaDestino,
                    StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            await NavigationSemaphore.WaitAsync();

            try
            {
                Shell? shell = Shell.Current;

                if (shell == null)
                    return;

                /*
                 * Otra llamada pudo completar la navegación mientras esta
                 * esperaba el semáforo. No se apila nuevamente la misma página.
                 */
                if (parameters == null &&
                    EsRutaActual(shell, rutaDestino))
                {
                    return;
                }

                rutaNavegacionEnCurso = rutaDestino;

                // Evita navegar mientras el teclado todavía ocupa parte
                // de la pantalla en Android.
                await KeyboardService.HideAsync();

                await EjecutarNavegacionSeguraAsync(
                    shell,
                    rutaDestino,
                    parameters);

                /*
                 * Algunas versiones de Shell terminan GoToAsync unas pocas
                 * milésimas antes de liberar su navegación interna.
                 */
                await Task.Delay(60);
            }
            finally
            {
                rutaNavegacionEnCurso = null;
                NavigationSemaphore.Release();
            }
        }

        private static string ResolverRutaDestino(string route)
        {
            string rutaDestino = route;

            /*
             * LoginViewModel navega históricamente a //MainPage después de
             * autenticar. Si el usuario no tiene lectura en MainPage, se cambia
             * la ruta antes de enviarla a Shell y se abre la primera sección
             * principal que realmente tenga permitida.
             */
            if (string.Equals(
                    route,
                    AppRoutes.Principal,
                    StringComparison.OrdinalIgnoreCase) &&
                !PermissionService.Instance.HasRead(
                    InterfazCodigos.AnalisisSuelo))
            {
                rutaDestino =
                    NavigationPermissionService
                        .ObtenerRutaInicialPermitida();
            }

            return rutaDestino;
        }

        private static async Task EjecutarNavegacionSeguraAsync(
            Shell shell,
            string rutaDestino,
            IDictionary<string, object>? parameters)
        {
            for (int intento = 1;
                 intento <= MaximoReintentosNavegacion;
                 intento++)
            {
                try
                {
                    if (parameters == null &&
                        EsRutaActual(shell, rutaDestino))
                    {
                        return;
                    }

                    if (parameters == null)
                    {
                        await shell.GoToAsync(
                            rutaDestino,
                            animate: false);
                    }
                    else
                    {
                        await shell.GoToAsync(
                            rutaDestino,
                            animate: false,
                            parameters);
                    }

                    return;
                }
                catch (InvalidOperationException ex)
                    when (EsNavegacionPendiente(ex))
                {
                    if (intento == MaximoReintentosNavegacion)
                    {
                        Debug.WriteLine(
                            "Shell permaneció ocupado después de varios " +
                            $"intentos. Ruta: {rutaDestino}. Error: {ex}");

                        await MostrarAdvertenciaAsync(
                            "La pantalla todavía estaba terminando de abrirse. " +
                            "Intente nuevamente.");

                        return;
                    }

                    await Task.Delay(75);
                }
            }
        }

        private static bool EsNavegacionPendiente(
            InvalidOperationException exception) =>
            exception.Message.Contains(
                "Pending Navigations still processing",
                StringComparison.OrdinalIgnoreCase);

        private static bool EsRutaActual(
            Shell shell,
            string rutaDestino)
        {
            if (string.Equals(
                    rutaDestino,
                    AppRoutes.Regresar,
                    StringComparison.OrdinalIgnoreCase) ||
                rutaDestino.StartsWith("..", StringComparison.Ordinal))
            {
                return false;
            }

            string rutaActual = NormalizarRutaComparacion(
                shell.CurrentState?.Location?.OriginalString);

            string destino = NormalizarRutaComparacion(
                rutaDestino);

            if (string.IsNullOrWhiteSpace(rutaActual) ||
                string.IsNullOrWhiteSpace(destino))
            {
                return false;
            }

            return string.Equals(
                       rutaActual,
                       destino,
                       StringComparison.OrdinalIgnoreCase) ||
                   rutaActual.EndsWith(
                       "/" + destino,
                       StringComparison.OrdinalIgnoreCase);
        }

        private static string NormalizarRutaComparacion(
            string? ruta)
        {
            if (string.IsNullOrWhiteSpace(ruta))
                return string.Empty;

            string valor = Uri.UnescapeDataString(ruta)
                .Trim()
                .Trim('/');

            while (valor.StartsWith("/", StringComparison.Ordinal))
                valor = valor[1..];

            return valor;
        }

        public bool ValidateNavigation(string interfaz)
        {
            var permission =
                PermissionService.Instance.Get(interfaz);

            if (permission == null || !permission.leer)
            {
                _ = MostrarInformacionAsync(
                    "No tiene permisos para acceder a esta sección.");
                return false;
            }

            return true;
        }

        private Task NavigateAsync(
            string permission,
            string route) =>
            IsBusy || !ValidateNavigation(permission)
                ? Task.CompletedTask
                : GoToAsyncParameters(route);

        private Task GoToMainPage() =>
            NavigateAsync("MainPage", AppRoutes.Principal);

        private async Task GoToConfiguracionPage()
        {
            if (IsBusy)
                return;

            if (!NavigationPermissionService
                    .PuedeVerConfiguracion())
            {
                await MostrarInformacionAsync(
                    "No tiene permisos para acceder a Configuración.");
                return;
            }

            await GoToAsyncParameters(AppRoutes.Configuracion);
        }

        private Task GoToAlbumFotosPage() =>
            NavigateAsync(
                "albumFotosPage",
                AppRoutes.AlbumFotos);

        private Task GoToUserPage() =>
            NavigateAsync("userPage", AppRoutes.Usuarios);

        public Task GoToRolPage() =>
            NavigateAsync("rolPage", AppRoutes.Roles);

        public Task GoToMatrizPermisosPage() =>
            NavigateAsync(
                "matrizPermisosPage",
                AppRoutes.MatrizPermisos);

        public Task GoToPaisPage() =>
            NavigateAsync("paisPage", AppRoutes.Paises);

        public Task GoToElementoQuimicoPage() =>
            NavigateAsync(
                "elementoQuimicoPage",
                AppRoutes.ElementosQuimicos);

        public Task GoToTerrenoPage() =>
            NavigateAsync("terrenoPage", AppRoutes.Terrenos);

        public Task GoToFuenteNutrientePage() =>
            NavigateAsync(
                "fuenteNutrientePage",
                AppRoutes.FuenteNutriente);

        public Task GoToTipoCultivoPage() =>
            NavigateAsync(
                "tipoCultivoPage",
                AppRoutes.TiposCultivo);

        public Task GoToTipoAnalisisSueloPage() =>
            NavigateAsync(
                "tipoAnalisisSueloPage",
                AppRoutes.TiposAnalisisSuelo);

        public Task GoToExtraccionNutrientePage() =>
            NavigateAsync(
                "extraccionNutrientePage",
                AppRoutes.ExtraccionNutrientes);

        public Task GoToRangoNutrientePage() =>
            NavigateAsync(
                "rangoNutrientePage",
                AppRoutes.RangosNutrientes);

        // ==========================================================
        // MENSAJES ESTANDARIZADOS
        // ==========================================================

        public static Task MostrarExitoAsync(string mensaje) =>
            AppNotificationService.ShowSuccessAsync(mensaje);

        public static Task MostrarErrorAsync(string mensaje) =>
            AppNotificationService.ShowErrorAsync(mensaje);

        public static Task MostrarAdvertenciaAsync(string mensaje) =>
            AppNotificationService.ShowWarningAsync(mensaje);

        public static Task MostrarInformacionAsync(string mensaje) =>
            AppNotificationService.ShowInformationAsync(mensaje);

        /// <summary>
        /// Se mantiene para no romper los ViewModels existentes.
        /// El nuevo servicio determina automáticamente el tipo de mensaje.
        /// </summary>
        public static Task MostrarToastAsync(string mensaje) =>
            AppNotificationService.ShowAutoAsync(mensaje);

        public static Task<bool> ConfirmarAsync(
            string titulo,
            string mensaje,
            string textoAceptar,
            string textoCancelar) =>
            AppNotificationService.ConfirmAsync(
                titulo,
                mensaje,
                textoAceptar,
                textoCancelar);

        public static Task<bool> ConfirmarGuardadoAsync(
            string nombreEntidad) =>
            AppNotificationService.ConfirmSaveAsync(nombreEntidad);

        public static Task<bool> ConfirmarActualizacionAsync(
            string nombreEntidad) =>
            AppNotificationService.ConfirmUpdateAsync(nombreEntidad);

        public static Task<bool> ConfirmarEliminacionAsync(
            string nombreEntidad) =>
            AppNotificationService.ConfirmDeleteAsync(nombreEntidad);

        public static Task<bool> ConfirmarSalidaSinGuardarAsync() =>
            AppNotificationService.ConfirmDiscardChangesAsync();

        public static async Task MostrarErrorInesperadoAsync(
            string operacion,
            Exception exception)
        {
            Debug.WriteLine(
                $"Error inesperado al {operacion}: {exception}");

            await MostrarErrorAsync(
                $"Ocurrió un error inesperado al {operacion}. Intente nuevamente.");
        }

        public Task<bool> TieneInternetAsync()
        {
            NetworkAccess networkAccess =
                Connectivity.Current.NetworkAccess;

#if WINDOWS
            // En Windows, NetworkAccess puede reportar Local o
            // ConstrainedInternet aunque la API sea accesible.
            // La llamada HTTP será la validación definitiva.
            return Task.FromResult(
                networkAccess != NetworkAccess.None);
#else
            return Task.FromResult(
                networkAccess == NetworkAccess.Internet);
#endif
        }

        public async Task<bool> ValidarInternetAsync()
        {
            bool tieneInternet = await TieneInternetAsync();

            if (!tieneInternet)
            {
                await MostrarAdvertenciaAsync(
                    "No hay conexión a internet. Verifique su red e intente nuevamente.");
            }

            return tieneInternet;
        }

        public void LoadPagePermissions(string pageName)
        {
            var permission =
                PermissionService.Instance.Get(pageName);

            CanAdd = permission?.agregar == true;
            CanEdit = permission?.actualizar == true;
            CanDelete = permission?.eliminar == true;
            CanView = permission?.leer == true;

            OnPropertyChanged(nameof(CanAdd));
            OnPropertyChanged(nameof(CanEdit));
            OnPropertyChanged(nameof(CanDelete));
            OnPropertyChanged(nameof(CanView));
        }

        public void OnPropertyChanged(
            [CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(name));
    }
}
