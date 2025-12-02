using CONATRADEC.Models;
using CONATRADEC.Services;
using System.Collections.ObjectModel;

namespace CONATRADEC.ViewModels
{
    public class PaisViewModel : GlobalService
    {
        private ObservableCollection<PaisResponse> list = new();
        private readonly PaisApiService paisApiService;

        public ObservableCollection<PaisResponse> List
        {
            get => list;
            set { list = value; OnPropertyChanged(); }
        }

        public Command AddCommand { get; }
        public Command EditCommand { get; }
        public Command DeleteCommand { get; }
        public Command ViewCommand { get; }

        public PaisViewModel()
        {
            paisApiService = new PaisApiService();

            AddCommand = new Command(async () => await OnAdd());
            EditCommand = new Command<PaisResponse>(OnEdit);
            DeleteCommand = new Command<PaisResponse>(OnDelete);
            ViewCommand = new Command<PaisResponse>(OnView);
        }

        // ---------------------------------------------------------
        //   CARGAR LISTA DE PAÍSES
        // ---------------------------------------------------------
        public async Task LoadPais(bool isBusy)
        {
            if (!CanView)
            {
                await MostrarToastAsync("No tiene permisos para ver países.");
                return;
            }

            IsBusy = isBusy;

            try
            {
                List.Clear();

                bool tieneInternet = await TieneInternetAsync();
                if (!tieneInternet)
                {
                    _ = MostrarToastAsync("Sin conexión a internet.");
                    return;
                }

                var response = await paisApiService.GetPaisAsync();

                if (response.Any())
                    List = response;
                else
                    _ = MostrarToastAsync("No se encontraron países registrados.");
            }
            catch (Exception ex)
            {
                _ = MostrarToastAsync("Error al cargar países: " + ex.Message);
            }
            finally
            {
                IsBusy = false;
            }
        }

        // ---------------------------------------------------------
        //   AGREGAR
        // ---------------------------------------------------------
        private async Task OnAdd()
        {
            if (!CanAdd)
            {
                await MostrarToastAsync("No tiene permisos para agregar países.");
                return;
            }

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
                _ = MostrarToastAsync("Error al abrir el formulario: " + ex.Message);
            }
        }

        // ---------------------------------------------------------
        //   EDITAR
        // ---------------------------------------------------------
        private async void OnEdit(PaisResponse pais)
        {
            if (!CanEdit)
            {
                await MostrarToastAsync("No tiene permisos para editar países.");
                return;
            }

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
                _ = MostrarToastAsync("Error al abrir el formulario: " + ex.Message);
            }
        }

        // ---------------------------------------------------------
        //   ELIMINAR
        // ---------------------------------------------------------
        private async void OnDelete(PaisResponse pais)
        {
            if (!CanDelete)
            {
                await MostrarToastAsync("No tiene permisos para eliminar países.");
                return;
            }

            if (IsBusy || pais == null) return;

            IsBusy = true;

            try
            {
                bool confirm = await App.Current.MainPage.DisplayAlert(
                    "Eliminar país",
                    $"¿Deseas eliminar el país '{pais.NombrePais}'?",
                    "Sí", "No");

                if (!confirm) return;

                bool tieneInternet = await TieneInternetAsync();
                if (!tieneInternet)
                {
                    _ = MostrarToastAsync("Sin conexión a internet.");
                    return;
                }

                var result = await paisApiService.DeletePaisAsync(new PaisRequest(pais));

                if (result)
                {
                    _ = MostrarToastAsync("País eliminado correctamente.");
                    await LoadPais(true);
                }
                else
                {
                    _ = MostrarToastAsync("No se pudo eliminar el país.");
                }
            }
            catch (Exception ex)
            {
                _ = MostrarToastAsync("Error al eliminar: " + ex.Message);
            }
            finally
            {
                IsBusy = false;
            }
        }

        // ---------------------------------------------------------
        //   VER DETALLES
        // ---------------------------------------------------------
        private async void OnView(PaisResponse pais)
        {
            if (!CanView)
            {
                await MostrarToastAsync("No tiene permisos para ver detalles.");
                return;
            }

            if (IsBusy || pais == null) return;

            var parameters = new Dictionary<string, object>
            {
                { "Pais", new PaisRequest(pais) },
                { "TitlePage", $"Departamento de {pais.NombrePais}" }
            };

            await GoToAsyncParameters("//DepartamentoPage", parameters);
        }
    }
}
