using CONATRADEC.Models;
using CONATRADEC.Services;
using Microsoft.Maui.Networking;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace CONATRADEC.ViewModels
{
    // ===============================================================
    // ViewModel: PaisViewModel
    // Descripción:
    //     Gestiona el listado, creación, edición, eliminación y visualización
    //     de países en la interfaz. Se comunica con PaisApiService para las
    //     operaciones CRUD.
    //
    //     Hereda de GlobalService para reutilizar propiedades y métodos comunes
    //     como IsBusy, GoToAsyncParameters, etc.
    // ===============================================================
    public class PaisViewModel : GlobalService
    {
        // ===========================================================
        // ================= ESTADO / PROPIEDADES BINDABLE ===========
        // ===========================================================

        // Colección observable de países para enlazar con la UI.
        private ObservableCollection<PaisResponse> list = new();

        // Servicio de API para las operaciones de País.
        private readonly PaisApiService paisApiService;

        // Propiedad pública enlazable (observable) de la lista.
        public ObservableCollection<PaisResponse> List
        {
            get => list;
            set { list = value; OnPropertyChanged(); }
        }

        // ===========================================================
        // ========================= COMANDOS ========================
        // ===========================================================

        // Comando: Agregar nuevo país
        public Command AddCommand { get; }

        // Comando: Editar país existente
        public Command EditCommand { get; }

        // Comando: Eliminar país existente
        public Command DeleteCommand { get; }

        // Comando: Ver detalles del país
        public Command ViewCommand { get; }

        // ===========================================================
        // ======================= CONSTRUCTOR =======================
        // ===========================================================

        public PaisViewModel()
        {
            // Instancia del servicio de API.
            paisApiService = new PaisApiService();

            // Inicialización de comandos y sus manejadores.
            AddCommand = new Command(async () => await OnAdd());
            EditCommand = new Command<PaisResponse>(OnEdit);
            DeleteCommand = new Command<PaisResponse>(OnDelete);
            ViewCommand = new Command<PaisResponse>(OnView);
        }

        // ===========================================================
        // ====================== MÉTODOS PÚBLICOS ===================
        // ===========================================================

        // Carga la lista de países desde la API.
        public async Task LoadPais(bool isBusy)
        {
            IsBusy = isBusy;

            try
            {
                List.Clear();

                // Valida que el usaurio tenga conexion a internet
                bool tieneInternet = await TieneInternetAsync();

                if (!tieneInternet)
                {
                    _ = MostrarToastAsync("Sin conexión a internet.");
                    IsBusy = false;
                    return;
                }

                // Llama al servicio API
                var response = await paisApiService.GetPaisAsync();

                if (response.Any())
                {
                    List = response;
                }
                else
                {
                    _ = MostrarToastAsync("Información" + "No se encontraron países registrados.");
                }
            }
            catch (Exception ex)
            {
                _ = MostrarToastAsync("Error" + $"No se pudo obtener la lista de países.\n{ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        // ===========================================================
        // ===================== HANDLERS (COMANDOS) =================
        // ===========================================================

        // Agregar un nuevo país
        private async Task OnAdd()
        {
            if (IsBusy) return;

            try
            {
                var parameters = new Dictionary<string, object>
                {
                    { "Mode", FormMode.FormModeSelect.Create },
                    { "Pais", new PaisRequest(new PaisResponse()) }
                };

                await GoToAsyncParameters("//PaisFormPage", parameters);
            }
            catch (Exception ex)
            {
                _ = MostrarToastAsync("Error" + $"No se pudo abrir el formulario.\n{ex.Message}");
            }
        }

        // Editar país seleccionado
        private async void OnEdit(PaisResponse pais)
        {
            if (IsBusy || pais == null) return;

            try
            {
                var parameters = new Dictionary<string, object>
                {
                    { "Mode", FormMode.FormModeSelect.Edit },
                    { "Pais", new PaisRequest(pais) }
                };

                await GoToAsyncParameters("//PaisFormPage", parameters);
            }
            catch (Exception ex)
            {
                _ = MostrarToastAsync("Error" + ex.Message);
            }
        }

        // Eliminar país seleccionado
        private async void OnDelete(PaisResponse pais)
        {
            if (IsBusy || pais == null) return;
            IsBusy = true;

            try
            {
                bool confirm = _ = await App.Current.MainPage.DisplayAlert(
                    "Eliminar país",
                    $"¿Deseas eliminar el país '{pais.NombrePais}'?",
                    "Sí", "No");

                if (!confirm)
                {
                    IsBusy = false;
                    return;
                }

                // Valida que el usaurio tenga conexion a internet
                bool tieneInternet = await TieneInternetAsync();

                if (!tieneInternet)
                {
                    _ = MostrarToastAsync("Sin conexión a internet.");
                    IsBusy = false;
                    return;
                }

                var result = await paisApiService.DeletePaisAsync(new PaisRequest(pais));

                if (result)
                {
                    _ = MostrarToastAsync("Éxito" + "País eliminado correctamente.");
                    await LoadPais(true);
                }
                else
                {
                    _ = MostrarToastAsync("Error" + "No se pudo eliminar el país. Intenta nuevamente.");
                }
            }
            catch (Exception ex)
            {
                _ = MostrarToastAsync("Error" + ex.Message);
            }
            finally
            {
                IsBusy = false;
            }
        }

        // Ver detalles del país seleccionado
        private async void OnView(PaisResponse pais)
        {
            if (IsBusy || pais == null) return;

            var parameters = new Dictionary<string, object>
            {
                { "Pais", new PaisRequest(pais) },
                { "TitlePage", $"Departamento de {pais.NombrePais.ToString()}"}
            };

            await GoToAsyncParameters("//DepartamentoPage", parameters);
        }
    }
}
