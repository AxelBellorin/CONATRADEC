using CONATRADEC.Models;
using CONATRADEC.Services;
using System.Collections.ObjectModel;
using System.Globalization;

namespace CONATRADEC.ViewModels
{
    public sealed class RangoNutrienteFormViewModel : GlobalService
    {
        public const string UnidadBaseFija =
            "lb/Mz";

        private readonly RangoNutrienteApiService
            apiService = new();

        private readonly RangoNutrienteConsultaApiService
            consultaApiService = new();

        private CancellationTokenSource? operacionCts;

        private RangoNutrienteRequest item = new();
        private FormMode.FormModeSelect mode;
        private RangoNutrienteCategoriaItem? categoria;
        private ElementoQuimicoSelectorItem? elementoSeleccionado;
        private string minimoTexto = string.Empty;
        private string maximoTexto = string.Empty;
        private string descripcion = string.Empty;
        private string errorTipoCultivo = string.Empty;
        private string errorElemento = string.Empty;
        private string errorMinimo = string.Empty;
        private string errorMaximo = string.Empty;
        private string errorDescripcion = string.Empty;
        private bool inicializando;

        public ObservableCollection<ElementoQuimicoSelectorItem>
            Elementos { get; } = new();

        public Command SaveCommand { get; }
        public Command CancelCommand { get; }

        public RangoNutrienteFormViewModel()
        {
            SaveCommand =
                new Command(
                    async () => await SaveAsync(),
                    () => !IsReadOnly && !IsBusy);

            CancelCommand =
                new Command(
                    async () => await CancelAsync(),
                    () => !IsBusy);
        }

        public RangoNutrienteRequest Item
        {
            get => item;
            private set
            {
                item = value ?? new RangoNutrienteRequest();

                if (Categoria != null)
                    item.TipoCultivoId = Categoria.TipoCultivoId;

                item.UnidadBase = UnidadBaseFija;

                MinimoTexto =
                    item.ParametroRangoNutrienteCultivoId > 0
                        ? FormatearDosDecimales(item.ValorMinimo)
                        : string.Empty;

                MaximoTexto =
                    item.ParametroRangoNutrienteCultivoId > 0
                        ? FormatearDosDecimales(item.ValorMaximo)
                        : string.Empty;

                Descripcion =
                    item.DescripcionParametro ?? string.Empty;

                ElementoSeleccionado = null;
                LimpiarErrores();
                OnPropertyChanged();
            }
        }

        public FormMode.FormModeSelect Mode
        {
            get => mode;
            private set
            {
                if (mode == value)
                    return;

                mode = value;

                OnPropertyChanged();
                OnPropertyChanged(nameof(IsReadOnly));
                OnPropertyChanged(nameof(IsEditable));
                OnPropertyChanged(nameof(ShowSaveButton));
                OnPropertyChanged(nameof(Title));
                OnPropertyChanged(nameof(Subtitulo));

                RefrescarComandos();
            }
        }

        public RangoNutrienteCategoriaItem? Categoria
        {
            get => categoria;
            private set
            {
                categoria = value;

                OnPropertyChanged();
                OnPropertyChanged(nameof(TipoCultivoNombre));
                OnPropertyChanged(nameof(TieneTipoCultivoValido));

                if (categoria != null)
                    ErrorTipoCultivo = string.Empty;
            }
        }

        public string TipoCultivoNombre =>
            Categoria?.NombreCategoria ?? string.Empty;

        public bool TieneTipoCultivoValido =>
            Categoria != null &&
            Categoria.TipoCultivoId > 0;

        public string UnidadBase =>
            UnidadBaseFija;

        public string UnidadBaseDescripcion =>
            "lb/Mz (libras por manzana)";

        public ElementoQuimicoSelectorItem? ElementoSeleccionado
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

                if (TryParseDosDecimales(
                        minimoTexto,
                        out decimal minimo) &&
                    minimo > 0)
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

                if (TryParseDosDecimales(
                        maximoTexto,
                        out decimal maximo) &&
                    maximo > 0)
                {
                    ErrorMaximo = string.Empty;
                }
            }
        }

        public string Descripcion
        {
            get => descripcion;
            set
            {
                descripcion = value ?? string.Empty;
                OnPropertyChanged();

                if (!string.IsNullOrWhiteSpace(descripcion) &&
                    descripcion.Trim().Length <= 150)
                {
                    ErrorDescripcion = string.Empty;
                }
            }
        }

        public string ErrorTipoCultivo
        {
            get => errorTipoCultivo;
            private set
            {
                if (errorTipoCultivo == value)
                    return;

                errorTipoCultivo = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TieneErrorTipoCultivo));
            }
        }

        public bool TieneErrorTipoCultivo =>
            !string.IsNullOrWhiteSpace(ErrorTipoCultivo);

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

        public bool IsEditable =>
            !IsReadOnly;

        public bool ShowSaveButton =>
            !IsReadOnly;

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

        public string Subtitulo =>
            Mode switch
            {
                FormMode.FormModeSelect.Create =>
                    "Defina el intervalo utilizado para evaluar el elemento.",
                FormMode.FormModeSelect.Edit =>
                    "Actualice el intervalo del elemento seleccionado.",
                _ =>
                    "Consulte la configuración del intervalo."
            };

        public void PrepararNavegacion(
            FormMode.FormModeSelect nuevoModo,
            RangoNutrienteCategoriaItem tipoCultivo,
            RangoNutrienteRequest nuevoItem)
        {
            Mode = nuevoModo;
            Categoria = tipoCultivo;
            Item = nuevoItem ?? new RangoNutrienteRequest();

            Item.TipoCultivoId = tipoCultivo.TipoCultivoId;
            Item.UnidadBase = UnidadBaseFija;
        }

        public async Task InitializeAsync()
        {
            if (inicializando ||
                !TieneTipoCultivoValido)
            {
                return;
            }

            inicializando = true;

            operacionCts?.Cancel();
            operacionCts?.Dispose();
            operacionCts = new CancellationTokenSource();

            try
            {
                IsBusy = true;
                RefrescarComandos();

                ApiResult<List<ElementoQuimicoSelectorItem>> resultado =
                    await consultaApiService
                        .ObtenerElementosDisponiblesAsync(
                            Categoria!.TipoCultivoId,
                            Item.ParametroRangoNutrienteCultivoId,
                            operacionCts.Token);

                if (!resultado.Success ||
                    resultado.Data == null)
                {
                    await MostrarErrorAsync(resultado.Message);
                    return;
                }

                Elementos.Clear();

                foreach (ElementoQuimicoSelectorItem elemento
                         in resultado.Data)
                {
                    Elementos.Add(elemento);
                }

                ElementoSeleccionado =
                    Item.ElementoQuimicosId > 0
                        ? Elementos.FirstOrDefault(elemento =>
                            elemento.ElementoQuimicosId ==
                                Item.ElementoQuimicosId)
                        : null;

                if (Mode == FormMode.FormModeSelect.Create &&
                    Elementos.Count == 0)
                {
                    await MostrarInformacionAsync(
                        "Todos los elementos químicos activos ya tienen " +
                        "un rango configurado para este cultivo.");
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                await MostrarErrorInesperadoAsync(
                    "cargar los elementos disponibles",
                    ex);
            }
            finally
            {
                inicializando = false;
                IsBusy = false;
                RefrescarComandos();
            }
        }

        public void CancelarOperaciones()
        {
            try
            {
                operacionCts?.Cancel();
            }
            catch (ObjectDisposedException)
            {
            }
        }

        private bool TryGetValues(
            out decimal minimo,
            out decimal maximo)
        {
            LimpiarErrores();

            minimo = 0;
            maximo = 0;

            if (!TieneTipoCultivoValido)
            {
                ErrorTipoCultivo =
                    "No se recibió un tipo de cultivo válido.";
            }

            if (ElementoSeleccionado == null)
            {
                ErrorElemento =
                    "Seleccione un elemento químico.";
            }

            if (!TryParseDosDecimales(MinimoTexto, out minimo) ||
                minimo <= 0)
            {
                ErrorMinimo =
                    "Ingrese un mínimo mayor que cero con máximo dos decimales.";
            }

            if (!TryParseDosDecimales(MaximoTexto, out maximo))
            {
                ErrorMaximo =
                    "Ingrese un máximo válido con máximo dos decimales.";
            }
            else if (maximo <= minimo)
            {
                ErrorMaximo =
                    "El máximo debe ser mayor que el mínimo.";
            }

            Descripcion = Descripcion.Trim();

            if (string.IsNullOrWhiteSpace(Descripcion))
            {
                ErrorDescripcion =
                    "Ingrese la descripción del rango.";
            }
            else if (Descripcion.Length > 150)
            {
                ErrorDescripcion =
                    "La descripción no puede superar 150 caracteres.";
            }

            return
                !TieneErrorTipoCultivo &&
                !TieneErrorElemento &&
                !TieneErrorMinimo &&
                !TieneErrorMaximo &&
                !TieneErrorDescripcion;
        }

        private bool HasChanges(
            decimal minimo,
            decimal maximo) =>
            (Categoria?.TipoCultivoId ?? 0) != Item.TipoCultivoId ||
            (ElementoSeleccionado?.ElementoQuimicosId ?? 0) !=
                Item.ElementoQuimicosId ||
            Math.Round(minimo, 2) != Math.Round(Item.ValorMinimo, 2) ||
            Math.Round(maximo, 2) != Math.Round(Item.ValorMaximo, 2) ||
            !string.Equals(
                UnidadBaseFija,
                Item.UnidadBase?.Trim() ?? string.Empty,
                StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(
                Descripcion.Trim(),
                Item.DescripcionParametro?.Trim() ?? string.Empty,
                StringComparison.Ordinal);

        private bool TieneCambiosPendientes()
        {
            if (Mode == FormMode.FormModeSelect.Create)
            {
                return
                    ElementoSeleccionado != null ||
                    !string.IsNullOrWhiteSpace(MinimoTexto) ||
                    !string.IsNullOrWhiteSpace(MaximoTexto) ||
                    !string.IsNullOrWhiteSpace(Descripcion);
            }

            if (!TryParseDosDecimales(
                    MinimoTexto,
                    out decimal minimo) ||
                !TryParseDosDecimales(
                    MaximoTexto,
                    out decimal maximo))
            {
                return true;
            }

            return HasChanges(minimo, maximo);
        }

        private async Task CancelAsync()
        {
            if (!IsReadOnly &&
                TieneCambiosPendientes())
            {
                bool confirmar =
                    await ConfirmarSalidaSinGuardarAsync();

                if (!confirmar)
                    return;
            }

            await RegresarADetalleAsync();
        }

        private async Task SaveAsync()
        {
            if (IsReadOnly || IsBusy)
                return;

            if (!TryGetValues(
                    out decimal minimo,
                    out decimal maximo))
            {
                await MostrarAdvertenciaAsync(
                    "Revise los campos marcados antes de continuar.");

                return;
            }

            minimo = Math.Round(minimo, 2);
            maximo = Math.Round(maximo, 2);

            if (!HasChanges(minimo, maximo))
            {
                await MostrarInformacionAsync(
                    "No hay cambios para guardar.");

                return;
            }

            /*
             * Si el usuario no cambió visualmente el intervalo,
             * se conserva el valor original de cuatro decimales.
             * Así una edición únicamente descriptiva no altera
             * la precisión histórica almacenada.
             */
            decimal minimoEnviar =
                Math.Round(Item.ValorMinimo, 2) == minimo
                    ? Item.ValorMinimo
                    : minimo;

            decimal maximoEnviar =
                Math.Round(Item.ValorMaximo, 2) == maximo
                    ? Item.ValorMaximo
                    : maximo;

            bool confirmar =
                Mode == FormMode.FormModeSelect.Create
                    ? await ConfirmarGuardadoAsync(
                        "el rango nutricional")
                    : await ConfirmarActualizacionAsync(
                        "el rango nutricional");

            if (!confirmar)
                return;

            Item.TipoCultivoId =
                Categoria!.TipoCultivoId;

            Item.ElementoQuimicosId =
                ElementoSeleccionado!.ElementoQuimicosId;

            Item.ValorMinimo = minimoEnviar;
            Item.ValorMaximo = maximoEnviar;
            Item.UnidadBase = UnidadBaseFija;
            Item.DescripcionParametro = Descripcion.Trim();

            operacionCts?.Cancel();
            operacionCts?.Dispose();
            operacionCts = new CancellationTokenSource();

            try
            {
                IsBusy = true;
                RefrescarComandos();

                ApiResult<bool> resultado =
                    Mode == FormMode.FormModeSelect.Create
                        ? await apiService.CreateDesdeRangosAsync(
                            Item,
                            operacionCts.Token)
                        : await apiService.UpdateDesdeRangosAsync(
                            Item,
                            operacionCts.Token);

                if (!resultado.Success)
                {
                    await MostrarErrorAsync(resultado.Message);
                    return;
                }

                RangoNutrienteVisitaService
                    .MarcarDetalleParaRecargar(
                        Categoria.TipoCultivoId);

                RangoNutrienteVisitaService
                    .MarcarListadoPrincipalParaRecargar();

                await RegresarADetalleAsync();

                await MostrarExitoAsync(resultado.Message);
            }
            catch (OperationCanceledException)
            {
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

        private Task RegresarADetalleAsync()
        {
            if (!TieneTipoCultivoValido)
            {
                return GoToAsyncParameters(
                    AppRoutes.RangosNutrientes);
            }

            return GoToAsyncParameters(
                AppRoutes.Regresar);
        }

        private static bool TryParseDosDecimales(
            string? texto,
            out decimal valor)
        {
            valor = 0;

            if (!NumeroFormularioHelper.TryParseDecimal(
                    texto,
                    out valor))
            {
                return false;
            }

            string limpio =
                (texto ?? string.Empty)
                    .Trim();

            int separador =
                Math.Max(
                    limpio.LastIndexOf('.'),
                    limpio.LastIndexOf(','));

            if (separador < 0)
                return true;

            return limpio.Length - separador - 1 <= 2;
        }

        private static string FormatearDosDecimales(
            decimal valor) =>
            Math.Round(valor, 2)
                .ToString(
                    "0.00",
                    CultureInfo.InvariantCulture);

        private void LimpiarErrores()
        {
            ErrorTipoCultivo = string.Empty;
            ErrorElemento = string.Empty;
            ErrorMinimo = string.Empty;
            ErrorMaximo = string.Empty;
            ErrorDescripcion = string.Empty;
        }

        private void RefrescarComandos()
        {
            SaveCommand.ChangeCanExecute();
            CancelCommand.ChangeCanExecute();
        }
    }
}
