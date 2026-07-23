using CONATRADEC.Models;
using CONATRADEC.Services;
using System.Collections.ObjectModel;

namespace CONATRADEC.ViewModels
{
    public class RangoNutrienteFormViewModel :
        GlobalService
    {
        public const string UnidadBaseFija =
            "lb/Mz";

        private readonly RangoNutrienteApiService
            apiService = new();

        private readonly ElementoQuimicoApiService
            elementoApiService = new();

        private RangoNutrienteRequest item = new();

        private FormMode.FormModeSelect mode;

        private RangoNutrienteCategoriaItem?
            categoria;

        private ElementoQuimicoSelectorItem?
            elementoSeleccionado;

        private string minimoTexto = "0";
        private string maximoTexto = string.Empty;
        private string descripcion = string.Empty;

        private bool loading;

        private string errorTipoCultivo =
            string.Empty;

        private string errorElemento =
            string.Empty;

        private string errorMinimo =
            string.Empty;

        private string errorMaximo =
            string.Empty;

        private string errorDescripcion =
            string.Empty;

        public ObservableCollection<
            ElementoQuimicoSelectorItem> Elementos
        {
            get;
        } = new();

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
                item =
                    value ??
                    new RangoNutrienteRequest();

                if (Categoria != null)
                {
                    item.TipoCultivoId =
                        Categoria.TipoCultivoId;
                }

                item.UnidadBase =
                    UnidadBaseFija;

                MinimoTexto =
                    item.ValorMinimo != 0
                        ? NumeroFormularioHelper.ToText(
                            item.ValorMinimo)
                        : "0";

                MaximoTexto =
                    item.ValorMaximo > 0
                        ? NumeroFormularioHelper.ToText(
                            item.ValorMaximo)
                        : string.Empty;

                Descripcion =
                    item.DescripcionParametro ??
                    string.Empty;

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
                mode = value;

                OnPropertyChanged();
                OnPropertyChanged(nameof(IsReadOnly));
                OnPropertyChanged(nameof(IsEditable));
                OnPropertyChanged(nameof(ShowSaveButton));
                OnPropertyChanged(nameof(Title));

                RefrescarComandos();
            }
        }

        public RangoNutrienteCategoriaItem?
            Categoria
        {
            get => categoria;
            private set
            {
                categoria = value;

                OnPropertyChanged();
                OnPropertyChanged(
                    nameof(TipoCultivoNombre));
                OnPropertyChanged(
                    nameof(TieneTipoCultivoValido));

                if (categoria != null)
                {
                    ErrorTipoCultivo =
                        string.Empty;
                }
            }
        }

        public string TipoCultivoNombre =>
            Categoria?.NombreCategoria ??
            string.Empty;

        public bool TieneTipoCultivoValido =>
            Categoria != null &&
            Categoria.TipoCultivoId > 0;

        public string UnidadBase =>
            UnidadBaseFija;

        public string UnidadBaseDescripcion =>
            "lb/Mz (libras por manzana)";

        public ElementoQuimicoSelectorItem?
            ElementoSeleccionado
        {
            get => elementoSeleccionado;
            set
            {
                elementoSeleccionado = value;
                OnPropertyChanged();

                if (elementoSeleccionado != null)
                {
                    ErrorElemento =
                        string.Empty;
                }
            }
        }

        public string MinimoTexto
        {
            get => minimoTexto;
            set
            {
                minimoTexto =
                    value ?? string.Empty;

                OnPropertyChanged();

                if (NumeroFormularioHelper
                        .TryParseDecimal(
                            minimoTexto,
                            out decimal minimo) &&
                    minimo >= 0)
                {
                    ErrorMinimo =
                        string.Empty;
                }
            }
        }

        public string MaximoTexto
        {
            get => maximoTexto;
            set
            {
                maximoTexto =
                    value ?? string.Empty;

                OnPropertyChanged();

                if (NumeroFormularioHelper
                        .TryParseDecimal(
                            maximoTexto,
                            out decimal maximo) &&
                    maximo > 0)
                {
                    ErrorMaximo =
                        string.Empty;
                }
            }
        }

        public string Descripcion
        {
            get => descripcion;
            set
            {
                descripcion =
                    value ?? string.Empty;

                OnPropertyChanged();

                if (!string.IsNullOrWhiteSpace(
                        descripcion))
                {
                    ErrorDescripcion =
                        string.Empty;
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
                OnPropertyChanged(
                    nameof(TieneErrorTipoCultivo));
            }
        }

        public bool TieneErrorTipoCultivo =>
            !string.IsNullOrWhiteSpace(
                ErrorTipoCultivo);

        public string ErrorElemento
        {
            get => errorElemento;
            private set
            {
                if (errorElemento == value)
                    return;

                errorElemento = value;

                OnPropertyChanged();
                OnPropertyChanged(
                    nameof(TieneErrorElemento));
            }
        }

        public bool TieneErrorElemento =>
            !string.IsNullOrWhiteSpace(
                ErrorElemento);

        public string ErrorMinimo
        {
            get => errorMinimo;
            private set
            {
                if (errorMinimo == value)
                    return;

                errorMinimo = value;

                OnPropertyChanged();
                OnPropertyChanged(
                    nameof(TieneErrorMinimo));
            }
        }

        public bool TieneErrorMinimo =>
            !string.IsNullOrWhiteSpace(
                ErrorMinimo);

        public string ErrorMaximo
        {
            get => errorMaximo;
            private set
            {
                if (errorMaximo == value)
                    return;

                errorMaximo = value;

                OnPropertyChanged();
                OnPropertyChanged(
                    nameof(TieneErrorMaximo));
            }
        }

        public bool TieneErrorMaximo =>
            !string.IsNullOrWhiteSpace(
                ErrorMaximo);

        public string ErrorDescripcion
        {
            get => errorDescripcion;
            private set
            {
                if (errorDescripcion == value)
                    return;

                errorDescripcion = value;

                OnPropertyChanged();
                OnPropertyChanged(
                    nameof(TieneErrorDescripcion));
            }
        }

        public bool TieneErrorDescripcion =>
            !string.IsNullOrWhiteSpace(
                ErrorDescripcion);

        public bool IsReadOnly =>
            Mode ==
            FormMode.FormModeSelect.View;

        public bool IsEditable =>
            !IsReadOnly;

        public bool ShowSaveButton =>
            !IsReadOnly;

        public string Title =>
            Mode switch
            {
                FormMode.FormModeSelect.Create =>
                    "Crear rango de aporte",

                FormMode.FormModeSelect.Edit =>
                    "Editar rango de aporte",

                _ =>
                    "Detalle del rango de aporte"
            };

        public void PrepararNavegacion(
            FormMode.FormModeSelect nuevoModo,
            RangoNutrienteCategoriaItem
                tipoCultivo,
            RangoNutrienteRequest nuevoItem)
        {
            Mode = nuevoModo;
            Categoria = tipoCultivo;
            Item =
                nuevoItem ??
                new RangoNutrienteRequest();

            Item.TipoCultivoId =
                tipoCultivo.TipoCultivoId;

            Item.UnidadBase =
                UnidadBaseFija;
        }

        public async Task InitializeAsync()
        {
            if (loading ||
                !TieneTipoCultivoValido)
            {
                return;
            }

            loading = true;
            IsBusy = true;
            RefrescarComandos();

            try
            {
                Task<ApiResult<ObservableCollection<
                    ElementoQuimicoResponse>>>
                    elementosTask =
                        elementoApiService
                            .GetElementoQuimicoResultAsync();

                Task<ApiResult<ObservableCollection<
                    RangoNutrienteResponse>>>
                    rangosTask =
                        apiService.GetAsync();

                await Task.WhenAll(
                    elementosTask,
                    rangosTask);

                ApiResult<ObservableCollection<
                    ElementoQuimicoResponse>>
                    elementos =
                        await elementosTask;

                if (!elementos.Success)
                {
                    await MostrarErrorAsync(
                        elementos.Message);
                    return;
                }

                ApiResult<ObservableCollection<
                    RangoNutrienteResponse>>
                    rangos =
                        await rangosTask;

                if (!rangos.Success)
                {
                    await MostrarErrorAsync(
                        rangos.Message);
                    return;
                }

                int elementoActualId =
                    Item.ElementoQuimicosId;

                int parametroActualId =
                    Item
                        .ParametroRangoNutrienteCultivoId;

                HashSet<int> elementosOcupados =
                    (rangos.Data ??
                     new ObservableCollection<
                         RangoNutrienteResponse>())
                    .Where(x =>
                        x.Activo &&
                        x.TipoCultivoId ==
                            Categoria!.TipoCultivoId &&
                        x.ParametroRangoNutrienteCultivoId !=
                            parametroActualId)
                    .Select(x =>
                        x.ElementoQuimicosId)
                    .Where(id => id > 0)
                    .ToHashSet();

                IEnumerable<
                    ElementoQuimicoResponse>
                    elementosDisponibles =
                        (elementos.Data ??
                         new ObservableCollection<
                             ElementoQuimicoResponse>())
                        .Where(x =>
                        {
                            int id =
                                x.ElementoQuimicosId ??
                                0;

                            return
                                id > 0 &&
                                (!elementosOcupados.Contains(id) ||
                                 id == elementoActualId);
                        })
                        .OrderBy(x =>
                            x.NombreElementoQuimico ??
                            string.Empty);

                Elementos.Clear();

                foreach (ElementoQuimicoResponse
                         elemento in
                         elementosDisponibles)
                {
                    Elementos.Add(
                        ElementoQuimicoSelectorItem
                            .FromResponse(elemento));
                }

                ElementoSeleccionado =
                    elementoActualId > 0
                        ? Elementos.FirstOrDefault(x =>
                            x.ElementoQuimicosId ==
                            elementoActualId)
                        : null;

                if (Mode ==
                        FormMode.FormModeSelect.Create &&
                    Elementos.Count == 0)
                {
                    await MostrarInformacionAsync(
                        "Todos los elementos químicos activos ya tienen un rango configurado para este tipo de cultivo.");
                }
            }
            catch (Exception ex)
            {
                await MostrarErrorInesperadoAsync(
                    "cargar los elementos disponibles para el tipo de cultivo",
                    ex);
            }
            finally
            {
                loading = false;
                IsBusy = false;
                RefrescarComandos();
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

            if (!NumeroFormularioHelper
                    .TryParseDecimal(
                        MinimoTexto,
                        out minimo) ||
                minimo < 0)
            {
                ErrorMinimo =
                    "El valor mínimo debe ser un número igual o mayor que cero.";
            }

            if (!NumeroFormularioHelper
                    .TryParseDecimal(
                        MaximoTexto,
                        out maximo))
            {
                ErrorMaximo =
                    "Ingrese un valor máximo válido.";
            }
            else if (maximo <= minimo)
            {
                ErrorMaximo =
                    "El valor máximo debe ser mayor que el valor mínimo.";
            }

            Descripcion =
                Descripcion.Trim();

            if (string.IsNullOrWhiteSpace(
                    Descripcion))
            {
                ErrorDescripcion =
                    "Ingrese la descripción del rango.";
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
            (Categoria?.TipoCultivoId ??
             0) != Item.TipoCultivoId ||
            (ElementoSeleccionado?
                .ElementoQuimicosId ??
             0) != Item.ElementoQuimicosId ||
            minimo != Item.ValorMinimo ||
            maximo != Item.ValorMaximo ||
            !string.Equals(
                UnidadBaseFija,
                Item.UnidadBase?.Trim() ??
                string.Empty,
                StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(
                Descripcion.Trim(),
                Item.DescripcionParametro?.Trim() ??
                string.Empty,
                StringComparison.Ordinal);

        private bool TieneCambiosPendientes()
        {
            if (Mode ==
                FormMode.FormModeSelect.Create)
            {
                return
                    ElementoSeleccionado != null ||
                    !string.IsNullOrWhiteSpace(
                        MaximoTexto) ||
                    !string.IsNullOrWhiteSpace(
                        Descripcion);
            }

            bool minimoValido =
                NumeroFormularioHelper
                    .TryParseDecimal(
                        MinimoTexto,
                        out decimal minimo);

            bool maximoValido =
                NumeroFormularioHelper
                    .TryParseDecimal(
                        MaximoTexto,
                        out decimal maximo);

            if (!minimoValido ||
                !maximoValido)
            {
                return true;
            }

            return HasChanges(
                minimo,
                maximo);
        }

        private async Task CancelAsync()
        {
            if (!IsReadOnly &&
                TieneCambiosPendientes())
            {
                bool confirm =
                    await ConfirmarSalidaSinGuardarAsync();

                if (!confirm)
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

            if (!HasChanges(
                    minimo,
                    maximo))
            {
                await MostrarInformacionAsync(
                    "No hay cambios para guardar.");
                return;
            }

            bool confirm =
                Mode ==
                    FormMode.FormModeSelect.Create
                    ? await ConfirmarGuardadoAsync(
                        "el rango de aporte")
                    : await ConfirmarActualizacionAsync(
                        "el rango de aporte");

            if (!confirm)
                return;

            Item.TipoCultivoId =
                Categoria!.TipoCultivoId;

            Item.ElementoQuimicosId =
                ElementoSeleccionado!
                    .ElementoQuimicosId;

            Item.ValorMinimo =
                minimo;

            Item.ValorMaximo =
                maximo;

            Item.UnidadBase =
                UnidadBaseFija;

            Item.DescripcionParametro =
                Descripcion.Trim();

            try
            {
                IsBusy = true;
                RefrescarComandos();

                ApiResult<bool> result =
                    Mode ==
                        FormMode.FormModeSelect.Create
                        ? await apiService.CreateAsync(Item)
                        : await apiService.UpdateAsync(Item);

                if (!result.Success)
                {
                    await MostrarErrorAsync(
                        result.Message);
                    return;
                }

                await RegresarADetalleAsync();

                await MostrarExitoAsync(
                    result.Message);
            }
            catch (Exception ex)
            {
                await MostrarErrorInesperadoAsync(
                    Mode ==
                        FormMode.FormModeSelect.Create
                        ? "guardar el rango de aporte"
                        : "actualizar el rango de aporte",
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
                AppRoutes.RangoNutrienteDetalle,
                new Dictionary<string, object>
                {
                    ["Categoria"] =
                        Categoria!
                });
        }

        private void LimpiarErrores()
        {
            ErrorTipoCultivo =
                string.Empty;

            ErrorElemento =
                string.Empty;

            ErrorMinimo =
                string.Empty;

            ErrorMaximo =
                string.Empty;

            ErrorDescripcion =
                string.Empty;
        }

        private void RefrescarComandos()
        {
            SaveCommand.ChangeCanExecute();
            CancelCommand.ChangeCanExecute();
        }
    }
}
