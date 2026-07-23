using CONATRADEC.Models;
using CONATRADEC.Services;
using System.Collections.ObjectModel;

namespace CONATRADEC.ViewModels
{
    public class RangoNutrienteDetalleViewModel :
        GlobalService
    {
        private readonly RangoNutrienteApiService
            apiService = new();

        private RangoNutrienteCategoriaItem?
            categoria;

        private ObservableCollection<
            RangoNutrienteResponse> aportes = new();

        private bool loading;
        private bool deleting;

        public RangoNutrienteCategoriaItem? Categoria
        {
            get => categoria;
            set
            {
                categoria = value;
                OnPropertyChanged();
                OnPropertyChanged(
                    nameof(NombreCategoria));
                OnPropertyChanged(
                    nameof(DescripcionCategoria));
                OnPropertyChanged(
                    nameof(Titulo));
                RefrescarComandos();
            }
        }

        public string NombreCategoria =>
            Categoria?.NombreCategoria ??
            string.Empty;

        public string DescripcionCategoria =>
            Categoria?.DescripcionCategoria ??
            string.Empty;

        public string Titulo =>
            string.IsNullOrWhiteSpace(
                NombreCategoria)
                ? "Rangos de aporte"
                : $"Aportes de {NombreCategoria}";

        public ObservableCollection<
            RangoNutrienteResponse> Aportes
        {
            get => aportes;
            private set
            {
                aportes = value;
                OnPropertyChanged();
                OnPropertyChanged(
                    nameof(ResumenAportes));
            }
        }

        public string ResumenAportes =>
            Aportes.Count == 1
                ? "1 rango configurado"
                : $"{Aportes.Count} rangos configurados";

        public Command AddCommand { get; }

        public Command<RangoNutrienteResponse>
            EditCommand { get; }

        public Command<RangoNutrienteResponse>
            ViewCommand { get; }

        public Command<RangoNutrienteResponse>
            DeleteCommand { get; }

        public Command BackCommand { get; }

        public RangoNutrienteDetalleViewModel()
        {
            AddCommand =
                new Command(
                    async () => await AddAsync(),
                    () =>
                        Categoria != null &&
                        CanAdd &&
                        !IsBusy);

            EditCommand =
                new Command<RangoNutrienteResponse>(
                    async item =>
                        await OpenAsync(
                            item,
                            FormMode.FormModeSelect.Edit),
                    item =>
                        item != null &&
                        CanEdit &&
                        !IsBusy);

            ViewCommand =
                new Command<RangoNutrienteResponse>(
                    async item =>
                        await OpenAsync(
                            item,
                            FormMode.FormModeSelect.View),
                    item =>
                        item != null &&
                        CanView &&
                        !IsBusy);

            DeleteCommand =
                new Command<RangoNutrienteResponse>(
                    async item =>
                        await DeleteAsync(item),
                    item =>
                        item != null &&
                        CanDelete &&
                        !IsBusy);

            BackCommand =
                new Command(
                    async () =>
                        await GoToAsyncParameters(
                            AppRoutes.RangosNutrientes),
                    () => !IsBusy);
        }

        public async Task LoadAsync(
            bool showIndicator)
        {
            if (!CanView ||
                loading ||
                Categoria == null)
            {
                return;
            }

            loading = true;

            if (showIndicator)
                IsBusy = true;

            RefrescarComandos();

            try
            {
                ApiResult<ObservableCollection<
                    RangoNutrienteResponse>> result =
                    await apiService.GetAsync();

                if (!result.Success)
                {
                    await MostrarToastAsync(
                        result.Message);
                    return;
                }

                IEnumerable<RangoNutrienteResponse>
                    datos =
                        (result.Data ??
                         new ObservableCollection<
                             RangoNutrienteResponse>())
                        .Where(x =>
                            x.Activo &&
                            x.TipoCultivoId ==
                            Categoria.TipoCultivoId)
                        .OrderBy(x =>
                            x.NombreElementoQuimico ??
                            string.Empty);

                Aportes =
                    new ObservableCollection<
                        RangoNutrienteResponse>(
                        datos);
            }
            catch (Exception ex)
            {
                await MostrarErrorInesperadoAsync(
                    "cargar los rangos de aporte",
                    ex);
            }
            finally
            {
                loading = false;

                if (showIndicator)
                    IsBusy = false;

                RefrescarComandos();
            }
        }

        private async Task AddAsync()
        {
            if (Categoria == null || IsBusy)
                return;

            if (!CanAdd)
            {
                await MostrarToastAsync(
                    "No tiene permisos para agregar rangos de aporte.");
                return;
            }

            await GoToAsyncParameters(
                AppRoutes.RangoNutrienteFormulario,
                new Dictionary<string, object>
                {
                    ["Mode"] =
                        FormMode.FormModeSelect.Create,

                    ["Categoria"] = Categoria,

                    ["Item"] =
                        new RangoNutrienteRequest
                        {
                            TipoCultivoId =
                                Categoria.TipoCultivoId
                        }
                });
        }

        private async Task OpenAsync(
            RangoNutrienteResponse? item,
            FormMode.FormModeSelect mode)
        {
            if (item == null ||
                Categoria == null ||
                IsBusy)
            {
                return;
            }

            if (mode ==
                    FormMode.FormModeSelect.Edit &&
                !CanEdit)
            {
                await MostrarToastAsync(
                    "No tiene permisos para editar rangos de aporte.");
                return;
            }

            await GoToAsyncParameters(
                AppRoutes.RangoNutrienteFormulario,
                new Dictionary<string, object>
                {
                    ["Mode"] = mode,
                    ["Categoria"] = Categoria,
                    ["Item"] =
                        new RangoNutrienteRequest(item)
                });
        }

        private async Task DeleteAsync(
            RangoNutrienteResponse? item)
        {
            if (item == null ||
                IsBusy ||
                deleting)
            {
                return;
            }

            if (!CanDelete)
            {
                await MostrarToastAsync(
                    "No tiene permisos para eliminar rangos de aporte.");
                return;
            }

            Page? page =
                Application.Current?.MainPage;

            if (page == null)
                return;

            bool confirm =
                await page.DisplayAlert(
                    "Eliminar rango de aporte",
                    $"¿Desea eliminar el rango " +
                    $"'{item.ElementoTexto}' de " +
                    $"'{NombreCategoria}'?",
                    "Sí",
                    "No");

            if (!confirm)
                return;

            deleting = true;
            IsBusy = true;
            RefrescarComandos();

            try
            {
                ApiResult<bool> result =
                    await apiService.DeleteAsync(
                        item
                            .ParametroRangoNutrienteCultivoId);

                if (!result.Success)
                {
                    await MostrarToastAsync(
                        result.Message);
                    return;
                }

                await MostrarToastAsync(
                    result.Message);

                await LoadAsync(false);
            }
            catch (Exception ex)
            {
                await MostrarErrorInesperadoAsync(
                    "eliminar el rango nutricional",
                    ex);
            }
            finally
            {
                deleting = false;
                IsBusy = false;
                RefrescarComandos();
            }
        }

        private void RefrescarComandos()
        {
            AddCommand.ChangeCanExecute();
            EditCommand.ChangeCanExecute();
            ViewCommand.ChangeCanExecute();
            DeleteCommand.ChangeCanExecute();
            BackCommand.ChangeCanExecute();
        }
    }
}
