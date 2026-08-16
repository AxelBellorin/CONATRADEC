using CONATRADEC.Models;
using CONATRADEC.Services;
using Microsoft.Maui.Devices;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Threading;

namespace CONATRADEC.ViewModels
{
    public sealed class TerrenoViewModel : GlobalService
    {
        private readonly TerrenoApiService terrenoApiService;
        private readonly TerrenoBusquedaApiService busquedaApiService;
        private readonly PaisApiService paisApiService;
        private readonly DepartamentoApiService departamentoApiService;
        private readonly MunicipioApiService municipioApiService;

        private CancellationTokenSource? cargaCts;
        private CancellationTokenSource? ubicacionCts;

        // Valores escritos actualmente en la interfaz.
        private string textoBusqueda = string.Empty;
        private string codigoFiltro = string.Empty;
        private string propietarioFiltro = string.Empty;
        private string identificacionFiltro = string.Empty;
        private string direccionFiltro = string.Empty;
        private string extensionMinimaTexto = string.Empty;
        private string extensionMaximaTexto = string.Empty;
        private string ordenarSeleccionado = "Código (A-Z)";

        // Instantánea de los filtros realmente aplicados al servidor. Escribir
        // nuevos valores no modifica la paginación hasta presionar Buscar.
        private string textoBusquedaAplicado = string.Empty;
        private string codigoFiltroAplicado = string.Empty;
        private string propietarioFiltroAplicado = string.Empty;
        private string identificacionFiltroAplicado = string.Empty;
        private string direccionFiltroAplicado = string.Empty;
        private int? paisIdAplicado;
        private int? departamentoIdAplicado;
        private int? municipioIdAplicado;
        private DateOnly? fechaDesdeAplicada;
        private DateOnly? fechaHastaAplicada;
        private decimal? extensionMinimaAplicada;
        private decimal? extensionMaximaAplicada;
        private string ordenarPorAplicado = "codigo";
        private bool descendenteAplicado;

        private string mensaje = string.Empty;
        private string tituloRelay = "Procesando...";
        private string detalleRelay = "Espere un momento.";

        private PaisResponse? paisSeleccionado;
        private DepartamentoResponse? departamentoSeleccionado;
        private MunicipioResponse? municipioSeleccionado;

        private bool mostrarFiltrosAvanzados;
        private bool filtrarPorFecha;
        private bool isRefreshing;
        private bool navegando;
        private bool ubicacionCargando;
        private bool pantallaCargada;
        private bool catalogosCargados;
        private bool actualizandoUbicacionInterna;
        private bool mostrandoRelay;

        private DateTime fechaDesde = DateTime.Today.AddYears(-1);
        private DateTime fechaHasta = DateTime.Today;

        private int paginaActual = 1;
        private int totalPaginas = 1;
        private int totalRegistros;
        private int tamanoPaginaActual;

        public TerrenoViewModel()
            : this(
                new TerrenoApiService(),
                new TerrenoBusquedaApiService(),
                new PaisApiService(),
                new DepartamentoApiService(),
                new MunicipioApiService())
        {
        }

        internal TerrenoViewModel(
            TerrenoApiService terrenoApiService,
            TerrenoBusquedaApiService busquedaApiService,
            PaisApiService paisApiService,
            DepartamentoApiService departamentoApiService,
            MunicipioApiService municipioApiService)
        {
            this.terrenoApiService = terrenoApiService;
            this.busquedaApiService = busquedaApiService;
            this.paisApiService = paisApiService;
            this.departamentoApiService = departamentoApiService;
            this.municipioApiService = municipioApiService;

            List = new ObservableCollection<TerrenoResponse>();
            Paises = new ObservableCollection<PaisResponse>();
            Departamentos = new ObservableCollection<DepartamentoResponse>();
            Municipios = new ObservableCollection<MunicipioResponse>();

            Ordenamientos = new ObservableCollection<string>
            {
                "Código (A-Z)",
                "Código (Z-A)",
                "Propietario (A-Z)",
                "Propietario (Z-A)",
                "Fecha más reciente",
                "Fecha más antigua",
                "Mayor extensión",
                "Menor extensión"
            };

            RegresarConfiguracionCommand = new Command(
                async () => await EjecutarSeguroAsync(
                    SalirAConfiguracionAsync,
                    "regresar a configuración"),
                () => !IsBusy && !Navegando);

            AddCommand = new Command(
                async () => await EjecutarSeguroAsync(
                    OnAddAsync,
                    "abrir el formulario de terreno"),
                () => CanAdd && !IsBusy && !Navegando);

            EditCommand = new Command<TerrenoResponse>(
                async item => await EjecutarSeguroAsync(
                    () => OnEditAsync(item),
                    "editar el terreno"),
                item => item != null && CanEdit && !IsBusy && !Navegando);

            DeleteCommand = new Command<TerrenoResponse>(
                async item => await EjecutarSeguroAsync(
                    () => OnDeleteAsync(item),
                    "eliminar el terreno"),
                item => item != null && CanDelete && !IsBusy && !Navegando);

            ViewCommand = new Command<TerrenoResponse>(
                async item => await EjecutarSeguroAsync(
                    () => OnViewAsync(item),
                    "abrir el terreno"),
                item => item != null && CanView && !IsBusy && !Navegando);

            BuscarCommand = new Command(
                async () => await EjecutarSeguroAsync(
                    BuscarAsync,
                    "buscar terrenos"),
                () => CanView && !IsBusy && !Navegando);

            LimpiarFiltrosCommand = new Command(
                async () => await EjecutarSeguroAsync(
                    LimpiarFiltrosAsync,
                    "limpiar los filtros"),
                () => CanView && !IsBusy && !Navegando);

            AlternarFiltrosCommand = new Command(
                () => MostrarFiltrosAvanzados = !MostrarFiltrosAvanzados,
                () => CanView && !Navegando);

            RefrescarCommand = new Command(
                async () => await EjecutarSeguroAsync(
                    RefrescarAsync,
                    "actualizar los terrenos"),
                () => CanView && !IsBusy && !Navegando);

            PaginaAnteriorCommand = new Command(
                async () => await EjecutarSeguroAsync(
                    IrPaginaAnteriorAsync,
                    "cargar la página anterior"),
                () => CanView && PuedeIrAnterior && !IsBusy && !Navegando);

            PaginaSiguienteCommand = new Command(
                async () => await EjecutarSeguroAsync(
                    IrPaginaSiguienteAsync,
                    "cargar la página siguiente"),
                () => CanView && PuedeIrSiguiente && !IsBusy && !Navegando);
        }

        public ObservableCollection<TerrenoResponse> List { get; }
        public ObservableCollection<PaisResponse> Paises { get; }
        public ObservableCollection<DepartamentoResponse> Departamentos { get; }
        public ObservableCollection<MunicipioResponse> Municipios { get; }
        public ObservableCollection<string> Ordenamientos { get; }

        public Command RegresarConfiguracionCommand { get; }
        public Command AddCommand { get; }
        public Command<TerrenoResponse> EditCommand { get; }
        public Command<TerrenoResponse> DeleteCommand { get; }
        public Command<TerrenoResponse> ViewCommand { get; }
        public Command BuscarCommand { get; }
        public Command LimpiarFiltrosCommand { get; }
        public Command AlternarFiltrosCommand { get; }
        public Command RefrescarCommand { get; }
        public Command PaginaAnteriorCommand { get; }
        public Command PaginaSiguienteCommand { get; }

        public string TextoBusqueda
        {
            get => textoBusqueda;
            set => AsignarTexto(ref textoBusqueda, value);
        }

        public string CodigoFiltro
        {
            get => codigoFiltro;
            set => AsignarTexto(ref codigoFiltro, value);
        }

        public string PropietarioFiltro
        {
            get => propietarioFiltro;
            set => AsignarTexto(ref propietarioFiltro, value);
        }

        public string IdentificacionFiltro
        {
            get => identificacionFiltro;
            set => AsignarTexto(ref identificacionFiltro, value);
        }

        public string DireccionFiltro
        {
            get => direccionFiltro;
            set => AsignarTexto(ref direccionFiltro, value);
        }

        public string ExtensionMinimaTexto
        {
            get => extensionMinimaTexto;
            set => AsignarTexto(ref extensionMinimaTexto, value);
        }

        public string ExtensionMaximaTexto
        {
            get => extensionMaximaTexto;
            set => AsignarTexto(ref extensionMaximaTexto, value);
        }

        public string OrdenarSeleccionado
        {
            get => ordenarSeleccionado;
            set
            {
                string nuevoValor = string.IsNullOrWhiteSpace(value)
                    ? "Código (A-Z)"
                    : value;

                if (ordenarSeleccionado == nuevoValor)
                    return;

                ordenarSeleccionado = nuevoValor;
                OnPropertyChanged();
            }
        }

        public string Mensaje
        {
            get => mensaje;
            private set
            {
                string nuevoValor = value ?? string.Empty;

                if (mensaje == nuevoValor)
                    return;

                mensaje = nuevoValor;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TieneMensaje));
            }
        }

        public PaisResponse? PaisSeleccionado
        {
            get => paisSeleccionado;
            set
            {
                if (ReferenceEquals(paisSeleccionado, value))
                    return;

                paisSeleccionado = value;
                OnPropertyChanged();

                if (!actualizandoUbicacionInterna)
                    _ = CargarDepartamentosPorPaisAsync(value);
            }
        }

        public DepartamentoResponse? DepartamentoSeleccionado
        {
            get => departamentoSeleccionado;
            set
            {
                if (ReferenceEquals(departamentoSeleccionado, value))
                    return;

                departamentoSeleccionado = value;
                OnPropertyChanged();

                if (!actualizandoUbicacionInterna)
                    _ = CargarMunicipiosPorDepartamentoAsync(value);
            }
        }

        public MunicipioResponse? MunicipioSeleccionado
        {
            get => municipioSeleccionado;
            set
            {
                if (ReferenceEquals(municipioSeleccionado, value))
                    return;

                municipioSeleccionado = value;
                OnPropertyChanged();
            }
        }

        public bool MostrarFiltrosAvanzados
        {
            get => mostrarFiltrosAvanzados;
            set
            {
                if (mostrarFiltrosAvanzados == value)
                    return;

                mostrarFiltrosAvanzados = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TextoBotonFiltros));
            }
        }

        public string TextoBotonFiltros =>
            MostrarFiltrosAvanzados ? "Ocultar filtros" : "Filtros";

        public bool FiltrarPorFecha
        {
            get => filtrarPorFecha;
            set
            {
                if (filtrarPorFecha == value)
                    return;

                filtrarPorFecha = value;
                OnPropertyChanged();
            }
        }

        public DateTime FechaDesde
        {
            get => fechaDesde;
            set
            {
                if (fechaDesde == value)
                    return;

                fechaDesde = value;
                OnPropertyChanged();
            }
        }

        public DateTime FechaHasta
        {
            get => fechaHasta;
            set
            {
                if (fechaHasta == value)
                    return;

                fechaHasta = value;
                OnPropertyChanged();
            }
        }

        public new bool IsBusy
        {
            get => base.IsBusy;
            set
            {
                if (base.IsBusy == value)
                    return;

                base.IsBusy = value;
                ActualizarComandos();
                NotificarEstadoLista();
            }
        }

        public bool IsRefreshing
        {
            get => isRefreshing;
            set
            {
                if (isRefreshing == value)
                    return;

                isRefreshing = value;
                OnPropertyChanged();
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

        public bool UbicacionCargando
        {
            get => ubicacionCargando;
            private set
            {
                if (ubicacionCargando == value)
                    return;

                ubicacionCargando = value;
                OnPropertyChanged();
            }
        }

        public bool MostrandoRelay
        {
            get => mostrandoRelay;
            private set
            {
                if (mostrandoRelay == value)
                    return;

                mostrandoRelay = value;
                OnPropertyChanged();
            }
        }

        public string TituloRelay
        {
            get => tituloRelay;
            private set
            {
                string nuevoValor = value ?? string.Empty;

                if (tituloRelay == nuevoValor)
                    return;

                tituloRelay = nuevoValor;
                OnPropertyChanged();
            }
        }

        public string DetalleRelay
        {
            get => detalleRelay;
            private set
            {
                string nuevoValor = value ?? string.Empty;

                if (detalleRelay == nuevoValor)
                    return;

                detalleRelay = nuevoValor;
                OnPropertyChanged();
            }
        }

        public bool TieneMensaje => !string.IsNullOrWhiteSpace(Mensaje);
        public bool TieneTerrenos => List.Count > 0;
        public bool MostrarAccesoDenegado => !CanView;
        public bool TienePaginaCargada => pantallaCargada;

        public bool MostrarVacio =>
            CanView && pantallaCargada && !IsBusy && List.Count == 0 && !TieneMensaje;

        public bool PuedeIrAnterior =>
            pantallaCargada && paginaActual > 1;

        public bool PuedeIrSiguiente =>
            pantallaCargada && paginaActual < totalPaginas;

        public bool MostrarPaginacion =>
            CanView && pantallaCargada && List.Count > 0;

        public int PaginaActual => paginaActual;
        public int TotalPaginas => totalPaginas;

        public string TotalTexto => totalRegistros == 1
            ? "1 terreno"
            : $"{totalRegistros:N0} terrenos";

        public string PaginaTexto =>
            $"Página {Math.Max(1, paginaActual)} de {Math.Max(1, totalPaginas)}";

        public string RangoPaginaTexto
        {
            get
            {
                if (totalRegistros <= 0 || List.Count == 0)
                    return "Sin registros en esta página";

                int tamano = Math.Max(1, tamanoPaginaActual);
                int inicio = ((Math.Max(1, paginaActual) - 1) * tamano) + 1;
                int fin = Math.Min(inicio + List.Count - 1, totalRegistros);

                return $"Mostrando {inicio}-{fin} de {totalRegistros}";
            }
        }

        public void ActualizarPermisos()
        {
            LoadPagePermissions("terrenoPage");
            OnPropertyChanged(nameof(MostrarAccesoDenegado));
            ActualizarComandos();
            NotificarEstadoLista();
        }

        public async Task IniciarNuevaVisitaAsync()
        {
            if (!CanView || Navegando)
                return;

            // AsegurarVisita ya creó el contenedor de la nueva visita.
            // Se reinicia únicamente el estado propio del listado.
            CancelarCarga();
            RestablecerFiltrosLocales();
            RestablecerFiltrosAplicados();
            List.Clear();
            paginaActual = 1;
            totalPaginas = 1;
            totalRegistros = 0;
            tamanoPaginaActual = ObtenerTamanoPagina();
            pantallaCargada = false;
            catalogosCargados = false;
            Paises.Clear();
            Departamentos.Clear();
            Municipios.Clear();

            await CargarCatalogosAsync();
            await CargarPaginaAsync(
                1,
                "Cargando terrenos...",
                "Consultando información actual del servidor");
        }

        public async Task InicializarAsync()
        {
            if (!CanView || Navegando || pantallaCargada)
                return;

            await CargarCatalogosAsync();
            await CargarPaginaAsync(
                1,
                "Cargando terrenos...",
                "Consultando información actual del servidor");
        }

        public Task RecargarPaginaActualAsync()
        {
            if (!CanView || Navegando)
                return Task.CompletedTask;

            return CargarPaginaAsync(
                Math.Max(1, paginaActual),
                "Actualizando terrenos...",
                "Consultando nuevamente la página actual");
        }

        public void CancelarCarga()
        {
            CancellationTokenSource? carga =
                Interlocked.Exchange(ref cargaCts, null);
            CancelarSeguro(carga);

            CancellationTokenSource? ubicacion =
                Interlocked.Exchange(ref ubicacionCts, null);
            CancelarSeguro(ubicacion);

            IsBusy = false;
            IsRefreshing = false;
            UbicacionCargando = false;
            OcultarRelay();
        }

        private async Task CargarCatalogosAsync()
        {
            if (catalogosCargados)
                return;

            if (TerrenoVisitaService.IntentarObtenerPaises(
                    out List<PaisResponse> cache))
            {
                AplicarPaises(cache);
                catalogosCargados = true;
                return;
            }

            ApiResult<ObservableCollection<PaisResponse>> result =
                await paisApiService.GetPaisResultAsync();

            if (!result.Success || result.Data == null)
            {
                Mensaje = result.Message;
                return;
            }

            List<PaisResponse> items = result.Data
                .Where(x => x.PaisId > 0)
                .OrderBy(x => x.NombrePais)
                .ToList();

            AplicarPaises(items);
            TerrenoVisitaService.GuardarPaises(items);
            catalogosCargados = true;
        }

        private void AplicarPaises(IEnumerable<PaisResponse> items)
        {
            Paises.Clear();
            foreach (PaisResponse item in items)
                Paises.Add(item);
            OnPropertyChanged(nameof(Paises));
        }

        private async Task CargarDepartamentosPorPaisAsync(PaisResponse? pais)
        {
            CancellationTokenSource source = PrepararUbicacion();

            try
            {
                UbicacionCargando = true;
                LimpiarDepartamentoYMunicipio();

                if (pais?.PaisId is not > 0)
                    return;

                if (TerrenoVisitaService.IntentarObtenerDepartamentos(
                        pais.PaisId,
                        out List<DepartamentoResponse> cache))
                {
                    if (!source.IsCancellationRequested && EsUbicacionActual(source))
                        AplicarDepartamentos(cache);
                    return;
                }

                ApiResult<ObservableCollection<DepartamentoResponse>> result =
                    await departamentoApiService.GetDepartamentosResultAsync(
                        pais.PaisId,
                        source.Token);

                if (source.IsCancellationRequested || !EsUbicacionActual(source))
                    return;

                if (!result.Success || result.Data == null)
                {
                    Mensaje = result.Message;
                    return;
                }

                List<DepartamentoResponse> items = result.Data
                    .OrderBy(x => x.NombreDepartamento)
                    .ToList();

                AplicarDepartamentos(items);
                TerrenoVisitaService.GuardarDepartamentos(pais.PaisId, items);
            }
            catch (OperationCanceledException)
            {
                // La selección cambió antes de terminar la consulta anterior.
            }
            finally
            {
                if (EsUbicacionActual(source))
                    UbicacionCargando = false;

                LiberarUbicacion(source);
            }
        }

        private async Task CargarMunicipiosPorDepartamentoAsync(
            DepartamentoResponse? departamento)
        {
            CancellationTokenSource source = PrepararUbicacion();

            try
            {
                UbicacionCargando = true;
                LimpiarMunicipio();

                if (departamento?.DepartamentoId is not > 0)
                    return;

                int departamentoId = departamento.DepartamentoId.Value;

                if (TerrenoVisitaService.IntentarObtenerMunicipios(
                        departamentoId,
                        out List<MunicipioResponse> cache))
                {
                    if (!source.IsCancellationRequested && EsUbicacionActual(source))
                        AplicarMunicipios(cache);
                    return;
                }

                ApiResult<ObservableCollection<MunicipioResponse>> result =
                    await municipioApiService.GetMunicipiosResultAsync(
                        departamentoId,
                        source.Token);

                if (source.IsCancellationRequested || !EsUbicacionActual(source))
                    return;

                if (!result.Success || result.Data == null)
                {
                    Mensaje = result.Message;
                    return;
                }

                List<MunicipioResponse> items = result.Data
                    .OrderBy(x => x.NombreMunicipio)
                    .ToList();

                AplicarMunicipios(items);
                TerrenoVisitaService.GuardarMunicipios(
                    departamentoId,
                    items);
            }
            catch (OperationCanceledException)
            {
                // La selección cambió antes de terminar la consulta anterior.
            }
            finally
            {
                if (EsUbicacionActual(source))
                    UbicacionCargando = false;

                LiberarUbicacion(source);
            }
        }

        private void AplicarDepartamentos(IEnumerable<DepartamentoResponse> items)
        {
            Departamentos.Clear();
            foreach (DepartamentoResponse item in items)
                Departamentos.Add(item);
            OnPropertyChanged(nameof(Departamentos));
        }

        private void AplicarMunicipios(IEnumerable<MunicipioResponse> items)
        {
            Municipios.Clear();
            foreach (MunicipioResponse item in items)
                Municipios.Add(item);
            OnPropertyChanged(nameof(Municipios));
        }

        private async Task BuscarAsync()
        {
            if (!ValidarFiltros())
                return;

            AplicarFiltrosActuales();

            await CargarPaginaAsync(
                1,
                "Buscando terrenos...",
                "Consultando los registros que coinciden con los filtros");
        }

        private Task IrPaginaAnteriorAsync()
        {
            if (!PuedeIrAnterior)
                return Task.CompletedTask;

            return CargarPaginaAsync(
                paginaActual - 1,
                "Cargando página anterior...",
                "Consultando la página anterior de terrenos");
        }

        private Task IrPaginaSiguienteAsync()
        {
            if (!PuedeIrSiguiente)
                return Task.CompletedTask;

            return CargarPaginaAsync(
                paginaActual + 1,
                "Cargando página siguiente...",
                "Consultando la siguiente página de terrenos");
        }

        private async Task CargarPaginaAsync(
            int paginaSolicitada,
            string tituloOperacion,
            string detalleOperacion)
        {
            if (!CanView || Navegando)
                return;

            paginaSolicitada = Math.Max(1, paginaSolicitada);
            CancellationTokenSource source = PrepararCarga();

            try
            {
                MostrarRelay(tituloOperacion, detalleOperacion);
                IsBusy = true;
                Mensaje = string.Empty;

                ApiResult<TerrenoBusquedaPaginadaResponse> result =
                    await ConsultarPaginaAsync(
                        paginaSolicitada,
                        source.Token);

                if (source.IsCancellationRequested || !EsCargaActual(source))
                    return;

                if (!result.Success || result.Data == null)
                {
                    if (!EsMensajeCancelacion(result.Message))
                        Mensaje = result.Message;
                    return;
                }

                TerrenoBusquedaPaginadaResponse pagina = result.Data;

                // Si una eliminación o cambio externo redujo el total de páginas,
                // se corrige una sola vez hacia la última página válida.
                int totalPaginasRespuesta = Math.Max(1, pagina.TotalPages);
                if (paginaSolicitada > totalPaginasRespuesta && pagina.Total > 0)
                {
                    result = await ConsultarPaginaAsync(
                        totalPaginasRespuesta,
                        source.Token);

                    if (source.IsCancellationRequested || !EsCargaActual(source))
                        return;

                    if (!result.Success || result.Data == null)
                    {
                        if (!EsMensajeCancelacion(result.Message))
                            Mensaje = result.Message;
                        return;
                    }

                    pagina = result.Data;
                }

                AplicarPagina(pagina);
            }
            catch (OperationCanceledException)
            {
                // Cancelación normal al reemplazar la búsqueda o navegar.
            }
            catch (ObjectDisposedException)
            {
                // La navegación puede cerrar el stream en Android.
            }
            finally
            {
                if (EsCargaActual(source))
                {
                    IsBusy = false;
                    IsRefreshing = false;
                    OcultarRelay();
                }

                LiberarCarga(source);
                ActualizarComandos();
                NotificarEstadoLista();
            }
        }

        private Task<ApiResult<TerrenoBusquedaPaginadaResponse>> ConsultarPaginaAsync(
            int pagina,
            CancellationToken cancellationToken) =>
            busquedaApiService.BuscarAsync(
                textoBusquedaAplicado,
                codigoFiltroAplicado,
                propietarioFiltroAplicado,
                identificacionFiltroAplicado,
                direccionFiltroAplicado,
                paisIdAplicado,
                departamentoIdAplicado,
                municipioIdAplicado,
                fechaDesdeAplicada,
                fechaHastaAplicada,
                extensionMinimaAplicada,
                extensionMaximaAplicada,
                ordenarPorAplicado,
                descendenteAplicado,
                pagina,
                ObtenerTamanoPagina(),
                cancellationToken);

        private void AplicarPagina(TerrenoBusquedaPaginadaResponse pagina)
        {
            List.Clear();

            foreach (TerrenoResponse item in pagina.Data)
                List.Add(item);

            paginaActual = Math.Max(1, pagina.Page);
            totalPaginas = Math.Max(1, pagina.TotalPages);
            totalRegistros = Math.Max(0, pagina.Total);
            tamanoPaginaActual = pagina.PageSize > 0
                ? pagina.PageSize
                : ObtenerTamanoPagina();
            pantallaCargada = true;
            Mensaje = string.Empty;

            NotificarEstadoLista();
        }

        private bool ValidarFiltros()
        {
            Mensaje = string.Empty;

            decimal? extensionMinima = ParseDecimal(ExtensionMinimaTexto);
            decimal? extensionMaxima = ParseDecimal(ExtensionMaximaTexto);

            if (!string.IsNullOrWhiteSpace(ExtensionMinimaTexto) &&
                !extensionMinima.HasValue)
            {
                Mensaje = "La extensión mínima no tiene un formato válido.";
                return false;
            }

            if (!string.IsNullOrWhiteSpace(ExtensionMaximaTexto) &&
                !extensionMaxima.HasValue)
            {
                Mensaje = "La extensión máxima no tiene un formato válido.";
                return false;
            }

            if (extensionMinima is < 0 || extensionMaxima is < 0)
            {
                Mensaje = "Las extensiones utilizadas como filtro no pueden ser negativas.";
                return false;
            }

            if (extensionMinima.HasValue &&
                extensionMaxima.HasValue &&
                extensionMinima.Value > extensionMaxima.Value)
            {
                Mensaje = "La extensión mínima no puede ser mayor que la extensión máxima.";
                return false;
            }

            if (FiltrarPorFecha && FechaDesde.Date > FechaHasta.Date)
            {
                Mensaje = "La fecha inicial no puede ser mayor que la fecha final.";
                return false;
            }

            return true;
        }

        private void AplicarFiltrosActuales()
        {
            textoBusquedaAplicado = TextoBusqueda.Trim();
            codigoFiltroAplicado = CodigoFiltro.Trim();
            propietarioFiltroAplicado = PropietarioFiltro.Trim();
            identificacionFiltroAplicado = IdentificacionFiltro.Trim();
            direccionFiltroAplicado = DireccionFiltro.Trim();
            paisIdAplicado = PaisSeleccionado?.PaisId;
            departamentoIdAplicado = DepartamentoSeleccionado?.DepartamentoId;
            municipioIdAplicado = MunicipioSeleccionado?.MunicipioId;
            fechaDesdeAplicada = FiltrarPorFecha
                ? DateOnly.FromDateTime(FechaDesde)
                : null;
            fechaHastaAplicada = FiltrarPorFecha
                ? DateOnly.FromDateTime(FechaHasta)
                : null;
            extensionMinimaAplicada = ParseDecimal(ExtensionMinimaTexto);
            extensionMaximaAplicada = ParseDecimal(ExtensionMaximaTexto);

            (ordenarPorAplicado, descendenteAplicado) = ResolverOrdenamiento();
        }

        private async Task LimpiarFiltrosAsync()
        {
            RestablecerFiltrosLocales();
            RestablecerFiltrosAplicados();

            await CargarPaginaAsync(
                1,
                "Actualizando terrenos...",
                "Quitando filtros y consultando la primera página");
        }

        private void RestablecerFiltrosLocales()
        {
            textoBusqueda = string.Empty;
            codigoFiltro = string.Empty;
            propietarioFiltro = string.Empty;
            identificacionFiltro = string.Empty;
            direccionFiltro = string.Empty;
            extensionMinimaTexto = string.Empty;
            extensionMaximaTexto = string.Empty;
            ordenarSeleccionado = "Código (A-Z)";
            filtrarPorFecha = false;
            fechaDesde = DateTime.Today.AddYears(-1);
            fechaHasta = DateTime.Today;

            actualizandoUbicacionInterna = true;
            try
            {
                paisSeleccionado = null;
                departamentoSeleccionado = null;
                municipioSeleccionado = null;
                Departamentos.Clear();
                Municipios.Clear();
            }
            finally
            {
                actualizandoUbicacionInterna = false;
            }

            OnPropertyChanged(nameof(TextoBusqueda));
            OnPropertyChanged(nameof(CodigoFiltro));
            OnPropertyChanged(nameof(PropietarioFiltro));
            OnPropertyChanged(nameof(IdentificacionFiltro));
            OnPropertyChanged(nameof(DireccionFiltro));
            OnPropertyChanged(nameof(ExtensionMinimaTexto));
            OnPropertyChanged(nameof(ExtensionMaximaTexto));
            OnPropertyChanged(nameof(OrdenarSeleccionado));
            OnPropertyChanged(nameof(FiltrarPorFecha));
            OnPropertyChanged(nameof(FechaDesde));
            OnPropertyChanged(nameof(FechaHasta));
            OnPropertyChanged(nameof(PaisSeleccionado));
            OnPropertyChanged(nameof(DepartamentoSeleccionado));
            OnPropertyChanged(nameof(MunicipioSeleccionado));
        }

        private void RestablecerFiltrosAplicados()
        {
            textoBusquedaAplicado = string.Empty;
            codigoFiltroAplicado = string.Empty;
            propietarioFiltroAplicado = string.Empty;
            identificacionFiltroAplicado = string.Empty;
            direccionFiltroAplicado = string.Empty;
            paisIdAplicado = null;
            departamentoIdAplicado = null;
            municipioIdAplicado = null;
            fechaDesdeAplicada = null;
            fechaHastaAplicada = null;
            extensionMinimaAplicada = null;
            extensionMaximaAplicada = null;
            ordenarPorAplicado = "codigo";
            descendenteAplicado = false;
        }

        private async Task RefrescarAsync()
        {
            IsRefreshing = true;
            try
            {
                await CargarPaginaAsync(
                    Math.Max(1, paginaActual),
                    "Actualizando terrenos...",
                    "Consultando nuevamente la página actual");
            }
            finally
            {
                IsRefreshing = false;
            }
        }

        private Task OnAddAsync() =>
            NavegarAsync(
                AppRoutes.TerrenoFormulario,
                new Dictionary<string, object>
                {
                    ["Mode"] = FormMode.FormModeSelect.Create,
                    ["Terreno"] = new TerrenoRequest()
                },
                "Abriendo nuevo terreno...",
                "Preparando el formulario de registro");

        private Task OnEditAsync(TerrenoResponse? item)
        {
            if (item?.TerrenoId is null or <= 0)
                return MostrarErrorAsync(
                    "No se recibió un terreno válido para editar.");

            return NavegarAsync(
                AppRoutes.TerrenoFormulario,
                new Dictionary<string, object>
                {
                    ["Mode"] = FormMode.FormModeSelect.Edit,
                    ["Terreno"] = new TerrenoRequest(item)
                },
                "Abriendo terreno...",
                "Preparando la edición del terreno seleccionado");
        }

        private Task OnViewAsync(TerrenoResponse? item)
        {
            if (item?.TerrenoId is null or <= 0)
                return MostrarErrorAsync(
                    "No se recibió un terreno válido para consultar.");

            return NavegarAsync(
                AppRoutes.TerrenoFormulario,
                new Dictionary<string, object>
                {
                    ["Mode"] = FormMode.FormModeSelect.View,
                    ["Terreno"] = new TerrenoRequest(item)
                },
                "Abriendo terreno...",
                "Preparando el detalle del terreno seleccionado");
        }

        private async Task OnDeleteAsync(TerrenoResponse? item)
        {
            if (item?.TerrenoId is null or <= 0)
                return;

            bool confirmar = await ConfirmarAsync(
                "Eliminar terreno",
                $"¿Desea eliminar el terreno {item.CodigoTerreno}? El registro se desactivará y se conservará su historial.",
                "Eliminar",
                "Cancelar");

            if (!confirmar)
                return;

            CancellationTokenSource source = PrepararCarga();
            bool recargarPaginaAnterior = false;
            int paginaDestino = Math.Max(1, paginaActual);

            try
            {
                MostrarRelay(
                    "Eliminando terreno...",
                    "Desactivando el registro en el servidor");
                IsBusy = true;

                ApiResult<bool> resultado =
                    await terrenoApiService.DeleteTerrenoResultAsync(
                        new TerrenoRequest(item),
                        source.Token);

                if (source.IsCancellationRequested || !EsCargaActual(source))
                    return;

                if (!resultado.Success)
                {
                    await MostrarErrorAsync(resultado.Message);
                    return;
                }

                List.Remove(item);
                totalRegistros = Math.Max(0, totalRegistros - 1);
                RecalcularPaginasLocales();

                if (List.Count == 0 && totalRegistros > 0 && paginaActual > 1)
                {
                    paginaDestino = Math.Min(
                        paginaActual - 1,
                        Math.Max(1, totalPaginas));
                    recargarPaginaAnterior = true;
                }

                await MostrarExitoAsync("Terreno eliminado correctamente.");
            }
            finally
            {
                if (EsCargaActual(source))
                {
                    IsBusy = false;
                    OcultarRelay();
                }

                LiberarCarga(source);
                NotificarEstadoLista();
            }

            if (recargarPaginaAnterior)
            {
                await CargarPaginaAsync(
                    paginaDestino,
                    "Actualizando terrenos...",
                    "Ajustando la página después de la eliminación");
            }
        }

        private void RecalcularPaginasLocales()
        {
            int tamano = Math.Max(1, tamanoPaginaActual);
            totalPaginas = totalRegistros == 0
                ? 1
                : (int)Math.Ceiling(totalRegistros / (double)tamano);
            paginaActual = Math.Min(
                Math.Max(1, paginaActual),
                Math.Max(1, totalPaginas));
        }

        private async Task SalirAConfiguracionAsync()
        {
            TerrenoVisitaService.FinalizarVisita();

            await NavegarAsync(
                AppRoutes.Configuracion,
                null,
                "Regresando a configuración...",
                "Cerrando la administración de terrenos");
        }

        private async Task NavegarAsync(
            string ruta,
            IDictionary<string, object>? parametros,
            string tituloOperacion,
            string detalleOperacion)
        {
            if (Navegando)
                return;

            Navegando = true;

            try
            {
                CancelarCarga();
                MostrarRelay(tituloOperacion, detalleOperacion);
                await Task.Yield();
                await GoToAsyncParameters(ruta, parametros);
            }
            finally
            {
                OcultarRelay();
                Navegando = false;
            }
        }

        private (string ordenarPor, bool descendente) ResolverOrdenamiento() =>
            OrdenarSeleccionado switch
            {
                "Código (Z-A)" => ("codigo", true),
                "Propietario (A-Z)" => ("propietario", false),
                "Propietario (Z-A)" => ("propietario", true),
                "Fecha más reciente" => ("fecha", true),
                "Fecha más antigua" => ("fecha", false),
                "Mayor extensión" => ("extension", true),
                "Menor extensión" => ("extension", false),
                _ => ("codigo", false)
            };

        private static decimal? ParseDecimal(string? valor)
        {
            if (string.IsNullOrWhiteSpace(valor))
                return null;

            string normalizado = valor.Trim().Replace(',', '.');

            return decimal.TryParse(
                normalizado,
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out decimal resultado)
                ? resultado
                : null;
        }

        private static int ObtenerTamanoPagina() =>
            DeviceInfo.Current.Platform == DevicePlatform.WinUI ? 40 : 20;

        private void LimpiarDepartamentoYMunicipio()
        {
            actualizandoUbicacionInterna = true;
            try
            {
                departamentoSeleccionado = null;
                municipioSeleccionado = null;
                Departamentos.Clear();
                Municipios.Clear();
                OnPropertyChanged(nameof(DepartamentoSeleccionado));
                OnPropertyChanged(nameof(MunicipioSeleccionado));
            }
            finally
            {
                actualizandoUbicacionInterna = false;
            }
        }

        private void LimpiarMunicipio()
        {
            actualizandoUbicacionInterna = true;
            try
            {
                municipioSeleccionado = null;
                Municipios.Clear();
                OnPropertyChanged(nameof(MunicipioSeleccionado));
            }
            finally
            {
                actualizandoUbicacionInterna = false;
            }
        }

        private void AsignarTexto(
            ref string campo,
            string? valor,
            [CallerMemberName] string? propertyName = null)
        {
            string nuevoValor = valor ?? string.Empty;

            if (campo == nuevoValor)
                return;

            campo = nuevoValor;
            OnPropertyChanged(propertyName);
        }

        private CancellationTokenSource PrepararCarga()
        {
            var source = new CancellationTokenSource();
            CancellationTokenSource? anterior =
                Interlocked.Exchange(ref cargaCts, source);
            CancelarSeguro(anterior);
            return source;
        }

        private bool EsCargaActual(CancellationTokenSource source) =>
            ReferenceEquals(Volatile.Read(ref cargaCts), source);

        private void LiberarCarga(CancellationTokenSource source)
        {
            Interlocked.CompareExchange(ref cargaCts, null, source);
            source.Dispose();
        }

        private CancellationTokenSource PrepararUbicacion()
        {
            var source = new CancellationTokenSource();
            CancellationTokenSource? anterior =
                Interlocked.Exchange(ref ubicacionCts, source);
            CancelarSeguro(anterior);
            return source;
        }

        private bool EsUbicacionActual(CancellationTokenSource source) =>
            ReferenceEquals(Volatile.Read(ref ubicacionCts), source);

        private void LiberarUbicacion(CancellationTokenSource source)
        {
            Interlocked.CompareExchange(ref ubicacionCts, null, source);
            source.Dispose();
        }

        private static void CancelarSeguro(CancellationTokenSource? source)
        {
            if (source == null)
                return;

            try
            {
                source.Cancel();
            }
            catch (ObjectDisposedException)
            {
            }
        }

        private static bool EsMensajeCancelacion(string? mensaje) =>
            !string.IsNullOrWhiteSpace(mensaje) &&
            mensaje.Contains("cancel", StringComparison.OrdinalIgnoreCase);

        private async Task EjecutarSeguroAsync(Func<Task> accion, string operacion)
        {
            try
            {
                await accion();
            }
            catch (OperationCanceledException)
            {
            }
            catch (ObjectDisposedException)
            {
            }
            catch (Exception ex)
            {
                OcultarRelay();
                await MostrarErrorInesperadoAsync(operacion, ex);
            }
        }

        private void MostrarRelay(string titulo, string detalle)
        {
            TituloRelay = titulo;
            DetalleRelay = detalle;
            MostrandoRelay = true;
        }

        private void OcultarRelay()
        {
            MostrandoRelay = false;
        }

        private void NotificarEstadoLista()
        {
            OnPropertyChanged(nameof(TieneTerrenos));
            OnPropertyChanged(nameof(MostrarVacio));
            OnPropertyChanged(nameof(MostrarPaginacion));
            OnPropertyChanged(nameof(PuedeIrAnterior));
            OnPropertyChanged(nameof(PuedeIrSiguiente));
            OnPropertyChanged(nameof(PaginaActual));
            OnPropertyChanged(nameof(TotalPaginas));
            OnPropertyChanged(nameof(PaginaTexto));
            OnPropertyChanged(nameof(RangoPaginaTexto));
            OnPropertyChanged(nameof(TotalTexto));
            ActualizarComandos();
        }

        private void ActualizarComandos()
        {
            RegresarConfiguracionCommand.ChangeCanExecute();
            AddCommand.ChangeCanExecute();
            EditCommand.ChangeCanExecute();
            DeleteCommand.ChangeCanExecute();
            ViewCommand.ChangeCanExecute();
            BuscarCommand.ChangeCanExecute();
            LimpiarFiltrosCommand.ChangeCanExecute();
            AlternarFiltrosCommand.ChangeCanExecute();
            RefrescarCommand.ChangeCanExecute();
            PaginaAnteriorCommand.ChangeCanExecute();
            PaginaSiguienteCommand.ChangeCanExecute();
        }
    }
}
