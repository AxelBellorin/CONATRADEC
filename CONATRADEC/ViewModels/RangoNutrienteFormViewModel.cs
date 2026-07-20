using CONATRADEC.Models;
using CONATRADEC.Services;
using System.Collections.ObjectModel;

namespace CONATRADEC.ViewModels
{
    public class RangoNutrienteFormViewModel : GlobalService
    {
        private readonly RangoNutrienteApiService apiService = new();
        private readonly TipoCultivoApiService cultivoApiService = new();
        private readonly ElementoQuimicoApiService elementoApiService = new();
        private RangoNutrienteRequest item = new();
        private FormMode.FormModeSelect mode;
        private TipoCultivoResponse? cultivoSeleccionado;
        private ElementoQuimicoSelectorItem? elementoSeleccionado;
        private string minimoTexto = string.Empty;
        private string maximoTexto = string.Empty;
        private string unidadBase = string.Empty;
        private string descripcion = string.Empty;
        private bool initialized;

        public ObservableCollection<TipoCultivoResponse> Cultivos { get; } = new();
        public ObservableCollection<ElementoQuimicoSelectorItem> Elementos { get; } = new();
        public Command SaveCommand { get; }
        public Command CancelCommand { get; }

        public RangoNutrienteFormViewModel()
        {
            SaveCommand = new Command(async () => await SaveAsync());
            CancelCommand = new Command(async () => await CancelAsync());
        }

        public RangoNutrienteRequest Item
        {
            get => item;
            set
            {
                item = value ?? new RangoNutrienteRequest();
                MinimoTexto = item.ValorMinimo != 0 ? NumeroFormularioHelper.ToText(item.ValorMinimo) : "0";
                MaximoTexto = item.ValorMaximo > 0 ? NumeroFormularioHelper.ToText(item.ValorMaximo) : string.Empty;
                UnidadBase = item.UnidadBase;
                Descripcion = item.DescripcionParametro;
                SelectCurrentValues();
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

        public TipoCultivoResponse? CultivoSeleccionado { get => cultivoSeleccionado; set { cultivoSeleccionado = value; OnPropertyChanged(); } }
        public ElementoQuimicoSelectorItem? ElementoSeleccionado { get => elementoSeleccionado; set { elementoSeleccionado = value; OnPropertyChanged(); } }
        public string MinimoTexto { get => minimoTexto; set { minimoTexto = value ?? string.Empty; OnPropertyChanged(); } }
        public string MaximoTexto { get => maximoTexto; set { maximoTexto = value ?? string.Empty; OnPropertyChanged(); } }
        public string UnidadBase { get => unidadBase; set { unidadBase = value ?? string.Empty; OnPropertyChanged(); } }
        public string Descripcion { get => descripcion; set { descripcion = value ?? string.Empty; OnPropertyChanged(); } }
        public bool IsReadOnly => Mode == FormMode.FormModeSelect.View;
        public bool IsEditable => !IsReadOnly;
        public bool ShowSaveButton => !IsReadOnly;
        public string Title => Mode switch
        {
            FormMode.FormModeSelect.Create => "Crear rango nutricional",
            FormMode.FormModeSelect.Edit => "Editar rango nutricional",
            _ => "Detalle del rango nutricional"
        };

        public async Task InitializeAsync()
        {
            if (initialized) return;
            initialized = true;
            IsBusy = true;
            try
            {
                var cropTask = cultivoApiService.GetAsync();
                var elementTask = elementoApiService.GetElementoQuimicoResultAsync();
                await Task.WhenAll(cropTask, elementTask);

                var crops = await cropTask;
                var elements = await elementTask;

                if (!crops.Success) { await MostrarToastAsync(crops.Message); return; }
                if (!elements.Success) { await MostrarToastAsync(elements.Message); return; }

                Cultivos.Clear();
                foreach (var crop in (crops.Data ?? new ObservableCollection<TipoCultivoResponse>()).OrderBy(x => x.NombreTipoCultivo ?? string.Empty))
                    Cultivos.Add(crop);

                Elementos.Clear();
                foreach (var element in (elements.Data ?? new ObservableCollection<ElementoQuimicoResponse>())
                    .Where(x => (x.ElementoQuimicosId ?? 0) > 0)
                    .OrderBy(x => x.NombreElementoQuimico ?? string.Empty))
                    Elementos.Add(ElementoQuimicoSelectorItem.FromResponse(element));

                SelectCurrentValues();
            }
            finally { IsBusy = false; }
        }

        private void SelectCurrentValues()
        {
            if (Item.TipoCultivoId > 0 && Cultivos.Count > 0)
                CultivoSeleccionado = Cultivos.FirstOrDefault(x => x.TipoCultivoId == Item.TipoCultivoId);
            if (Item.ElementoQuimicosId > 0 && Elementos.Count > 0)
                ElementoSeleccionado = Elementos.FirstOrDefault(x => x.ElementoQuimicosId == Item.ElementoQuimicosId);
        }

        private bool TryGetValues(out decimal min, out decimal max)
        {
            min = 0; max = 0;
            if (CultivoSeleccionado == null) { _ = MostrarToastAsync("Seleccione un tipo de cultivo."); return false; }
            if (ElementoSeleccionado == null) { _ = MostrarToastAsync("Seleccione un elemento químico."); return false; }
            if (!NumeroFormularioHelper.TryParseDecimal(MinimoTexto, out min) || min < 0) { _ = MostrarToastAsync("El valor mínimo debe ser un número igual o mayor que cero."); return false; }
            if (!NumeroFormularioHelper.TryParseDecimal(MaximoTexto, out max) || max <= min) { _ = MostrarToastAsync("El valor máximo debe ser mayor que el valor mínimo."); return false; }
            if (string.IsNullOrWhiteSpace(UnidadBase)) { _ = MostrarToastAsync("Ingrese la unidad base."); return false; }
            if (string.IsNullOrWhiteSpace(Descripcion)) { _ = MostrarToastAsync("Ingrese la descripción del rango."); return false; }
            return true;
        }

        private bool HasChanges(decimal min, decimal max) =>
            (CultivoSeleccionado?.TipoCultivoId ?? 0) != Item.TipoCultivoId ||
            (ElementoSeleccionado?.ElementoQuimicosId ?? 0) != Item.ElementoQuimicosId ||
            min != Item.ValorMinimo ||
            max != Item.ValorMaximo ||
            !string.Equals(UnidadBase.Trim(), Item.UnidadBase.Trim(), StringComparison.Ordinal) ||
            !string.Equals(Descripcion.Trim(), Item.DescripcionParametro.Trim(), StringComparison.Ordinal);

        private async Task CancelAsync()
        {
            decimal min = 0;
            decimal max = 0;

            bool parsedMinimo =
                NumeroFormularioHelper.TryParseDecimal(MinimoTexto, out min);

            bool parsedMaximo =
                NumeroFormularioHelper.TryParseDecimal(MaximoTexto, out max);

            bool parsed = parsedMinimo && parsedMaximo;

            if (!IsReadOnly && parsed && HasChanges(min, max))
            {
                Page? page = Application.Current?.MainPage;
                if (page != null && !await page.DisplayAlert("Cancelar", "¿Desea salir sin guardar los cambios?", "Sí", "No")) return;
            }
            await GoToAsyncParameters(AppRoutes.RangosNutrientes);
        }

        private async Task SaveAsync()
        {
            if (IsReadOnly || IsBusy) return;
            if (!TryGetValues(out decimal min, out decimal max)) return;
            if (!HasChanges(min, max)) { await MostrarToastAsync("No hay cambios para guardar."); return; }

            Page? page = Application.Current?.MainPage;
            if (page != null && !await page.DisplayAlert("Confirmar", "¿Desea guardar el rango nutricional?", "Sí", "No")) return;

            Item.TipoCultivoId = CultivoSeleccionado!.TipoCultivoId;
            Item.ElementoQuimicosId = ElementoSeleccionado!.ElementoQuimicosId;
            Item.ValorMinimo = min;
            Item.ValorMaximo = max;
            Item.UnidadBase = UnidadBase.Trim();
            Item.DescripcionParametro = Descripcion.Trim();
            IsBusy = true;
            try
            {
                ApiResult<bool> result = Mode == FormMode.FormModeSelect.Create
                    ? await apiService.CreateAsync(Item)
                    : await apiService.UpdateAsync(Item);
                if (!result.Success) { await MostrarToastAsync(result.Message); return; }
                await MostrarToastAsync(result.Message);
                await GoToAsyncParameters(AppRoutes.RangosNutrientes);
            }
            finally { IsBusy = false; }
        }
    }
}
