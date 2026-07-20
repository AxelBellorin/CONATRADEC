using CONATRADEC.Models;
using CONATRADEC.Services;
using System.Collections.ObjectModel;

namespace CONATRADEC.ViewModels
{
    public class ExtraccionNutrienteFormViewModel : GlobalService
    {
        private readonly ExtraccionNutrienteApiService apiService = new();
        private readonly ElementoQuimicoApiService elementoApiService = new();
        private ExtraccionNutrienteRequest item = new();
        private FormMode.FormModeSelect mode;
        private ElementoQuimicoSelectorItem? elementoSeleccionado;
        private string cantidadTexto = string.Empty;
        private string descripcion = string.Empty;
        private bool initialized;

        public ObservableCollection<ElementoQuimicoSelectorItem> Elementos { get; } = new();
        public Command SaveCommand { get; }
        public Command CancelCommand { get; }

        public ExtraccionNutrienteFormViewModel()
        {
            SaveCommand = new Command(async () => await SaveAsync());
            CancelCommand = new Command(async () => await CancelAsync());
        }

        public ExtraccionNutrienteRequest Item
        {
            get => item;
            set
            {
                item = value ?? new ExtraccionNutrienteRequest();
                CantidadTexto = item.CantidadExtraidaPorQQOro > 0
                    ? NumeroFormularioHelper.ToText(item.CantidadExtraidaPorQQOro)
                    : string.Empty;
                Descripcion = item.DescripcionParametro;
                SelectCurrentElement();
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

        public ElementoQuimicoSelectorItem? ElementoSeleccionado
        {
            get => elementoSeleccionado;
            set { elementoSeleccionado = value; OnPropertyChanged(); }
        }

        public string CantidadTexto { get => cantidadTexto; set { cantidadTexto = value ?? string.Empty; OnPropertyChanged(); } }
        public string Descripcion { get => descripcion; set { descripcion = value ?? string.Empty; OnPropertyChanged(); } }
        public bool IsReadOnly => Mode == FormMode.FormModeSelect.View;
        public bool IsEditable => !IsReadOnly;
        public bool ShowSaveButton => !IsReadOnly;
        public string Title => Mode switch
        {
            FormMode.FormModeSelect.Create => "Crear extracción por quintal oro",
            FormMode.FormModeSelect.Edit => "Editar extracción por quintal oro",
            _ => "Detalle de extracción por quintal oro"
        };

        public async Task InitializeAsync()
        {
            if (initialized) return;
            initialized = true;
            IsBusy = true;
            try
            {
                var result = await elementoApiService.GetElementoQuimicoResultAsync();
                if (!result.Success) { await MostrarToastAsync(result.Message); return; }

                Elementos.Clear();
                foreach (ElementoQuimicoResponse element in
                    (result.Data ?? new ObservableCollection<ElementoQuimicoResponse>())
                    .Where(x => (x.ElementoQuimicosId ?? 0) > 0)
                    .OrderBy(x => x.NombreElementoQuimico ?? string.Empty))
                {
                    Elementos.Add(ElementoQuimicoSelectorItem.FromResponse(element));
                }

                SelectCurrentElement();
            }
            finally { IsBusy = false; }
        }

        private void SelectCurrentElement()
        {
            if (Item.ElementoQuimicosId <= 0 || Elementos.Count == 0) return;
            ElementoSeleccionado = Elementos.FirstOrDefault(x => x.ElementoQuimicosId == Item.ElementoQuimicosId);
        }

        private bool TryGetValues(out decimal cantidad)
        {
            cantidad = 0;
            if (ElementoSeleccionado == null) { _ = MostrarToastAsync("Seleccione un elemento químico."); return false; }
            if (!NumeroFormularioHelper.TryParseDecimal(CantidadTexto, out cantidad) || cantidad <= 0) { _ = MostrarToastAsync("Ingrese una cantidad mayor que cero."); return false; }
            if (string.IsNullOrWhiteSpace(Descripcion)) { _ = MostrarToastAsync("Ingrese la descripción del parámetro."); return false; }
            return true;
        }

        private bool HasChanges(decimal cantidad) =>
            (ElementoSeleccionado?.ElementoQuimicosId ?? 0) != Item.ElementoQuimicosId ||
            cantidad != Item.CantidadExtraidaPorQQOro ||
            !string.Equals(Descripcion.Trim(), Item.DescripcionParametro.Trim(), StringComparison.Ordinal);

        private async Task CancelAsync()
        {
            bool changed = NumeroFormularioHelper.TryParseDecimal(CantidadTexto, out decimal value) && HasChanges(value);
            if (!IsReadOnly && changed)
            {
                Page? page = Application.Current?.MainPage;
                if (page != null && !await page.DisplayAlert("Cancelar", "¿Desea salir sin guardar los cambios?", "Sí", "No")) return;
            }
            await GoToAsyncParameters(AppRoutes.ExtraccionNutrientes);
        }

        private async Task SaveAsync()
        {
            if (IsReadOnly || IsBusy) return;
            if (!TryGetValues(out decimal cantidad)) return;
            if (!HasChanges(cantidad)) { await MostrarToastAsync("No hay cambios para guardar."); return; }

            Page? page = Application.Current?.MainPage;
            if (page != null && !await page.DisplayAlert("Confirmar", "¿Desea guardar el parámetro de extracción?", "Sí", "No")) return;

            Item.ElementoQuimicosId = ElementoSeleccionado!.ElementoQuimicosId;
            Item.CantidadExtraidaPorQQOro = cantidad;
            Item.DescripcionParametro = Descripcion.Trim();
            IsBusy = true;
            try
            {
                ApiResult<bool> result = Mode == FormMode.FormModeSelect.Create
                    ? await apiService.CreateAsync(Item)
                    : await apiService.UpdateAsync(Item);
                if (!result.Success) { await MostrarToastAsync(result.Message); return; }
                await MostrarToastAsync(result.Message);
                await GoToAsyncParameters(AppRoutes.ExtraccionNutrientes);
            }
            finally { IsBusy = false; }
        }
    }
}
