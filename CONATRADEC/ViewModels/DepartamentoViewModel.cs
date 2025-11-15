using CONATRADEC.Models;
using CONATRADEC.Services;
using System.Collections.ObjectModel;

namespace CONATRADEC.ViewModels
{
    // ===============================================================
    // ViewModel: DepartamentoViewModel
    // - Maneja el listado y acciones (Agregar/Editar/Eliminar/Ver)
    // - Hereda de GlobalService para IsBusy y navegación con Shell
    // ===============================================================
    public class DepartamentoViewModel : GlobalService
    {
        // Objetos
        private PaisRequest paisRequest;
        private string titlePage;

        // Lista observable para la UI
        private ObservableCollection<DepartamentoResponse> list = new();
        public ObservableCollection<DepartamentoResponse> List
        {
            get => list;
            set { list = value; OnPropertyChanged(); }
        }

        // Servicio API
        private readonly DepartamentoApiService departamentoApiService;

        // Comandos

        // Comando para navegar hacia atras.
        public Command ReturnCommand { get; }
        public Command AddCommand { get; }
        public Command EditCommand { get; }
        public Command DeleteCommand { get; }
        public Command ViewCommand { get; }
        public PaisRequest PaisRequest { get => paisRequest; set { paisRequest = value; OnPropertyChanged(); } }

        public string TitlePage { get => titlePage; set { titlePage = value; OnPropertyChanged(); } }

        public DepartamentoViewModel()
        {
            departamentoApiService = new DepartamentoApiService();
            ReturnCommand = new Command(async () => await GoToAsyncParameters("//PaisPage"));
            AddCommand = new Command(async () => await OnAdd());
            EditCommand = new Command<DepartamentoResponse>(OnEdit);
            DeleteCommand = new Command<DepartamentoResponse>(OnDelete);
            ViewCommand = new Command<DepartamentoResponse>(OnView);
        }

        // Carga/recarga
        public async Task LoadDepartamento(bool isBusy)
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

                var response = await departamentoApiService.GetDepartamentosAsync(PaisRequest.PaisId);
 
                // Revisar aca
                if (response.Any())
                {
                    List = response;
                }
                else
                {
                    _ = MostrarToastAsync("No se encontraron departamentos.");
                }
            }
            catch (Exception ex)
            {
                _ = MostrarToastAsync("Error " + ex.Message);
            }
            finally { IsBusy = false; }
        }

        // Agregar
        private async Task OnAdd()
        {
            if (IsBusy) return;
            try
            {
                var parameters = new Dictionary<string, object>
                {
                    { "Mode", FormMode.FormModeSelect.Create },
                    { "Pais", PaisRequest},
                    { "Departamento", new DepartamentoRequest(new DepartamentoResponse()) }
                };
                await GoToAsyncParameters("//DepartamentoFormPage", parameters);
            }
            catch (Exception ex)
            {
                _ = MostrarToastAsync("Error " + ex.Message);
            }
        }

        // Editar
        private async void OnEdit(DepartamentoResponse departamento)
        {
            if (IsBusy || departamento == null) return;
            try
            {
                var parameters = new Dictionary<string, object>
                {
                    { "Pais", PaisRequest},
                    { "Departamento", new DepartamentoRequest(departamento) },
                    { "TitlePage", $"Municipios de {departamento.NombreDepartamento.ToString()} - {PaisRequest.NombrePais.ToString()}"}
                };
                await GoToAsyncParameters("//DepartamentoFormPage", parameters);
            }
            catch (Exception ex)
            {
                _ = MostrarToastAsync("Error " + ex.Message);
            }
        }

        // Eliminar
        private async void OnDelete(DepartamentoResponse dpto)
        {
            if (IsBusy || dpto == null) return;
            IsBusy = true;
            try
            {
                bool confirm = _ = await App.Current.MainPage.DisplayAlert(
                    "Eliminar", $"¿Deseas eliminar el departamento '{dpto.NombreDepartamento}'?", "Sí", "No");

                if (!confirm) return;

                // Valida que el usaurio tenga conexion a internet
                bool tieneInternet = await TieneInternetAsync();

                if (!tieneInternet)
                {
                    _ = MostrarToastAsync("Sin conexión a internet.");
                    IsBusy = false;
                    return;
                }

                var response = await departamentoApiService.DeleteDepartamentoAsync(new DepartamentoRequest(dpto));

                if (response)
                {
                    _ = MostrarToastAsync("Departamento eliminado.");
                    await LoadDepartamento(true);
                }
                else
                {
                    _ = MostrarToastAsync("No se pudo eliminar el departamento.");
                }
            }
            catch (Exception ex)
            {
                _ = MostrarToastAsync("Error " + ex.Message);
            }
            finally { IsBusy = false; }
        }

        // Ver detalle
        private async void OnView(DepartamentoResponse departamento)
        {
            if (IsBusy || departamento == null) return;

            var parameters = new Dictionary<string, object>
            {
                { "Pais", PaisRequest},
                { "Departamento", new DepartamentoRequest(departamento) },
                { "TitlePage", $"Municipios de {departamento.NombreDepartamento.ToString()} - {PaisRequest.NombrePais.ToString()}"}
            };
            await GoToAsyncParameters("//MunicipioPage", parameters);
        }
    }
}
