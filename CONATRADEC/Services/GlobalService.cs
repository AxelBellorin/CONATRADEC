using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using CONATRADEC.ViewModels;
using CONATRADEC.Views;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

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
        public Command goToBack { get; }

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
                isBusy = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(NotIsBusy));

                ((Command)goToMainPageCommand).ChangeCanExecute();
                ((Command)goToUserPageButtonCommand).ChangeCanExecute();
                ((Command)goToRolPageButtonCommand).ChangeCanExecute();
                ((Command)goToMatrizPermisosPageButtonCommad).ChangeCanExecute();
                ((Command)goToElementoQuimicoPageButtonCommand).ChangeCanExecute();
                ((Command)goToPaisPageButtonCommand).ChangeCanExecute();
                ((Command)goToBack).ChangeCanExecute();
                ((Command)goToTerrenoPageButtonCommand).ChangeCanExecute();
            }
        }

        public bool NotIsBusy => !IsBusy;

        // ============================
        // CONSTRUCTOR
        // ============================
        public GlobalService()
        {
            goToMainPageCommand = new Command(async () => await GoToMainPage(), () => !IsBusy);
            goToUserPageButtonCommand = new Command(async () => await GoToUserPage(), () => !IsBusy);
            goToRolPageButtonCommand = new Command(async () => await GoToRolPage(), () => !IsBusy);
            goToMatrizPermisosPageButtonCommad = new Command(async () => await GoToMatrizPermisosPage(), () => !IsBusy);
            goToPaisPageButtonCommand = new Command(async () => await GoToPaisPage(), () => !IsBusy);
            goToElementoQuimicoPageButtonCommand = new Command(async () => await GoToElementoQuimicoPage(), () => !IsBusy);
            goToTerrenoPageButtonCommand = new Command(async () => await GoToTerrenoPage(), () => !IsBusy);

            goToBack = new Command(async () => await GoToAsyncParameters("//.."));
        }

        // ============================
        // HELPER DE NAVEGACIÓN
        // ============================
        public async Task GoToAsyncParameters(string route, IDictionary<string, object>? parameters = null)
        {
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
                _ = MostrarToastAsync("No tiene permisos para acceder a esta sección.");
                return false;
            }

            return true;
        }

        // ============================
        // MÉTODOS DE NAVEGACIÓN
        // ============================

        private async Task GoToMainPage()
        {
            if (IsBusy) return;

            if (!ValidateNavigation("MainPage"))
                return;

            await GoToAsyncParameters("//MainPage");
        }

        private async Task GoToUserPage()
        {
            if (IsBusy) return;

            if (!ValidateNavigation("userPage"))
                return;

            await GoToAsyncParameters("//UserPage");
        }

        public async Task GoToRolPage()
        {
            if (IsBusy) return;

            if (!ValidateNavigation("rolPage"))
                return;

            await GoToAsyncParameters("//RolPage");
        }

        public async Task GoToMatrizPermisosPage()
        {
            if (IsBusy) return;

            if (!ValidateNavigation("matrizPermisosPage"))
                return;

            await GoToAsyncParameters("//MatrizPermisosPage");
        }

        public async Task GoToPaisPage()
        {
            if (IsBusy) return;

            if (!ValidateNavigation("paisPage"))
                return;

            await GoToAsyncParameters("//PaisPage");
        }

        public async Task GoToElementoQuimicoPage()
        {
            if (IsBusy) return;

            if (!ValidateNavigation("elementoQuimicoPage"))
                return;

            await GoToAsyncParameters("//ElementoQuimicoPage");
        }

        public async Task GoToTerrenoPage()
        {
            if (IsBusy) return;

            if (!ValidateNavigation("terrenoPage"))
                return;

            await GoToAsyncParameters("//TerrenoPage");
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

                var toast = Toast.Make(mensaje, ToastDuration.Short, 14);
                await toast.Show();
            }
            catch { }
        }

        public async Task<bool> TieneInternetAsync()
        {
            bool tieneInternet = Connectivity.Current.NetworkAccess == NetworkAccess.Internet;
            if (!tieneInternet)
                return await ValidacionRealInternetAsync();

            return await ValidacionRealInternetAsync();
        }

        private async Task<bool> ValidacionRealInternetAsync()
        {
            try
            {
                using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
                var response = await http.GetAsync("https://www.google.com");
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
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
        public void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
