using CONATRADEC.Models;
using CONATRADEC.Services;
using System.Collections.ObjectModel;

namespace CONATRADEC.ViewModels
{
    public class PaisViewModel : GlobalService
    {
        private ObservableCollection<PaisResponse> list = new();
        private readonly PaisApiService paisApiService;
        private bool cargandoPaises;
        private bool eliminandoPais;

        public ObservableCollection<PaisResponse> List
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

        public Command AddCommand { get; }
        public Command EditCommand { get; }
        public Command DeleteCommand { get; }
        public Command ViewCommand { get; }

        public PaisViewModel()
            : this(new PaisApiService())
        {
        }

        public PaisViewModel(PaisApiService paisApiService)
        {
            this.paisApiService = paisApiService
                ?? throw new ArgumentNullException(nameof(paisApiService));

            AddCommand = new Command(
                async () => await OnAddAsync());

            EditCommand = new Command<PaisResponse>(
                async pais => await OnEditAsync(pais));

            DeleteCommand = new Command<PaisResponse>(
                async pais => await OnDeleteAsync(pais));

            ViewCommand = new Command<PaisResponse>(
                async pais => await OnViewAsync(pais));
        }

        public async Task LoadPais(bool mostrarIndicadorCarga)
        {
            if (!CanView)
            {
                await MostrarToastAsync(
                    "No tiene permisos para ver países.");
                return;
            }

            if (cargandoPaises)
                return;

            cargandoPaises = true;

            if (mostrarIndicadorCarga)
                IsBusy = true;

            try
            {
                var resultado = await paisApiService.GetPaisResultAsync();

                if (!resultado.Success)
                {
                    await MostrarToastAsync(resultado.Message);
                    return;
                }

                List = new ObservableCollection<PaisResponse>(
                    (resultado.Data ?? new ObservableCollection<PaisResponse>())
                    .OrderBy(x => x.NombrePais ?? string.Empty));

                if (List.Count == 0)
                    await MostrarToastAsync("No se encontraron países registrados.");
            }
            catch
            {
                await MostrarToastAsync(
                    "Ocurrió un error inesperado al cargar los países.");
            }
            finally
            {
                cargandoPaises = false;

                if (mostrarIndicadorCarga)
                    IsBusy = false;
            }
        }

        private async Task OnAddAsync()
        {
            if (!CanAdd)
            {
                await MostrarToastAsync(
                    "No tiene permisos para agregar países.");
                return;
            }

            if (IsBusy)
                return;

            var parameters = new Dictionary<string, object>
            {
                { "Mode", FormMode.FormModeSelect.Create },
                { "Pais", new PaisRequest(new PaisResponse()) }
            };

            await GoToAsyncParameters("//PaisFormPage", parameters);
        }

        private async Task OnEditAsync(PaisResponse? pais)
        {
            if (!CanEdit)
            {
                await MostrarToastAsync(
                    "No tiene permisos para editar países.");
                return;
            }

            if (IsBusy || pais == null)
                return;

            var parameters = new Dictionary<string, object>
            {
                { "Mode", FormMode.FormModeSelect.Edit },
                { "Pais", new PaisRequest(pais) }
            };

            await GoToAsyncParameters("//PaisFormPage", parameters);
        }

        private async Task OnViewAsync(PaisResponse? pais)
        {
            if (!CanView)
            {
                await MostrarToastAsync(
                    "No tiene permisos para ver detalles.");
                return;
            }

            if (IsBusy || pais == null)
                return;

            var parameters = new Dictionary<string, object>
            {
                { "Pais", new PaisRequest(pais) },
                { "TitlePage", $"Departamentos de {pais.NombrePais}" }
            };

            await GoToAsyncParameters("//DepartamentoPage", parameters);
        }

        private async Task OnDeleteAsync(PaisResponse? pais)
        {
            if (!CanDelete)
            {
                await MostrarToastAsync(
                    "No tiene permisos para eliminar países.");
                return;
            }

            if (IsBusy || eliminandoPais || pais == null)
                return;

            bool confirmar = await App.Current.MainPage.DisplayAlert(
                "Eliminar país",
                $"¿Desea eliminar el país '{pais.NombrePais}'?",
                "Sí",
                "No");

            if (!confirmar)
                return;

            eliminandoPais = true;
            IsBusy = true;

            try
            {
                var resultado = await paisApiService.DeletePaisResultAsync(
                    new PaisRequest(pais));

                if (!resultado.Success)
                {
                    await MostrarToastAsync(resultado.Message);
                    return;
                }

                List.Remove(pais);
                await MostrarToastAsync(
                    string.IsNullOrWhiteSpace(resultado.Message)
                        ? "País eliminado correctamente."
                        : resultado.Message);
            }
            catch
            {
                await MostrarToastAsync(
                    "Ocurrió un error inesperado al eliminar el país.");
            }
            finally
            {
                eliminandoPais = false;
                IsBusy = false;
            }
        }
    }
}
