using CONATRADEC.Models;
using CONATRADEC.Services;
using System.Collections.ObjectModel;

namespace CONATRADEC.ViewModels
{
    // ===============================================================
    // ViewModel: MunicipioViewModel
    // ---------------------------------------------------------------
    // - Maneja el listado y acciones de Municipios:
    //   (Agregar / Editar / Eliminar / Ver Detalle)
    // - Hereda de GlobalService para usar:
    //     * IsBusy (control de estado de carga)
    //     * Navegación con Shell (GoToAsyncParameters)
    // - Contexto: trabaja subordinado al Departamento seleccionado
    // ===============================================================
    public class MunicipioViewModel : GlobalService
    {
        // ===========================================================
        // ================== CONTEXTO / ESTADO ======================
        // ===========================================================

        // Departamento que actúa como contexto para cargar municipios.
        private DepartamentoRequest departamentoRequest;

        private PaisRequest paisRequest;

        // Título dinámico para la página (si lo usas en XAML).
        private string titlePage;

        // Lista observable que se enlaza a la UI con CollectionView/ListView.
        private ObservableCollection<MunicipioResponse> list = new();

        // Servicio de API para operaciones CRUD de Municipios.
        private readonly MunicipioApiService municipioApiService;

        // ===========================================================
        // ==================== PROPIEDADES BINDABLE =================
        // ===========================================================

        // Colección observable: la vista se actualiza cuando cambia la referencia.
        public ObservableCollection<MunicipioResponse> List
        {
            get => list;
            set { list = value; OnPropertyChanged(); }
        }

        // Departamento actual (contexto). La vista o la página lo establece vía QueryProperty.
        public DepartamentoRequest DepartamentoRequest
        {
            get => departamentoRequest;
            set { departamentoRequest = value; OnPropertyChanged(); }
        }

        // Título de la página (opcional).
        public string TitlePage
        {
            get => titlePage;
            set { titlePage = value; OnPropertyChanged(); }
        }

        // ===========================================================
        // ======================== COMANDOS ==========================
        // ===========================================================

        // Comando para navegar hacia atras.
        public Command ReturnCommand { get; }

        // Comando para navegar al formulario en modo Crear.
        public Command AddCommand { get; }

        // Comando para navegar al formulario en modo Editar (con ítem seleccionado).
        public Command EditCommand { get; }

        // Comando para eliminar un municipio (con confirmación).
        public Command DeleteCommand { get; }

        // Comando para navegar al formulario en modo Ver (solo lectura).
        public Command ViewCommand { get; }
        public PaisRequest PaisRequest { get => paisRequest; set => paisRequest = value; }

        // ===========================================================
        // ======================= CONSTRUCTOR =======================
        // ===========================================================

        public MunicipioViewModel()
        {
            // Instancia de servicio API (puedes inyectarlo por DI más adelante).
            municipioApiService = new MunicipioApiService();

            // Inicializamos los comandos con sus handlers.
            ReturnCommand = new Command(async () => await GoToAsyncParameters("//DepartamentoPage", new Dictionary<string, object>
            {
                { "Pais", PaisRequest },
                { "TitlePage", $"Departamento de {PaisRequest.NombrePais.ToString()}"}
            }));
            AddCommand = new Command(async () => await OnAdd());
            EditCommand = new Command<MunicipioResponse>(OnEdit);
            DeleteCommand = new Command<MunicipioResponse>(OnDelete);
            ViewCommand = new Command<MunicipioResponse>(OnView);
        }

        // ===========================================================
        // ====================== CARGA DE DATOS ======================
        // ===========================================================

        /// <summary>
        /// Carga/recarga la lista de municipios del Departamento actual.
        /// </summary>
        /// <param name="isBusy">Controla el ActivityIndicator en la UI</param>
        public async Task LoadMunicipio(bool isBusy)
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

                // Llama al endpoint que devuelve los municipios por departamento.
                var response = await municipioApiService.GetMunicipiosAsync(DepartamentoRequest.DepartamentoId);
  
                if (response.Any())
                {
                    List = response;
                }
                else
                {
                    _ = MostrarToastAsync("No se encontraron municipios.");
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

        // ===========================================================
        // ===================== HANDLERS (COMANDOS) =================
        // ===========================================================

        /// <summary>
        /// Handler: Agregar nuevo municipio (navega al formulario en modo Create).
        /// </summary>
        private async Task OnAdd()
        {
            if (IsBusy) return;
            try
            {
                // Parámetros para el formulario de Municipio.
                var parameters = new Dictionary<string, object>
                {
                    { "Mode", FormMode.FormModeSelect.Create },
                    { "Pais", PaisRequest },
                    { "Departamento", DepartamentoRequest },
                    { "Municipio", new MunicipioRequest(new MunicipioResponse()) } // objeto vacío
                };

                await GoToAsyncParameters("//MunicipioFormPage", parameters);
            }
            catch (Exception ex)
            {
                _ = MostrarToastAsync("Error" + ex.Message);
            }
        }

        /// <summary>
        /// Handler: Editar municipio (navega al formulario en modo Edit con el ítem).
        /// </summary>
        private async void OnEdit(MunicipioResponse municipio)
        {
            if (IsBusy || municipio == null) return;

            try
            {
                var parameters = new Dictionary<string, object>
                {
                    { "Mode", FormMode.FormModeSelect.Edit },
                    { "Pais", PaisRequest },
                    { "Departamento", DepartamentoRequest },
                    { "Municipio", new MunicipioRequest(municipio) }
                };

                await GoToAsyncParameters("//MunicipioFormPage", parameters);
            }
            catch (Exception ex)
            {
                _ = MostrarToastAsync("Error" + ex.Message);
            }
        }

        /// <summary>
        /// Handler: Eliminar municipio (pide confirmación y llama a la API).
        /// </summary>
        private async void OnDelete(MunicipioResponse municipio)
        {
            if (IsBusy || municipio == null) return;

            IsBusy = true;
            try
            {
                bool confirm = _ = await App.Current.MainPage.DisplayAlert(
                    "Eliminar",
                    $"¿Deseas eliminar el municipio '{municipio.NombreMunicipio}'?",
                    "Sí", "No");

                if (!confirm) return;

                // Valida que el usaurio tenga conexion a internet
                bool tieneInternet = await TieneInternetAsync();

                if (!tieneInternet)
                {
                    _ = MostrarToastAsync("Sin conexión a internet.");
                    IsBusy = false;
                    return;
                }

                var response = await municipioApiService.DeleteMunicipioAsync(new MunicipioRequest(municipio));

                if (response)
                {
                    _ = MostrarToastAsync("Éxito" + "Municipio eliminado.");
                    await LoadMunicipio(true); // recargar lista
                }
                else
                {
                    _ = MostrarToastAsync("Error" + "No se pudo eliminar el municipio.");
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

        /// <summary>
        /// Handler: Ver detalle de municipio (navega al formulario en modo View).
        /// </summary>
        private async void OnView(MunicipioResponse municipio)
        {
            if (IsBusy || municipio == null) return;

            var parameters = new Dictionary<string, object>
            {
                { "Mode", FormMode.FormModeSelect.View },
                { "Pais", PaisRequest },
                { "Departamento", DepartamentoRequest },
                { "Municipio", new MunicipioRequest(municipio) }
            };

            await GoToAsyncParameters("//MunicipioFormPage", parameters);
        }
    }
}
