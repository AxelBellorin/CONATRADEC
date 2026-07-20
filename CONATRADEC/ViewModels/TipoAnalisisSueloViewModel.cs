using CONATRADEC.Models;
using CONATRADEC.Services;
using System.Collections.ObjectModel;

namespace CONATRADEC.ViewModels
{
    public class TipoAnalisisSueloViewModel : GlobalService
    {
        private readonly TipoAnalisisSueloApiService apiService = new();
        private ObservableCollection<TipoAnalisisSueloResponse> list = new();
        private bool loading;
        private bool deleting;

        public ObservableCollection<TipoAnalisisSueloResponse> List
        {
            get => list;
            private set { list = value; OnPropertyChanged(); }
        }

        public Command AddCommand { get; }
        public Command<TipoAnalisisSueloResponse> EditCommand { get; }
        public Command<TipoAnalisisSueloResponse> ViewCommand { get; }
        public Command<TipoAnalisisSueloResponse> DeleteCommand { get; }

        public TipoAnalisisSueloViewModel()
        {
            AddCommand = new Command(async () => await AddAsync());
            EditCommand = new Command<TipoAnalisisSueloResponse>(async x => await OpenAsync(x, FormMode.FormModeSelect.Edit));
            ViewCommand = new Command<TipoAnalisisSueloResponse>(async x => await OpenAsync(x, FormMode.FormModeSelect.View));
            DeleteCommand = new Command<TipoAnalisisSueloResponse>(async x => await DeleteAsync(x));
        }

        public async Task LoadAsync(bool showIndicator)
        {
            if (!CanView || loading) return;
            loading = true;
            if (showIndicator) IsBusy = true;

            try
            {
                var result = await apiService.GetAsync();
                if (!result.Success) { await MostrarToastAsync(result.Message); return; }
                List = new ObservableCollection<TipoAnalisisSueloResponse>(
                    (result.Data ?? new ObservableCollection<TipoAnalisisSueloResponse>())
                    .OrderBy(x => x.NombreTipoAnalisisSuelo ?? string.Empty));
            }
            finally { loading = false; if (showIndicator) IsBusy = false; }
        }

        private async Task AddAsync()
        {
            if (!CanAdd) { await MostrarToastAsync("No tiene permisos para agregar tipos de análisis."); return; }
            if (IsBusy) return;

            await GoToAsyncParameters(
                AppRoutes.TipoAnalisisSueloFormulario,
                new Dictionary<string, object>
                {
                    ["Mode"] = FormMode.FormModeSelect.Create,
                    ["Item"] = new TipoAnalisisSueloRequest()
                });
        }

        private async Task OpenAsync(TipoAnalisisSueloResponse? item, FormMode.FormModeSelect mode)
        {
            if (item == null || IsBusy) return;
            if (mode == FormMode.FormModeSelect.Edit && !CanEdit) { await MostrarToastAsync("No tiene permisos para editar tipos de análisis."); return; }

            await GoToAsyncParameters(
                AppRoutes.TipoAnalisisSueloFormulario,
                new Dictionary<string, object>
                {
                    ["Mode"] = mode,
                    ["Item"] = new TipoAnalisisSueloRequest(item)
                });
        }

        private async Task DeleteAsync(TipoAnalisisSueloResponse? item)
        {
            if (item == null || IsBusy || deleting) return;
            if (!CanDelete) { await MostrarToastAsync("No tiene permisos para eliminar tipos de análisis."); return; }

            Page? page = Application.Current?.MainPage;
            if (page == null) return;

            bool confirm = await page.DisplayAlert(
                "Eliminar tipo de análisis",
                $"¿Desea eliminar '{item.NombreTipoAnalisisSuelo}'?",
                "Sí",
                "No");
            if (!confirm) return;

            deleting = true;
            IsBusy = true;
            try
            {
                var result = await apiService.DeleteAsync(item.TipoAnalisisSueloId);
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
