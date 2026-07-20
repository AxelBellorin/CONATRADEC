using CONATRADEC.Models;
using CONATRADEC.Services;
using System.Collections.ObjectModel;

namespace CONATRADEC.ViewModels
{
    public class TipoCultivoViewModel : GlobalService
    {
        private readonly TipoCultivoApiService apiService = new();
        private ObservableCollection<TipoCultivoResponse> list = new();
        private bool loading;
        private bool deleting;

        public ObservableCollection<TipoCultivoResponse> List
        {
            get => list;
            private set { list = value; OnPropertyChanged(); }
        }

        public Command AddCommand { get; }
        public Command<TipoCultivoResponse> EditCommand { get; }
        public Command<TipoCultivoResponse> ViewCommand { get; }
        public Command<TipoCultivoResponse> DeleteCommand { get; }

        public TipoCultivoViewModel()
        {
            AddCommand = new Command(async () => await AddAsync());
            EditCommand = new Command<TipoCultivoResponse>(async x => await OpenAsync(x, FormMode.FormModeSelect.Edit));
            ViewCommand = new Command<TipoCultivoResponse>(async x => await OpenAsync(x, FormMode.FormModeSelect.View));
            DeleteCommand = new Command<TipoCultivoResponse>(async x => await DeleteAsync(x));
        }

        public async Task LoadAsync(bool showIndicator)
        {
            if (!CanView || loading) return;
            loading = true;
            if (showIndicator) IsBusy = true;

            try
            {
                var result = await apiService.GetAsync();
                if (!result.Success)
                {
                    await MostrarToastAsync(result.Message);
                    return;
                }

                List = new ObservableCollection<TipoCultivoResponse>(
                    (result.Data ?? new ObservableCollection<TipoCultivoResponse>())
                    .OrderBy(x => x.NombreTipoCultivo ?? string.Empty));
            }
            finally
            {
                loading = false;
                if (showIndicator) IsBusy = false;
            }
        }

        private async Task AddAsync()
        {
            if (!CanAdd) { await MostrarToastAsync("No tiene permisos para agregar tipos de cultivo."); return; }
            if (IsBusy) return;

            await GoToAsyncParameters(
                AppRoutes.TipoCultivoFormulario,
                new Dictionary<string, object>
                {
                    ["Mode"] = FormMode.FormModeSelect.Create,
                    ["Item"] = new TipoCultivoRequest()
                });
        }

        private async Task OpenAsync(TipoCultivoResponse? item, FormMode.FormModeSelect mode)
        {
            if (item == null || IsBusy) return;
            if (mode == FormMode.FormModeSelect.Edit && !CanEdit) { await MostrarToastAsync("No tiene permisos para editar tipos de cultivo."); return; }

            await GoToAsyncParameters(
                AppRoutes.TipoCultivoFormulario,
                new Dictionary<string, object>
                {
                    ["Mode"] = mode,
                    ["Item"] = new TipoCultivoRequest(item)
                });
        }

        private async Task DeleteAsync(TipoCultivoResponse? item)
        {
            if (item == null || IsBusy || deleting) return;
            if (!CanDelete) { await MostrarToastAsync("No tiene permisos para eliminar tipos de cultivo."); return; }

            Page? page = Application.Current?.MainPage;
            if (page == null) return;

            bool confirm = await page.DisplayAlert(
                "Eliminar tipo de cultivo",
                $"¿Desea eliminar '{item.NombreTipoCultivo}'?",
                "Sí",
                "No");
            if (!confirm) return;

            deleting = true;
            IsBusy = true;
            try
            {
                var result = await apiService.DeleteAsync(item.TipoCultivoId);
                if (!result.Success)
                {
                    if (result.StatusCode == 409)
                        await page.DisplayAlert("No se puede eliminar", result.Message, "Aceptar");
                    else
                        await MostrarToastAsync(result.Message);
                    return;
                }

                await MostrarToastAsync(result.Message);
                await LoadAsync(false);
            }
            finally { deleting = false; IsBusy = false; }
        }
    }
}
