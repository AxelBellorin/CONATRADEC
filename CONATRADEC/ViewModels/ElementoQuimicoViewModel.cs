using CONATRADEC.Models;
using CONATRADEC.Services;
using System.Collections.ObjectModel;

namespace CONATRADEC.ViewModels
{
    public class ElementoQuimicoViewModel : GlobalService
    {
        private ObservableCollection<ElementoQuimicoResponse> list = new();
        private readonly ElementoQuimicoApiService elementoApiService;
        private bool cargandoElementos;
        private bool eliminandoElemento;

        public Command AddCommand { get; }
        public Command EditCommand { get; }
        public Command DeleteCommand { get; }
        public Command ViewCommand { get; }

        public ObservableCollection<ElementoQuimicoResponse> List
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

        public ElementoQuimicoViewModel()
            : this(new ElementoQuimicoApiService())
        {
        }

        public ElementoQuimicoViewModel(
            ElementoQuimicoApiService elementoApiService)
        {
            this.elementoApiService = elementoApiService
                ?? throw new ArgumentNullException(nameof(elementoApiService));

            AddCommand = new Command(
                async () => await OnAddAsync());

            EditCommand = new Command<ElementoQuimicoResponse>(
                async elemento => await OnEditAsync(elemento));

            DeleteCommand = new Command<ElementoQuimicoResponse>(
                async elemento => await OnDeleteAsync(elemento));

            ViewCommand = new Command<ElementoQuimicoResponse>(
                async elemento => await OnViewAsync(elemento));

            LoadPagePermissions("elementoQuimicoPage");
        }

        public async Task LoadElementoQuimico(bool mostrarIndicadorCarga)
        {
            if (!CanView)
            {
                await MostrarToastAsync(
                    "No tiene permisos para ver elementos químicos.");
                return;
            }

            if (cargandoElementos)
                return;

            cargandoElementos = true;

            if (mostrarIndicadorCarga)
                IsBusy = true;

            try
            {
                var resultado = await elementoApiService
                    .GetElementoQuimicoResultAsync();

                if (!resultado.Success)
                {
                    await MostrarToastAsync(resultado.Message);
                    return;
                }

                List = new ObservableCollection<ElementoQuimicoResponse>(
                    (resultado.Data ?? new ObservableCollection<ElementoQuimicoResponse>())
                    .OrderBy(x => x.NombreElementoQuimico ?? string.Empty));

                if (List.Count == 0)
                {
                    await MostrarToastAsync(
                        "No se encontraron elementos químicos.");
                }
            }
            catch
            {
                await MostrarToastAsync(
                    "Ocurrió un error inesperado al cargar los elementos químicos.");
            }
            finally
            {
                cargandoElementos = false;

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
                {
                    "ElementoQuimico",
                    new ElementoQuimicoRequest(new ElementoQuimicoResponse())
                }
            };

            await GoToAsyncParameters(
                "//ElementoQuimicoFormPage",
                parameters);
        }

        private async Task OnEditAsync(ElementoQuimicoResponse? elemento)
        {
            if (!CanEdit)
            {
                await MostrarToastAsync("No tiene permisos para editar.");
                return;
            }

            if (IsBusy || elemento == null)
                return;

            var parameters = new Dictionary<string, object>
            {
                { "Mode", FormMode.FormModeSelect.Edit },
                {
                    "ElementoQuimico",
                    new ElementoQuimicoRequest(elemento)
                }
            };

            await GoToAsyncParameters(
                "//ElementoQuimicoFormPage",
                parameters);
        }

        private async Task OnViewAsync(ElementoQuimicoResponse? elemento)
        {
            if (!CanView)
            {
                await MostrarToastAsync(
                    "No tiene permisos para ver detalles.");
                return;
            }

            if (IsBusy || elemento == null)
                return;

            var parameters = new Dictionary<string, object>
            {
                { "Mode", FormMode.FormModeSelect.View },
                {
                    "ElementoQuimico",
                    new ElementoQuimicoRequest(elemento)
                }
            };

            await GoToAsyncParameters(
                "//ElementoQuimicoFormPage",
                parameters);
        }

        private async Task OnDeleteAsync(ElementoQuimicoResponse? elemento)
        {
            if (!CanDelete)
            {
                await MostrarToastAsync("No tiene permisos para eliminar.");
                return;
            }

            if (IsBusy || eliminandoElemento || elemento == null)
                return;

            bool confirmar = await App.Current.MainPage.DisplayAlert(
                "Eliminar elemento químico",
                $"¿Desea eliminar el elemento '{elemento.NombreElementoQuimico}'?",
                "Sí",
                "No");

            if (!confirmar)
                return;

            eliminandoElemento = true;
            IsBusy = true;

            try
            {
                var resultado = await elementoApiService
                    .DeleteElementoQuimicoResultAsync(
                        new ElementoQuimicoRequest(elemento));

                if (!resultado.Success)
                {
                    await MostrarToastAsync(resultado.Message);
                    return;
                }

                List.Remove(elemento);
                await MostrarToastAsync(
                    string.IsNullOrWhiteSpace(resultado.Message)
                        ? "Elemento químico eliminado correctamente."
                        : resultado.Message);
            }
            catch
            {
                await MostrarToastAsync(
                    "Ocurrió un error inesperado al eliminar el elemento químico.");
            }
            finally
            {
                eliminandoElemento = false;
                IsBusy = false;
            }
        }
    }
}
