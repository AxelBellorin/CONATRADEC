using CONATRADEC.Models;
using CONATRADEC.Services;
using System.Collections.ObjectModel;

namespace CONATRADEC.ViewModels
{
    public class ExtraccionNutrienteViewModel : GlobalService
    {
        private readonly ExtraccionNutrienteApiService apiService = new();
        private ObservableCollection<ExtraccionNutrienteResponse> list = new();
        private bool loading;
        private bool deleting;

        public ObservableCollection<ExtraccionNutrienteResponse> List
        {
            get => list;
            private set { list = value; OnPropertyChanged(); }
        }

        public Command AddCommand { get; }
        public Command<ExtraccionNutrienteResponse> EditCommand { get; }
        public Command<ExtraccionNutrienteResponse> ViewCommand { get; }
        public Command<ExtraccionNutrienteResponse> DeleteCommand { get; }

        public ExtraccionNutrienteViewModel()
        {
            AddCommand = new Command(async () => await AddAsync());
            EditCommand = new Command<ExtraccionNutrienteResponse>(async x => await OpenAsync(x, FormMode.FormModeSelect.Edit));
            ViewCommand = new Command<ExtraccionNutrienteResponse>(async x => await OpenAsync(x, FormMode.FormModeSelect.View));
            DeleteCommand = new Command<ExtraccionNutrienteResponse>(async x => await DeleteAsync(x));
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
                List = new ObservableCollection<ExtraccionNutrienteResponse>(
                    (result.Data ?? new ObservableCollection<ExtraccionNutrienteResponse>())
                    .OrderBy(x => x.NombreElementoQuimico ?? string.Empty));
            }
            finally { loading = false; if (showIndicator) IsBusy = false; }
        }

        private async Task AddAsync()
        {
            if (!CanAdd) { await MostrarToastAsync("No tiene permisos para agregar parámetros de extracción."); return; }
            if (IsBusy) return;

            await GoToAsyncParameters(
                AppRoutes.ExtraccionNutrienteFormulario,
                new Dictionary<string, object>
                {
                    ["Mode"] = FormMode.FormModeSelect.Create,
                    ["Item"] = new ExtraccionNutrienteRequest()
                });
        }

        private async Task OpenAsync(ExtraccionNutrienteResponse? item, FormMode.FormModeSelect mode)
        {
            if (item == null || IsBusy) return;
            if (mode == FormMode.FormModeSelect.Edit && !CanEdit) { await MostrarToastAsync("No tiene permisos para editar parámetros de extracción."); return; }

            await GoToAsyncParameters(
                AppRoutes.ExtraccionNutrienteFormulario,
                new Dictionary<string, object>
                {
                    ["Mode"] = mode,
                    ["Item"] = new ExtraccionNutrienteRequest(item)
                });
        }

        private async Task DeleteAsync(ExtraccionNutrienteResponse? item)
        {
            if (item == null || IsBusy || deleting) return;
            if (!CanDelete) { await MostrarToastAsync("No tiene permisos para eliminar parámetros de extracción."); return; }

            Page? page = Application.Current?.MainPage;
            if (page == null) return;

            bool confirm = await page.DisplayAlert(
                "Eliminar parámetro de extracción",
                $"¿Desea eliminar la extracción configurada para '{item.ElementoTexto}'?",
                "Sí",
                "No");
            if (!confirm) return;

            deleting = true;
            IsBusy = true;
            try
            {
                var result = await apiService.DeleteAsync(item.ParametroExtraccionNutrienteCafeId);
                if (!result.Success) { await MostrarToastAsync(result.Message); return; }
                await MostrarToastAsync(result.Message);
                await LoadAsync(false);
            }
            finally { deleting = false; IsBusy = false; }
        }
    }
}
