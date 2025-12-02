using CONATRADEC.Models;
using CONATRADEC.Services;
using System.Collections.ObjectModel;

namespace CONATRADEC.ViewModels
{
    public class MunicipioViewModel : GlobalService
    {
        private DepartamentoRequest departamentoRequest;
        private PaisRequest paisRequest;
        private string titlePage;
        private ObservableCollection<MunicipioResponse> list = new();

        private readonly MunicipioApiService municipioApiService;

        public ObservableCollection<MunicipioResponse> List
        {
            get => list;
            set { list = value; OnPropertyChanged(); }
        }

        public DepartamentoRequest DepartamentoRequest
        {
            get => departamentoRequest;
            set { departamentoRequest = value; OnPropertyChanged(); }
        }

        public PaisRequest PaisRequest
        {
            get => paisRequest;
            set { paisRequest = value; OnPropertyChanged(); }
        }

        public string TitlePage
        {
            get => titlePage;
            set { titlePage = value; OnPropertyChanged(); }
        }

        public Command ReturnCommand { get; }
        public Command AddCommand { get; }
        public Command EditCommand { get; }
        public Command DeleteCommand { get; }
        public Command ViewCommand { get; }

        public MunicipioViewModel()
        {
            municipioApiService = new MunicipioApiService();

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

        public async Task LoadMunicipio(bool isBusy)
        {
            if (!CanView)
            {
                await MostrarToastAsync("No tiene permisos para ver municipios.");
                return;
            }

            IsBusy = isBusy;
            try
            {
                List.Clear();

                if (!await TieneInternetAsync())
                {
                    _ = MostrarToastAsync("Sin conexión a internet.");
                    return;
                }

                var response = await municipioApiService.GetMunicipiosAsync(DepartamentoRequest.DepartamentoId);
                List = response.Any() ? response : new ObservableCollection<MunicipioResponse>();
            }
            catch (Exception ex)
            {
                _ = MostrarToastAsync("Error: " + ex.Message);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task OnAdd()
        {
            if (!CanAdd)
            {
                await MostrarToastAsync("No tiene permisos para agregar.");
                return;
            }

            var parameters = new Dictionary<string, object>
            {
                { "Mode", FormMode.FormModeSelect.Create },
                { "Pais", PaisRequest },
                { "Departamento", DepartamentoRequest },
                { "Municipio", new MunicipioRequest(new MunicipioResponse()) }
            };

            await GoToAsyncParameters("//MunicipioFormPage", parameters);
        }

        private async void OnEdit(MunicipioResponse municipio)
        {
            if (!CanEdit)
            {
                await MostrarToastAsync("No tiene permisos para editar.");
                return;
            }

            var parameters = new Dictionary<string, object>
            {
                { "Mode", FormMode.FormModeSelect.Edit },
                { "Pais", PaisRequest },
                { "Departamento", DepartamentoRequest },
                { "Municipio", new MunicipioRequest(municipio) }
            };

            await GoToAsyncParameters("//MunicipioFormPage", parameters);
        }

        private async void OnDelete(MunicipioResponse municipio)
        {
            if (!CanDelete)
            {
                await MostrarToastAsync("No tiene permisos para eliminar.");
                return;
            }

            IsBusy = true;
            try
            {
                bool confirm = await App.Current.MainPage.DisplayAlert(
                    "Eliminar",
                    $"¿Deseas eliminar el municipio '{municipio.NombreMunicipio}'?",
                    "Sí", "No");

                if (!confirm) return;

                if (!await TieneInternetAsync())
                {
                    await MostrarToastAsync("Sin conexión a internet.");
                    return;
                }

                var ok = await municipioApiService.DeleteMunicipioAsync(new MunicipioRequest(municipio));

                if (ok)
                {
                    await MostrarToastAsync("Municipio eliminado.");
                    await LoadMunicipio(true);
                }
                else
                {
                    await MostrarToastAsync("No se pudo eliminar.");
                }
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async void OnView(MunicipioResponse municipio)
        {
            if (!CanView)
            {
                await MostrarToastAsync("No tiene permisos para ver detalles.");
                return;
            }

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
