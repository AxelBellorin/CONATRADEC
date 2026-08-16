using CONATRADEC.Models;
using CONATRADEC.Services;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Threading;

namespace CONATRADEC.ViewModels
{
    public sealed class FuenteNutrienteFormViewModel : GlobalService
    {
        private readonly FuenteNutrienteApiService
            fuenteNutrienteApiService;

        private readonly ElementoQuimicoApiService
            elementoQuimicoApiService;

        private CancellationTokenSource? inicializacionCts;
        private CancellationTokenSource? guardadoCts;

        private FuenteNutrienteRequest fuente = new();
        private FormMode.FormModeSelect mode;

        private string estadoInicial = string.Empty;
        private string nombreOriginal = string.Empty;

        private string categoriaOriginalCodigo =
            FuenteNutrienteCategoriaOption.CodigoBalanceNutricional;

        private string nombreNutriente = string.Empty;
        private string descripcionNutriente = string.Empty;
        private string precioNutrienteTexto = string.Empty;
        private string prntEnmiendaCalcareaTexto = string.Empty;

        private string descripcionParametroEnmiendaCalcarea =
            string.Empty;

        private string errorNombre = string.Empty;
        private string errorPrecio = string.Empty;
        private string errorAportes = string.Empty;
        private string errorCategoria = string.Empty;
        private string errorPrntEnmiendaCalcarea = string.Empty;

        private string errorDescripcionParametroEnmiendaCalcarea =
            string.Empty;

        private bool tieneErrorNombre;
        private bool tieneErrorPrecio;
        private bool tieneErrorAportes;
        private bool tieneErrorCategoria;
        private bool tieneErrorPrntEnmiendaCalcarea;
        private bool tieneErrorDescripcionParametroEnmiendaCalcarea;
        private bool cargandoDatosIniciales;
        private bool contextoValido;
        private bool inicializado;
        private int guardadoEnCurso;

        private FuenteNutrienteCategoriaOption?
            categoriaSeleccionada;

        private ObservableCollection<ElementoQuimicoResponse>
            elementosQuimicos = new();

        private ObservableCollection<FuenteNutrienteAporteFormItem>
            aportes = new();

        public FuenteNutrienteFormViewModel()
            : this(
                new FuenteNutrienteApiService(),
                new ElementoQuimicoApiService())
        {
        }

        public FuenteNutrienteFormViewModel(
            FuenteNutrienteApiService fuenteNutrienteApiService,
            ElementoQuimicoApiService elementoQuimicoApiService)
        {
            this.fuenteNutrienteApiService =
                fuenteNutrienteApiService
                ?? throw new ArgumentNullException(
                    nameof(fuenteNutrienteApiService));

            this.elementoQuimicoApiService =
                elementoQuimicoApiService
                ?? throw new ArgumentNullException(
                    nameof(elementoQuimicoApiService));

            CategoriasFuente =
                new ObservableCollection<FuenteNutrienteCategoriaOption>();

            CargarCategoriasFuente();

            SaveCommand =
                new Command(
                    async () => await SaveAsync(),
                    () =>
                        CanSave &&
                        !IsBusy &&
                        Volatile.Read(ref guardadoEnCurso) == 0);

            CancelCommand =
                new Command(
                    async () => await CancelAsync(),
                    () => !IsBusy);

            AddAporteCommand =
                new Command(
                    AddAporte,
                    () =>
                        IsFormEnabled &&
                        MostrarAportesElementosQuimicos &&
                        !IsBusy);

            RemoveAporteCommand =
                new Command<FuenteNutrienteAporteFormItem>(
                    RemoveAporte,
                    item =>
                        item != null &&
                        IsFormEnabled &&
                        MostrarAportesElementosQuimicos &&
                        !IsBusy);
        }

        public Command SaveCommand { get; }
        public Command CancelCommand { get; }
        public Command AddAporteCommand { get; }
        public Command<FuenteNutrienteAporteFormItem> RemoveAporteCommand { get; }

        public FuenteNutrienteRequest Fuente
        {
            get => fuente;
            private set
            {
                fuente = value ?? new FuenteNutrienteRequest();
                OnPropertyChanged();
            }
        }

        public FormMode.FormModeSelect Mode
        {
            get => mode;
            private set
            {
                mode = value;
                NotificarEstadoModo();
            }
        }

        public bool ContextoValido =>
            contextoValido;

        public string NombreNutriente
        {
            get => nombreNutriente;
            set
            {
                nombreNutriente =
                    (value ?? string.Empty)
                        .ReplaceLineEndings(" ");

                OnPropertyChanged();

                if (!string.IsNullOrWhiteSpace(nombreNutriente))
                {
                    ErrorNombre = string.Empty;
                    TieneErrorNombre = false;
                }
            }
        }

        public string DescripcionNutriente
        {
            get => descripcionNutriente;
            set
            {
                descripcionNutriente = value ?? string.Empty;
                OnPropertyChanged();
            }
        }

        public string PrecioNutrienteTexto
        {
            get => precioNutrienteTexto;
            set
            {
                precioNutrienteTexto = value ?? string.Empty;
                OnPropertyChanged();

                if (TryParseDecimal(
                        precioNutrienteTexto,
                        out decimal precio) &&
                    precio > 0)
                {
                    ErrorPrecio = string.Empty;
                    TieneErrorPrecio = false;
                }
            }
        }

        public ObservableCollection<FuenteNutrienteCategoriaOption>
            CategoriasFuente { get; }

        public FuenteNutrienteCategoriaOption?
            CategoriaSeleccionada
        {
            get => categoriaSeleccionada;
            set
            {
                if (ReferenceEquals(categoriaSeleccionada, value))
                    return;

                categoriaSeleccionada = value;

                OnPropertyChanged();
                OnPropertyChanged(nameof(MostrarDatosEnmiendaCalcarea));
                OnPropertyChanged(nameof(MostrarAportesElementosQuimicos));
                OnPropertyChanged(nameof(MostrarBotonAgregarAporte));
                OnPropertyChanged(nameof(TituloSeccionAportes));
                OnPropertyChanged(nameof(DescripcionSeccionAportes));

                if (!cargandoDatosIniciales &&
                    IsFormEnabled &&
                    MostrarAportesElementosQuimicos &&
                    Aportes.Count == 0)
                {
                    AddAporte();
                }

                ActualizarComandos();
            }
        }

        public string PrntEnmiendaCalcareaTexto
        {
            get => prntEnmiendaCalcareaTexto;
            set
            {
                prntEnmiendaCalcareaTexto = value ?? string.Empty;
                OnPropertyChanged();

                if (TryParseDecimal(
                        prntEnmiendaCalcareaTexto,
                        out decimal prnt) &&
                    prnt > 0)
                {
                    ErrorPrntEnmiendaCalcarea = string.Empty;
                    TieneErrorPrntEnmiendaCalcarea = false;
                }
            }
        }

        public string DescripcionParametroEnmiendaCalcarea
        {
            get => descripcionParametroEnmiendaCalcarea;
            set
            {
                descripcionParametroEnmiendaCalcarea = value ?? string.Empty;
                OnPropertyChanged();

                if (!string.IsNullOrWhiteSpace(
                        descripcionParametroEnmiendaCalcarea))
                {
                    ErrorDescripcionParametroEnmiendaCalcarea = string.Empty;
                    TieneErrorDescripcionParametroEnmiendaCalcarea = false;
                }
            }
        }

        public ObservableCollection<ElementoQuimicoResponse>
            ElementosQuimicos
        {
            get => elementosQuimicos;
            private set
            {
                elementosQuimicos =
                    value ??
                    new ObservableCollection<ElementoQuimicoResponse>();

                OnPropertyChanged();
            }
        }

        public ObservableCollection<FuenteNutrienteAporteFormItem>
            Aportes
        {
            get => aportes;
            private set
            {
                aportes =
                    value ??
                    new ObservableCollection<FuenteNutrienteAporteFormItem>();

                OnPropertyChanged();
            }
        }

        public string ErrorNombre
        {
            get => errorNombre;
            private set
            {
                errorNombre = value ?? string.Empty;
                OnPropertyChanged();
            }
        }

        public string ErrorPrecio
        {
            get => errorPrecio;
            private set
            {
                errorPrecio = value ?? string.Empty;
                OnPropertyChanged();
            }
        }

        public string ErrorAportes
        {
            get => errorAportes;
            private set
            {
                errorAportes = value ?? string.Empty;
                OnPropertyChanged();
            }
        }

        public string ErrorCategoria
        {
            get => errorCategoria;
            private set
            {
                errorCategoria = value ?? string.Empty;
                OnPropertyChanged();
            }
        }

        public string ErrorPrntEnmiendaCalcarea
        {
            get => errorPrntEnmiendaCalcarea;
            private set
            {
                errorPrntEnmiendaCalcarea = value ?? string.Empty;
                OnPropertyChanged();
            }
        }

        public string ErrorDescripcionParametroEnmiendaCalcarea
        {
            get => errorDescripcionParametroEnmiendaCalcarea;
            private set
            {
                errorDescripcionParametroEnmiendaCalcarea =
                    value ?? string.Empty;

                OnPropertyChanged();
            }
        }

        public bool TieneErrorNombre
        {
            get => tieneErrorNombre;
            private set
            {
                tieneErrorNombre = value;
                OnPropertyChanged();
            }
        }

        public bool TieneErrorPrecio
        {
            get => tieneErrorPrecio;
            private set
            {
                tieneErrorPrecio = value;
                OnPropertyChanged();
            }
        }

        public bool TieneErrorAportes
        {
            get => tieneErrorAportes;
            private set
            {
                tieneErrorAportes = value;
                OnPropertyChanged();
            }
        }

        public bool TieneErrorCategoria
        {
            get => tieneErrorCategoria;
            private set
            {
                tieneErrorCategoria = value;
                OnPropertyChanged();
            }
        }

        public bool TieneErrorPrntEnmiendaCalcarea
        {
            get => tieneErrorPrntEnmiendaCalcarea;
            private set
            {
                tieneErrorPrntEnmiendaCalcarea = value;
                OnPropertyChanged();
            }
        }

        public bool TieneErrorDescripcionParametroEnmiendaCalcarea
        {
            get => tieneErrorDescripcionParametroEnmiendaCalcarea;
            private set
            {
                tieneErrorDescripcionParametroEnmiendaCalcarea = value;
                OnPropertyChanged();
            }
        }

        public bool MostrarDatosEnmiendaCalcarea =>
            CategoriaSeleccionada?.Codigo ==
            FuenteNutrienteCategoriaOption.CodigoEnmiendaCalcarea;

        public bool MostrarAportesElementosQuimicos
        {
            get
            {
                string? codigo =
                    CategoriaSeleccionada?.Codigo;

                return codigo ==
                           FuenteNutrienteCategoriaOption.CodigoBalanceNutricional ||
                       codigo ==
                           FuenteNutrienteCategoriaOption.CodigoFertilizacionMixta;
            }
        }

        public bool MostrarBotonAgregarAporte =>
            ShowSaveButton &&
            MostrarAportesElementosQuimicos;

        public string TituloSeccionAportes =>
            CategoriaSeleccionada?.Codigo ==
            FuenteNutrienteCategoriaOption.CodigoFertilizacionMixta
                ? "Aportes para fertilización mixta"
                : "Aportes de elementos químicos";

        public string DescripcionSeccionAportes =>
            CategoriaSeleccionada?.Codigo ==
            FuenteNutrienteCategoriaOption.CodigoFertilizacionMixta
                ? "Agregue los elementos que aporta esta fuente y su porcentaje. Estos valores se utilizarán en el cálculo de fertilización mixta."
                : "Agregue los elementos que aporta esta fuente y su porcentaje. Estos valores se utilizarán en el balance nutricional.";

        public bool CanSave =>
            Mode switch
            {
                FormMode.FormModeSelect.Create => CanAdd,
                FormMode.FormModeSelect.Edit => CanEdit,
                _ => false
            };

        public bool IsReadOnly =>
            Mode == FormMode.FormModeSelect.View ||
            !CanSave;

        public bool IsFormEnabled =>
            !IsReadOnly;

        public bool ShowSaveButton =>
            CanSave;

        public string Title =>
            Mode switch
            {
                FormMode.FormModeSelect.Create =>
                    "Crear Fuente de Nutriente",

                FormMode.FormModeSelect.Edit =>
                    "Editar Fuente de Nutriente",

                FormMode.FormModeSelect.View =>
                    "Detalles de Fuente de Nutriente",

                _ =>
                    "Fuente de Nutriente"
            };

        public void ActualizarPermisos()
        {
            LoadPagePermissions(
                "fuenteNutrientePage");

            NotificarEstadoModo();
        }

        public bool AplicarContexto(
            FuenteNutrienteFormNavigationContext? contexto)
        {
            CancelarOperaciones();
            inicializado = false;
            contextoValido = false;

            if (contexto == null ||
                !Enum.IsDefined(typeof(FormMode.FormModeSelect), contexto.Mode))
            {
                OnPropertyChanged(nameof(ContextoValido));
                return false;
            }

            FuenteNutrienteRequest fuenteContexto =
                contexto.Fuente ?? new FuenteNutrienteRequest();

            if (contexto.Mode != FormMode.FormModeSelect.Create &&
                fuenteContexto.FuenteNutrientesId is not > 0)
            {
                OnPropertyChanged(nameof(ContextoValido));
                return false;
            }

            Mode = contexto.Mode;
            Fuente = fuenteContexto;
            contextoValido = true;
            OnPropertyChanged(nameof(ContextoValido));
            return true;
        }

        public async Task<bool> InitializeAsync()
        {
            if (!ContextoValido)
                return false;

            if (inicializado)
                return true;

            CancellationTokenSource source =
                PrepararInicializacion();

            try
            {
                IsBusy = true;
                ActualizarComandos();
                LimpiarErrores();

                if (CategoriasFuente.Count == 0)
                    CargarCategoriasFuente();

                await CargarElementosQuimicosAsync(
                    source.Token);

                if (source.IsCancellationRequested ||
                    !EsInicializacionActual(source))
                {
                    return false;
                }

                CargarDatosIniciales();

                estadoInicial =
                    ObtenerEstadoActual();

                inicializado = true;
                return true;
            }
            catch (OperationCanceledException)
            {
                return false;
            }
            catch (ObjectDisposedException)
            {
                return false;
            }
            catch (Exception ex)
            {
                await MostrarToastAsync(
                    "Error: " + ex.Message);

                return false;
            }
            finally
            {
                if (EsInicializacionActual(source))
                    IsBusy = false;

                LiberarInicializacion(source);
                ActualizarComandos();
            }
        }

        public void CancelarOperaciones()
        {
            CancellationTokenSource? inicializacion =
                Interlocked.Exchange(
                    ref inicializacionCts,
                    null);

            CancellationTokenSource? guardado =
                Interlocked.Exchange(
                    ref guardadoCts,
                    null);

            CancelarSeguro(inicializacion);
            CancelarSeguro(guardado);

            IsBusy = false;
            ActualizarComandos();
        }

        private void CargarCategoriasFuente()
        {
            CategoriasFuente.Clear();

            CategoriasFuente.Add(
                new FuenteNutrienteCategoriaOption
                {
                    Codigo =
                        FuenteNutrienteCategoriaOption.CodigoBalanceNutricional,
                    Nombre = "Balance nutricional"
                });

            CategoriasFuente.Add(
                new FuenteNutrienteCategoriaOption
                {
                    Codigo =
                        FuenteNutrienteCategoriaOption.CodigoEnmiendaCalcarea,
                    Nombre = "Enmienda calcárea"
                });

            CategoriasFuente.Add(
                new FuenteNutrienteCategoriaOption
                {
                    Codigo =
                        FuenteNutrienteCategoriaOption.CodigoFertilizacionMixta,
                    Nombre = "Fertilización mixta"
                });
        }

        private async Task CargarElementosQuimicosAsync(
            CancellationToken cancellationToken)
        {
            ApiResult<ObservableCollection<ElementoQuimicoResponse>> resultado =
                await elementoQuimicoApiService
                    .GetElementoQuimicoResultAsync(
                        cancellationToken);

            if (!resultado.Success)
            {
                ElementosQuimicos =
                    new ObservableCollection<ElementoQuimicoResponse>();

                if (!EsMensajeCancelacion(resultado.Message))
                    await MostrarToastAsync(resultado.Message);

                return;
            }

            ElementosQuimicos =
                new ObservableCollection<ElementoQuimicoResponse>(
                    (resultado.Data ??
                     new ObservableCollection<ElementoQuimicoResponse>())
                    .Where(x => x.ElementoQuimicosId.HasValue)
                    .OrderBy(x =>
                        x.NombreElementoQuimico ?? string.Empty));
        }

        private void CargarDatosIniciales()
        {
            cargandoDatosIniciales = true;

            try
            {
                Aportes.Clear();

                if (Mode == FormMode.FormModeSelect.Create)
                {
                    Fuente = new FuenteNutrienteRequest();
                    NombreNutriente = string.Empty;
                    DescripcionNutriente = string.Empty;
                    PrecioNutrienteTexto = string.Empty;
                    PrntEnmiendaCalcareaTexto = string.Empty;
                    DescripcionParametroEnmiendaCalcarea = string.Empty;

                    nombreOriginal = string.Empty;
                    categoriaOriginalCodigo =
                        FuenteNutrienteCategoriaOption.CodigoBalanceNutricional;

                    CategoriaSeleccionada =
                        BuscarCategoriaPorCodigo(
                            categoriaOriginalCodigo);

                    if (MostrarAportesElementosQuimicos &&
                        IsFormEnabled)
                    {
                        AddAporte();
                    }

                    return;
                }

                NombreNutriente =
                    Fuente.NombreNutriente ?? string.Empty;

                DescripcionNutriente =
                    Fuente.DescripcionNutriente ?? string.Empty;

                PrecioNutrienteTexto =
                    Fuente.PrecioNutriente > 0
                        ? Fuente.PrecioNutriente.ToString(
                            "0.##",
                            CultureInfo.InvariantCulture)
                        : string.Empty;

                PrntEnmiendaCalcareaTexto =
                    Fuente.PrntEnmiendaCalcarea.HasValue
                        ? Fuente.PrntEnmiendaCalcarea.Value.ToString(
                            "0.##",
                            CultureInfo.InvariantCulture)
                        : string.Empty;

                DescripcionParametroEnmiendaCalcarea =
                    Fuente.DescripcionParametroEnmiendaCalcarea ??
                    string.Empty;

                nombreOriginal =
                    NombreNutriente.Trim();

                categoriaOriginalCodigo =
                    ObtenerCodigoCategoriaDesdeFuente();

                CategoriaSeleccionada =
                    BuscarCategoriaPorCodigo(
                        categoriaOriginalCodigo);

                if (Fuente.ElementosQuimicos != null &&
                    Fuente.ElementosQuimicos.Count > 0)
                {
                    foreach (FuenteNutrienteElementoQuimicoRequest item
                             in Fuente.ElementosQuimicos)
                    {
                        ElementoQuimicoResponse? elemento =
                            ElementosQuimicos.FirstOrDefault(x =>
                                x.ElementoQuimicosId ==
                                item.ElementoQuimicosId);

                        Aportes.Add(
                            new FuenteNutrienteAporteFormItem
                            {
                                ElementoQuimicosId =
                                    item.ElementoQuimicosId,
                                ElementoSeleccionado =
                                    elemento,
                                CantidadAporteTexto =
                                    item.CantidadAporte.ToString(
                                        "0.##",
                                        CultureInfo.InvariantCulture)
                            });
                    }
                }

                if (IsFormEnabled &&
                    MostrarAportesElementosQuimicos &&
                    Aportes.Count == 0)
                {
                    AddAporte();
                }
            }
            finally
            {
                cargandoDatosIniciales = false;
            }
        }

        private FuenteNutrienteCategoriaOption?
            BuscarCategoriaPorCodigo(
                string codigo) =>
            CategoriasFuente.FirstOrDefault(x =>
                string.Equals(
                    x.Codigo,
                    codigo,
                    StringComparison.OrdinalIgnoreCase));

        private string ObtenerCodigoCategoriaDesdeFuente()
        {
            if (Fuente.HabilitadaEnmiendaCalcarea)
            {
                return FuenteNutrienteCategoriaOption
                    .CodigoEnmiendaCalcarea;
            }

            if (Fuente.HabilitadaFertilizacionMixta)
            {
                return FuenteNutrienteCategoriaOption
                    .CodigoFertilizacionMixta;
            }

            return FuenteNutrienteCategoriaOption
                .CodigoBalanceNutricional;
        }

        private string ObtenerCodigoCategoriaSeleccionada() =>
            CategoriaSeleccionada?.Codigo ??
            FuenteNutrienteCategoriaOption.CodigoBalanceNutricional;

        private void AddAporte()
        {
            if (!IsFormEnabled ||
                !MostrarAportesElementosQuimicos)
            {
                return;
            }

            Aportes.Add(
                new FuenteNutrienteAporteFormItem());
        }

        private void RemoveAporte(
            FuenteNutrienteAporteFormItem? item)
        {
            if (!IsFormEnabled ||
                !MostrarAportesElementosQuimicos ||
                item == null)
            {
                return;
            }

            Aportes.Remove(item);
        }

        private async Task CancelAsync()
        {
            try
            {
                bool hayCambios =
                    inicializado &&
                    ObtenerEstadoActual() != estadoInicial;

                if (hayCambios &&
                    IsFormEnabled)
                {
                    bool confirm =
                        await App.Current.MainPage.DisplayAlert(
                            "Cancelar",
                            "¿Desea salir sin guardar los cambios?",
                            "Aceptar",
                            "Cancelar");

                    if (!confirm)
                        return;
                }

                CancelarOperaciones();

                await GoToAsyncParameters(
                    "//FuenteNutrientePage");
            }
            catch (Exception ex)
            {
                await MostrarToastAsync(
                    "Error: " + ex.Message);
            }
        }

        private async Task SaveAsync()
        {
            if (!CanSave ||
                IsBusy ||
                Interlocked.CompareExchange(
                    ref guardadoEnCurso,
                    1,
                    0) != 0)
            {
                return;
            }

            try
            {
                if (!ValidarFormulario())
                    return;

                string mensaje =
                    Mode == FormMode.FormModeSelect.Create
                        ? "¿Desea guardar la fuente de nutriente?"
                        : "¿Desea actualizar la fuente de nutriente?";

                bool confirm =
                    await App.Current.MainPage.DisplayAlert(
                        "Confirmar",
                        mensaje,
                        "Aceptar",
                        "Cancelar");

                if (!confirm)
                    return;

                CancellationTokenSource source =
                    PrepararGuardado();

                try
                {
                    IsBusy = true;
                    ActualizarComandos();

                    FuenteNutrienteAdministracionRequest request =
                        ConstruirRequestAdministrativo();

                    bool eraCreacion =
                        Mode == FormMode.FormModeSelect.Create;

                    ApiResult<FuenteNutrienteResponse> resultado;

                    if (eraCreacion)
                    {
                        resultado =
                            await fuenteNutrienteApiService
                                .CreateFuenteNutrienteAdminResultAsync(
                                    request,
                                    source.Token);
                    }
                    else if (Mode == FormMode.FormModeSelect.Edit &&
                             Fuente.FuenteNutrientesId is > 0)
                    {
                        resultado =
                            await fuenteNutrienteApiService
                                .UpdateFuenteNutrienteAdminResultAsync(
                                    Fuente.FuenteNutrientesId.Value,
                                    request,
                                    source.Token);
                    }
                    else
                    {
                        return;
                    }

                    if (source.IsCancellationRequested ||
                        !EsGuardadoActual(source))
                    {
                        return;
                    }

                    if (!resultado.Success ||
                        resultado.Data?.FuenteNutrientesId is not > 0)
                    {
                        if (!EsMensajeCancelacion(resultado.Message))
                        {
                            await MostrarToastAsync(
                                string.IsNullOrWhiteSpace(resultado.Message)
                                    ? "No se pudo guardar la fuente de nutriente."
                                    : resultado.Message);
                        }

                        return;
                    }

                    FuenteNutrienteResponse guardada =
                        resultado.Data;

                    bool nombreCambio =
                        !eraCreacion &&
                        !string.Equals(
                            nombreOriginal,
                            guardada.NombreNutriente?.Trim(),
                            StringComparison.OrdinalIgnoreCase);

                    bool categoriaCambio =
                        !eraCreacion &&
                        !string.Equals(
                            categoriaOriginalCodigo,
                            guardada.CategoriaFuenteCodigo,
                            StringComparison.OrdinalIgnoreCase);

                    if (eraCreacion ||
                        nombreCambio ||
                        categoriaCambio)
                    {
                        FuenteNutrienteListadoEstadoService
                            .MarcarParaRecargar();
                    }
                    else
                    {
                        FuenteNutrienteListadoEstadoService
                            .RegistrarEdicionLocal(
                                guardada);
                    }

                    Fuente =
                        new FuenteNutrienteRequest(
                            guardada);

                    nombreOriginal =
                        guardada.NombreNutriente?.Trim() ??
                        string.Empty;

                    categoriaOriginalCodigo =
                        guardada.CategoriaFuenteCodigo;

                    estadoInicial =
                        ObtenerEstadoActual();

                    await GoToAsyncParameters(
                        "//FuenteNutrientePage");

                    await MostrarToastAsync(
                        string.IsNullOrWhiteSpace(resultado.Message)
                            ? eraCreacion
                                ? "Fuente de nutriente guardada correctamente."
                                : "Fuente de nutriente actualizada correctamente."
                            : resultado.Message);
                }
                finally
                {
                    IsBusy = false;
                    LiberarGuardado(source);
                    ActualizarComandos();
                }
            }
            catch (OperationCanceledException)
            {
                // Cancelación normal al abandonar el formulario.
            }
            catch (Exception ex)
            {
                await MostrarToastAsync(
                    "Error: " + ex.Message);
            }
            finally
            {
                Interlocked.Exchange(
                    ref guardadoEnCurso,
                    0);

                ActualizarComandos();
            }
        }

        private FuenteNutrienteAdministracionRequest
            ConstruirRequestAdministrativo()
        {
            string categoria =
                ObtenerCodigoCategoriaSeleccionada();

            var request =
                new FuenteNutrienteAdministracionRequest
                {
                    FuenteNutrientesId =
                        Fuente.FuenteNutrientesId,

                    NombreNutriente =
                        NombreNutriente?.Trim() ??
                        string.Empty,

                    DescripcionNutriente =
                        DescripcionNutriente?.Trim() ??
                        string.Empty,

                    PrecioNutriente =
                        ParseDecimal(
                            PrecioNutrienteTexto),

                    Categoria = categoria,

                    Prnt =
                        categoria ==
                        FuenteNutrienteCategoriaOption.CodigoEnmiendaCalcarea
                            ? ParseDecimal(
                                PrntEnmiendaCalcareaTexto)
                            : null,

                    DescripcionParametro =
                        categoria ==
                        FuenteNutrienteCategoriaOption.CodigoEnmiendaCalcarea
                            ? DescripcionParametroEnmiendaCalcarea?.Trim()
                            : null,

                    ElementosQuimicos =
                        new List<FuenteNutrienteElementoQuimicoRequest>()
                };

            if (!MostrarAportesElementosQuimicos)
                return request;

            foreach (FuenteNutrienteAporteFormItem item
                     in Aportes)
            {
                if (!item.ElementoQuimicosId.HasValue)
                    continue;

                decimal cantidad =
                    ParseDecimal(
                        item.CantidadAporteTexto);

                if (cantidad <= 0)
                    continue;

                request.ElementosQuimicos.Add(
                    new FuenteNutrienteElementoQuimicoRequest
                    {
                        ElementoQuimicosId =
                            item.ElementoQuimicosId.Value,
                        CantidadAporte =
                            cantidad
                    });
            }

            return request;
        }

        private bool ValidarFormulario()
        {
            LimpiarErrores();
            bool valido = true;

            if (string.IsNullOrWhiteSpace(
                    NombreNutriente))
            {
                ErrorNombre =
                    "El nombre de la fuente es obligatorio.";
                TieneErrorNombre = true;
                valido = false;
            }
            else if (NombreNutriente.Trim().Length > 100)
            {
                ErrorNombre =
                    "El nombre no puede superar 100 caracteres.";
                TieneErrorNombre = true;
                valido = false;
            }

            if (!TryParseDecimal(
                    PrecioNutrienteTexto,
                    out decimal precio) ||
                precio <= 0)
            {
                ErrorPrecio =
                    "Ingrese un precio válido mayor a 0.";
                TieneErrorPrecio = true;
                valido = false;
            }

            if (CategoriaSeleccionada == null ||
                string.IsNullOrWhiteSpace(
                    CategoriaSeleccionada.Codigo))
            {
                ErrorCategoria =
                    "Debe seleccionar la clasificación de la fuente.";
                TieneErrorCategoria = true;
                valido = false;
            }

            if (DebeEnviarHabilitarEnmiendaCalcarea())
            {
                if (!TryParseDecimal(
                        PrntEnmiendaCalcareaTexto,
                        out decimal prnt) ||
                    prnt <= 0)
                {
                    ErrorPrntEnmiendaCalcarea =
                        "Ingrese un PRNT válido mayor a 0.";
                    TieneErrorPrntEnmiendaCalcarea = true;
                    valido = false;
                }

                if (string.IsNullOrWhiteSpace(
                        DescripcionParametroEnmiendaCalcarea))
                {
                    ErrorDescripcionParametroEnmiendaCalcarea =
                        "Debe ingresar la descripción del parámetro.";
                    TieneErrorDescripcionParametroEnmiendaCalcarea = true;
                    valido = false;
                }
                else if (DescripcionParametroEnmiendaCalcarea.Trim().Length > 200)
                {
                    ErrorDescripcionParametroEnmiendaCalcarea =
                        "La descripción del parámetro no puede superar 200 caracteres.";
                    TieneErrorDescripcionParametroEnmiendaCalcarea = true;
                    valido = false;
                }
            }

            if (MostrarAportesElementosQuimicos)
            {
                List<FuenteNutrienteAporteFormItem> aportesCompletos =
                    Aportes
                        .Where(x =>
                            x.ElementoQuimicosId.HasValue ||
                            !string.IsNullOrWhiteSpace(
                                x.CantidadAporteTexto))
                        .ToList();

                if (aportesCompletos.Count == 0)
                {
                    ErrorAportes =
                        "Debe agregar al menos un aporte de elemento químico para la clasificación seleccionada.";
                    TieneErrorAportes = true;
                    valido = false;
                }

                decimal totalAporte = 0;

                foreach (FuenteNutrienteAporteFormItem aporte
                         in aportesCompletos)
                {
                    if (!aporte.ElementoQuimicosId.HasValue)
                    {
                        ErrorAportes =
                            "Hay un aporte sin elemento químico seleccionado.";
                        TieneErrorAportes = true;
                        valido = false;
                        break;
                    }

                    if (!TryParseDecimal(
                            aporte.CantidadAporteTexto,
                            out decimal cantidad) ||
                        cantidad <= 0)
                    {
                        ErrorAportes =
                            "Hay un aporte con porcentaje inválido.";
                        TieneErrorAportes = true;
                        valido = false;
                        break;
                    }

                    if (cantidad > 100)
                    {
                        ErrorAportes =
                            "El porcentaje de aporte no puede ser mayor a 100.";
                        TieneErrorAportes = true;
                        valido = false;
                        break;
                    }

                    totalAporte += cantidad;
                }

                if (totalAporte > 100)
                {
                    ErrorAportes =
                        $"La suma total de los aportes no puede superar el 100%. Total actual: {totalAporte:N2}%.";
                    TieneErrorAportes = true;
                    valido = false;
                }

                bool duplicados =
                    aportesCompletos
                        .Where(x => x.ElementoQuimicosId.HasValue)
                        .GroupBy(x => x.ElementoQuimicosId!.Value)
                        .Any(g => g.Count() > 1);

                if (duplicados)
                {
                    ErrorAportes =
                        "No puede repetir el mismo elemento químico en la fuente.";
                    TieneErrorAportes = true;
                    valido = false;
                }
            }

            return valido;
        }

        private bool DebeEnviarHabilitarEnmiendaCalcarea() =>
            ObtenerCodigoCategoriaSeleccionada() ==
            FuenteNutrienteCategoriaOption.CodigoEnmiendaCalcarea;

        private void LimpiarErrores()
        {
            ErrorNombre = string.Empty;
            ErrorPrecio = string.Empty;
            ErrorAportes = string.Empty;
            ErrorCategoria = string.Empty;
            ErrorPrntEnmiendaCalcarea = string.Empty;
            ErrorDescripcionParametroEnmiendaCalcarea = string.Empty;

            TieneErrorNombre = false;
            TieneErrorPrecio = false;
            TieneErrorAportes = false;
            TieneErrorCategoria = false;
            TieneErrorPrntEnmiendaCalcarea = false;
            TieneErrorDescripcionParametroEnmiendaCalcarea = false;
        }

        private string ObtenerEstadoActual()
        {
            string aportesTexto =
                string.Join(
                    "|",
                    Aportes.Select(x =>
                        $"{x.ElementoQuimicosId}-" +
                        $"{x.CantidadAporteTexto?.Trim()}"));

            return
                $"{NombreNutriente?.Trim()}|" +
                $"{DescripcionNutriente?.Trim()}|" +
                $"{PrecioNutrienteTexto?.Trim()}|" +
                $"{ObtenerCodigoCategoriaSeleccionada()}|" +
                $"{PrntEnmiendaCalcareaTexto?.Trim()}|" +
                $"{DescripcionParametroEnmiendaCalcarea?.Trim()}|" +
                $"{aportesTexto}";
        }

        private decimal ParseDecimal(
            string? value) =>
            TryParseDecimal(
                value,
                out decimal result)
                ? result
                : 0;

        private bool TryParseDecimal(
            string? value,
            out decimal result)
        {
            result = 0;

            if (string.IsNullOrWhiteSpace(value))
                return false;

            string texto = value.Trim();

            if (decimal.TryParse(
                    texto,
                    NumberStyles.Number,
                    CultureInfo.CurrentCulture,
                    out result))
            {
                return true;
            }

            if (decimal.TryParse(
                    texto,
                    NumberStyles.Number,
                    CultureInfo.InvariantCulture,
                    out result))
            {
                return true;
            }

            texto = texto.Replace(",", ".");

            return decimal.TryParse(
                texto,
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out result);
        }

        private void NotificarEstadoModo()
        {
            OnPropertyChanged(nameof(IsReadOnly));
            OnPropertyChanged(nameof(IsFormEnabled));
            OnPropertyChanged(nameof(ShowSaveButton));
            OnPropertyChanged(nameof(CanSave));
            OnPropertyChanged(nameof(Title));
            OnPropertyChanged(nameof(MostrarDatosEnmiendaCalcarea));
            OnPropertyChanged(nameof(MostrarAportesElementosQuimicos));
            OnPropertyChanged(nameof(MostrarBotonAgregarAporte));
            OnPropertyChanged(nameof(TituloSeccionAportes));
            OnPropertyChanged(nameof(DescripcionSeccionAportes));
            ActualizarComandos();
        }

        private void ActualizarComandos()
        {
            SaveCommand.ChangeCanExecute();
            CancelCommand.ChangeCanExecute();
            AddAporteCommand.ChangeCanExecute();
            RemoveAporteCommand.ChangeCanExecute();
        }

        private CancellationTokenSource PrepararInicializacion()
        {
            var source = new CancellationTokenSource();

            CancellationTokenSource? anterior =
                Interlocked.Exchange(
                    ref inicializacionCts,
                    source);

            CancelarSeguro(anterior);
            return source;
        }

        private CancellationTokenSource PrepararGuardado()
        {
            var source = new CancellationTokenSource();

            CancellationTokenSource? anterior =
                Interlocked.Exchange(
                    ref guardadoCts,
                    source);

            CancelarSeguro(anterior);
            return source;
        }

        private bool EsInicializacionActual(
            CancellationTokenSource source) =>
            ReferenceEquals(
                Volatile.Read(ref inicializacionCts),
                source);

        private bool EsGuardadoActual(
            CancellationTokenSource source) =>
            ReferenceEquals(
                Volatile.Read(ref guardadoCts),
                source);

        private void LiberarInicializacion(
            CancellationTokenSource source)
        {
            Interlocked.CompareExchange(
                ref inicializacionCts,
                null,
                source);

            source.Dispose();
        }

        private void LiberarGuardado(
            CancellationTokenSource source)
        {
            Interlocked.CompareExchange(
                ref guardadoCts,
                null,
                source);

            source.Dispose();
        }

        private static void CancelarSeguro(
            CancellationTokenSource? source)
        {
            if (source == null)
                return;

            try
            {
                source.Cancel();
            }
            catch (ObjectDisposedException)
            {
                // La operación ya había terminado.
            }
        }

        private static bool EsMensajeCancelacion(
            string? valor) =>
            !string.IsNullOrWhiteSpace(valor) &&
            valor.Contains(
                "cancel",
                StringComparison.OrdinalIgnoreCase);
    }
}
