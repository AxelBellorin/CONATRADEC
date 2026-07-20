using CONATRADEC.Models;
using CONATRADEC.Services;

namespace CONATRADEC.ViewModels
{
    public class TipoCultivoFormViewModel : GlobalService
    {
        private readonly TipoCultivoApiService apiService = new();
        private TipoCultivoRequest item = new();
        private FormMode.FormModeSelect mode;
        private string nombre = string.Empty;
        private string descripcion = string.Empty;

        public Command SaveCommand { get; }
        public Command CancelCommand { get; }

        public TipoCultivoFormViewModel()
        {
            SaveCommand = new Command(async () => await SaveAsync());
            CancelCommand = new Command(async () => await CancelAsync());
        }

        public TipoCultivoRequest Item
        {
            get => item;
            set
            {
                item = value ?? new TipoCultivoRequest();
                Nombre = item.NombreTipoCultivo;
                Descripcion = item.DescripcionTipoCultivo;
                OnPropertyChanged();
            }
        }

        public FormMode.FormModeSelect Mode
        {
            get => mode;
            set
            {
                mode = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsReadOnly));
                OnPropertyChanged(nameof(IsEditable));
                OnPropertyChanged(nameof(ShowSaveButton));
                OnPropertyChanged(nameof(Title));
            }
        }

        public string Nombre { get => nombre; set { nombre = value ?? string.Empty; OnPropertyChanged(); } }
        public string Descripcion { get => descripcion; set { descripcion = value ?? string.Empty; OnPropertyChanged(); } }
        public bool IsReadOnly => Mode == FormMode.FormModeSelect.View;
        public bool IsEditable => !IsReadOnly;
        public bool ShowSaveButton => !IsReadOnly;
        public string Title => Mode switch
        {
            FormMode.FormModeSelect.Create => "Crear tipo de cultivo",
            FormMode.FormModeSelect.Edit => "Editar tipo de cultivo",
            _ => "Detalle del tipo de cultivo"
        };

        private bool HasChanges() =>
            !string.Equals(Nombre.Trim(), Item.NombreTipoCultivo.Trim(), StringComparison.Ordinal) ||
            !string.Equals(Descripcion.Trim(), Item.DescripcionTipoCultivo.Trim(), StringComparison.Ordinal);

        private async Task CancelAsync()
        {
            if (!IsReadOnly && HasChanges())
            {
                Page? page = Application.Current?.MainPage;
                if (page != null && !await page.DisplayAlert("Cancelar", "¿Desea salir sin guardar los cambios?", "Sí", "No"))
                    return;
            }
            await GoToAsyncParameters(AppRoutes.TiposCultivo);
        }

        private async Task SaveAsync()
        {
            if (IsReadOnly || IsBusy) return;
            if (string.IsNullOrWhiteSpace(Nombre)) { await MostrarToastAsync("Ingrese el nombre del tipo de cultivo."); return; }
            if (!HasChanges()) { await MostrarToastAsync("No hay cambios para guardar."); return; }

            Page? page = Application.Current?.MainPage;
            if (page != null && !await page.DisplayAlert("Confirmar", "¿Desea guardar el tipo de cultivo?", "Sí", "No"))
                return;

            Item.NombreTipoCultivo = Nombre.Trim();
            Item.DescripcionTipoCultivo = Descripcion.Trim();
            IsBusy = true;
            try
            {
                ApiResult<bool> result = Mode == FormMode.FormModeSelect.Create
                    ? await apiService.CreateAsync(Item)
                    : await apiService.UpdateAsync(Item);

                if (!result.Success) { await MostrarToastAsync(result.Message); return; }
                await MostrarToastAsync(result.Message);
                await GoToAsyncParameters(AppRoutes.TiposCultivo);
            }
            finally { IsBusy = false; }
        }
    }
}
