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
        private string errorElemento = string.Empty;
        private string errorCantidad = string.Empty;
        private string errorDescripcion = string.Empty;
        private bool cargandoElementos;

        public ObservableCollection<ElementoQuimicoSelectorItem>
            Elementos { get; } = new();

        public Command SaveCommand { get; }
        public Command CancelCommand { get; }

        public ExtraccionNutrienteFormViewModel()
        {
            SaveCommand = new Command(
                async () => await SaveAsync(),
                () => !IsReadOnly && !IsBusy);

            CancelCommand = new Command(
                async () => await CancelAsync(),
                () => !IsBusy);
        }

        public ExtraccionNutrienteRequest Item
        {
            get => item;
            set
            {
                item = value ?? new ExtraccionNutrienteRequest();

                CantidadTexto =
                    item.CantidadExtraidaPorQQOro > 0
                        ? NumeroFormularioHelper.ToText(
                            item.CantidadExtraidaPorQQOro)
                        : string.Empty;

                Descripcion =
                    item.DescripcionParametro
                    ?? string.Empty;

                SelectCurrentElement();
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

        public string CantidadTexto
        {
            get => cantidadTexto;
            set
            {
                cantidadTexto = value ?? string.Empty;
                OnPropertyChanged();

                if (NumeroFormularioHelper.TryParseDecimal(
                        cantidadTexto,
                        out decimal cantidad) &&
                    cantidad > 0)
                {
                    ErrorCantidad = string.Empty;
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

                if (!string.IsNullOrWhiteSpace(descripcion))
                    ErrorDescripcion = string.Empty;
            }
        }

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

        public string ErrorCantidad
        {
            get => errorCantidad;
            private set
            {
                if (errorCantidad == value)
                    return;

                errorCantidad = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TieneErrorCantidad));
            }
        }

        public bool TieneErrorCantidad =>
            !string.IsNullOrWhiteSpace(ErrorCantidad);

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
                    "Crear extracción por quintal oro",
                FormMode.FormModeSelect.Edit =>
                    "Editar extracción por quintal oro",
                _ =>
                    "Detalle de extracción por quintal oro"
            };

        public async Task InitializeAsync()
        {
            if (cargandoElementos)
                return;

            cargandoElementos = true;
            IsBusy = true;
            RefrescarComandos();

            try
            {
                /*
                 * Se consultan nuevamente los elementos y las extracciones
                 * cada vez que aparece el formulario.
                 */
                Task<ApiResult<ObservableCollection<ElementoQuimicoResponse>>>
                    elementosTask =
                        elementoApiService.GetElementoQuimicoResultAsync();

                Task<ApiResult<ObservableCollection<ExtraccionNutrienteResponse>>>
                    extraccionesTask =
                        apiService.GetAsync();

                await Task.WhenAll(
                    elementosTask,
                    extraccionesTask);

                ApiResult<ObservableCollection<ElementoQuimicoResponse>>
                    elementosResult = await elementosTask;

                if (!elementosResult.Success)
                {
                    await MostrarErrorAsync(elementosResult.Message);
                    return;
                }

                ApiResult<ObservableCollection<ExtraccionNutrienteResponse>>
                    extraccionesResult = await extraccionesTask;

                if (!extraccionesResult.Success)
                {
                    await MostrarErrorAsync(extraccionesResult.Message);
                    return;
                }

                int elementoActualId =
                    Item.ElementoQuimicosId;

                int parametroActualId =
                    Item.ParametroExtraccionNutrienteCafeId;

                /*
                 * No deben mostrarse elementos que ya tienen una extracción
                 * activa, porque el backend no permite duplicarlos.
                 *
                 * Al editar o visualizar se conserva el elemento del registro
                 * actual.
                 */
                HashSet<int> elementosOcupados =
                    (extraccionesResult.Data ??
                     new ObservableCollection<ExtraccionNutrienteResponse>())
                    .Where(x =>
                        x.Activo &&
                        x.ParametroExtraccionNutrienteCafeId !=
                            parametroActualId)
                    .Select(x => x.ElementoQuimicosId)
                    .Where(id => id > 0)
                    .ToHashSet();

                IEnumerable<ElementoQuimicoResponse>
                    elementosDisponibles =
                        (elementosResult.Data ??
                         new ObservableCollection<ElementoQuimicoResponse>())
                        .Where(x =>
                        {
                            int id =
                                x.ElementoQuimicosId ?? 0;

                            return
                                id > 0 &&
                                (!elementosOcupados.Contains(id) ||
                                 id == elementoActualId);
                        })
                        .OrderBy(x =>
                            x.NombreElementoQuimico ??
                            string.Empty);

                Elementos.Clear();

                foreach (ElementoQuimicoResponse elemento in
                         elementosDisponibles)
                {
                    Elementos.Add(
                        ElementoQuimicoSelectorItem
                            .FromResponse(elemento));
                }

                if (elementoActualId > 0)
                {
                    ElementoSeleccionado =
                        Elementos.FirstOrDefault(x =>
                            x.ElementoQuimicosId ==
                            elementoActualId);
                }
                else
                {
                    ElementoSeleccionado = null;
                }

                if (Mode == FormMode.FormModeSelect.Create &&
                    Elementos.Count == 0)
                {
                    await MostrarInformacionAsync(
                        "Todos los elementos químicos activos ya tienen " +
                        "configurado su aporte de extracción de nutriente.");
                }
            }
            catch (Exception ex)
            {
                await MostrarErrorInesperadoAsync(
                    "cargar los elementos químicos disponibles",
                    ex);
            }
            finally
            {
                cargandoElementos = false;
                IsBusy = false;
                RefrescarComandos();
            }
        }

        private void SelectCurrentElement()
        {
            if (Item.ElementoQuimicosId <= 0)
            {
                ElementoSeleccionado = null;
                return;
            }

            if (Elementos.Count == 0)
                return;

            ElementoSeleccionado =
                Elementos.FirstOrDefault(x =>
                    x.ElementoQuimicosId ==
                    Item.ElementoQuimicosId);
        }

        private bool TryGetValues(
            out decimal cantidad)
        {
            LimpiarErrores();
            cantidad = 0;

            if (ElementoSeleccionado == null)
            {
                ErrorElemento =
                    "Seleccione un elemento químico.";
            }

            if (!NumeroFormularioHelper.TryParseDecimal(
                    CantidadTexto,
                    out cantidad) ||
                cantidad <= 0)
            {
                ErrorCantidad =
                    "Ingrese una cantidad mayor que cero.";
            }

            Descripcion = Descripcion.Trim();

            if (string.IsNullOrWhiteSpace(Descripcion))
            {
                ErrorDescripcion =
                    "Ingrese la descripción del parámetro.";
            }

            return
                !TieneErrorElemento &&
                !TieneErrorCantidad &&
                !TieneErrorDescripcion;
        }

        private bool HasChanges(decimal cantidad) =>
            (ElementoSeleccionado?.ElementoQuimicosId
             ?? 0) != Item.ElementoQuimicosId ||
            cantidad != Item.CantidadExtraidaPorQQOro ||
            !string.Equals(
                Descripcion.Trim(),
                Item.DescripcionParametro?.Trim()
                    ?? string.Empty,
                StringComparison.Ordinal);

        private async Task CancelAsync()
        {
            bool changed =
                NumeroFormularioHelper.TryParseDecimal(
                    CantidadTexto,
                    out decimal value) &&
                HasChanges(value);

            if (!IsReadOnly && changed)
            {
                bool confirm =
                    await ConfirmarSalidaSinGuardarAsync();

                if (!confirm)
                    return;
            }

            await GoToAsyncParameters(
                AppRoutes.ExtraccionNutrientes);
        }

        private async Task SaveAsync()
        {
            if (IsReadOnly || IsBusy)
                return;

            if (!TryGetValues(out decimal cantidad))
            {
                await MostrarAdvertenciaAsync(
                    "Revise los campos marcados antes de continuar.");
                return;
            }

            if (!HasChanges(cantidad))
            {
                await MostrarInformacionAsync(
                    "No hay cambios para guardar.");
                return;
            }

            bool confirm =
                Mode == FormMode.FormModeSelect.Create
                    ? await ConfirmarGuardadoAsync(
                        "el parámetro de extracción")
                    : await ConfirmarActualizacionAsync(
                        "el parámetro de extracción");

            if (!confirm)
                return;

            Item.ElementoQuimicosId =
                ElementoSeleccionado!
                    .ElementoQuimicosId;

            Item.CantidadExtraidaPorQQOro = cantidad;
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
                    AppRoutes.ExtraccionNutrientes);

                await MostrarExitoAsync(result.Message);
            }
            catch (Exception ex)
            {
                await MostrarErrorInesperadoAsync(
                    Mode == FormMode.FormModeSelect.Create
                        ? "guardar el parámetro de extracción"
                        : "actualizar el parámetro de extracción",
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
            ErrorElemento = string.Empty;
            ErrorCantidad = string.Empty;
            ErrorDescripcion = string.Empty;
        }

        private void RefrescarComandos()
        {
            SaveCommand.ChangeCanExecute();
            CancelCommand.ChangeCanExecute();
        }
    }
}
