using CONATRADEC.Models;
using CONATRADEC.Services;
using System.Collections.ObjectModel;

namespace CONATRADEC.ViewModels
{
    public class RangoNutrienteViewModel : GlobalService
    {
        private readonly TipoCultivoApiService
            cultivoApiService = new();

        private readonly RangoNutrienteApiService
            rangoApiService = new();

        private ObservableCollection<
            RangoNutrienteCategoriaItem> list = new();

        private bool loading;
        private bool deleting;

        public ObservableCollection<
            RangoNutrienteCategoriaItem> List
        {
            get => list;
            private set
            {
                list = value;
                OnPropertyChanged();
            }
        }

        public Command AddCategoryCommand { get; }

        public Command<RangoNutrienteCategoriaItem>
            OpenCategoryCommand { get; }

        public Command<RangoNutrienteCategoriaItem>
            EditCategoryCommand { get; }

        public Command<RangoNutrienteCategoriaItem>
            DeleteCategoryCommand { get; }

        public RangoNutrienteViewModel()
        {
            AddCategoryCommand =
                new Command(
                    async () => await AddCategoryAsync(),
                    () => CanAdd && !IsBusy);

            OpenCategoryCommand =
                new Command<RangoNutrienteCategoriaItem>(
                    async item =>
                        await OpenCategoryAsync(item),
                    item =>
                        item != null &&
                        CanView &&
                        !IsBusy);

            EditCategoryCommand =
                new Command<RangoNutrienteCategoriaItem>(
                    async item =>
                        await EditCategoryAsync(item),
                    item =>
                        item != null &&
                        CanEdit &&
                        !IsBusy);

            DeleteCategoryCommand =
                new Command<RangoNutrienteCategoriaItem>(
                    async item =>
                        await DeleteCategoryAsync(item),
                    item =>
                        item != null &&
                        CanDelete &&
                        !IsBusy);
        }

        public async Task LoadAsync(bool showIndicator)
        {
            if (!CanView || loading)
                return;

            loading = true;

            if (showIndicator)
                IsBusy = true;

            RefrescarComandos();

            try
            {
                Task<ApiResult<ObservableCollection<
                    TipoCultivoResponse>>> cultivosTask =
                    cultivoApiService.GetAsync();

                Task<ApiResult<ObservableCollection<
                    RangoNutrienteResponse>>> rangosTask =
                    rangoApiService.GetAsync();

                await Task.WhenAll(
                    cultivosTask,
                    rangosTask);

                ApiResult<ObservableCollection<
                    TipoCultivoResponse>> cultivos =
                    await cultivosTask;

                if (!cultivos.Success)
                {
                    await MostrarToastAsync(
                        cultivos.Message);
                    return;
                }

                ApiResult<ObservableCollection<
                    RangoNutrienteResponse>> rangos =
                    await rangosTask;

                if (!rangos.Success)
                {
                    await MostrarToastAsync(
                        rangos.Message);
                    return;
                }

                List<RangoNutrienteResponse>
                    rangosActivos =
                        (rangos.Data ??
                         new ObservableCollection<
                             RangoNutrienteResponse>())
                        .Where(x => x.Activo)
                        .ToList();

                IEnumerable<RangoNutrienteCategoriaItem>
                    categorias =
                        (cultivos.Data ??
                         new ObservableCollection<
                             TipoCultivoResponse>())
                        .Where(x =>
                            x.Activo &&
                            x.TipoCultivoId > 0)
                        .Select(cultivo =>
                            new RangoNutrienteCategoriaItem
                            {
                                TipoCultivoId =
                                    cultivo.TipoCultivoId,

                                NombreCategoria =
                                    cultivo.NombreMostrar,

                                DescripcionCategoria =
                                    cultivo
                                        .DescripcionTipoCultivo?
                                        .Trim() ??
                                    string.Empty,

                                CantidadAportes =
                                    rangosActivos.Count(x =>
                                        x.TipoCultivoId ==
                                        cultivo.TipoCultivoId)
                            })
                        .OrderBy(x =>
                            x.NombreCategoria);

                List =
                    new ObservableCollection<
                        RangoNutrienteCategoriaItem>(
                        categorias);
            }
            catch (Exception ex)
            {
                await MostrarErrorInesperadoAsync(
                    "cargar las tipos de cultivo",
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

        private async Task AddCategoryAsync()
        {
            if (!CanAdd)
            {
                await MostrarToastAsync(
                    "No tiene permisos para agregar tipos de cultivo.");
                return;
            }

            if (IsBusy)
                return;

            await GoToAsyncParameters(
                AppRoutes
                    .RangoNutrienteCategoriaFormulario,
                new Dictionary<string, object>
                {
                    ["Mode"] =
                        FormMode.FormModeSelect.Create,

                    ["Item"] =
                        new TipoCultivoRequest()
                });
        }

        private async Task OpenCategoryAsync(
            RangoNutrienteCategoriaItem? item)
        {
            if (item == null || IsBusy)
                return;

            await GoToAsyncParameters(
                AppRoutes.RangoNutrienteDetalle,
                new Dictionary<string, object>
                {
                    ["Categoria"] = item
                });
        }

        private async Task EditCategoryAsync(
            RangoNutrienteCategoriaItem? item)
        {
            if (item == null || IsBusy)
                return;

            if (!CanEdit)
            {
                await MostrarToastAsync(
                    "No tiene permisos para editar tipos de cultivo.");
                return;
            }

            await GoToAsyncParameters(
                AppRoutes
                    .RangoNutrienteCategoriaFormulario,
                new Dictionary<string, object>
                {
                    ["Mode"] =
                        FormMode.FormModeSelect.Edit,

                    ["Item"] =
                        new TipoCultivoRequest(
                            item.ToTipoCultivoResponse())
                });
        }

        private async Task DeleteCategoryAsync(
            RangoNutrienteCategoriaItem? item)
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
                    "No tiene permisos para eliminar tipos de cultivo.");
                return;
            }

            Page? page =
                Application.Current?.MainPage;

            if (page == null)
                return;

            string detalleDependencia =
                item.CantidadAportes > 0
                    ? "\n\nLa categoría tiene aportes " +
                      "nutricionales configurados. El servidor " +
                      "puede impedir su eliminación para proteger " +
                      "las relaciones existentes."
                    : string.Empty;

            bool confirm =
                await page.DisplayAlert(
                    "Eliminar tipo de cultivo",
                    $"¿Desea eliminar la categoría " +
                    $"'{item.NombreCategoria}'?" +
                    detalleDependencia,
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
                    await cultivoApiService.DeleteAsync(
                        item.TipoCultivoId);

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
                    "eliminar el tipo de cultivo",
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
            AddCategoryCommand.ChangeCanExecute();
            OpenCategoryCommand.ChangeCanExecute();
            EditCategoryCommand.ChangeCanExecute();
            DeleteCategoryCommand.ChangeCanExecute();
        }
    }
}
