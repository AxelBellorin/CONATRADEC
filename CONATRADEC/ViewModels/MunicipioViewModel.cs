using CONATRADEC.Models;
using CONATRADEC.Services;
using System.Collections.ObjectModel;
using System.Threading;

namespace CONATRADEC.ViewModels
{
    public class MunicipioViewModel : GlobalService
    {
        private DepartamentoRequest departamentoRequest = new();
        private PaisRequest paisRequest = new();
        private string titlePage = string.Empty;
        private ObservableCollection<MunicipioResponse> list = new();
        private readonly MunicipioApiService municipioApiService;
        private bool eliminandoMunicipio;
        private long versionCargaMunicipios;

        public ObservableCollection<MunicipioResponse> List
        {
            get => list;
            set
            {
                if (ReferenceEquals(list, value))
                    return;

                list = value ?? new ObservableCollection<MunicipioResponse>();
                OnPropertyChanged();
            }
        }

        public DepartamentoRequest DepartamentoRequest
        {
            get => departamentoRequest;
            set
            {
                departamentoRequest =
                    value ?? new DepartamentoRequest();

                OnPropertyChanged();

                // Descarta cualquier carga perteneciente al departamento
                // anteriormente visualizado y limpia su colección.
                Interlocked.Increment(ref versionCargaMunicipios);
                List = new ObservableCollection<MunicipioResponse>();
            }
        }

        public PaisRequest PaisRequest
        {
            get => paisRequest;
            set
            {
                paisRequest = value ?? new PaisRequest();
                OnPropertyChanged();

                // También se invalida al cambiar el país porque Shell puede
                // aplicar las QueryProperty en un orden diferente.
                Interlocked.Increment(ref versionCargaMunicipios);
                List = new ObservableCollection<MunicipioResponse>();
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

        public MunicipioViewModel(
            MunicipioApiService municipioApiService)
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
                List = new ObservableCollection<MunicipioResponse>();

                await MostrarToastAsync(
                    "No tiene permisos para ver municipios.");
                return;
            }

            int? departamentoId =
                DepartamentoRequest.DepartamentoId;

            if (!departamentoId.HasValue ||
                departamentoId.Value <= 0)
            {
                // Shell todavía puede estar aplicando QueryProperty.
                // La página volverá a intentar cuando llegue DepartamentoId.
                List = new ObservableCollection<MunicipioResponse>();
                return;
            }

            int idSeleccionado = departamentoId.Value;

            // La versión evita que una respuesta tardía de otro departamento
            // reemplace la colección del departamento actualmente seleccionado.
            long versionActual =
                Interlocked.Increment(ref versionCargaMunicipios);

            if (mostrarIndicadorCarga)
                IsBusy = true;

            try
            {
                var resultado = await municipioApiService
                    .GetMunicipiosResultAsync(idSeleccionado);

                if (!EsCargaActual(
                        versionActual,
                        idSeleccionado))
                {
                    return;
                }

                if (!resultado.Success)
                {
                    List = new ObservableCollection<MunicipioResponse>();

                    await MostrarToastAsync(resultado.Message);
                    return;
                }

                List = new ObservableCollection<MunicipioResponse>(
                    (resultado.Data ??
                     new ObservableCollection<MunicipioResponse>())
                    .OrderBy(
                        municipio =>
                            municipio.NombreMunicipio ??
                            string.Empty));

                if (List.Count == 0)
                {
                    string nombreDepartamento =
                        string.IsNullOrWhiteSpace(
                            DepartamentoRequest.NombreDepartamento)
                            ? "seleccionado"
                            : DepartamentoRequest.NombreDepartamento;

                    await MostrarInformacionAsync(
                        $"El departamento '{nombreDepartamento}' todavía no tiene municipios registrados.");
                }
            }
            catch
            {
                if (!EsCargaActual(
                        versionActual,
                        idSeleccionado))
                {
                    return;
                }

                List = new ObservableCollection<MunicipioResponse>();

                await MostrarToastAsync(
                    "Ocurrió un error inesperado al cargar los municipios.");
            }
            finally
            {
                if (mostrarIndicadorCarga &&
                    EsCargaActual(
                        versionActual,
                        idSeleccionado))
                {
                    IsBusy = false;
                }
            }
        }

        private bool EsCargaActual(
            long version,
            int departamentoId)
        {
            return Volatile.Read(ref versionCargaMunicipios) == version &&
                   DepartamentoRequest.DepartamentoId ==
                   departamentoId;
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

            await GoToAsyncParameters(
                "//DepartamentoPage",
                parameters);
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
                {
                    "Municipio",
                    new MunicipioRequest(new MunicipioResponse())
                }
            };

            await GoToAsyncParameters(
                "//MunicipioFormPage",
                parameters);
        }

        private async Task OnEditAsync(
            MunicipioResponse? municipio)
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
                {
                    "Municipio",
                    new MunicipioRequest(municipio)
                }
            };

            await GoToAsyncParameters(
                "//MunicipioFormPage",
                parameters);
        }

        private async Task OnViewAsync(
            MunicipioResponse? municipio)
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
                {
                    "Municipio",
                    new MunicipioRequest(municipio)
                }
            };

            await GoToAsyncParameters(
                "//MunicipioFormPage",
                parameters);
        }

        private async Task OnDeleteAsync(
            MunicipioResponse? municipio)
        {
            if (!CanDelete)
            {
                await MostrarToastAsync("No tiene permisos para eliminar.");
                return;
            }

            if (IsBusy ||
                eliminandoMunicipio ||
                municipio == null)
            {
                return;
            }

            bool confirmar =
                await App.Current.MainPage.DisplayAlert(
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
