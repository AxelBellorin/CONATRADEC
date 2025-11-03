using CONATRADEC.ViewModels;
using CONATRADEC.Views;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace CONATRADEC.Services
{
    // Servicio base compartido para navegación y estado común (IsBusy).
    // Implementa INotifyPropertyChanged para permitir data binding desde ViewModels/Views.
    public class GlobalService : INotifyPropertyChanged
    {
        // ===========================================================
        // ======================== COMANDOS ==========================
        // ===========================================================
        public Command goToMainPageCommand { get; }                 // Comando para navegar a MainPage.
        public Command goToUserPageButtonCommand { get; }           // Comando para navegar a UserPage.
        public Command goToRolPageButtonCommand { get; }            // Comando para navegar a RolPage.
        public Command goToCargoPageButtonCommand { get; }          // Comando para navegar a CargoPage.
        public Command goToMatrizPermisosPageButtonCommad { get; }  // Comando para navegar a MatrizPermisosPage. (sic: nombre conserva 'Commad')
        public Command goToBack { get; }                            // Comando para navegar hacia atrás en Shell.

        // ===========================================================
        // =================== ESTADO / NOTIFICACIÓN =================
        // ===========================================================
        private bool isBusy;                                        // Bandera de operación en curso para bloquear UI/acciones concurrentes.

        // Evento de notificación de cambios de propiedades (INotifyPropertyChanged).
        public event PropertyChangedEventHandler? PropertyChanged;

        // Propiedad enlazable que expone el estado de ocupación (busy).
        public bool IsBusy
        {
            get => isBusy;                                         // Retorna el estado actual.
            set
            {
                isBusy = value;                                     // Actualiza el estado interno.
                OnPropertyChanged();                                // Notifica a la UI el cambio de IsBusy.

                // Propaga ChangeCanExecute a cada Command para recalcular su disponibilidad (CanExecute).
                ((Command)goToMainPageCommand).ChangeCanExecute();
                ((Command)goToUserPageButtonCommand).ChangeCanExecute();
                ((Command)goToRolPageButtonCommand).ChangeCanExecute();
                ((Command)goToCargoPageButtonCommand).ChangeCanExecute();
                ((Command)goToMatrizPermisosPageButtonCommad).ChangeCanExecute();
            }
        }

        // ===========================================================
        // ======================= CONSTRUCTOR =======================
        // ===========================================================
        public GlobalService()
        {
            // Inicializa los comandos con acciones asincrónicas y predicados de habilitación basados en !IsBusy.
            goToMainPageCommand = new Command(async () => await GoToMainPage(), () => !IsBusy);
            goToUserPageButtonCommand = new Command(async () => await GoToUserPage(), () => !IsBusy);
            goToRolPageButtonCommand = new Command(async () => await GoToRolPage(), () => !IsBusy);
            goToCargoPageButtonCommand = new Command(async () => await GoToCargoPage(), () => !IsBusy);
            goToMatrizPermisosPageButtonCommad = new Command(async () => await GoToMatrizPermisosPage(), () => !IsBusy);

            // Comando de navegación hacia atrás usando ruta relativa (“//..” mantiene esquema de Shell).
            goToBack = new Command(async () => await GoToAsyncParameters("//.."));
        }

        // ===========================================================
        // =============== HELPER GENERAL DE NAVEGACIÓN ==============
        // ===========================================================
        public async Task GoToAsyncParameters(string route, IDictionary<string, object>? parameters = null)
        {
            // Si no hay parámetros, navega con animación deshabilitada (false).
            if (parameters == null)
                await Shell.Current.GoToAsync(route, false);
            else
                // Si hay parámetros, los inyecta en la navegación (también sin animación).
                await Shell.Current.GoToAsync(route, false, parameters);
        }

        // ===========================================================
        // ================= HANDLERS DE NAVEGACIÓN ==================
        // ===========================================================
        private async Task GoToMainPage()
        {
            if (IsBusy) return;                                     // Evita reentradas si ya está ocupado.
            //IsBusy = true;                                        // Comentado: se mantiene la lógica actual que no marca busy al inicio.

            await GoToAsyncParameters("//MainPage");                 // Navega a la ruta absoluta de MainPage.

            // Tras la navegación, intenta resolver la página actual y su VM para actualizar estado.
            if (Shell.Current.CurrentPage is MainPage page &&
                page.BindingContext is MainPageViewModel vm)
                vm.IsBusy = false;                                  // Fuerza a la VM a estado no ocupado.

            IsBusy = false;                                         // Asegura liberar busy en el servicio.

            // Bloque comentado: historial previo de carga de datos al navegar.
            //if (Shell.Current.CurrentPage is MainPage page &&
            //     page.BindingContext is MainPageViewModel vm)
            //{
            //    //await vm.LoadUsers(IsBusy);
            //   // vm.IsBusy = false;
            //}
            //IsBusy = false;
        }

        private async Task GoToUserPage()
        {
            if (IsBusy) return;                                     // Evita dobles clics/condiciones de carrera.

            await GoToAsyncParameters("//UserPage");                // Navega a UserPage.

            //IsBusy = false;                                         // Libera estado ocupado.
        }

        public async Task GoToRolPage()
        {
            if (IsBusy) return;                                     // Evita reentradas.

            await GoToAsyncParameters("//RolPage");                 // Navega a RolPage.

            IsBusy = false;                                         // Libera estado ocupado.
        }

        public async Task GoToCargoPage()
        {
            if (IsBusy) return;                                     // Evita reentradas.

            await GoToAsyncParameters("//CargoPage");               // Navega a CargoPage.

            //  IsBusy = false;                                         // Libera estado ocupado.
        }

        public async Task GoToMatrizPermisosPage()
        {
            if (IsBusy) return;                                     // Evita reentradas.
            IsBusy = true;                                          // Marca inicio de operación.

            await GoToAsyncParameters("//MatrizPermisosPage");      // Navega a MatrizPermisosPage.

            // Resuelve la página y VM; llamadas de carga están comentadas según lógica actual.
            if (Shell.Current.CurrentPage is matrizPermisosPage page &&
                page.BindingContext is MatrizPermisosViewModel vm)
                //await vm.LoadCargo(IsBusy);                       // Comentado intencionalmente (respeta tu código).
                //await Task.Delay(1000);                           // Comentado intencionalmente (respeta tu código).
                IsBusy = false;                                         // Libera estado ocupado.
        }

        // ===========================================================
        // ===================== NOTIFICACIÓN INotify ================
        // ===========================================================
        public void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name)); // Dispara el evento con el nombre de la propiedad.
    }
}
