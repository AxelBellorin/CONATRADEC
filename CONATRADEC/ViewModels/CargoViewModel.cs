using CONATRADEC.Models;
using CONATRADEC.Services;
using Microsoft.Maui.Networking;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace CONATRADEC.ViewModels
{
    // ViewModel del listado/gestión de Cargos.
    // Hereda de GlobalService para reutilizar navegación (GoToAsyncParameters) y estado (IsBusy).
    public class CargoViewModel : GlobalService
    {
        // ===========================================================
        // ================= ESTADO / PROPIEDADES BINDABLE ===========
        // ===========================================================

        // Colección observable enlazada a la vista para mostrar los cargos.
        private ObservableCollection<CargoResponse> list = new ObservableCollection<CargoResponse>();

        // Servicio de API para operaciones CRUD sobre Cargo.
        private readonly CargoApiService cargoApiService;

        // ===========================================================
        // ========================= COMANDOS ========================
        // ===========================================================

        // Comando para agregar un nuevo cargo (navega al formulario en modo Create).
        public Command AddCommand { get; }

        // Comando para editar un cargo seleccionado (navega al formulario en modo Edit).
        public Command EditCommand { get; }

        // Comando para eliminar un cargo seleccionado (pide confirmación y llama a API).
        public Command DeleteCommand { get; }

        // Comando para ver detalles de un cargo (navega al formulario en modo View).
        public Command ViewCommand { get; }

        // Propiedad bindable de la lista (notifica a la UI cuando cambia la referencia).
        public ObservableCollection<CargoResponse> List
        {
            get => list;
            set { list = value; OnPropertyChanged(); }
        }

        // ===========================================================
        // ======================= CONSTRUCTOR =======================
        // ===========================================================

        public CargoViewModel()
        {
            // Instancia explícita del servicio (más adelante podrías inyectarlo por DI).
            cargoApiService = new CargoApiService();

            // Inicialización de comandos con los handlers correspondientes.
            AddCommand = new Command(async () => await OnAdd());
            EditCommand = new Command<CargoResponse>(OnEdit);
            DeleteCommand = new Command<CargoResponse>(OnDelete);
            ViewCommand = new Command<CargoResponse>(OnView);
        }

        // ===========================================================
        // ====================== MÉTODOS PÚBLICOS ===================
        // ===========================================================

        // Carga/recarga la lista de cargos desde la API.
        // El parámetro isBusy permite reflejar el estado visual mientras se consulta.
        public async Task LoadCargo(bool isBusy)
        {
            IsBusy = isBusy; // Marca el inicio/estado según la llamada.

            // Ejemplo de chequeo de conectividad (actualmente comentado).
            // bool tieneInternet = Connectivity.Current.NetworkAccess == NetworkAccess.Internet;

            // if (tieneInternet)
            // {
            // Obtiene los cargos desde el servicio; retorna colección vacía en error.
            var response = await cargoApiService.GetCargoAsync();

            if (response.Count() != 0)
            {
                // Limpia y reemplaza la lista para refrescar la UI.
                List.Clear();
                List = response;
            }
            else
            {
                // Feedback si no hay resultados.
                await App.Current.MainPage.DisplayAlert("Información", "No se encontraron cargos.", "OK");
            }
            // }
            // else
            // {
            //     await App.Current.MainPage.DisplayAlert("Error", "No hay conexión a internet. Por favor, verifica tu conexión e inténtalo de nuevo.", "OK");
            // }

            IsBusy = false; // Libera el estado ocupado.
        }

        // ===========================================================
        // ===================== HANDLERS (COMANDOS) =================
        // ===========================================================

        // Handler para agregar un nuevo cargo: navega al formulario en modo Create.
        private async Task OnAdd()
        {
            if (IsBusy) return; // Evita dobles toques.

            try
            {
                // Parámetros para el formulario (Create con un CargoRequest vacío).
                var parameters = new Dictionary<string, object>
                {
                    { "Mode", FormMode.FormModeSelect.Create},
                    { "Cargo", new CargoRequest(new CargoResponse()) }
                };

                // Navega a la página del formulario.
                await GoToAsyncParameters("//CargoFormPage", parameters);
            }
            catch (Exception ex)
            {
                // Error general (conexión, navegación, etc.).
                await App.Current.MainPage.DisplayAlert("Error", $"No se pudo conectar al servidor {ex}", "OK");
            }
        }

        // Handler para editar: navega al formulario en modo Edit con el cargo seleccionado.
        private async void OnEdit(CargoResponse cargo)
        {
            if (IsBusy) return;       // Evita reentradas.
            try
            {
                if (cargo == null) return; // No procede si no hay selección.

                // Parámetros para el formulario (Edit con el cargo elegido).
                var parameters = new Dictionary<string, object>
                {
                    { "Mode", FormMode.FormModeSelect.Edit },
                    { "Cargo", new CargoRequest(cargo) }
                };

                // Navega a la página del formulario con los parámetros.
                await GoToAsyncParameters("//CargoFormPage", parameters);
            }
            catch (Exception ex)
            {
                await App.Current.MainPage.DisplayAlert("Error", $"{ex}", "OK");
            }
        }

        // Handler para eliminar: pide confirmación y llama a la API.
        private async void OnDelete(CargoResponse cargo)
        {
            if (IsBusy) return; // Evita reentradas.

            IsBusy = true;      // Marca comienzo de operación.
            try
            {
                if (cargo == null) return; // Sin selección, no procede.

                // Confirmación previa con el nombre del cargo.
                bool confirm = await App.Current.MainPage.DisplayAlert(
                    "Eliminar",
                    $"¿Seguro que deseas eliminar al cargo {cargo.NombreCargo}",
                    "Sí", "No");

                if (confirm)
                {
                    // Llama al servicio de eliminación (Delete) con el ID del cargo.
                    var response = await cargoApiService.DeleteCargoAsyn(new CargoRequest(cargo));

                    if (response)
                    {
                        // Feedback de éxito y recarga del listado (en segundo plano).
                        await App.Current.MainPage.DisplayAlert("Éxito", "Cargo eliminado correctamente", "OK");
                        await LoadCargo(true);
                    }
                    else
                    {
                        // Mensaje de error si la API no confirmó el borrado.
                        await App.Current.MainPage.DisplayAlert("Error", "El cargo no se pudo eliminar, intente nuevamente", "OK");
                    }
                }
                else
                {
                    // Si cancela, libera el estado ocupado.
                    IsBusy = false;
                }
            }
            catch (Exception ex)
            {
                await App.Current.MainPage.DisplayAlert("Error", $"{ex}", "OK");
            }
        }

        // Handler para ver detalles: navega al formulario en modo View.
        private async void OnView(CargoResponse cargo)
        {
            if (IsBusy) return; // Evita reentradas.

            // Parámetros para el formulario (View con el cargo elegido).
            var parameters = new Dictionary<string, object>
            {
                { "Mode", FormMode.FormModeSelect.View},
                { "Cargo", new CargoRequest(cargo) }
            };

            // Navega a la página del formulario con los parámetros.
            await GoToAsyncParameters("//CargoFormPage", parameters);
        }
    }
}
