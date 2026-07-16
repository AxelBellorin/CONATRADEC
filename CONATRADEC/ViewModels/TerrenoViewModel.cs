using CONATRADEC.Models;
using CONATRADEC.Services;
using System.Collections.ObjectModel;

namespace CONATRADEC.ViewModels
{
    public class TerrenoViewModel : GlobalService
    {
        private ObservableCollection<TerrenoResponse> list = new();
        private readonly TerrenoApiService terrenoApiService;
        private string cantidadPlantasTerreno = string.Empty;
        private bool cargandoTerrenos;

        public ObservableCollection<TerrenoResponse> List
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

        public string CantidadPlantasTerreno
        {
            get => cantidadPlantasTerreno;
            set
            {
                if (cantidadPlantasTerreno == value)
                    return;

                cantidadPlantasTerreno = value;
                OnPropertyChanged();
            }
        }

        public Command AddCommand { get; }
        public Command EditCommand { get; }
        public Command DeleteCommand { get; }
        public Command ViewCommand { get; }

        public TerrenoViewModel()
            : this(new TerrenoApiService())
        {
        }

        public TerrenoViewModel(TerrenoApiService terrenoApiService)
        {
            this.terrenoApiService = terrenoApiService
                ?? throw new ArgumentNullException(nameof(terrenoApiService));

            AddCommand = new Command(async () =>
            {
                if (!CanAdd)
                {
                    await MostrarToastAsync("No tiene permisos para agregar.");
                    return;
                }

                await OnAddAsync();
            });

            EditCommand = new Command<TerrenoResponse>(async item =>
            {
                if (!CanEdit)
                {
                    await MostrarToastAsync("No tiene permisos para editar.");
                    return;
                }

                await OnEditAsync(item);
            });

            DeleteCommand = new Command<TerrenoResponse>(async item =>
            {
                if (!CanDelete)
                {
                    await MostrarToastAsync("No tiene permisos para eliminar.");
                    return;
                }

                await OnDeleteAsync(item);
            });

            ViewCommand = new Command<TerrenoResponse>(async item =>
            {
                if (!CanView)
                {
                    await MostrarToastAsync("No tiene permisos para ver detalles.");
                    return;
                }

                await OnViewAsync(item);
            });
        }

        public async Task LoadTerrenosAsync(bool mostrarIndicadorCarga)
        {
            if (!CanView || cargandoTerrenos)
                return;

            cargandoTerrenos = true;

            if (mostrarIndicadorCarga)
                IsBusy = true;

            try
            {
                var resultado = await terrenoApiService.GetTerrenosResultAsync();

                if (!resultado.Success)
                {
                    await MostrarToastAsync(resultado.Message);
                    return;
                }

                List = resultado.Data ?? new ObservableCollection<TerrenoResponse>();

                if (List.Count == 0)
                {
                    await MostrarToastAsync("No se encontraron terrenos.");
                }
            }
            catch
            {
                await MostrarToastAsync(
                    "Ocurrió un error inesperado al cargar los terrenos.");
            }
            finally
            {
                cargandoTerrenos = false;

                if (mostrarIndicadorCarga)
                    IsBusy = false;
            }
        }

        private async Task OnAddAsync()
        {
            if (IsBusy)
                return;

            var parameters = new Dictionary<string, object>
            {
                { "Mode", FormMode.FormModeSelect.Create },
                { "Terreno", new TerrenoRequest(new TerrenoResponse()) }
            };

            await GoToAsyncParameters(
                AppRoutes.TerrenoFormulario,
                parameters);
        }

        private async Task OnEditAsync(TerrenoResponse? item)
        {
            if (IsBusy || item == null)
                return;

            var parameters = new Dictionary<string, object>
            {
                { "Mode", FormMode.FormModeSelect.Edit },
                { "Terreno", new TerrenoRequest(item) }
            };

            await GoToAsyncParameters(
                AppRoutes.TerrenoFormulario,
                parameters);
        }

        private async Task OnDeleteAsync(TerrenoResponse? item)
        {
            if (IsBusy || item == null)
                return;

            bool confirmar = await Shell.Current.DisplayAlert(
                "Eliminar",
                $"¿Desea eliminar el terreno {item.CodigoTerreno}?",
                "Sí",
                "No");

            if (!confirmar)
                return;

            IsBusy = true;

            try
            {
                var resultado = await terrenoApiService.DeleteTerrenoResultAsync(
                    new TerrenoRequest(item));

                if (!resultado.Success)
                {
                    await MostrarToastAsync(resultado.Message);
                    return;
                }

                List.Remove(item);
                await MostrarToastAsync("Terreno eliminado correctamente.");
            }
            catch
            {
                await MostrarToastAsync(
                    "Ocurrió un error inesperado al eliminar el terreno.");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task OnViewAsync(TerrenoResponse? item)
        {
            if (IsBusy || item == null)
                return;

            var parameters = new Dictionary<string, object>
            {
                { "Mode", FormMode.FormModeSelect.View },
                { "Terreno", new TerrenoRequest(item) }
            };

            await GoToAsyncParameters(
                AppRoutes.TerrenoFormulario,
                parameters);
        }
    }
}
