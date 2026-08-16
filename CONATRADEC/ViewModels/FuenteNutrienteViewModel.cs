using CONATRADEC.Models;
using CONATRADEC.Services;
using Microsoft.Maui.Devices;
using System.Collections.ObjectModel;
using System.Threading;

namespace CONATRADEC.ViewModels
{
    public sealed class FuenteNutrienteViewModel : GlobalService
    {
        private readonly FuenteNutrienteConsultaApiService consultaApiService;
        private readonly FuenteNutrienteApiService fuenteNutrienteApiService;

        private CancellationTokenSource? cargaCts;
        private CancellationTokenSource? composicionCts;
        private CancellationTokenSource? accionCts;

        private string textoBusqueda = string.Empty;
        private string textoBusquedaAplicado = string.Empty;
        private string categoriaAplicadaCodigo =
            FuenteNutrienteCategoriaOption.CodigoTodas;
        private string mensaje = string.Empty;
        private string mensajeComposicion = string.Empty;

        private bool isRefreshing;
        private bool cargandoComposicion;
        private bool navegando;
        private bool pantallaCargada;
        private bool mostrarTablaComposicion;

        private int paginaActual = 1;
        private int totalPaginas = 1;
        private int totalRegistros;
        private int tamanoPaginaActual;
        private int versionAplicada = -1;
        private int eliminacionEnCurso;

        private FuenteNutrienteCategoriaOption?
            filtroCategoriaSeleccionada;

        public FuenteNutrienteViewModel()
            : this(
                new FuenteNutrienteConsultaApiService(),
                new FuenteNutrienteApiService())
        {
        }

        public FuenteNutrienteViewModel(
            FuenteNutrienteConsultaApiService consultaApiService,
            FuenteNutrienteApiService fuenteNutrienteApiService)
        {
            this.consultaApiService =
                consultaApiService
                ?? throw new ArgumentNullException(
                    nameof(consultaApiService));

            this.fuenteNutrienteApiService =
                fuenteNutrienteApiService
                ?? throw new ArgumentNullException(
                    nameof(fuenteNutrienteApiService));

            tamanoPaginaActual =
                ObtenerTamanoPagina();

            CargarFiltrosCategoria();

            RegresarConfiguracionCommand =
                new Command(
                    async () => await EjecutarSeguroAsync(
                        () => NavegarAsync(
                            AppRoutes.Configuracion),
                        "regresar a configuración"),
                    () =>
                        !IsBusy &&
                        !Navegando);

            AddCommand =
                new Command(
                    async () => await EjecutarSeguroAsync(
                        OnAddAsync,
                        "abrir el formulario de fuente"),
                    () =>
                        CanAdd &&
                        !IsBusy &&
                        !Navegando);

            EditCommand =
                new Command<FuenteNutrienteResponse>(
                    async fuente =>
                        await EjecutarSeguroAsync(
                            () => OnEditAsync(fuente),
                            "editar la fuente"),
                    fuente =>
                        fuente?.FuenteNutrientesId is > 0 &&
                        CanEdit &&
                        !IsBusy &&
                        !Navegando);

            ViewCommand =
                new Command<FuenteNutrienteResponse>(
                    async fuente =>
                        await EjecutarSeguroAsync(
                            () => OnViewAsync(fuente),
                            "consultar la fuente"),
                    fuente =>
                        fuente?.FuenteNutrientesId is > 0 &&
                        CanView &&
                        !IsBusy &&
                        !Navegando);

            DeleteCommand =
                new Command<FuenteNutrienteResponse>(
                    async fuente =>
                        await EjecutarSeguroAsync(
                            () => OnDeleteAsync(fuente),
                            "eliminar la fuente"),
                    fuente =>
                        fuente?.FuenteNutrientesId is > 0 &&
                        CanDelete &&
                        !IsBusy &&
                        !Navegando &&
                        Volatile.Read(ref eliminacionEnCurso) == 0);

            BuscarCommand =
                new Command(
                    async () => await EjecutarSeguroAsync(
                        AplicarBusquedaAsync,
                        "buscar fuentes"),
                    () =>
                        CanView &&
                        !IsBusy &&
                        !Navegando);

            LimpiarFiltrosCommand =
                new Command(
                    async () => await EjecutarSeguroAsync(
                        LimpiarFiltrosAsync,
                        "limpiar los filtros"),
                    () =>
                        CanView &&
                        !IsBusy &&
                        !Navegando);

            RefrescarCommand =
                new Command(
                    async () => await EjecutarSeguroAsync(
                        RefrescarAsync,
                        "actualizar las fuentes"),
                    () =>
                        CanView &&
                        !IsBusy &&
                        !Navegando);

            PaginaAnteriorCommand =
                new Command(
                    async () => await EjecutarSeguroAsync(
                        IrPaginaAnteriorAsync,
                        "cargar la página anterior"),
                    () =>
                        CanView &&
                        PuedeIrAnterior &&
                        !IsBusy &&
                        !Navegando);

            PaginaSiguienteCommand =
                new Command(
                    async () => await EjecutarSeguroAsync(
                        IrPaginaSiguienteAsync,
                        "cargar la página siguiente"),
                    () =>
                        CanView &&
                        PuedeIrSiguiente &&
                        !IsBusy &&
                        !Navegando);

            ToggleTablaComposicionCommand =
                new Command(
                    async () => await EjecutarSeguroAsync(
                        ToggleTablaComposicionAsync,
                        "cargar la composición de fuentes"),
                    () =>
                        CanView &&
                        MostrarSeccionTablaComposicion &&
                        !CargandoComposicion &&
                        !IsBusy &&
                        !Navegando);
        }

        public event EventHandler? SolicitarDesplazamientoInicio;

        public ObservableCollection<FuenteNutrienteResponse>
            List { get; } = new();

        public ObservableCollection<FuenteNutrienteCategoriaOption>
            FiltrosCategoria { get; } = new();

        public ObservableCollection<string>
            ElementosTabla { get; } = new();

        public ObservableCollection<FuenteNutrienteTablaDinamicaRow>
            TablaComposicion { get; } = new();

        public Command RegresarConfiguracionCommand { get; }
        public Command AddCommand { get; }
        public Command<FuenteNutrienteResponse> EditCommand { get; }
        public Command<FuenteNutrienteResponse> ViewCommand { get; }
        public Command<FuenteNutrienteResponse> DeleteCommand { get; }
        public Command BuscarCommand { get; }
        public Command LimpiarFiltrosCommand { get; }
        public Command RefrescarCommand { get; }
        public Command PaginaAnteriorCommand { get; }
        public Command PaginaSiguienteCommand { get; }
        public Command ToggleTablaComposicionCommand { get; }

        public string TextoBusqueda
        {
            get => textoBusqueda;
            set
            {
                string nuevoValor =
                    value ?? string.Empty;

                if (textoBusqueda == nuevoValor)
                    return;

                textoBusqueda = nuevoValor;
                OnPropertyChanged();

                // Escribir no genera HTTP; únicamente invalida la matriz visible.
                InvalidarComposicion();
            }
        }

        public FuenteNutrienteCategoriaOption?
            FiltroCategoriaSeleccionada
        {
            get => filtroCategoriaSeleccionada;
            set
            {
                if (ReferenceEquals(
                        filtroCategoriaSeleccionada,
                        value))
                {
                    return;
                }

                filtroCategoriaSeleccionada = value;
                OnPropertyChanged();

                // La categoría se aplica solo con Buscar.
                InvalidarComposicion();
            }
        }

        public string Mensaje
        {
            get => mensaje;
            private set
            {
                string nuevoValor =
                    value ?? string.Empty;

                if (mensaje == nuevoValor)
                    return;

                mensaje = nuevoValor;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TieneMensaje));
            }
        }

        public bool TieneMensaje =>
            !string.IsNullOrWhiteSpace(Mensaje);

        public string MensajeComposicion
        {
            get => mensajeComposicion;
            private set
            {
                string nuevoValor =
                    value ?? string.Empty;

                if (mensajeComposicion == nuevoValor)
                    return;

                mensajeComposicion = nuevoValor;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TieneMensajeComposicion));
            }
        }

        public bool TieneMensajeComposicion =>
            !string.IsNullOrWhiteSpace(MensajeComposicion);

        public bool IsRefreshing
        {
            get => isRefreshing;
            set
            {
                if (isRefreshing == value)
                    return;

                isRefreshing = value;
                OnPropertyChanged();
                ActualizarComandos();
            }
        }

        public bool CargandoComposicion
        {
            get => cargandoComposicion;
            private set
            {
                if (cargandoComposicion == value)
                    return;

                cargandoComposicion = value;
                OnPropertyChanged();
                ActualizarComandos();
                NotificarEstadoComposicion();
            }
        }

        public bool Navegando
        {
            get => navegando;
            private set
            {
                if (navegando == value)
                    return;

                navegando = value;
                OnPropertyChanged();
                ActualizarComandos();
            }
        }

        public bool MostrarTablaComposicion
        {
            get => mostrarTablaComposicion;
            private set
            {
                if (mostrarTablaComposicion == value)
                    return;

                mostrarTablaComposicion = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TextoBotonTablaComposicion));
                NotificarEstadoComposicion();
            }
        }

        public string TextoBotonTablaComposicion =>
            MostrarTablaComposicion
                ? "Ocultar matriz"
                : "Ver matriz";

        public bool MostrarSeccionTablaComposicion =>
            !string.Equals(
                categoriaAplicadaCodigo,
                FuenteNutrienteCategoriaOption.CodigoEnmiendaCalcarea,
                StringComparison.OrdinalIgnoreCase);

        public bool MostrarTablaConDatos =>
            MostrarSeccionTablaComposicion &&
            MostrarTablaComposicion &&
            !CargandoComposicion &&
            ElementosTabla.Count > 0 &&
            TablaComposicion.Count > 0;

        public bool MostrarMensajeTablaVacia =>
            MostrarSeccionTablaComposicion &&
            MostrarTablaComposicion &&
            !CargandoComposicion &&
            ElementosTabla.Count == 0 &&
            !TieneMensajeComposicion;

        public int TotalRegistros
        {
            get => totalRegistros;
            private set
            {
                if (totalRegistros == value)
                    return;

                totalRegistros = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ResumenResultados));
                OnPropertyChanged(nameof(RangoPaginaTexto));
                OnPropertyChanged(nameof(MostrarPaginacion));
            }
        }

        public string ResumenResultados =>
            TotalRegistros == 1
                ? "1 fuente encontrada"
                : $"{TotalRegistros} fuentes encontradas";

        public int PaginaActual => paginaActual;

        public int TotalPaginas => totalPaginas;

        public bool PuedeIrAnterior =>
            pantallaCargada &&
            paginaActual > 1;

        public bool PuedeIrSiguiente =>
            pantallaCargada &&
            paginaActual < totalPaginas;

        public bool MostrarPaginacion =>
            CanView &&
            pantallaCargada &&
            List.Count > 0;

        public string PaginaTexto =>
            $"Página {Math.Max(1, paginaActual)} de {Math.Max(1, totalPaginas)}";

        public string RangoPaginaTexto
        {
            get
            {
                if (TotalRegistros <= 0 ||
                    List.Count == 0)
                {
                    return "Sin registros en esta página";
                }

                int tamano =
                    Math.Max(1, tamanoPaginaActual);

                int inicio =
                    ((Math.Max(1, paginaActual) - 1) * tamano) + 1;

                int fin =
                    Math.Min(
                        inicio + List.Count - 1,
                        TotalRegistros);

                return $"Mostrando {inicio}-{fin} de {TotalRegistros}";
            }
        }

        public bool MostrarVacio =>
            CanView &&
            pantallaCargada &&
            !IsBusy &&
            List.Count == 0 &&
            !TieneMensaje;

        public bool MostrarAccesoDenegado =>
            !CanView;

        public void ActualizarPermisos()
        {
            LoadPagePermissions(
                "fuenteNutrientePage");

            OnPropertyChanged(nameof(MostrarAccesoDenegado));
            NotificarEstadoLista();
            NotificarEstadoComposicion();
            ActualizarComandos();
        }

        public async Task IniciarNuevaVisitaAsync()
        {
            if (!CanView || Navegando)
                return;

            CancelarCargas();

            TextoBusqueda = string.Empty;
            textoBusquedaAplicado = string.Empty;
            categoriaAplicadaCodigo =
                FuenteNutrienteCategoriaOption.CodigoTodas;

            filtroCategoriaSeleccionada =
                FiltrosCategoria.FirstOrDefault();
            OnPropertyChanged(nameof(FiltroCategoriaSeleccionada));

            Mensaje = string.Empty;
            paginaActual = 1;
            totalPaginas = 1;
            TotalRegistros = 0;
            tamanoPaginaActual = ObtenerTamanoPagina();
            pantallaCargada = false;
            versionAplicada = -1;

            List.Clear();
            InvalidarComposicion();
            NotificarEstadoLista();

            await CargarPaginaAsync(
                1,
                cargaInicial: true);
        }

        public async Task InicializarAsync()
        {
            if (!CanView || Navegando)
                return;

            if (!pantallaCargada)
            {
                await CargarPaginaAsync(
                    1,
                    cargaInicial: true);
                return;
            }

            int versionActual =
                FuenteNutrienteListadoEstadoService.VersionActual;

            if (FuenteNutrienteListadoEstadoService
                    .IntentarConsumirEdicion(
                        out FuenteNutrienteResponse editada))
            {
                bool sinFiltrosAplicados =
                    string.IsNullOrWhiteSpace(textoBusquedaAplicado) &&
                    string.Equals(
                        categoriaAplicadaCodigo,
                        FuenteNutrienteCategoriaOption.CodigoTodas,
                        StringComparison.OrdinalIgnoreCase);

                if (sinFiltrosAplicados &&
                    AplicarEdicionLocal(editada))
                {
                    versionAplicada = versionActual;
                    InvalidarComposicion();
                    NotificarEstadoLista();
                    return;
                }
            }

            if (versionAplicada != versionActual)
            {
                await CargarPaginaAsync(
                    Math.Max(1, paginaActual));
            }
        }

        public Task RecargarPaginaActualAsync() =>
            CargarPaginaAsync(
                Math.Max(1, paginaActual));

        public void CancelarCargas()
        {
            CancellationTokenSource? carga =
                Interlocked.Exchange(
                    ref cargaCts,
                    null);

            CancellationTokenSource? composicion =
                Interlocked.Exchange(
                    ref composicionCts,
                    null);

            CancellationTokenSource? accion =
                Interlocked.Exchange(
                    ref accionCts,
                    null);

            CancelarSeguro(carga);
            CancelarSeguro(composicion);
            CancelarSeguro(accion);

            IsBusy = false;
            IsRefreshing = false;
            CargandoComposicion = false;
            ActualizarComandos();
        }

        private async Task AplicarBusquedaAsync()
        {
            textoBusquedaAplicado =
                (TextoBusqueda ?? string.Empty).Trim();

            categoriaAplicadaCodigo =
                ObtenerCodigoCategoriaSeleccionada();

            InvalidarComposicion();
            OnPropertyChanged(nameof(MostrarSeccionTablaComposicion));

            await CargarPaginaAsync(
                1,
                desplazarAlInicio: true);
        }

        private async Task LimpiarFiltrosAsync()
        {
            TextoBusqueda = string.Empty;
            textoBusquedaAplicado = string.Empty;
            categoriaAplicadaCodigo =
                FuenteNutrienteCategoriaOption.CodigoTodas;

            filtroCategoriaSeleccionada =
                FiltrosCategoria.FirstOrDefault();
            OnPropertyChanged(nameof(FiltroCategoriaSeleccionada));

            InvalidarComposicion();
            OnPropertyChanged(nameof(MostrarSeccionTablaComposicion));

            await CargarPaginaAsync(
                1,
                desplazarAlInicio: true);
        }

        private async Task RefrescarAsync()
        {
            IsRefreshing = true;

            try
            {
                InvalidarComposicion();

                await CargarPaginaAsync(
                    Math.Max(1, paginaActual));
            }
            finally
            {
                IsRefreshing = false;
            }
        }

        private Task IrPaginaAnteriorAsync()
        {
            if (!PuedeIrAnterior)
                return Task.CompletedTask;

            return CargarPaginaAsync(
                paginaActual - 1,
                desplazarAlInicio: true);
        }

        private Task IrPaginaSiguienteAsync()
        {
            if (!PuedeIrSiguiente)
                return Task.CompletedTask;

            return CargarPaginaAsync(
                paginaActual + 1,
                desplazarAlInicio: true);
        }

        private async Task CargarPaginaAsync(
            int paginaSolicitada,
            bool cargaInicial = false,
            bool desplazarAlInicio = false)
        {
            if (!CanView ||
                Navegando ||
                IsBusy)
            {
                return;
            }

            paginaSolicitada =
                Math.Max(1, paginaSolicitada);

            CancellationTokenSource source =
                PrepararNuevaCarga();

            try
            {
                IsBusy = true;
                Mensaje = string.Empty;
                ActualizarComandos();

                int tamanoPagina =
                    ObtenerTamanoPagina();

                ApiResult<FuenteNutrientePaginaResponse> resultado =
                    await consultaApiService.BuscarAsync(
                        textoBusquedaAplicado,
                        categoriaAplicadaCodigo,
                        paginaSolicitada,
                        tamanoPagina,
                        source.Token);

                if (source.IsCancellationRequested ||
                    !EsCargaActual(source))
                {
                    return;
                }

                if (!resultado.Success ||
                    resultado.Data == null)
                {
                    if (!EsMensajeCancelacion(resultado.Message))
                        Mensaje = resultado.Message;

                    return;
                }

                FuenteNutrientePaginaResponse pagina =
                    resultado.Data;

                int paginasServidor =
                    Math.Max(1, pagina.TotalPaginas);

                if (paginaSolicitada > paginasServidor &&
                    pagina.TotalRegistros > 0)
                {
                    resultado =
                        await consultaApiService.BuscarAsync(
                            textoBusquedaAplicado,
                            categoriaAplicadaCodigo,
                            paginasServidor,
                            tamanoPagina,
                            source.Token);

                    if (source.IsCancellationRequested ||
                        !EsCargaActual(source))
                    {
                        return;
                    }

                    if (!resultado.Success ||
                        resultado.Data == null)
                    {
                        if (!EsMensajeCancelacion(resultado.Message))
                            Mensaje = resultado.Message;

                        return;
                    }

                    pagina = resultado.Data;
                }

                AplicarPagina(pagina);

                pantallaCargada = true;
                versionAplicada =
                    FuenteNutrienteListadoEstadoService.VersionActual;

                if (!cargaInicial &&
                    desplazarAlInicio)
                {
                    SolicitarDesplazamientoInicio?.Invoke(
                        this,
                        EventArgs.Empty);
                }
            }
            catch (OperationCanceledException)
            {
                // Cancelación normal al navegar o reemplazar una consulta.
            }
            catch (ObjectDisposedException)
            {
                // La solicitud terminó mientras se abandonaba la pantalla.
            }
            catch (Exception ex)
            {
                if (!source.IsCancellationRequested &&
                    EsCargaActual(source))
                {
                    Mensaje =
                        "No fue posible cargar las fuentes de nutrientes.";

                    await MostrarErrorInesperadoAsync(
                        "cargar las fuentes de nutrientes",
                        ex);
                }
            }
            finally
            {
                if (EsCargaActual(source))
                {
                    IsBusy = false;
                    IsRefreshing = false;
                }

                LiberarCarga(source);
                ActualizarComandos();
                NotificarEstadoLista();
            }
        }

        private void AplicarPagina(
            FuenteNutrientePaginaResponse pagina)
        {
            List.Clear();

            foreach (FuenteNutrienteResponse fuente
                     in pagina.Items)
            {
                if (fuente.FuenteNutrientesId is > 0)
                    List.Add(fuente);
            }

            paginaActual =
                Math.Max(1, pagina.PaginaActual);

            totalPaginas =
                Math.Max(1, pagina.TotalPaginas);

            tamanoPaginaActual =
                pagina.TamanoPagina > 0
                    ? pagina.TamanoPagina
                    : ObtenerTamanoPagina();

            TotalRegistros =
                Math.Max(0, pagina.TotalRegistros);

            Mensaje = string.Empty;
            NotificarEstadoLista();
        }

        private bool AplicarEdicionLocal(
            FuenteNutrienteResponse editada)
        {
            if (editada.FuenteNutrientesId is not > 0)
                return false;

            for (int i = 0; i < List.Count; i++)
            {
                if (List[i].FuenteNutrientesId ==
                    editada.FuenteNutrientesId)
                {
                    List[i] = editada;
                    Mensaje = string.Empty;
                    return true;
                }
            }

            return false;
        }

        private async Task ToggleTablaComposicionAsync()
        {
            if (!MostrarSeccionTablaComposicion)
                return;

            if (MostrarTablaComposicion)
            {
                MostrarTablaComposicion = false;
                return;
            }

            MostrarTablaComposicion = true;
            await CargarComposicionAsync();
        }

        private async Task CargarComposicionAsync()
        {
            CancellationTokenSource source =
                PrepararNuevaCargaComposicion();

            try
            {
                CargandoComposicion = true;
                MensajeComposicion = string.Empty;
                ElementosTabla.Clear();
                TablaComposicion.Clear();

                ApiResult<List<FuenteNutrienteResponse>> resultado =
                    await consultaApiService.ObtenerComposicionAsync(
                        textoBusquedaAplicado,
                        categoriaAplicadaCodigo,
                        source.Token);

                if (source.IsCancellationRequested ||
                    !EsCargaComposicionActual(source))
                {
                    return;
                }

                if (!resultado.Success ||
                    resultado.Data == null)
                {
                    if (!EsMensajeCancelacion(resultado.Message))
                        MensajeComposicion = resultado.Message;

                    return;
                }

                ConstruirTablaComposicion(
                    resultado.Data);
            }
            catch (OperationCanceledException)
            {
                // Cancelación normal.
            }
            catch (ObjectDisposedException)
            {
                // La página se cerró.
            }
            catch (Exception ex)
            {
                if (!source.IsCancellationRequested &&
                    EsCargaComposicionActual(source))
                {
                    MensajeComposicion =
                        "No fue posible construir la matriz de composición.";

                    await MostrarErrorInesperadoAsync(
                        "cargar la composición de fuentes",
                        ex);
                }
            }
            finally
            {
                if (EsCargaComposicionActual(source))
                    CargandoComposicion = false;

                LiberarCargaComposicion(source);
                NotificarEstadoComposicion();
            }
        }

        private void ConstruirTablaComposicion(
            IEnumerable<FuenteNutrienteResponse> fuentes)
        {
            List<FuenteNutrienteResponse> fuentesConAporte =
                fuentes
                    .Where(FuenteTieneAporteElementoQuimico)
                    .OrderBy(item =>
                        item.NombreNutriente ?? string.Empty)
                    .ToList();

            List<string> simbolos =
                fuentesConAporte
                    .SelectMany(item =>
                        item.ElementosQuimicos ??
                        new List<FuenteNutrienteElementoQuimicoResponse>())
                    .Where(item =>
                        !string.IsNullOrWhiteSpace(
                            item.SimboloElementoQuimico) &&
                        (item.CantidadAporte ?? 0) > 0)
                    .Select(item =>
                        item.SimboloElementoQuimico!.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(ObtenerOrdenElemento)
                    .ThenBy(item => item)
                    .ToList();

            foreach (string simbolo in simbolos)
                ElementosTabla.Add(simbolo);

            foreach (FuenteNutrienteResponse fuente
                     in fuentesConAporte)
            {
                var fila =
                    new FuenteNutrienteTablaDinamicaRow
                    {
                        FuenteNutrientesId =
                            fuente.FuenteNutrientesId,

                        Fuente =
                            fuente.NombreNutriente ??
                            "Fuente sin nombre"
                    };

                foreach (string simbolo in simbolos)
                {
                    FuenteNutrienteElementoQuimicoResponse? aporte =
                        fuente.ElementosQuimicos?
                            .FirstOrDefault(item =>
                                string.Equals(
                                    item.SimboloElementoQuimico?.Trim(),
                                    simbolo,
                                    StringComparison.OrdinalIgnoreCase));

                    fila.Celdas.Add(
                        new FuenteNutrienteTablaDinamicaCell
                        {
                            SimboloElemento = simbolo,
                            Valor = aporte?.CantidadAporte ?? 0
                        });
                }

                TablaComposicion.Add(fila);
            }

            NotificarEstadoComposicion();
        }

        private void InvalidarComposicion()
        {
            CancellationTokenSource? source =
                Interlocked.Exchange(
                    ref composicionCts,
                    null);

            CancelarSeguro(source);

            MostrarTablaComposicion = false;
            CargandoComposicion = false;
            MensajeComposicion = string.Empty;
            ElementosTabla.Clear();
            TablaComposicion.Clear();
            NotificarEstadoComposicion();
        }

        private Task OnAddAsync() =>
            NavegarAsync(
                AppRoutes.FuenteNutrienteFormulario,
                new Dictionary<string, object>
                {
                    {
                        "ContextoFuente",
                        new FuenteNutrienteFormNavigationContext
                        {
                            Mode = FormMode.FormModeSelect.Create,
                            Fuente = new FuenteNutrienteRequest()
                        }
                    }
                });

        private Task OnEditAsync(
            FuenteNutrienteResponse? fuente)
        {
            if (fuente?.FuenteNutrientesId is not > 0)
                return Task.CompletedTask;

            return NavegarAsync(
                AppRoutes.FuenteNutrienteFormulario,
                new Dictionary<string, object>
                {
                    {
                        "ContextoFuente",
                        new FuenteNutrienteFormNavigationContext
                        {
                            Mode = FormMode.FormModeSelect.Edit,
                            Fuente = new FuenteNutrienteRequest(fuente)
                        }
                    }
                });
        }

        private Task OnViewAsync(
            FuenteNutrienteResponse? fuente)
        {
            if (fuente?.FuenteNutrientesId is not > 0)
                return Task.CompletedTask;

            return NavegarAsync(
                AppRoutes.FuenteNutrienteFormulario,
                new Dictionary<string, object>
                {
                    {
                        "ContextoFuente",
                        new FuenteNutrienteFormNavigationContext
                        {
                            Mode = FormMode.FormModeSelect.View,
                            Fuente = new FuenteNutrienteRequest(fuente)
                        }
                    }
                });
        }

        private async Task OnDeleteAsync(
            FuenteNutrienteResponse? fuente)
        {
            if (fuente?.FuenteNutrientesId is not > 0 ||
                IsBusy ||
                Interlocked.CompareExchange(
                    ref eliminacionEnCurso,
                    1,
                    0) != 0)
            {
                return;
            }

            bool recargarPagina = false;
            int paginaOriginal = paginaActual;
            int paginaARecargar = paginaActual;

            try
            {
                bool confirmar =
                    await Application.Current!
                        .MainPage!
                        .DisplayAlert(
                            "Eliminar fuente",
                            $"¿Desea eliminar la fuente '{fuente.NombreNutriente}'?",
                            "Eliminar",
                            "Cancelar");

                if (!confirmar)
                    return;

                CancellationTokenSource source =
                    PrepararNuevaAccion();

                try
                {
                    IsBusy = true;
                    ActualizarComandos();

                    int totalPaginasAntes =
                        totalPaginas;

                    ApiResult<bool> resultado =
                        await fuenteNutrienteApiService
                            .DeleteFuenteNutrienteAdminResultAsync(
                                fuente.FuenteNutrientesId.Value,
                                source.Token);

                    if (source.IsCancellationRequested ||
                        !EsAccionActual(source))
                    {
                        return;
                    }

                    if (!resultado.Success)
                    {
                        if (!EsMensajeCancelacion(resultado.Message))
                            await MostrarToastAsync(resultado.Message);

                        return;
                    }

                    List.Remove(fuente);
                    TotalRegistros =
                        Math.Max(0, TotalRegistros - 1);

                    int nuevoTotalPaginas =
                        TotalRegistros == 0
                            ? 1
                            : (int)Math.Ceiling(
                                TotalRegistros /
                                (double)Math.Max(
                                    1,
                                    tamanoPaginaActual));

                    totalPaginas =
                        Math.Max(1, nuevoTotalPaginas);

                    if (paginaActual > totalPaginas)
                        paginaActual = totalPaginas;

                    paginaARecargar =
                        Math.Max(1, paginaActual);

                    recargarPagina =
                        TotalRegistros > 0 &&
                        (paginaARecargar < totalPaginasAntes ||
                         List.Count == 0);

                    versionAplicada =
                        FuenteNutrienteListadoEstadoService.MarcarCambio();

                    InvalidarComposicion();

                    await MostrarToastAsync(
                        string.IsNullOrWhiteSpace(resultado.Message)
                            ? "Fuente eliminada correctamente."
                            : resultado.Message);

                    NotificarEstadoLista();
                }
                finally
                {
                    IsBusy = false;
                    LiberarAccion(source);
                    ActualizarComandos();
                    NotificarEstadoLista();
                }
            }
            finally
            {
                Interlocked.Exchange(
                    ref eliminacionEnCurso,
                    0);

                ActualizarComandos();
            }

            if (recargarPagina &&
                CanView &&
                !Navegando)
            {
                await CargarPaginaAsync(
                    paginaARecargar,
                    desplazarAlInicio:
                        paginaARecargar != paginaOriginal ||
                        List.Count == 0);
            }
        }

        private async Task NavegarAsync(
            string ruta,
            IDictionary<string, object>? parametros = null)
        {
            if (Navegando)
                return;

            Navegando = true;

            try
            {
                CancelarCargas();

                if (parametros == null)
                {
                    await GoToAsyncParameters(ruta);
                }
                else
                {
                    await GoToAsyncParameters(
                        ruta,
                        parametros);
                }
            }
            finally
            {
                Navegando = false;
            }
        }

        private void CargarFiltrosCategoria()
        {
            FiltrosCategoria.Clear();

            FiltrosCategoria.Add(
                new FuenteNutrienteCategoriaOption
                {
                    Codigo = FuenteNutrienteCategoriaOption.CodigoTodas,
                    Nombre = "Todas"
                });

            FiltrosCategoria.Add(
                new FuenteNutrienteCategoriaOption
                {
                    Codigo =
                        FuenteNutrienteCategoriaOption.CodigoBalanceNutricional,
                    Nombre = "Balance nutricional"
                });

            FiltrosCategoria.Add(
                new FuenteNutrienteCategoriaOption
                {
                    Codigo =
                        FuenteNutrienteCategoriaOption.CodigoEnmiendaCalcarea,
                    Nombre = "Enmienda calcárea"
                });

            FiltrosCategoria.Add(
                new FuenteNutrienteCategoriaOption
                {
                    Codigo =
                        FuenteNutrienteCategoriaOption.CodigoFertilizacionMixta,
                    Nombre = "Fertilización mixta"
                });

            filtroCategoriaSeleccionada =
                FiltrosCategoria.FirstOrDefault();

            OnPropertyChanged(nameof(FiltroCategoriaSeleccionada));
        }

        private string ObtenerCodigoCategoriaSeleccionada() =>
            FiltroCategoriaSeleccionada?.Codigo ??
            FuenteNutrienteCategoriaOption.CodigoTodas;

        private async Task EjecutarSeguroAsync(
            Func<Task> accion,
            string descripcion)
        {
            try
            {
                await accion();
            }
            catch (Exception ex)
            {
                await MostrarErrorInesperadoAsync(
                    descripcion,
                    ex);
            }
        }

        private void ActualizarComandos()
        {
            RegresarConfiguracionCommand.ChangeCanExecute();
            AddCommand.ChangeCanExecute();
            EditCommand.ChangeCanExecute();
            ViewCommand.ChangeCanExecute();
            DeleteCommand.ChangeCanExecute();
            BuscarCommand.ChangeCanExecute();
            LimpiarFiltrosCommand.ChangeCanExecute();
            RefrescarCommand.ChangeCanExecute();
            PaginaAnteriorCommand.ChangeCanExecute();
            PaginaSiguienteCommand.ChangeCanExecute();
            ToggleTablaComposicionCommand.ChangeCanExecute();
        }

        private void NotificarEstadoLista()
        {
            OnPropertyChanged(nameof(PaginaActual));
            OnPropertyChanged(nameof(TotalPaginas));
            OnPropertyChanged(nameof(PuedeIrAnterior));
            OnPropertyChanged(nameof(PuedeIrSiguiente));
            OnPropertyChanged(nameof(MostrarPaginacion));
            OnPropertyChanged(nameof(PaginaTexto));
            OnPropertyChanged(nameof(RangoPaginaTexto));
            OnPropertyChanged(nameof(MostrarVacio));
            OnPropertyChanged(nameof(ResumenResultados));
            OnPropertyChanged(nameof(MostrarSeccionTablaComposicion));
        }

        private void NotificarEstadoComposicion()
        {
            OnPropertyChanged(nameof(MostrarTablaConDatos));
            OnPropertyChanged(nameof(MostrarMensajeTablaVacia));
            OnPropertyChanged(nameof(TextoBotonTablaComposicion));
        }

        private static bool FuenteTieneAporteElementoQuimico(
            FuenteNutrienteResponse fuente) =>
            fuente.ElementosQuimicos != null &&
            fuente.ElementosQuimicos.Any(item =>
                !string.IsNullOrWhiteSpace(
                    item.SimboloElementoQuimico) &&
                (item.CantidadAporte ?? 0) > 0);

        private static int ObtenerOrdenElemento(
            string simbolo) =>
            simbolo.Trim().ToUpperInvariant() switch
            {
                "N" => 1,
                "P" => 2,
                "K" => 3,
                "CA" => 4,
                "MG" => 5,
                "ZN" => 6,
                "S" => 7,
                "B" => 8,
                _ => 100
            };

        private static int ObtenerTamanoPagina() =>
            DeviceInfo.Platform == DevicePlatform.WinUI
                ? 40
                : 20;

        private CancellationTokenSource PrepararNuevaCarga()
        {
            var source = new CancellationTokenSource();

            CancellationTokenSource? anterior =
                Interlocked.Exchange(
                    ref cargaCts,
                    source);

            CancelarSeguro(anterior);
            return source;
        }

        private CancellationTokenSource PrepararNuevaCargaComposicion()
        {
            var source = new CancellationTokenSource();

            CancellationTokenSource? anterior =
                Interlocked.Exchange(
                    ref composicionCts,
                    source);

            CancelarSeguro(anterior);
            return source;
        }

        private CancellationTokenSource PrepararNuevaAccion()
        {
            var source = new CancellationTokenSource();

            CancellationTokenSource? anterior =
                Interlocked.Exchange(
                    ref accionCts,
                    source);

            CancelarSeguro(anterior);
            return source;
        }

        private bool EsCargaActual(
            CancellationTokenSource source) =>
            ReferenceEquals(
                Volatile.Read(ref cargaCts),
                source);

        private bool EsCargaComposicionActual(
            CancellationTokenSource source) =>
            ReferenceEquals(
                Volatile.Read(ref composicionCts),
                source);

        private bool EsAccionActual(
            CancellationTokenSource source) =>
            ReferenceEquals(
                Volatile.Read(ref accionCts),
                source);

        private void LiberarCarga(
            CancellationTokenSource source)
        {
            Interlocked.CompareExchange(
                ref cargaCts,
                null,
                source);

            source.Dispose();
        }

        private void LiberarCargaComposicion(
            CancellationTokenSource source)
        {
            Interlocked.CompareExchange(
                ref composicionCts,
                null,
                source);

            source.Dispose();
        }

        private void LiberarAccion(
            CancellationTokenSource source)
        {
            Interlocked.CompareExchange(
                ref accionCts,
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
                // La solicitud ya había terminado.
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
