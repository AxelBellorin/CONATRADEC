using CONATRADEC.Models;
using CONATRADEC.Services;
using System.Collections.ObjectModel;

namespace CONATRADEC.ViewModels
{
    public class RangoNutrienteViewModel : GlobalService
    {
        private readonly RangoNutrienteApiService apiService = new();
        private ObservableCollection<RangoNutrienteResponse> list = new();
        private bool loading;
        private bool deleting;

        public ObservableCollection<RangoNutrienteResponse> List
        {
            get => list;
            private set { list = value; OnPropertyChanged(); }
        }

        public Command AddCommand { get; }
        public Command<RangoNutrienteResponse> EditCommand { get; }
        public Command<RangoNutrienteResponse> ViewCommand { get; }
        public Command<RangoNutrienteResponse> DeleteCommand { get; }

        public RangoNutrienteViewModel()
        {
            AddCommand = new Command(async () => await AddAsync());
            EditCommand = new Command<RangoNutrienteResponse>(async x => await OpenAsync(x, FormMode.FormModeSelect.Edit));
            ViewCommand = new Command<RangoNutrienteResponse>(async x => await OpenAsync(x, FormMode.FormModeSelect.View));
            DeleteCommand = new Command<RangoNutrienteResponse>(async x => await DeleteAsync(x));
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
                List = new ObservableCollection<RangoNutrienteResponse>(
                    (result.Data ?? new ObservableCollection<RangoNutrienteResponse>())
                    .OrderBy(x => x.NombreTipoCultivo ?? string.Empty)
                    .ThenBy(x => x.NombreElementoQuimico ?? string.Empty));
            }
            finally { loading = false; if (showIndicator) IsBusy = false; }
        }

        private async Task AddAsync()
        {
            if (!CanAdd) { await MostrarToastAsync("No tiene permisos para agregar rangos nutricionales."); return; }
            if (IsBusy) return;

            await GoToAsyncParameters(
                AppRoutes.RangoNutrienteFormulario,
                new Dictionary<string, object>
                {
                    ["Mode"] = FormMode.FormModeSelect.Create,
                    ["Item"] = new RangoNutrienteRequest()
                });
        }

        private async Task OpenAsync(RangoNutrienteResponse? item, FormMode.FormModeSelect mode)
        {
            if (item == null || IsBusy) return;
            if (mode == FormMode.FormModeSelect.Edit && !CanEdit) { await MostrarToastAsync("No tiene permisos para editar rangos nutricionales."); return; }

            await GoToAsyncParameters(
                AppRoutes.RangoNutrienteFormulario,
                new Dictionary<string, object>
                {
                    ["Mode"] = mode,
                    ["Item"] = new RangoNutrienteRequest(item)
                });
        }

        private async Task DeleteAsync(RangoNutrienteResponse? item)
        {
            if (item == null || IsBusy || deleting) return;
            if (!CanDelete) { await MostrarToastAsync("No tiene permisos para eliminar rangos nutricionales."); return; }

            Page? page = Application.Current?.MainPage;
            if (page == null) return;

            bool confirm = await page.DisplayAlert(
                "Eliminar rango nutricional",
                $"¿Desea eliminar el rango de {item.ElementoTexto} para {item.NombreTipoCultivo}?",
                "Sí",
                "No");
            if (!confirm) return;

            deleting = true;
            IsBusy = true;
            try
            {
                var result = await apiService.DeleteAsync(item.ParametroRangoNutrienteCultivoId);
                if (!result.Success) { await MostrarToastAsync(result.Message); return; }
                await MostrarToastAsync(result.Message);
                await LoadAsync(false);
            }
            finally { deleting = false; IsBusy = false; }
        }
    }
}
