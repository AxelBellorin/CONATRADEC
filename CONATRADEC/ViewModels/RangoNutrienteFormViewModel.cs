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

        private string errorCultivo = string.Empty;
        private string errorElemento = string.Empty;
        private string errorMinimo = string.Empty;
        private string errorMaximo = string.Empty;
        private string errorUnidad = string.Empty;
        private string errorDescripcion = string.Empty;

        public ObservableCollection<TipoCultivoResponse>
            Cultivos { get; } = new();

        public ObservableCollection<ElementoQuimicoSelectorItem>
            Elementos { get; } = new();

        public Command SaveCommand { get; }
        public Command CancelCommand { get; }

        public RangoNutrienteFormViewModel()
        {
            SaveCommand = new Command(
                async () => await SaveAsync(),
                () => !IsReadOnly && !IsBusy);

            CancelCommand = new Command(
                async () => await CancelAsync(),
                () => !IsBusy);
        }

        public RangoNutrienteRequest Item
        {
            get => item;
            set
            {
                item = value ?? new RangoNutrienteRequest();

                MinimoTexto = item.ValorMinimo != 0
                    ? NumeroFormularioHelper.ToText(
                        item.ValorMinimo)
                    : "0";

                MaximoTexto = item.ValorMaximo > 0
                    ? NumeroFormularioHelper.ToText(
                        item.ValorMaximo)
                    : string.Empty;

                UnidadBase = item.UnidadBase ?? string.Empty;
                Descripcion =
                    item.DescripcionParametro ?? string.Empty;

                SelectCurrentValues();
                LimpiarErrores();
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
                RefrescarComandos();
            }
        }

        public TipoCultivoResponse? CultivoSeleccionado
        {
            get => cultivoSeleccionado;
            set
            {
                cultivoSeleccionado = value;
                OnPropertyChanged();

                if (cultivoSeleccionado != null)
                    ErrorCultivo = string.Empty;
            }
        }

        public ElementoQuimicoSelectorItem?
            ElementoSeleccionado
        {
            get => elementoSeleccionado;
            set
            {
                elementoSeleccionado = value;
                OnPropertyChanged();

                if (elementoSeleccionado != null)
                    ErrorElemento = string.Empty;
            }
        }

        public string MinimoTexto
        {
            get => minimoTexto;
            set
            {
                minimoTexto = value ?? string.Empty;
                OnPropertyChanged();

                if (NumeroFormularioHelper.TryParseDecimal(
                        minimoTexto,
                        out decimal minimo) &&
                    minimo >= 0)
                {
                    ErrorMinimo = string.Empty;
                }
            }
        }

        public string MaximoTexto
        {
            get => maximoTexto;
            set
            {
                maximoTexto = value ?? string.Empty;
                OnPropertyChanged();
            }
        }

        public string UnidadBase
        {
            get => unidadBase;
            set
            {
                unidadBase = value ?? string.Empty;
                OnPropertyChanged();

                if (!string.IsNullOrWhiteSpace(unidadBase))
                    ErrorUnidad = string.Empty;
            }
        }

        public string Descripcion
        {
            get => descripcion;
            set
            {
                descripcion = value ?? string.Empty;
                OnPropertyChanged();

                if (!string.IsNullOrWhiteSpace(descripcion))
                    ErrorDescripcion = string.Empty;
            }
        }

        public string ErrorCultivo
        {
            get => errorCultivo;
            private set
            {
                if (errorCultivo == value)
                    return;

                errorCultivo = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TieneErrorCultivo));
            }
        }

        public bool TieneErrorCultivo =>
            !string.IsNullOrWhiteSpace(ErrorCultivo);

        public string ErrorElemento
        {
            get => errorElemento;
            private set
            {
                if (errorElemento == value)
                    return;

                errorElemento = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TieneErrorElemento));
            }
        }

        public bool TieneErrorElemento =>
            !string.IsNullOrWhiteSpace(ErrorElemento);

        public string ErrorMinimo
        {
            get => errorMinimo;
            private set
            {
                if (errorMinimo == value)
                    return;

                errorMinimo = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TieneErrorMinimo));
            }
        }

        public bool TieneErrorMinimo =>
            !string.IsNullOrWhiteSpace(ErrorMinimo);

        public string ErrorMaximo
        {
            get => errorMaximo;
            private set
            {
                if (errorMaximo == value)
                    return;

                errorMaximo = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TieneErrorMaximo));
            }
        }

        public bool TieneErrorMaximo =>
            !string.IsNullOrWhiteSpace(ErrorMaximo);

        public string ErrorUnidad
        {
            get => errorUnidad;
            private set
            {
                if (errorUnidad == value)
                    return;

                errorUnidad = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TieneErrorUnidad));
            }
        }

        public bool TieneErrorUnidad =>
            !string.IsNullOrWhiteSpace(ErrorUnidad);

        public string ErrorDescripcion
        {
            get => errorDescripcion;
            private set
            {
                if (errorDescripcion == value)
                    return;

                errorDescripcion = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TieneErrorDescripcion));
            }
        }

        public bool TieneErrorDescripcion =>
            !string.IsNullOrWhiteSpace(ErrorDescripcion);

        public bool IsReadOnly =>
            Mode == FormMode.FormModeSelect.View;

        public bool IsEditable => !IsReadOnly;

        public bool ShowSaveButton => !IsReadOnly;

        public string Title =>
            Mode switch
            {
                FormMode.FormModeSelect.Create =>
                    "Crear rango nutricional",
                FormMode.FormModeSelect.Edit =>
                    "Editar rango nutricional",
                _ =>
                    "Detalle del rango nutricional"
            };

        public async Task InitializeAsync()
        {
            if (initialized)
                return;

            initialized = true;
            IsBusy = true;
            RefrescarComandos();

            try
            {
                var cropTask =
                    cultivoApiService.GetAsync();

                var elementTask =
                    elementoApiService
                        .GetElementoQuimicoResultAsync();

                await Task.WhenAll(
                    cropTask,
                    elementTask);

                var crops = await cropTask;
                var elements = await elementTask;

                if (!crops.Success)
                {
                    await MostrarErrorAsync(crops.Message);
                    initialized = false;
                    return;
                }

                if (!elements.Success)
                {
                    await MostrarErrorAsync(elements.Message);
                    initialized = false;
                    return;
                }

                Cultivos.Clear();

                foreach (var crop in
                         (crops.Data ??
                          new ObservableCollection<
                              TipoCultivoResponse>())
                         .OrderBy(x =>
                             x.NombreTipoCultivo
                             ?? string.Empty))
                {
                    Cultivos.Add(crop);
                }

                Elementos.Clear();

                foreach (var element in
                         (elements.Data ??
                          new ObservableCollection<
                              ElementoQuimicoResponse>())
                         .Where(x =>
                             (x.ElementoQuimicosId ?? 0) > 0)
                         .OrderBy(x =>
                             x.NombreElementoQuimico
                             ?? string.Empty))
                {
                    Elementos.Add(
                        ElementoQuimicoSelectorItem
                            .FromResponse(element));
                }

                SelectCurrentValues();
            }
            catch (Exception ex)
            {
                initialized = false;

                await MostrarErrorInesperadoAsync(
                    "cargar los catálogos del rango nutricional",
                    ex);
            }
            finally
            {
                IsBusy = false;
                RefrescarComandos();
            }
        }

        private void SelectCurrentValues()
        {
            if (Item.TipoCultivoId > 0 &&
                Cultivos.Count > 0)
            {
                CultivoSeleccionado =
                    Cultivos.FirstOrDefault(x =>
                        x.TipoCultivoId ==
                        Item.TipoCultivoId);
            }

            if (Item.ElementoQuimicosId > 0 &&
                Elementos.Count > 0)
            {
                ElementoSeleccionado =
                    Elementos.FirstOrDefault(x =>
                        x.ElementoQuimicosId ==
                        Item.ElementoQuimicosId);
            }
        }

        private bool TryGetValues(
            out decimal min,
            out decimal max)
        {
            LimpiarErrores();
            min = 0;
            max = 0;

            if (CultivoSeleccionado == null)
            {
                ErrorCultivo =
                    "Seleccione un tipo de cultivo.";
            }

            if (ElementoSeleccionado == null)
            {
                ErrorElemento =
                    "Seleccione un elemento químico.";
            }

            if (!NumeroFormularioHelper.TryParseDecimal(
                    MinimoTexto,
                    out min) ||
                min < 0)
            {
                ErrorMinimo =
                    "El valor mínimo debe ser un número igual o mayor que cero.";
            }

            if (!NumeroFormularioHelper.TryParseDecimal(
                    MaximoTexto,
                    out max))
            {
                ErrorMaximo =
                    "Ingrese un valor máximo válido.";
            }
            else if (max <= min)
            {
                ErrorMaximo =
                    "El valor máximo debe ser mayor que el valor mínimo.";
            }

            UnidadBase = UnidadBase.Trim();

            if (string.IsNullOrWhiteSpace(UnidadBase))
            {
                ErrorUnidad =
                    "Ingrese la unidad base.";
            }

            Descripcion = Descripcion.Trim();

            if (string.IsNullOrWhiteSpace(Descripcion))
            {
                ErrorDescripcion =
                    "Ingrese la descripción del rango.";
            }

            return
                !TieneErrorCultivo &&
                !TieneErrorElemento &&
                !TieneErrorMinimo &&
                !TieneErrorMaximo &&
                !TieneErrorUnidad &&
                !TieneErrorDescripcion;
        }

        private bool HasChanges(
            decimal min,
            decimal max) =>
            (CultivoSeleccionado?.TipoCultivoId
             ?? 0) != Item.TipoCultivoId ||
            (ElementoSeleccionado?.ElementoQuimicosId
             ?? 0) != Item.ElementoQuimicosId ||
            min != Item.ValorMinimo ||
            max != Item.ValorMaximo ||
            !string.Equals(
                UnidadBase.Trim(),
                Item.UnidadBase?.Trim() ?? string.Empty,
                StringComparison.Ordinal) ||
            !string.Equals(
                Descripcion.Trim(),
                Item.DescripcionParametro?.Trim()
                    ?? string.Empty,
                StringComparison.Ordinal);

        private async Task CancelAsync()
        {
            decimal min = 0;
            decimal max = 0;

            bool parsed =
                NumeroFormularioHelper.TryParseDecimal(
                    MinimoTexto,
                    out min) &&
                NumeroFormularioHelper.TryParseDecimal(
                    MaximoTexto,
                    out max);

            if (!IsReadOnly &&
                parsed &&
                HasChanges(min, max))
            {
                bool confirm =
                    await ConfirmarSalidaSinGuardarAsync();

                if (!confirm)
                    return;
            }

            await GoToAsyncParameters(
                AppRoutes.RangosNutrientes);
        }

        private async Task SaveAsync()
        {
            if (IsReadOnly || IsBusy)
                return;

            if (!TryGetValues(
                    out decimal min,
                    out decimal max))
            {
                await MostrarAdvertenciaAsync(
                    "Revise los campos marcados antes de continuar.");
                return;
            }

            if (!HasChanges(min, max))
            {
                await MostrarInformacionAsync(
                    "No hay cambios para guardar.");
                return;
            }

            bool confirm =
                Mode == FormMode.FormModeSelect.Create
                    ? await ConfirmarGuardadoAsync(
                        "el rango nutricional")
                    : await ConfirmarActualizacionAsync(
                        "el rango nutricional");

            if (!confirm)
                return;

            Item.TipoCultivoId =
                CultivoSeleccionado!.TipoCultivoId;

            Item.ElementoQuimicosId =
                ElementoSeleccionado!
                    .ElementoQuimicosId;

            Item.ValorMinimo = min;
            Item.ValorMaximo = max;
            Item.UnidadBase = UnidadBase.Trim();
            Item.DescripcionParametro =
                Descripcion.Trim();

            try
            {
                IsBusy = true;
                RefrescarComandos();

                ApiResult<bool> result =
                    Mode == FormMode.FormModeSelect.Create
                        ? await apiService.CreateAsync(Item)
                        : await apiService.UpdateAsync(Item);

                if (!result.Success)
                {
                    await MostrarErrorAsync(result.Message);
                    return;
                }

                await GoToAsyncParameters(
                    AppRoutes.RangosNutrientes);

                await MostrarExitoAsync(result.Message);
            }
            catch (Exception ex)
            {
                await MostrarErrorInesperadoAsync(
                    Mode == FormMode.FormModeSelect.Create
                        ? "guardar el rango nutricional"
                        : "actualizar el rango nutricional",
                    ex);
            }
            finally
            {
                IsBusy = false;
                RefrescarComandos();
            }
        }

        private void LimpiarErrores()
        {
            ErrorCultivo = string.Empty;
            ErrorElemento = string.Empty;
            ErrorMinimo = string.Empty;
            ErrorMaximo = string.Empty;
            ErrorUnidad = string.Empty;
            ErrorDescripcion = string.Empty;
        }

        private void RefrescarComandos()
        {
            SaveCommand.ChangeCanExecute();
            CancelCommand.ChangeCanExecute();
        }
    }
}
