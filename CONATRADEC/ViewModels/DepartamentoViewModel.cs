using CONATRADEC.Models;
using CONATRADEC.Services;
using System.Collections.ObjectModel;
using System.Threading;

namespace CONATRADEC.ViewModels
{
    public class DepartamentoViewModel : GlobalService
    {
        private PaisRequest paisRequest = new();
        private string titlePage = string.Empty;
        private ObservableCollection<DepartamentoResponse> list = new();
        private readonly DepartamentoApiService departamentoApiService;
        private bool eliminandoDepartamento;
        private long versionCargaDepartamentos;

        public ObservableCollection<DepartamentoResponse> List
        {
            get => list;
            set
            {
                if (ReferenceEquals(list, value))
                    return;

                list = value ?? new ObservableCollection<DepartamentoResponse>();
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

                // La página puede reutilizar el mismo ViewModel al navegar.
                // Invalidamos cualquier consulta anterior y limpiamos de inmediato
                // los departamentos del país previamente visualizado.
                Interlocked.Increment(ref versionCargaDepartamentos);
                List = new ObservableCollection<DepartamentoResponse>();
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

        public DepartamentoViewModel()
            : this(new DepartamentoApiService())
        {
        }

        public DepartamentoViewModel(
            DepartamentoApiService departamentoApiService)
        {
            this.departamentoApiService = departamentoApiService
                ?? throw new ArgumentNullException(nameof(departamentoApiService));

            ReturnCommand = new Command(
                async () => await GoToAsyncParameters("//PaisPage"));

            AddCommand = new Command(
                async () => await OnAddAsync());

            EditCommand = new Command<DepartamentoResponse>(
                async departamento => await OnEditAsync(departamento));

            DeleteCommand = new Command<DepartamentoResponse>(
                async departamento => await OnDeleteAsync(departamento));

            ViewCommand = new Command<DepartamentoResponse>(
                async departamento => await OnViewAsync(departamento));
        }

        public async Task LoadDepartamento(bool mostrarIndicadorCarga)
        {
            if (!CanView)
            {
                List = new ObservableCollection<DepartamentoResponse>();

                await MostrarToastAsync(
                    "No tiene permisos para ver departamentos.");
                return;
            }

            int paisId = PaisRequest.PaisId;

            if (paisId <= 0)
            {
                // Shell todavía puede estar aplicando QueryProperty.
                // La página volverá a intentar la carga cuando llegue el PaisId.
                List = new ObservableCollection<DepartamentoResponse>();
                return;
            }

            // Cada carga recibe una versión. Si el usuario cambia de país antes
            // de que la solicitud termine, la respuesta anterior se descarta.
            long versionActual =
                Interlocked.Increment(ref versionCargaDepartamentos);

            if (mostrarIndicadorCarga)
                IsBusy = true;

            try
            {
                var resultado = await departamentoApiService
                    .GetDepartamentosResultAsync(paisId);

                if (!EsCargaActual(versionActual, paisId))
                    return;

                if (!resultado.Success)
                {
                    List = new ObservableCollection<DepartamentoResponse>();

                    await MostrarToastAsync(resultado.Message);
                    return;
                }

                List = new ObservableCollection<DepartamentoResponse>(
                    (resultado.Data ??
                     new ObservableCollection<DepartamentoResponse>())
                    .OrderBy(
                        departamento =>
                            departamento.NombreDepartamento ??
                            string.Empty));

                if (List.Count == 0)
                {
                    string nombrePais =
                        string.IsNullOrWhiteSpace(PaisRequest.NombrePais)
                            ? "seleccionado"
                            : PaisRequest.NombrePais;

                    await MostrarInformacionAsync(
                        $"El país '{nombrePais}' todavía no tiene departamentos registrados.");
                }
            }
            catch
            {
                if (!EsCargaActual(versionActual, paisId))
                    return;

                List = new ObservableCollection<DepartamentoResponse>();

                await MostrarToastAsync(
                    "Ocurrió un error inesperado al cargar los departamentos.");
            }
            finally
            {
                // Una solicitud anterior no puede apagar el indicador de una
                // solicitud más reciente.
                if (mostrarIndicadorCarga &&
                    EsCargaActual(versionActual, paisId))
                {
                    IsBusy = false;
                }
            }
        }

        private bool EsCargaActual(
            long version,
            int paisId)
        {
            return Volatile.Read(ref versionCargaDepartamentos) == version &&
                   PaisRequest.PaisId == paisId;
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
                {
                    "Departamento",
                    new DepartamentoRequest(new DepartamentoResponse())
                }
            };

            await GoToAsyncParameters(
                "//DepartamentoFormPage",
                parameters);
        }

        private async Task OnEditAsync(
            DepartamentoResponse? departamento)
        {
            if (!CanEdit)
            {
                await MostrarToastAsync("No tiene permisos para editar.");
                return;
            }

            if (IsBusy || departamento == null)
                return;

            var parameters = new Dictionary<string, object>
            {
                { "Pais", PaisRequest },
                { "Mode", FormMode.FormModeSelect.Edit },
                {
                    "Departamento",
                    new DepartamentoRequest(departamento)
                }
            };

            await GoToAsyncParameters(
                "//DepartamentoFormPage",
                parameters);
        }

        private async Task OnViewAsync(
            DepartamentoResponse? departamento)
        {
            if (!CanView)
            {
                await MostrarToastAsync(
                    "No tiene permisos para ver detalles.");
                return;
            }

            if (IsBusy || departamento == null)
                return;

            var parameters = new Dictionary<string, object>
            {
                { "Pais", PaisRequest },
                {
                    "Departamento",
                    new DepartamentoRequest(departamento)
                },
                {
                    "TitlePage",
                    $"Municipios de {departamento.NombreDepartamento} - {PaisRequest.NombrePais}"
                }
            };

            await GoToAsyncParameters(
                "//MunicipioPage",
                parameters);
        }

        private async Task OnDeleteAsync(
            DepartamentoResponse? departamento)
        {
            if (!CanDelete)
            {
                await MostrarToastAsync("No tiene permisos para eliminar.");
                return;
            }

            if (IsBusy ||
                eliminandoDepartamento ||
                departamento == null)
            {
                return;
            }

            bool confirmar =
                await App.Current.MainPage.DisplayAlert(
                    "Eliminar departamento",
                    $"¿Desea eliminar el departamento '{departamento.NombreDepartamento}'?",
                    "Sí",
                    "No");

            if (!confirmar)
                return;

            eliminandoDepartamento = true;
            IsBusy = true;

            try
            {
                var resultado = await departamentoApiService
                    .DeleteDepartamentoResultAsync(
                        new DepartamentoRequest(departamento));

                if (!resultado.Success)
                {
                    await MostrarToastAsync(resultado.Message);
                    return;
                }

                List.Remove(departamento);

                await MostrarToastAsync(
                    string.IsNullOrWhiteSpace(resultado.Message)
                        ? "Departamento eliminado correctamente."
                        : resultado.Message);
            }
            catch
            {
                await MostrarToastAsync(
                    "Ocurrió un error inesperado al eliminar el departamento.");
            }
            finally
            {
                eliminandoDepartamento = false;
                IsBusy = false;
            }
        }
    }
}
