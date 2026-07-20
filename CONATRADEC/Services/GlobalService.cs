using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using Microsoft.Maui.Networking;
using Microsoft.Maui.Storage;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace CONATRADEC.Services
{
    public class GlobalService : INotifyPropertyChanged
    {
        public bool CanAdd { get; protected set; }
        public bool CanEdit { get; protected set; }
        public bool CanDelete { get; protected set; }
        public bool CanView { get; protected set; }

        public Command goToMainPageCommand { get; }
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
                if (isBusy == value) return;
                isBusy = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(NotIsBusy));
                UpdateNavigationCommands();
            }
        }

        public bool NotIsBusy => !IsBusy;

        public GlobalService()
        {
            goToMainPageCommand = new Command(async () => await GoToMainPage(), () => !IsBusy);
            goToUserPageButtonCommand = new Command(async () => await GoToUserPage(), () => !IsBusy);
            goToRolPageButtonCommand = new Command(async () => await GoToRolPage(), () => !IsBusy);
            goToMatrizPermisosPageButtonCommad = new Command(async () => await GoToMatrizPermisosPage(), () => !IsBusy);
            goToPaisPageButtonCommand = new Command(async () => await GoToPaisPage(), () => !IsBusy);
            goToElementoQuimicoPageButtonCommand = new Command(async () => await GoToElementoQuimicoPage(), () => !IsBusy);
            goToTerrenoPageButtonCommand = new Command(async () => await GoToTerrenoPage(), () => !IsBusy);
            goToFuenteNutrientePageButtonCommand = new Command(async () => await GoToFuenteNutrientePage(), () => !IsBusy);
            goToTipoCultivoPageButtonCommand = new Command(async () => await GoToTipoCultivoPage(), () => !IsBusy);
            goToTipoAnalisisSueloPageButtonCommand = new Command(async () => await GoToTipoAnalisisSueloPage(), () => !IsBusy);
            goToExtraccionNutrientePageButtonCommand = new Command(async () => await GoToExtraccionNutrientePage(), () => !IsBusy);
            goToRangoNutrientePageButtonCommand = new Command(async () => await GoToRangoNutrientePage(), () => !IsBusy);
            goToBack = new Command(async () => await GoToAsyncParameters(AppRoutes.Regresar), () => !IsBusy);
            CerrarSesionCommand = new Command(async () => await CerrarSesionAsync(), () => !IsBusy);
        }

        private void UpdateNavigationCommands()
        {
            goToMainPageCommand.ChangeCanExecute();
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
            if (IsBusy) return;
            Page? page = Application.Current?.MainPage;
            if (page == null) return;

            bool confirm = await page.DisplayAlert("Cerrar sesión", "¿Está seguro que desea cerrar sesión?", "Sí, cerrar", "Cancelar");
            if (!confirm) return;

            Preferences.Remove(SessionKeys.KeyUserId);
            Preferences.Remove(SessionKeys.KeyNombreCompletoUsuario);
            Preferences.Remove(SessionKeys.KeyCorreoUsuario);
            Preferences.Remove(SessionKeys.KeyUrlImagenUsuario);
            PermissionService.Instance.ClearPermissions();
            await Shell.Current.GoToAsync(AppRoutes.Login);
        }

        public async Task GoToAsyncParameters(string route, IDictionary<string, object>? parameters = null)
        {
            if (string.IsNullOrWhiteSpace(route)) return;
            if (parameters == null)
                await Shell.Current.GoToAsync(route, false);
            else
                await Shell.Current.GoToAsync(route, false, parameters);
        }

        public bool ValidateNavigation(string interfaz)
        {
            var permission = PermissionService.Instance.Get(interfaz);
            if (permission == null || !permission.leer)
            {
                _ = MostrarToastAsync("No tiene permisos para acceder a esta sección.");
                return false;
            }
            return true;
        }

        private Task NavigateAsync(string permission, string route) =>
            IsBusy || !ValidateNavigation(permission)
                ? Task.CompletedTask
                : GoToAsyncParameters(route);

        private Task GoToMainPage() => NavigateAsync("MainPage", AppRoutes.Principal);
        private Task GoToUserPage() => NavigateAsync("userPage", AppRoutes.Usuarios);
        public Task GoToRolPage() => NavigateAsync("rolPage", AppRoutes.Roles);
        public Task GoToMatrizPermisosPage() => NavigateAsync("matrizPermisosPage", AppRoutes.MatrizPermisos);
        public Task GoToPaisPage() => NavigateAsync("paisPage", AppRoutes.Paises);
        public Task GoToElementoQuimicoPage() => NavigateAsync("elementoQuimicoPage", AppRoutes.ElementosQuimicos);
        public Task GoToTerrenoPage() => NavigateAsync("terrenoPage", AppRoutes.Terrenos);
        public Task GoToFuenteNutrientePage() => NavigateAsync("fuenteNutrientePage", AppRoutes.FuenteNutriente);
        public Task GoToTipoCultivoPage() => NavigateAsync("tipoCultivoPage", AppRoutes.TiposCultivo);
        public Task GoToTipoAnalisisSueloPage() => NavigateAsync("tipoAnalisisSueloPage", AppRoutes.TiposAnalisisSuelo);
        public Task GoToExtraccionNutrientePage() => NavigateAsync("extraccionNutrientePage", AppRoutes.ExtraccionNutrientes);
        public Task GoToRangoNutrientePage() => NavigateAsync("rangoNutrientePage", AppRoutes.RangosNutrientes);

        public static async Task MostrarToastAsync(string mensaje)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(mensaje)) return;
                await Toast.Make(mensaje, ToastDuration.Short, 14).Show();
            }
            catch { }
        }

        public Task<bool> TieneInternetAsync() =>
            Task.FromResult(Connectivity.Current.NetworkAccess == NetworkAccess.Internet);

        public void LoadPagePermissions(string pageName)
        {
            var permission = PermissionService.Instance.Get(pageName);
            CanAdd = permission?.agregar == true;
            CanEdit = permission?.actualizar == true;
            CanDelete = permission?.eliminar == true;
            CanView = permission?.leer == true;
            OnPropertyChanged(nameof(CanAdd));
            OnPropertyChanged(nameof(CanEdit));
            OnPropertyChanged(nameof(CanDelete));
            OnPropertyChanged(nameof(CanView));
        }

        public void OnPropertyChanged([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
