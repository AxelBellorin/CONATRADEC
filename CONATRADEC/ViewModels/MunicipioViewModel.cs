using CONATRADEC.Models;
using CONATRADEC.Services;
using System.Collections.ObjectModel;

namespace CONATRADEC.ViewModels
{
    public class MunicipioViewModel : GlobalService
    {
        private DepartamentoRequest departamentoRequest = new();
        private PaisRequest paisRequest = new();
        private string titlePage = string.Empty;
        private ObservableCollection<MunicipioResponse> list = new();
        private readonly MunicipioApiService municipioApiService;
        private bool cargandoMunicipios;
        private bool eliminandoMunicipio;

        public ObservableCollection<MunicipioResponse> List
        {
            get => list;
            set
            {
                if (ReferenceEquals(list, value))
                    return;

                list = value;
                OnPropertyChanged();
            }
        }

        public DepartamentoRequest DepartamentoRequest
        {
            get => departamentoRequest;
            set
            {
                departamentoRequest = value ?? new DepartamentoRequest();
                OnPropertyChanged();
            }
        }

        public PaisRequest PaisRequest
        {
            get => paisRequest;
            set
            {
                paisRequest = value ?? new PaisRequest();
                OnPropertyChanged();
            }
        }

        public string TitlePage
        {
            get => titlePage;
            set
            {
                titlePage = value ?? string.Empty;
                OnPropertyChanged();
            }
        }

        public Command ReturnCommand { get; }
        public Command AddCommand { get; }
        public Command EditCommand { get; }
        public Command DeleteCommand { get; }
        public Command ViewCommand { get; }

        public MunicipioViewModel()
            : this(new MunicipioApiService())
        {
        }

        public MunicipioViewModel(MunicipioApiService municipioApiService)
        {
            this.municipioApiService = municipioApiService
                ?? throw new ArgumentNullException(nameof(municipioApiService));

            ReturnCommand = new Command(
                async () => await ReturnToDepartamentoAsync());

            AddCommand = new Command(
                async () => await OnAddAsync());

            EditCommand = new Command<MunicipioResponse>(
                async municipio => await OnEditAsync(municipio));

            DeleteCommand = new Command<MunicipioResponse>(
                async municipio => await OnDeleteAsync(municipio));

            ViewCommand = new Command<MunicipioResponse>(
                async municipio => await OnViewAsync(municipio));
        }

        public async Task LoadMunicipio(bool mostrarIndicadorCarga)
        {
            if (!CanView)
            {
                await MostrarToastAsync(
                    "No tiene permisos para ver municipios.");
                return;
            }

            if (cargandoMunicipios)
                return;

            cargandoMunicipios = true;

            if (mostrarIndicadorCarga)
                IsBusy = true;

            try
            {
                var resultado = await municipioApiService
                    .GetMunicipiosResultAsync(
                        DepartamentoRequest.DepartamentoId);

                if (!resultado.Success)
                {
                    await MostrarToastAsync(resultado.Message);
                    return;
                }

                List = new ObservableCollection<MunicipioResponse>(
                    (resultado.Data ?? new ObservableCollection<MunicipioResponse>())
                    .OrderBy(x => x.NombreMunicipio ?? string.Empty));

                if (List.Count == 0)
                    await MostrarToastAsync("No se encontraron municipios.");
            }
            catch
            {
                await MostrarToastAsync(
                    "Ocurrió un error inesperado al cargar los municipios.");
            }
            finally
            {
                cargandoMunicipios = false;

                if (mostrarIndicadorCarga)
                    IsBusy = false;
            }
        }

        private async Task ReturnToDepartamentoAsync()
        {
            if (IsBusy)
                return;

            var parameters = new Dictionary<string, object>
            {
                { "Pais", PaisRequest },
                {
                    "TitlePage",
                    $"Departamentos de {PaisRequest.NombrePais}"
                }
            };

            await GoToAsyncParameters("//DepartamentoPage", parameters);
        }

        private async Task OnAddAsync()
        {
            if (!CanAdd)
            {
                await MostrarToastAsync("No tiene permisos para agregar.");
                return;
            }

            if (IsBusy)
                return;

            var parameters = new Dictionary<string, object>
            {
                { "Mode", FormMode.FormModeSelect.Create },
                { "Pais", PaisRequest },
                { "Departamento", DepartamentoRequest },
                { "Municipio", new MunicipioRequest(new MunicipioResponse()) }
            };

            await GoToAsyncParameters("//MunicipioFormPage", parameters);
        }

        private async Task OnEditAsync(MunicipioResponse? municipio)
        {
            if (!CanEdit)
            {
                await MostrarToastAsync("No tiene permisos para editar.");
                return;
            }

            if (IsBusy || municipio == null)
                return;

            var parameters = new Dictionary<string, object>
            {
                { "Mode", FormMode.FormModeSelect.Edit },
                { "Pais", PaisRequest },
                { "Departamento", DepartamentoRequest },
                { "Municipio", new MunicipioRequest(municipio) }
            };

            await GoToAsyncParameters("//MunicipioFormPage", parameters);
        }

        private async Task OnViewAsync(MunicipioResponse? municipio)
        {
            if (!CanView)
            {
                await MostrarToastAsync(
                    "No tiene permisos para ver detalles.");
                return;
            }

            if (IsBusy || municipio == null)
                return;

            var parameters = new Dictionary<string, object>
            {
                { "Mode", FormMode.FormModeSelect.View },
                { "Pais", PaisRequest },
                { "Departamento", DepartamentoRequest },
                { "Municipio", new MunicipioRequest(municipio) }
            };

            await GoToAsyncParameters("//MunicipioFormPage", parameters);
        }

        private async Task OnDeleteAsync(MunicipioResponse? municipio)
        {
            if (!CanDelete)
            {
                await MostrarToastAsync("No tiene permisos para eliminar.");
                return;
            }

            if (IsBusy || eliminandoMunicipio || municipio == null)
                return;

            bool confirmar = await App.Current.MainPage.DisplayAlert(
                "Eliminar municipio",
                $"¿Desea eliminar el municipio '{municipio.NombreMunicipio}'?",
                "Sí",
                "No");

            if (!confirmar)
                return;

            eliminandoMunicipio = true;
            IsBusy = true;

            try
            {
                var resultado = await municipioApiService
                    .DeleteMunicipioResultAsync(
                        new MunicipioRequest(municipio));

                if (!resultado.Success)
                {
                    await MostrarToastAsync(resultado.Message);
                    return;
                }

                List.Remove(municipio);
                await MostrarToastAsync(
                    string.IsNullOrWhiteSpace(resultado.Message)
                        ? "Municipio eliminado correctamente."
                        : resultado.Message);
            }
            catch
            {
                await MostrarToastAsync(
                    "Ocurrió un error inesperado al eliminar el municipio.");
            }
            finally
            {
                eliminandoMunicipio = false;
                IsBusy = false;
            }
        }
    }
}
