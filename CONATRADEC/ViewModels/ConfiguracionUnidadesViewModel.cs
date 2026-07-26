using CONATRADEC.Models;
using CONATRADEC.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;

namespace CONATRADEC.ViewModels
{
    /// <summary>
    /// Administra las unidades permitidas, fórmulas y factores de conversión
    /// utilizados por el análisis de suelo.
    /// </summary>
    public sealed class ConfiguracionUnidadesViewModel :
        GlobalService
    {
        private const string ModoElemento =
            "ELEMENTO";

        private const string ModoMateriaOrganica =
            "MATERIA_ORGANICA";

        private readonly ConfiguracionUnidadesApiService
            apiService = new();

        private readonly UnidadMedidaApiService
            unidadMedidaApiService = new();

        private readonly List<
            ElementoConfiguracionUnidadesResponse>
            catalogoElementos = new();

        private readonly List<
            UnidadConversionConfiguradaResponse>
            configuracionMateriaOrganica = new();

        private string modoActual =
            ModoElemento;

        private string textoBusquedaElemento =
            string.Empty;

        private string nombreNuevaUnidad =
            string.Empty;

        private ElementoConfiguracionUnidadesResponse?
            elementoSeleccionado;

        private UnidadMedidaCatalogoConfiguracionResponse?
            unidadParaAgregar;

        private ConfiguracionUnidadItemViewModel?
            unidadPruebaSeleccionada;

        private string valorPrueba =
            string.Empty;

        private string materiaOrganicaPrueba =
            "3";

        private string resultadoPrueba =
            string.Empty;

        private string mensaje =
            string.Empty;

        private bool inicializado;

        public ConfiguracionUnidadesViewModel()
        {
            ElementosFiltrados =
                new ObservableCollection<
                    ElementoConfiguracionUnidadesResponse>();

            Formulas =
                new ObservableCollection<
                    FormulaConversionDisponibleResponse>();

            CatalogoUnidades =
                new ObservableCollection<
                    UnidadMedidaCatalogoConfiguracionResponse>();

            UnidadesConfiguradas =
                new ObservableCollection<
                    ConfiguracionUnidadItemViewModel>();

            UnidadesDisponibles =
                new ObservableCollection<
                    UnidadMedidaCatalogoConfiguracionResponse>();

            SeleccionarModoElementosCommand =
                new Command(
                    SeleccionarModoElementos,
                    () => !IsBusy);

            SeleccionarModoMateriaOrganicaCommand =
                new Command(
                    SeleccionarModoMateriaOrganica,
                    () => !IsBusy);

            RecargarCommand =
                new Command(
                    async () =>
                        await InicializarAsync(
                            forzarRecarga: true),
                    () => !IsBusy);

            CrearUnidadMedidaCommand =
                new Command(
                    async () =>
                        await CrearNuevaUnidadMedidaAsync(),
                    () =>
                        !IsBusy &&
                        CanEdit &&
                        !string.IsNullOrWhiteSpace(
                            NombreNuevaUnidad));

            GuardarCommand =
                new Command(
                    async () =>
                        await GuardarAsync(),
                    () =>
                        !IsBusy &&
                        CanEdit &&
                        UnidadesConfiguradas.Count > 0);

            AgregarUnidadCommand =
                new Command(
                    AgregarUnidad,
                    () =>
                        !IsBusy &&
                        CanEdit &&
                        UnidadParaAgregar != null);

            QuitarUnidadCommand =
                new Command<
                    ConfiguracionUnidadItemViewModel>(
                        async item =>
                            await QuitarUnidadAsync(item),
                        item =>
                            !IsBusy &&
                            CanEdit &&
                            item != null &&
                            item.PuedeQuitar);

            ProbarConversionCommand =
                new Command(
                    async () =>
                        await ProbarConversionAsync(),
                    () =>
                        !IsBusy &&
                        UnidadPruebaSeleccionada != null);

            VolverCommand =
                new Command(
                    async () =>
                        await GoToAsyncParameters(
                            AppRoutes.Regresar),
                    () => !IsBusy);
        }

        public ObservableCollection<
            ElementoConfiguracionUnidadesResponse>
            ElementosFiltrados { get; }

        public ObservableCollection<
            FormulaConversionDisponibleResponse>
            Formulas { get; }

        public ObservableCollection<
            UnidadMedidaCatalogoConfiguracionResponse>
            CatalogoUnidades { get; }

        public ObservableCollection<
            ConfiguracionUnidadItemViewModel>
            UnidadesConfiguradas { get; }

        public ObservableCollection<
            UnidadMedidaCatalogoConfiguracionResponse>
            UnidadesDisponibles { get; }

        public Command SeleccionarModoElementosCommand
        {
            get;
        }

        public Command
            SeleccionarModoMateriaOrganicaCommand
        {
            get;
        }

        public Command RecargarCommand { get; }

        public Command CrearUnidadMedidaCommand { get; }

        public Command GuardarCommand { get; }

        public Command AgregarUnidadCommand { get; }

        public Command<
            ConfiguracionUnidadItemViewModel>
            QuitarUnidadCommand { get; }

        public Command ProbarConversionCommand { get; }

        public Command VolverCommand { get; }

        public string ModoActual
        {
            get => modoActual;
            private set
            {
                if (modoActual == value)
                    return;

                modoActual = value;
                OnPropertyChanged();
                OnPropertyChanged(
                    nameof(EsModoElementos));
                OnPropertyChanged(
                    nameof(EsModoMateriaOrganica));
                OnPropertyChanged(
                    nameof(TituloContexto));
                OnPropertyChanged(
                    nameof(DescripcionContexto));
                OnPropertyChanged(
                    nameof(TextoUnidadDestino));

                ResultadoPrueba =
                    string.Empty;

                ReconstruirUnidadesModoActual();
                RefrescarComandos();
            }
        }

        public bool EsModoElementos =>
            ModoActual == ModoElemento;

        public bool EsModoMateriaOrganica =>
            ModoActual ==
            ModoMateriaOrganica;

        public string TituloContexto =>
            EsModoElementos
                ? "Elementos químicos"
                : "Materia orgánica";

        public string DescripcionContexto =>
            EsModoElementos
                ? "Configure qué unidades puede reportar el laboratorio para cada elemento y cómo se convierten a lb/Mz."
                : "Configure las unidades aceptadas para materia orgánica y su conversión final a porcentaje.";

        public string TextoUnidadDestino =>
            EsModoElementos
                ? "lb/Mz"
                : "%";

        public string NombreNuevaUnidad
        {
            get => nombreNuevaUnidad;
            set
            {
                string nuevoValor =
                    (value ??
                     string.Empty)
                        .TrimStart();

                if (nuevoValor.Length > 50)
                {
                    nuevoValor =
                        nuevoValor[..50];
                }

                if (nombreNuevaUnidad ==
                    nuevoValor)
                {
                    return;
                }

                nombreNuevaUnidad =
                    nuevoValor;

                OnPropertyChanged();

                CrearUnidadMedidaCommand
                    .ChangeCanExecute();
            }
        }

        public string TextoBusquedaElemento
        {
            get => textoBusquedaElemento;
            set
            {
                string nuevoValor =
                    value ??
                    string.Empty;

                if (textoBusquedaElemento ==
                    nuevoValor)
                {
                    return;
                }

                textoBusquedaElemento =
                    nuevoValor;

                OnPropertyChanged();

                AplicarFiltroElementos();
            }
        }

        public ElementoConfiguracionUnidadesResponse?
            ElementoSeleccionado
        {
            get => elementoSeleccionado;
            set
            {
                if (ReferenceEquals(
                        elementoSeleccionado,
                        value))
                {
                    return;
                }

                elementoSeleccionado =
                    value;

                OnPropertyChanged();
                OnPropertyChanged(
                    nameof(TieneElementoSeleccionado));
                OnPropertyChanged(
                    nameof(ResumenElementoSeleccionado));

                if (EsModoElementos)
                {
                    ResultadoPrueba =
                        string.Empty;

                    ReconstruirUnidadesModoActual();
                }
            }
        }

        public bool TieneElementoSeleccionado =>
            ElementoSeleccionado != null;

        public string ResumenElementoSeleccionado
        {
            get
            {
                if (ElementoSeleccionado == null)
                    return "Seleccione un elemento químico.";

                return
                    $"Peso equivalente: " +
                    $"{ElementoSeleccionado.PesoEquivalenteElementoQuimico:0.##}";
            }
        }

        public UnidadMedidaCatalogoConfiguracionResponse?
            UnidadParaAgregar
        {
            get => unidadParaAgregar;
            set
            {
                if (ReferenceEquals(
                        unidadParaAgregar,
                        value))
                {
                    return;
                }

                unidadParaAgregar =
                    value;

                OnPropertyChanged();

                AgregarUnidadCommand
                    .ChangeCanExecute();
            }
        }

        public ConfiguracionUnidadItemViewModel?
            UnidadPruebaSeleccionada
        {
            get => unidadPruebaSeleccionada;
            set
            {
                if (ReferenceEquals(
                        unidadPruebaSeleccionada,
                        value))
                {
                    return;
                }

                unidadPruebaSeleccionada =
                    value;

                OnPropertyChanged();

                ProbarConversionCommand
                    .ChangeCanExecute();
            }
        }

        public string ValorPrueba
        {
            get => valorPrueba;
            set
            {
                valorPrueba =
                    LimitarTextoNumero(value);

                OnPropertyChanged();
            }
        }

        public string MateriaOrganicaPrueba
        {
            get => materiaOrganicaPrueba;
            set
            {
                materiaOrganicaPrueba =
                    LimitarTextoNumero(value);

                OnPropertyChanged();
            }
        }

        public string ResultadoPrueba
        {
            get => resultadoPrueba;
            private set
            {
                resultadoPrueba =
                    value ??
                    string.Empty;

                OnPropertyChanged();
                OnPropertyChanged(
                    nameof(TieneResultadoPrueba));
            }
        }

        public bool TieneResultadoPrueba =>
            !string.IsNullOrWhiteSpace(
                ResultadoPrueba);

        public string Mensaje
        {
            get => mensaje;
            private set
            {
                mensaje =
                    value ??
                    string.Empty;

                OnPropertyChanged();
                OnPropertyChanged(
                    nameof(TieneMensaje));
            }
        }

        public bool TieneMensaje =>
            !string.IsNullOrWhiteSpace(
                Mensaje);

        public string ResumenConfiguracion
        {
            get
            {
                int activas =
                    UnidadesConfiguradas
                        .Count(x => x.Activo);

                int visibles =
                    UnidadesConfiguradas
                        .Count(x =>
                            x.Activo &&
                            x.VisibleEnFormulario);

                return
                    $"{activas} activa(s) · " +
                    $"{visibles} visible(s)";
            }
        }

        public async Task InicializarAsync(
            bool forzarRecarga = false)
        {
            if (IsBusy)
                return;

            LoadPagePermissions(
                "elementoQuimicoPage");

            if (!CanView)
                return;

            if (inicializado &&
                !forzarRecarga)
            {
                ReconstruirUnidadesModoActual();
                return;
            }

            try
            {
                IsBusy = true;
                RefrescarComandos();

                Mensaje =
                    "Cargando configuración de unidades...";

                Task<
                    ConfiguracionUnidadesApiResult<
                        List<
                            ElementoConfiguracionUnidadesResponse>>>
                    elementosTask =
                        apiService.ListarElementosAsync(
                            incluirInactivas: false);

                Task<
                    ConfiguracionUnidadesApiResult<
                        List<
                            UnidadConversionConfiguradaResponse>>>
                    materiaTask =
                        apiService.ObtenerMateriaOrganicaAsync(
                            incluirInactivas: true);

                Task<
                    ConfiguracionUnidadesApiResult<
                        List<
                            FormulaConversionDisponibleResponse>>>
                    formulasTask =
                        apiService.ListarFormulasAsync();

                Task<
                    ConfiguracionUnidadesApiResult<
                        List<
                            UnidadMedidaCatalogoConfiguracionResponse>>>
                    catalogoTask =
                        apiService.ListarCatalogoUnidadesAsync(
                            incluirInactivas: false);

                await Task.WhenAll(
                    elementosTask,
                    materiaTask,
                    formulasTask,
                    catalogoTask);

                ConfiguracionUnidadesApiResult<
                    List<ElementoConfiguracionUnidadesResponse>>
                    elementosResultado =
                        await elementosTask;

                ConfiguracionUnidadesApiResult<
                    List<UnidadConversionConfiguradaResponse>>
                    materiaResultado =
                        await materiaTask;

                ConfiguracionUnidadesApiResult<
                    List<FormulaConversionDisponibleResponse>>
                    formulasResultado =
                        await formulasTask;

                ConfiguracionUnidadesApiResult<
                    List<UnidadMedidaCatalogoConfiguracionResponse>>
                    catalogoResultado =
                        await catalogoTask;

                string? error =
                    ObtenerPrimerError(
                        elementosResultado,
                        materiaResultado,
                        formulasResultado,
                        catalogoResultado);

                if (!string.IsNullOrWhiteSpace(error))
                {
                    Mensaje = error;
                    await MostrarErrorAsync(error);
                    return;
                }

                catalogoElementos.Clear();
                catalogoElementos.AddRange(
                    elementosResultado.Data ??
                    new List<
                        ElementoConfiguracionUnidadesResponse>());

                configuracionMateriaOrganica.Clear();
                configuracionMateriaOrganica.AddRange(
                    materiaResultado.Data ??
                    new List<
                        UnidadConversionConfiguradaResponse>());

                ReemplazarColeccion(
                    Formulas,
                    formulasResultado.Data ??
                    new List<
                        FormulaConversionDisponibleResponse>());

                ReemplazarColeccion(
                    CatalogoUnidades,
                    catalogoResultado.Data ??
                    new List<
                        UnidadMedidaCatalogoConfiguracionResponse>());

                AplicarFiltroElementos();

                if (ElementoSeleccionado == null ||
                    !catalogoElementos.Any(x =>
                        x.ElementoQuimicosId ==
                            ElementoSeleccionado
                                .ElementoQuimicosId))
                {
                    ElementoSeleccionado =
                        ElementosFiltrados
                            .FirstOrDefault()
                        ??
                        catalogoElementos
                            .FirstOrDefault();
                }

                inicializado = true;

                Mensaje =
                    string.Empty;

                ReconstruirUnidadesModoActual();
            }
            catch (Exception ex)
            {
                Mensaje =
                    "No fue posible cargar la configuración: " +
                    ex.Message;

                await MostrarErrorAsync(
                    Mensaje);
            }
            finally
            {
                IsBusy = false;
                RefrescarComandos();
            }
        }

        private async Task CrearNuevaUnidadMedidaAsync()
        {
            if (IsBusy ||
                !CanEdit)
            {
                return;
            }

            string nombre =
                NombreNuevaUnidad
                    .Trim();

            if (string.IsNullOrWhiteSpace(
                    nombre))
            {
                await MostrarAdvertenciaAsync(
                    "Ingrese el nombre de la nueva unidad de medida.");

                return;
            }

            UnidadMedidaCatalogoConfiguracionResponse?
                unidadCreada = null;

            try
            {
                IsBusy = true;
                RefrescarComandos();

                Mensaje =
                    "Creando la nueva unidad de medida...";

                UnidadMedidaApiOperationResult<
                    UnidadMedidaResponse>
                    resultado =
                        await unidadMedidaApiService
                            .CrearUnidadMedidaDetalladaAsync(
                                nombre);

                if (!resultado.Success)
                {
                    Mensaje =
                        resultado.Message;

                    await MostrarErrorAsync(
                        resultado.Message);

                    return;
                }

                ConfiguracionUnidadesApiService
                    .InvalidarCache();

                ConfiguracionUnidadesApiResult<
                    List<
                        UnidadMedidaCatalogoConfiguracionResponse>>
                    catalogoResultado =
                        await apiService
                            .ListarCatalogoUnidadesAsync(
                                incluirInactivas: false);

                if (!catalogoResultado.Success ||
                    catalogoResultado.Data == null)
                {
                    Mensaje =
                        catalogoResultado.Message;

                    await MostrarAdvertenciaAsync(
                        "La unidad fue creada, pero no fue posible recargar el catálogo. Pulse Recargar.");

                    return;
                }

                ReemplazarColeccion(
                    CatalogoUnidades,
                    catalogoResultado.Data);

                int? unidadCreadaId =
                    resultado.Data?.UnidadMedidaId;

                unidadCreada =
                    unidadCreadaId.HasValue
                        ? CatalogoUnidades
                            .FirstOrDefault(x =>
                                x.UnidadMedidaId ==
                                    unidadCreadaId.Value)
                        : null;

                unidadCreada ??=
                    CatalogoUnidades
                        .FirstOrDefault(x =>
                            string.Equals(
                                x.NombreUnidadMedida
                                    .Trim(),
                                nombre,
                                StringComparison
                                    .OrdinalIgnoreCase));

                NombreNuevaUnidad =
                    string.Empty;

                Mensaje =
                    resultado.Message;
            }
            catch (Exception ex)
            {
                Mensaje =
                    "No fue posible crear la unidad: " +
                    ex.Message;

                await MostrarErrorAsync(
                    Mensaje);
            }
            finally
            {
                IsBusy = false;
                RefrescarComandos();
            }

            if (unidadCreada == null)
                return;

            /*
             * Después de crear la unidad base, se agrega al contexto actual
             * con una conversión LINEAL inicial. El usuario todavía debe
             * configurar sus factores y pulsar Guardar cambios.
             */
            ActualizarUnidadesDisponibles();

            UnidadParaAgregar =
                UnidadesDisponibles
                    .FirstOrDefault(x =>
                        x.UnidadMedidaId ==
                            unidadCreada
                                .UnidadMedidaId);

            if (UnidadParaAgregar != null)
            {
                AgregarUnidad();
            }

            Mensaje =
                $"La unidad {unidadCreada.NombreUnidadMedida} fue creada y agregada. Configure la fórmula y los factores, luego pulse Guardar cambios.";

            await MostrarExitoAsync(
                "Unidad creada correctamente. Ahora configure su conversión y guarde los cambios.");
        }

        private void SeleccionarModoElementos()
        {
            if (IsBusy)
                return;

            ModoActual =
                ModoElemento;
        }

        private void SeleccionarModoMateriaOrganica()
        {
            if (IsBusy)
                return;

            ModoActual =
                ModoMateriaOrganica;
        }

        private void AplicarFiltroElementos()
        {
            string filtro =
                TextoBusquedaElemento
                    .Trim();

            IEnumerable<
                ElementoConfiguracionUnidadesResponse>
                consulta =
                    catalogoElementos;

            if (!string.IsNullOrWhiteSpace(
                    filtro))
            {
                consulta =
                    consulta.Where(x =>
                        x.NombreMostrar.Contains(
                            filtro,
                            StringComparison
                                .OrdinalIgnoreCase));
            }

            List<
                ElementoConfiguracionUnidadesResponse>
                lista =
                    consulta
                        .OrderBy(x =>
                            x.NombreElementoQuimico)
                        .ToList();

            ReemplazarColeccion(
                ElementosFiltrados,
                lista);

            if (ElementoSeleccionado != null &&
                !ElementosFiltrados.Any(x =>
                    x.ElementoQuimicosId ==
                        ElementoSeleccionado
                            .ElementoQuimicosId))
            {
                ElementoSeleccionado =
                    ElementosFiltrados
                        .FirstOrDefault();
            }
        }

        private void ReconstruirUnidadesModoActual()
        {
            DesconectarEventosUnidades();

            UnidadesConfiguradas.Clear();

            IReadOnlyList<
                FormulaConversionDisponibleResponse>
                formulasDisponibles =
                    EsModoMateriaOrganica
                        ? Formulas
                            .Where(x =>
                                string.Equals(
                                    x.Codigo,
                                    "LINEAL",
                                    StringComparison
                                        .OrdinalIgnoreCase))
                            .ToList()
                        : Formulas.ToList();

            IEnumerable<
                UnidadConversionConfiguradaResponse>
                origen =
                    EsModoMateriaOrganica
                        ? configuracionMateriaOrganica
                        : ElementoSeleccionado?.Unidades
                        ??
                        Enumerable.Empty<
                            UnidadConversionConfiguradaResponse>();

            foreach (
                UnidadConversionConfiguradaResponse
                    unidad
                in origen
                    .OrderBy(x => x.Orden)
                    .ThenBy(x =>
                        x.NombreUnidadMedida))
            {
                ConfiguracionUnidadItemViewModel item =
                    ConfiguracionUnidadItemViewModel
                        .DesdeRespuesta(
                            unidad,
                            formulasDisponibles);

                ConectarEventosUnidad(item);
                UnidadesConfiguradas.Add(item);
            }

            NormalizarPredeterminadasConfiguradas();

            UnidadPruebaSeleccionada =
                UnidadesConfiguradas
                    .FirstOrDefault(x =>
                        x.UnidadPredeterminada)
                ??
                UnidadesConfiguradas
                    .FirstOrDefault(x =>
                        x.Activo)
                ??
                UnidadesConfiguradas
                    .FirstOrDefault();

            ActualizarUnidadesDisponibles();
            NotificarResumen();
            RefrescarComandos();
        }

        private void AgregarUnidad()
        {
            if (UnidadParaAgregar == null ||
                IsBusy ||
                !CanEdit)
            {
                return;
            }

            if (UnidadesConfiguradas.Any(x =>
                    x.UnidadMedidaId ==
                        UnidadParaAgregar
                            .UnidadMedidaId))
            {
                Mensaje =
                    "La unidad seleccionada ya forma parte de la configuración.";

                return;
            }

            IReadOnlyList<
                FormulaConversionDisponibleResponse>
                formulasDisponibles =
                    EsModoMateriaOrganica
                        ? Formulas
                            .Where(x =>
                                string.Equals(
                                    x.Codigo,
                                    "LINEAL",
                                    StringComparison
                                        .OrdinalIgnoreCase))
                            .ToList()
                        : Formulas.ToList();

            int ordenSugerido =
                UnidadesConfiguradas.Count == 0
                    ? 10
                    : UnidadesConfiguradas
                        .Max(x =>
                            ParseEnteroSeguro(
                                x.Orden)) +
                        10;

            ConfiguracionUnidadItemViewModel
                nueva =
                    ConfiguracionUnidadItemViewModel
                        .Nueva(
                            UnidadParaAgregar,
                            formulasDisponibles,
                            ordenSugerido,
                            predeterminada:
                                false);

            ConectarEventosUnidad(nueva);

            UnidadesConfiguradas.Add(
                nueva);

            UnidadPruebaSeleccionada ??=
                nueva;

            UnidadParaAgregar =
                null;

            ActualizarUnidadesDisponibles();
            NotificarResumen();
            RefrescarComandos();
        }

        private async Task QuitarUnidadAsync(
            ConfiguracionUnidadItemViewModel?
                item)
        {
            if (item == null ||
                !CanEdit ||
                IsBusy)
            {
                return;
            }

            if (!item.PuedeQuitar)
            {
                await MostrarAdvertenciaAsync(
                    "kg/ha es una conversión interna necesaria para los rangos nutricionales. Puede ocultarla del formulario, pero no quitarla.");

                return;
            }

            bool confirmar =
                await ConfirmarAsync(
                    "Quitar unidad",
                    $"¿Desea quitar {item.NombreMostrar} de esta configuración?",
                    "Quitar",
                    "Cancelar");

            if (!confirmar)
                return;

            item.PredeterminadaActivada -=
                Unidad_PredeterminadaActivada;

            item.PropertyChanged -=
                Unidad_PropertyChanged;

            UnidadesConfiguradas.Remove(
                item);

            if (ReferenceEquals(
                    UnidadPruebaSeleccionada,
                    item))
            {
                UnidadPruebaSeleccionada =
                    UnidadesConfiguradas
                        .FirstOrDefault();
            }

            NormalizarPredeterminadasConfiguradas();
            ActualizarUnidadesDisponibles();
            NotificarResumen();
            RefrescarComandos();
        }

        private async Task GuardarAsync()
        {
            if (IsBusy ||
                !CanEdit)
            {
                return;
            }

            if (EsModoElementos &&
                ElementoSeleccionado == null)
            {
                await MostrarAdvertenciaAsync(
                    "Seleccione un elemento químico.");

                return;
            }

            if (!TryCrearRequests(
                    out List<
                        GuardarUnidadConversionRequest>
                        unidades,
                    out string error))
            {
                await MostrarAdvertenciaAsync(
                    error);

                return;
            }

            bool confirmar =
                await ConfirmarAsync(
                    "Guardar configuración",
                    "¿Desea guardar las unidades, fórmulas y factores configurados?",
                    "Guardar",
                    "Cancelar");

            if (!confirmar)
                return;

            try
            {
                IsBusy = true;
                RefrescarComandos();

                Mensaje =
                    "Guardando configuración...";

                if (EsModoElementos)
                {
                    int elementoId =
                        ElementoSeleccionado!
                            .ElementoQuimicosId;

                    ConfiguracionUnidadesApiResult<
                        ElementoConfiguracionUnidadesResponse>
                        resultado =
                            await apiService
                                .GuardarElementoAsync(
                                    elementoId,
                                    new
                                        GuardarConfiguracionElementoUnidadesRequest
                                        {
                                            Unidades =
                                                unidades
                                        });

                    if (!resultado.Success ||
                        resultado.Data == null)
                    {
                        Mensaje =
                            resultado.Message;

                        await MostrarErrorAsync(
                            resultado.Message);

                        return;
                    }

                    ActualizarElementoCatalogo(
                        resultado.Data);
                }
                else
                {
                    ConfiguracionUnidadesApiResult<
                        List<
                            UnidadConversionConfiguradaResponse>>
                        resultado =
                            await apiService
                                .GuardarMateriaOrganicaAsync(
                                    new
                                        GuardarConfiguracionMateriaOrganicaRequest
                                        {
                                            Unidades =
                                                unidades
                                        });

                    if (!resultado.Success ||
                        resultado.Data == null)
                    {
                        Mensaje =
                            resultado.Message;

                        await MostrarErrorAsync(
                            resultado.Message);

                        return;
                    }

                    configuracionMateriaOrganica
                        .Clear();

                    configuracionMateriaOrganica
                        .AddRange(
                            resultado.Data);
                }

                ConfiguracionUnidadesApiService
                    .InvalidarCache();

                Mensaje =
                    "Configuración guardada correctamente.";

                await MostrarExitoAsync(
                    Mensaje);

                ReconstruirUnidadesModoActual();
            }
            catch (Exception ex)
            {
                Mensaje =
                    "No fue posible guardar la configuración: " +
                    ex.Message;

                await MostrarErrorAsync(
                    Mensaje);
            }
            finally
            {
                IsBusy = false;
                RefrescarComandos();
            }
        }

        private async Task ProbarConversionAsync()
        {
            if (IsBusy ||
                UnidadPruebaSeleccionada == null)
            {
                return;
            }

            if (UnidadPruebaSeleccionada
                    .ConfiguracionId <= 0)
            {
                await MostrarAdvertenciaAsync(
                    "Guarde primero la unidad nueva antes de probar su conversión.");

                return;
            }

            if (!TryParseDecimal(
                    ValorPrueba,
                    out decimal valor))
            {
                await MostrarAdvertenciaAsync(
                    "Ingrese un valor reportado válido.");

                return;
            }

            decimal? materiaOrganica =
                null;

            if (EsModoElementos &&
                UnidadPruebaSeleccionada
                    .RequiereMateriaOrganica)
            {
                if (!TryParseDecimal(
                        MateriaOrganicaPrueba,
                        out decimal materia))
                {
                    await MostrarAdvertenciaAsync(
                        "Ingrese el porcentaje de materia orgánica utilizado para la prueba.");

                    return;
                }

                materiaOrganica =
                    materia;
            }

            try
            {
                IsBusy = true;
                RefrescarComandos();

                ResultadoPrueba =
                    string.Empty;

                ConfiguracionUnidadesApiResult<
                    ResultadoPruebaConversionResponse>
                    resultado =
                        await apiService
                            .ProbarConversionAsync(
                                new
                                    ProbarConversionUnidadRequest
                                    {
                                        Contexto =
                                            EsModoElementos
                                                ? ModoElemento
                                                : ModoMateriaOrganica,
                                        ElementoQuimicosId =
                                            EsModoElementos
                                                ? ElementoSeleccionado?.ElementoQuimicosId
                                                : null,
                                        UnidadMedidaId =
                                            UnidadPruebaSeleccionada
                                                .UnidadMedidaId,
                                        ValorReportado =
                                            valor,
                                        MateriaOrganicaPorcentaje =
                                            materiaOrganica
                                    });

                if (!resultado.Success ||
                    resultado.Data == null)
                {
                    await MostrarErrorAsync(
                        resultado.Message);

                    return;
                }

                ResultadoPruebaConversionResponse
                    data =
                        resultado.Data;

                ResultadoPrueba =
                    $"{data.ValorReportado:0.####} " +
                    $"{data.UnidadOrigen} = " +
                    $"{data.ValorConvertido:0.####} " +
                    $"{data.UnidadDestino}\n" +
                    $"Fórmula: " +
                    $"{data.CodigoFormulaConversion}\n" +
                    $"{data.Descripcion}";
            }
            catch (Exception ex)
            {
                await MostrarErrorAsync(
                    "No fue posible probar la conversión: " +
                    ex.Message);
            }
            finally
            {
                IsBusy = false;
                RefrescarComandos();
            }
        }

        private bool TryCrearRequests(
            out List<
                GuardarUnidadConversionRequest>
                requests,
            out string error)
        {
            requests = new();
            error = string.Empty;

            if (UnidadesConfiguradas.Count == 0)
            {
                error =
                    "Debe configurar al menos una unidad.";

                return false;
            }

            bool duplicadas =
                UnidadesConfiguradas
                    .GroupBy(x =>
                        x.UnidadMedidaId)
                    .Any(x =>
                        x.Count() > 1);

            if (duplicadas)
            {
                error =
                    "No puede repetir una unidad.";

                return false;
            }

            int predeterminadas =
                UnidadesConfiguradas
                    .Count(x =>
                        x.Activo &&
                        x.VisibleEnFormulario &&
                        x.UnidadPredeterminada);

            if (predeterminadas > 1)
            {
                error =
                    "Solo puede existir una unidad predeterminada entre las unidades activas y visibles.";

                return false;
            }

            if (EsModoElementos)
            {
                ConfiguracionUnidadItemViewModel?
                    kgHa =
                        UnidadesConfiguradas
                            .FirstOrDefault(x =>
                                x.EsUnidadInternaKgHa);

                if (kgHa == null ||
                    !kgHa.Activo)
                {
                    error =
                        "La conversión interna kg/ha debe permanecer activa porque los rangos nutricionales utilizan esa unidad.";

                    return false;
                }
            }

            foreach (
                ConfiguracionUnidadItemViewModel item
                in UnidadesConfiguradas)
            {
                if (!item.TryCrearRequest(
                        out GuardarUnidadConversionRequest
                            request,
                        out error))
                {
                    return false;
                }

                requests.Add(
                    request);
            }

            return true;
        }

        private void
            Unidad_PredeterminadaActivada(
                object? sender,
                EventArgs e)
        {
            if (sender is not
                ConfiguracionUnidadItemViewModel
                    seleccionada)
            {
                return;
            }

            foreach (
                ConfiguracionUnidadItemViewModel item
                in UnidadesConfiguradas)
            {
                if (!ReferenceEquals(
                        item,
                        seleccionada) &&
                    item.UnidadPredeterminada)
                {
                    item.UnidadPredeterminada =
                        false;
                }
            }

            NotificarResumen();
        }

        private void ConectarEventosUnidad(
            ConfiguracionUnidadItemViewModel
                item)
        {
            item.PredeterminadaActivada +=
                Unidad_PredeterminadaActivada;

            item.PropertyChanged +=
                Unidad_PropertyChanged;
        }

        private void DesconectarEventosUnidades()
        {
            foreach (
                ConfiguracionUnidadItemViewModel item
                in UnidadesConfiguradas)
            {
                item.PredeterminadaActivada -=
                    Unidad_PredeterminadaActivada;

                item.PropertyChanged -=
                    Unidad_PropertyChanged;
            }
        }

        private void Unidad_PropertyChanged(
            object? sender,
            PropertyChangedEventArgs e)
        {
            if (e.PropertyName is
                nameof(
                    ConfiguracionUnidadItemViewModel
                        .Activo) or
                nameof(
                    ConfiguracionUnidadItemViewModel
                        .VisibleEnFormulario) or
                nameof(
                    ConfiguracionUnidadItemViewModel
                        .UnidadPredeterminada))
            {
                NotificarResumen();
                RefrescarComandos();
            }
        }

        private void NormalizarPredeterminadasConfiguradas()
        {
            ConfiguracionUnidadItemViewModel?
                predeterminadaConservada =
                    null;

            foreach (
                ConfiguracionUnidadItemViewModel item
                in UnidadesConfiguradas)
            {
                /*
                 * Una unidad inactiva u oculta no puede seguir marcada como
                 * predeterminada.
                 */
                if (!item.Activo ||
                    !item.VisibleEnFormulario)
                {
                    if (item.UnidadPredeterminada)
                    {
                        item.UnidadPredeterminada =
                            false;
                    }

                    continue;
                }

                if (!item.UnidadPredeterminada)
                    continue;

                /*
                 * Se permite cero o una predeterminada. Si por datos antiguos
                 * llegan varias, se conserva la primera según el orden actual.
                 */
                if (predeterminadaConservada == null)
                {
                    predeterminadaConservada =
                        item;

                    continue;
                }

                item.UnidadPredeterminada =
                    false;
            }
        }

        private void ActualizarUnidadesDisponibles()
        {
            int? seleccionAnterior =
                UnidadParaAgregar?.UnidadMedidaId;

            HashSet<int> utilizadas =
                UnidadesConfiguradas
                    .Select(x =>
                        x.UnidadMedidaId)
                    .ToHashSet();

            List<
                UnidadMedidaCatalogoConfiguracionResponse>
                disponibles =
                    CatalogoUnidades
                        .Where(x =>
                            x.Activo &&
                            !utilizadas.Contains(
                                x.UnidadMedidaId))
                        .OrderBy(x =>
                            x.NombreUnidadMedida)
                        .ToList();

            ReemplazarColeccion(
                UnidadesDisponibles,
                disponibles);

            UnidadParaAgregar =
                seleccionAnterior.HasValue
                    ? UnidadesDisponibles
                        .FirstOrDefault(x =>
                            x.UnidadMedidaId ==
                                seleccionAnterior)
                    : null;
        }

        private void ActualizarElementoCatalogo(
            ElementoConfiguracionUnidadesResponse
                actualizado)
        {
            int indice =
                catalogoElementos
                    .FindIndex(x =>
                        x.ElementoQuimicosId ==
                            actualizado
                                .ElementoQuimicosId);

            if (indice >= 0)
            {
                catalogoElementos[indice] =
                    actualizado;
            }
            else
            {
                catalogoElementos.Add(
                    actualizado);
            }

            AplicarFiltroElementos();

            ElementoSeleccionado =
                catalogoElementos
                    .FirstOrDefault(x =>
                        x.ElementoQuimicosId ==
                            actualizado
                                .ElementoQuimicosId)
                ??
                actualizado;
        }

        private void NotificarResumen()
        {
            OnPropertyChanged(
                nameof(ResumenConfiguracion));

            GuardarCommand
                .ChangeCanExecute();
        }

        private void RefrescarComandos()
        {
            SeleccionarModoElementosCommand
                .ChangeCanExecute();

            SeleccionarModoMateriaOrganicaCommand
                .ChangeCanExecute();

            RecargarCommand
                .ChangeCanExecute();

            CrearUnidadMedidaCommand
                .ChangeCanExecute();

            GuardarCommand
                .ChangeCanExecute();

            AgregarUnidadCommand
                .ChangeCanExecute();

            QuitarUnidadCommand
                .ChangeCanExecute();

            ProbarConversionCommand
                .ChangeCanExecute();

            VolverCommand
                .ChangeCanExecute();
        }

        private static string? ObtenerPrimerError(
            params IConfiguracionUnidadesApiResult[]
                resultados)
        {
            foreach (
                IConfiguracionUnidadesApiResult resultado
                in resultados)
            {
                if (resultado.Success)
                    continue;

                return string.IsNullOrWhiteSpace(
                    resultado.Message)
                    ? "No fue posible cargar la configuración de unidades."
                    : resultado.Message;
            }

            return null;
        }

        private static void ReemplazarColeccion<T>(
            ObservableCollection<T> destino,
            IEnumerable<T> origen)
        {
            destino.Clear();

            foreach (T item in origen)
                destino.Add(item);
        }

        private static bool TryParseDecimal(
            string? texto,
            out decimal valor)
        {
            string limpio =
                (texto ?? string.Empty)
                    .Trim();

            string normalizado =
                limpio.Replace(
                    ',',
                    '.');

            if (decimal.TryParse(
                    normalizado,
                    NumberStyles.Number,
                    CultureInfo.InvariantCulture,
                    out valor))
            {
                return true;
            }

            return decimal.TryParse(
                limpio,
                NumberStyles.Number,
                CultureInfo.CurrentCulture,
                out valor);
        }

        private static int ParseEnteroSeguro(
            string? texto)
        {
            return int.TryParse(
                    texto,
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out int valor)
                ? valor
                : 0;
        }

        private static string LimitarTextoNumero(
            string? texto)
        {
            string valor =
                (texto ?? string.Empty)
                    .Trim();

            return valor.Length <= 30
                ? valor
                : valor[..30];
        }
    }
}
