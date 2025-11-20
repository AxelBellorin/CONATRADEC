using CONATRADEC.Models;            // Modelos: RolResponse, RolRequest, FormMode, etc.
using CONATRADEC.Services;          // Servicios de datos: RolApiService.
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel; // Colecciones observables para data binding en la vista.
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;          // Command / ICommand (enlace con botones de la UI).

namespace CONATRADEC.ViewModels
{
    // ViewModel para el listado/gestión de Roles.
    // Hereda de GlobalService para reutilizar navegación (GoToAsyncParameters) y notificaciones (OnPropertyChanged).
    public class RolViewModel : GlobalService
    {
        // ===========================================================
        // ==================== ESTADO / DATOS =======================
        // ===========================================================

        // Lista observable que se bindea en la vista (CollectionView/ListView).
        private ObservableCollection<RolResponse> list = new ObservableCollection<RolResponse>();

        // Servicio HTTP para operaciones con Roles (listar, eliminar, etc.).
        private readonly RolApiService rolApiService;

        // ===========================================================
        // ======================= COMANDOS ===========================
        // ===========================================================

        public Command AddCommand { get; }                 // Navega al formulario en modo Crear.
        public Command EditCommand { get; }                // Navega al formulario en modo Editar.
        public Command DeleteCommand { get; }              // Elimina un rol seleccionado.
        public Command ViewCommand { get; }                // Navega al formulario en modo Ver.

        // Propiedad bindable que expone la lista a la UI.
        public ObservableCollection<RolResponse> List
        {
            get => list;
            set { list = value; OnPropertyChanged(); }     // Notifica a la vista para refrescar el binding.
        }

        // ===========================================================
        // ======================== CTOR =============================
        // ===========================================================
        public RolViewModel()
        {
            rolApiService = new RolApiService();

            // Inicializa comandos con sus handlers.
            AddCommand = new Command(async () => await OnAdd());
            EditCommand = new Command<RolResponse>(OnEdit);
            DeleteCommand = new Command<RolResponse>(OnDelete);
            ViewCommand = new Command<RolResponse>(OnView);
        }

        // ===========================================================
        // ================== CARGA INICIAL / REFRESH =================
        // ===========================================================
        // Carga la lista de roles desde la API. Recibe un flag isBusy para sincronizar con la UI.
        public async Task LoadRol(bool isBusy)
        {
            IsBusy = isBusy; // Propiedad heredada de GlobalService (puede activar spinners/disable botones).

            List.Clear();   // Limpia lista actual.

            // Valida que el usaurio tenga conexion a internet
            bool tieneInternet = await TieneInternetAsync();

            if (!tieneInternet)
            {
                _ = MostrarToastAsync("Sin conexión a internet.");
                IsBusy = false;
                return;
            }

            var response = await rolApiService.GetRolAsync();

            if (response.Count() != 0)
            {
                List = response; // Asigna la colección devuelta por la API (ObservableCollection).
            }
            else
            {
                // Mensaje informativo si no hay registros.
                _ = MostrarToastAsync("Información" + "No se encontraron roles.");
                // (Nota: el texto dice "cargos", probablemente quisiste decir "roles". Mantengo tu literal.)
            }

            IsBusy = false; // Fin de operación.
        }

        // ===========================================================
        // ===================== HANDLERS (UI) =======================
        // ===========================================================

        // Handler para agregar un nuevo rol (navega al formulario en modo Crear).
        private async Task OnAdd()
        {
            if (IsBusy) return; // Evita reentradas o taps múltiples.

            try
            {
                // Parámetros que se pasan a la página destino (Shell navigation).
                var parameters = new Dictionary<string, object>
                {
                    { "Mode", FormMode.FormModeSelect.Create},
                    { "Rol", new RolRequest(new RolResponse()) } // RolRequest vacío basado en un RolResponse vacío.
                };

                await GoToAsyncParameters("//RolFormPage", parameters);
            }
            catch (Exception ex)
            {
                _ = MostrarToastAsync("Error" + $"No se pudo conectar al servidor {ex}");
            }
        }

        // Handler para editar un rol (navega al formulario en modo Editar).
        private async void OnEdit(RolResponse rol)
        {
            if (IsBusy) return;

            try
            {
                if (rol == null) return; // Protección ante elemento nulo.

                var parameters = new Dictionary<string, object>
                {
                    { "Mode", FormMode.FormModeSelect.Edit },
                    { "Rol", new RolRequest(rol) } // Mapea el rol seleccionado al DTO de solicitud.
                };

                await GoToAsyncParameters("//RolFormPage", parameters);
            }
            catch (Exception ex)
            {
                _ = MostrarToastAsync("Error" + $"{ex}");
            }
        }

        // Handler para eliminar un rol (pide confirmación y llama al servicio).
        private async void OnDelete(RolResponse rol)
        {
            if (IsBusy) return;

            IsBusy = true; // Marca inicio de operación para la UI (loader/deshabilitar).
            try
            {
                if (rol == null) return;

                // Confirmación con el usuario antes de eliminar.
                bool confirm = _ = await App.Current.MainPage.DisplayAlert(
                    "Eliminar",
                    $"¿Seguro que deseas eliminar al rol {rol.NombreRol}",
                    "Sí",
                    "No");

                if (confirm)
                {
                    // Valida que el usaurio tenga conexion a internet
                    bool tieneInternet = await TieneInternetAsync();

                    if (!tieneInternet)
                    {
                        _ = MostrarToastAsync("Sin conexión a internet.");
                        IsBusy = false;
                        return;
                    }

                    // Llama al servicio de eliminación. (Asegúrate que el método exista con el nombre exacto.)
                    var response = await rolApiService.DeleteRolAsync(new RolRequest(rol));

                    if (response)
                    {
                        _ = MostrarToastAsync("Éxito" + "\nRol eliminado correctamente");
                        await LoadRol(IsBusy); // Recarga la lista. (IsBusy es true en este punto.)
                    }
                    else
                    {
                        _ = MostrarToastAsync("Error" + "\nEl rol no se pudo eliminar, intente nuevamente");
                    }
                }
                else
                {
                    IsBusy = false; // Si canceló, restablece la UI manualmente.
                }
            }
            catch (Exception ex)
            {
                _ = MostrarToastAsync("Error" + $"{ex}");
            }
        }

        // Handler para ver los detalles de un rol (modo lectura).
        private async void OnView(RolResponse rol)
        {
            if (IsBusy) return;

            // Parámetros para abrir el formulario en modo View (solo lectura).
            var parameters = new Dictionary<string, object>
            {
                { "Mode", FormMode.FormModeSelect.View},
                { "Rol", new RolRequest(rol) }
            };

            await GoToAsyncParameters("//RolFormPage", parameters);
        }
    }
}
