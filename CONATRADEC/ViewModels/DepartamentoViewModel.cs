using CONATRADEC.Models;
using CONATRADEC.Services;
using System.Collections.ObjectModel;

namespace CONATRADEC.ViewModels
{
    public class DepartamentoViewModel : GlobalService
    {
        private PaisRequest paisRequest = new();
        private string titlePage = string.Empty;
        private ObservableCollection<DepartamentoResponse> list = new();
        private readonly DepartamentoApiService departamentoApiService;
        private bool cargandoDepartamentos;
        private bool eliminandoDepartamento;

        public ObservableCollection<DepartamentoResponse> List
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
                await MostrarToastAsync(
                    "No tiene permisos para ver departamentos.");
                return;
            }

            if (cargandoDepartamentos)
                return;

            cargandoDepartamentos = true;

            if (mostrarIndicadorCarga)
                IsBusy = true;

            try
            {
                var resultado = await departamentoApiService
                    .GetDepartamentosResultAsync(PaisRequest.PaisId);

                if (!resultado.Success)
                {
                    await MostrarToastAsync(resultado.Message);
                    return;
                }

                List = new ObservableCollection<DepartamentoResponse>(
                    (resultado.Data ?? new ObservableCollection<DepartamentoResponse>())
                    .OrderBy(x => x.NombreDepartamento ?? string.Empty));

                if (List.Count == 0)
                    await MostrarToastAsync("No se encontraron departamentos.");
            }
            catch
            {
                await MostrarToastAsync(
                    "Ocurrió un error inesperado al cargar los departamentos.");
            }
            finally
            {
                cargandoDepartamentos = false;

                if (mostrarIndicadorCarga)
                    IsBusy = false;
            }
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

            await GoToAsyncParameters("//DepartamentoFormPage", parameters);
        }

        private async Task OnEditAsync(DepartamentoResponse? departamento)
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
                { "Departamento", new DepartamentoRequest(departamento) }
            };

            await GoToAsyncParameters("//DepartamentoFormPage", parameters);
        }

        private async Task OnViewAsync(DepartamentoResponse? departamento)
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
                { "Departamento", new DepartamentoRequest(departamento) },
                {
                    "TitlePage",
                    $"Municipios de {departamento.NombreDepartamento} - {PaisRequest.NombrePais}"
                }
            };

            await GoToAsyncParameters("//MunicipioPage", parameters);
        }

        private async Task OnDeleteAsync(DepartamentoResponse? departamento)
        {
            if (!CanDelete)
            {
                await MostrarToastAsync("No tiene permisos para eliminar.");
                return;
            }

            if (IsBusy || eliminandoDepartamento || departamento == null)
                return;

            bool confirmar = await App.Current.MainPage.DisplayAlert(
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
