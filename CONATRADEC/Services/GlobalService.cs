using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace CONATRADEC.Services
{
    public class GlobalService : INotifyPropertyChanged
    {
        // ================== PERMISOS GLOBALES POR PAGE ==================
        public bool CanAdd { get; protected set; }
        public bool CanEdit { get; protected set; }
        public bool CanDelete { get; protected set; }
        public bool CanView { get; protected set; }

        // ============================
        // COMANDOS DE NAVEGACIÓN
        // ============================
        public Command goToMainPageCommand { get; }
        public Command goToUserPageButtonCommand { get; }
        public Command goToRolPageButtonCommand { get; }
        public Command goToMatrizPermisosPageButtonCommad { get; }
        public Command goToPaisPageButtonCommand { get; }
        public Command goToElementoQuimicoPageButtonCommand { get; }
        public Command goToTerrenoPageButtonCommand { get; }
        public Command goToFuenteNutrientePageButtonCommand { get; }
        public Command goToBack { get; }
        public Command CerrarSesionCommand { get; }

        // ============================
        // ESTADO Y NOTIFICACIONES
        // ============================
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
                ActualizarComandosNavegacion();
            }
        }

        public bool NotIsBusy => !IsBusy;

        // ============================
        // CONSTRUCTOR
        // ============================
        public GlobalService()
        {
            goToMainPageCommand = new Command(
                async () => await GoToMainPage(),
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

            goToBack = new Command(
                async () => await GoToAsyncParameters(AppRoutes.Regresar),
                () => !IsBusy);

            CerrarSesionCommand = new Command(
                async () => await CerrarSesionAsync(),
                () => !IsBusy);
        }

        private void ActualizarComandosNavegacion()
        {
            goToMainPageCommand?.ChangeCanExecute();
            goToUserPageButtonCommand?.ChangeCanExecute();
            goToRolPageButtonCommand?.ChangeCanExecute();
            goToMatrizPermisosPageButtonCommad?.ChangeCanExecute();
            goToElementoQuimicoPageButtonCommand?.ChangeCanExecute();
            goToPaisPageButtonCommand?.ChangeCanExecute();
            goToBack?.ChangeCanExecute();
            goToTerrenoPageButtonCommand?.ChangeCanExecute();
            goToFuenteNutrientePageButtonCommand?.ChangeCanExecute();
            CerrarSesionCommand?.ChangeCanExecute();
        }

        private async Task CerrarSesionAsync()
        {
            if (IsBusy)
                return;

            bool confirmar = await Application.Current!.MainPage!.DisplayAlert(
                "Cerrar sesión",
                "¿Está seguro que desea cerrar sesión?",
                "Sí, cerrar",
                "Cancelar");

            if (!confirmar)
                return;

            Preferences.Remove(SessionKeys.KeyUserId);
            Preferences.Remove(SessionKeys.KeyNombreCompletoUsuario);
            Preferences.Remove(SessionKeys.KeyCorreoUsuario);
            Preferences.Remove(SessionKeys.KeyUrlImagenUsuario);

            PermissionService.Instance.ClearPermissions();

            await Shell.Current.GoToAsync(AppRoutes.Login);
        }

        // ============================
        // HELPER DE NAVEGACIÓN
        // ============================
        public async Task GoToAsyncParameters(
            string route,
            IDictionary<string, object>? parameters = null)
        {
            if (string.IsNullOrWhiteSpace(route))
                return;

            if (parameters == null)
                await Shell.Current.GoToAsync(route, false);
            else
                await Shell.Current.GoToAsync(route, false, parameters);
        }

        // ============================
        // VALIDACIÓN DE PERMISOS
        // ============================
        public bool ValidateNavigation(string interfaz)
        {
            var permiso = PermissionService.Instance.Get(interfaz);

            if (permiso == null || !permiso.leer)
            {
                _ = MostrarToastAsync(
                    "No tiene permisos para acceder a esta sección.");

                return false;
            }

            return true;
        }

        // ============================
        // MÉTODOS DE NAVEGACIÓN
        // ============================
        private async Task GoToMainPage()
        {
            if (IsBusy || !ValidateNavigation("MainPage"))
                return;

            await GoToAsyncParameters(AppRoutes.Principal);
        }

        private async Task GoToUserPage()
        {
            if (IsBusy || !ValidateNavigation("userPage"))
                return;

            await GoToAsyncParameters(AppRoutes.Usuarios);
        }

        public async Task GoToRolPage()
        {
            if (IsBusy || !ValidateNavigation("rolPage"))
                return;

            await GoToAsyncParameters(AppRoutes.Roles);
        }

        public async Task GoToMatrizPermisosPage()
        {
            if (IsBusy || !ValidateNavigation("matrizPermisosPage"))
                return;

            await GoToAsyncParameters(AppRoutes.MatrizPermisos);
        }

        public async Task GoToPaisPage()
        {
            if (IsBusy || !ValidateNavigation("paisPage"))
                return;

            await GoToAsyncParameters(AppRoutes.Paises);
        }

        public async Task GoToFuenteNutrientePage()
        {
            if (IsBusy || !ValidateNavigation("fuenteNutrientePage"))
                return;

            await GoToAsyncParameters(AppRoutes.FuenteNutriente);
        }

        public async Task GoToElementoQuimicoPage()
        {
            if (IsBusy || !ValidateNavigation("elementoQuimicoPage"))
                return;

            await GoToAsyncParameters(AppRoutes.ElementosQuimicos);
        }

        public async Task GoToTerrenoPage()
        {
            if (IsBusy || !ValidateNavigation("terrenoPage"))
                return;

            await GoToAsyncParameters(AppRoutes.Terrenos);
        }

        // ============================
        // UTILIDADES
        // ============================
        public static async Task MostrarToastAsync(string mensaje)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(mensaje))
                    return;

                var toast = Toast.Make(
                    mensaje,
                    ToastDuration.Short,
                    14);

                await toast.Show();
            }
            catch
            {
                // Un fallo del Toast no debe cerrar la aplicación.
            }
        }

        /// <summary>
        /// Se conserva temporalmente para no romper formularios que todavía
        /// llaman este método. Ya no realiza peticiones externas a Google.
        /// Los módulos migrados deben llamar directamente a la API y manejar
        /// el resultado real de la operación.
        /// </summary>
        public Task<bool> TieneInternetAsync()
        {
            bool disponible =
                Connectivity.Current.NetworkAccess == NetworkAccess.Internet;

            return Task.FromResult(disponible);
        }

        public void LoadPagePermissions(string pageName)
        {
            var permiso = PermissionService.Instance.Get(pageName);

            CanAdd = permiso.agregar;
            CanEdit = permiso.actualizar;
            CanDelete = permiso.eliminar;
            CanView = permiso.leer;

            OnPropertyChanged(nameof(CanAdd));
            OnPropertyChanged(nameof(CanEdit));
            OnPropertyChanged(nameof(CanDelete));
            OnPropertyChanged(nameof(CanView));
        }

        // ============================
        // INotifyPropertyChanged
        // ============================
        public void OnPropertyChanged(
            [CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(name));
        }
    }
}
