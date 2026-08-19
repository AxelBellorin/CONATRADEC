using CONATRADEC.Models;
using CONATRADEC.Services;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;

namespace CONATRADEC.ViewModels
{
    /// <summary>
    /// Listado auditado de Solicitudes/Historial. Se mantiene separado del
    /// ViewModel de captura para no alterar el flujo de fotografías e IA.
    /// Conserva una sola página en memoria y separa filtros escritos de filtros
    /// aplicados.
    /// </summary>
    public sealed class DiagnosticoIASolicitudListadoViewModel :
        DiagnosticoIAViewModelBase
    {
        private const int TamanoPaginaPredeterminado = 20;

        private static readonly DateTime FechaMinimaPermitida =
            new(2000, 1, 1);

        private readonly InspeccionFitosanitariaBandejaNumeradaApiService api =
            new();
        private readonly InspeccionFitosanitariaBandejaApiService bandejaContexto =
            InspeccionFitosanitariaBandejaApiService.Instance;
        private readonly TipoFotografiaIAApiService tiposFotografiaApi = new();
        private readonly SemaphoreSlim cargaLock = new(1, 1);

        private CancellationTokenSource? cargaCts;
        private bool inicializado;
        private bool paginaActiva;
        private bool catalogoCargado;
        private bool cargaInicialCompletada;
        private string modoVista = DiagnosticoIARoutes.ModoMisInspecciones;
        private bool filtrosExpandidos;

        // Filtros escritos: cambiar estos valores nunca genera HTTP.
        private string buscarInspeccion = string.Empty;
        private string propietarioFiltro = string.Empty;
        private string departamentoFiltro = string.Empty;
        private bool usarFechaDesde;
        private bool usarFechaHasta;
        private DateTime fechaDesde = FechaDesdePredeterminada;
        private DateTime fechaHasta = DateTime.Today;
        private FiltroCodigoOpcionV2? tipoFotografiaFiltroSeleccionado;
        private FiltroCodigoOpcionV2? estadoFiltroSeleccionado;
        private int? tecnicoEscritoId;

        // Filtros aplicados: búsqueda, actualización y paginación usan solo esto.
        private string buscarAplicado = string.Empty;
        private string propietarioAplicado = string.Empty;
        private string departamentoAplicado = string.Empty;
        private bool usarFechaDesdeAplicada;
        private bool usarFechaHastaAplicada;
        private DateTime fechaDesdeAplicada = FechaDesdePredeterminada;
        private DateTime fechaHastaAplicada = DateTime.Today;
        private string tipoFotografiaAplicado = string.Empty;
        private string estadoAplicado = string.Empty;
        private int? tecnicoAplicadoId;

        private int paginaActual = 1;
        private int totalPaginas;
        private int totalRegistros;

        public DiagnosticoIASolicitudListadoViewModel()
        {
            tipoFotografiaFiltroSeleccionado = TiposFotografiaFiltro[0];
            estadoFiltroSeleccionado = EstadosInspeccionFiltro[0];

            RegresarSolicitudCommand = new Command(
                async () => await RegresarSolicitudAsync(),
                () => !IsBusy);

            BuscarInspeccionesCommand = new Command(
                async () => await BuscarAsync(),
                () => !IsBusy);

            LimpiarFiltrosCommand = new Command(
                async () => await LimpiarFiltrosAsync(),
                () => !IsBusy);

            ActualizarCommand = new Command(
                async () => await ActualizarAsync(),
                () => !IsBusy && cargaInicialCompletada);

            AlternarFiltrosCommand = new Command(
                AlternarFiltros,
                () => !IsBusy);

            PaginaAnteriorCommand = new Command(
                async () => await CambiarPaginaAsync(paginaActual - 1),
                () => !IsBusy && PuedePaginaAnterior);

            PaginaSiguienteCommand = new Command(
                async () => await CambiarPaginaAsync(paginaActual + 1),
                () => !IsBusy && PuedePaginaSiguiente);

            AbrirResultadoCommand =
                new Command<InspeccionFitosanitariaBandejaItemV2>(
                    async item => await AbrirResultadoAsync(item),
                    item => item != null && !IsBusy);

            // Compatibilidad visual con el Footer histórico. El nuevo paginador
            // lo reemplaza desde la Page y este comando nunca carga más datos.
            CargarMasCommand = new Command(() => { }, () => false);
        }

        public event EventHandler? PaginaCargada;

        public ObservableCollection<InspeccionFitosanitariaBandejaItemV2>
            Solicitudes { get; } = [];

        public ObservableCollection<FiltroCodigoOpcionV2>
            TiposFotografiaFiltro { get; } =
        [
            new(string.Empty, "Todos los tipos")
        ];

        public IReadOnlyList<FiltroCodigoOpcionV2>
            EstadosInspeccionFiltro { get; } =
        [
            new(string.Empty, "Todos los estados"),
            new("BORRADOR", "Borrador"),
            new("EN_PROCESO", "En proceso"),
            new("EN_PROCESO_CON_ERRORES", "En proceso con errores"),
            new("PARCIAL", "Avance parcial"),
            new("PENDIENTE_REVISION", "Pendiente de revisión"),
            new("PENDIENTE_APROBACION", "Pendiente de aprobación"),
            new("FINALIZADA", "Finalizada"),
            new("FINALIZADA_PARCIALMENTE", "Finalizada parcialmente")
        ];

        public Command RegresarSolicitudCommand { get; }
        public Command BuscarInspeccionesCommand { get; }
        public Command LimpiarFiltrosCommand { get; }
        public Command ActualizarCommand { get; }
        public Command AlternarFiltrosCommand { get; }
        public Command PaginaAnteriorCommand { get; }
        public Command PaginaSiguienteCommand { get; }
        public Command CargarMasCommand { get; }
        public Command<InspeccionFitosanitariaBandejaItemV2>
            AbrirResultadoCommand { get; }

        public bool EsModoNueva => false;
        public bool EsModoListado => true;
        public bool VisorCerrado => true;
        public bool EsVisorAbierto => false;
        public bool EstaCargandoMas => false;
        public bool MostrarCargarMas => false;
        public bool PuedeCargarMas => false;

        public DateTime FechaMinimaFiltro => FechaMinimaPermitida;
        public DateTime FechaMaximaFiltro => DateTime.Today;

        private static DateTime FechaDesdePredeterminada
        {
            get
            {
                DateTime candidata = DateTime.Today.AddDays(-30);
                return candidata < FechaMinimaPermitida
                    ? FechaMinimaPermitida
                    : candidata;
            }
        }

        public string TituloPagina => modoVista switch
        {
            DiagnosticoIARoutes.ModoDecisionesPendientes =>
                "Decisiones pendientes",
            DiagnosticoIARoutes.ModoHistorial =>
                "Historial de inspecciones",
            _ => "Mis inspecciones"
        };

        public string BuscarInspeccion
        {
            get => buscarInspeccion;
            set => CambiarTexto(ref buscarInspeccion, value);
        }

        public string PropietarioFiltro
        {
            get => propietarioFiltro;
            set => CambiarTexto(ref propietarioFiltro, value);
        }

        public string DepartamentoFiltro
        {
            get => departamentoFiltro;
            set => CambiarTexto(ref departamentoFiltro, value);
        }

        public bool UsarFechaDesde
        {
            get => usarFechaDesde;
            set
            {
                if (usarFechaDesde == value)
                    return;

                usarFechaDesde = value;
                OnPropertyChanged();
                NotificarFiltrosEscritos();
            }
        }

        public bool UsarFechaHasta
        {
            get => usarFechaHasta;
            set
            {
                if (usarFechaHasta == value)
                    return;

                usarFechaHasta = value;
                OnPropertyChanged();
                NotificarFiltrosEscritos();
            }
        }

        public DateTime FechaDesde
        {
            get => fechaDesde;
            set
            {
                DateTime nueva = LimitarFecha(value);
                if (fechaDesde == nueva)
                    return;

                fechaDesde = nueva;
                OnPropertyChanged();
            }
        }

        public DateTime FechaHasta
        {
            get => fechaHasta;
            set
            {
                DateTime nueva = LimitarFecha(value);
                if (fechaHasta == nueva)
                    return;

                fechaHasta = nueva;
                OnPropertyChanged();
            }
        }

        public FiltroCodigoOpcionV2 TipoFotografiaFiltroSeleccionado
        {
            get => tipoFotografiaFiltroSeleccionado ?? TiposFotografiaFiltro[0];
            set
            {
                FiltroCodigoOpcionV2 nuevo = value ?? TiposFotografiaFiltro[0];
                if (ReferenceEquals(tipoFotografiaFiltroSeleccionado, nuevo))
                    return;

                tipoFotografiaFiltroSeleccionado = nuevo;
                OnPropertyChanged();
                NotificarFiltrosEscritos();
            }
        }

        public FiltroCodigoOpcionV2 EstadoFiltroSeleccionado
        {
            get => estadoFiltroSeleccionado ?? EstadosInspeccionFiltro[0];
            set
            {
                FiltroCodigoOpcionV2 nuevo = value ?? EstadosInspeccionFiltro[0];
                if (ReferenceEquals(estadoFiltroSeleccionado, nuevo))
                    return;

                estadoFiltroSeleccionado = nuevo;
                OnPropertyChanged();
                NotificarFiltrosEscritos();
            }
        }

        public bool FiltrosExpandidos
        {
            get => filtrosExpandidos;
            private set
            {
                if (filtrosExpandidos == value)
                    return;

                filtrosExpandidos = value;
                OnPropertyChanged();
            }
        }

        public bool TieneSolicitudes => Solicitudes.Count > 0;

        public bool SinSolicitudes =>
            cargaInicialCompletada && !IsBusy && Solicitudes.Count == 0;

        public int PaginaActual => paginaActual;
        public int TotalPaginas => totalPaginas;
        public int TotalRegistros => totalRegistros;
        public bool PuedePaginaAnterior => paginaActual > 1 && totalPaginas > 0;
        public bool PuedePaginaSiguiente =>
            totalPaginas > 0 && paginaActual < totalPaginas;

        public string TextoPaginacion => totalPaginas <= 0
            ? "Página 0 de 0"
            : $"Página {paginaActual:N0} de {totalPaginas:N0}";

        public string TextoResultadoListado
        {
            get
            {
                if (!cargaInicialCompletada && IsBusy)
                    return "Cargando inspecciones...";

                return totalRegistros == 1
                    ? "1 inspección encontrada"
                    : $"{totalRegistros:N0} inspecciones encontradas";
            }
        }

        public int CantidadFiltrosActivos =>
            ContarFiltrosEscritos();

        public bool TieneFiltrosActivos => CantidadFiltrosActivos > 0;

        public string TextoBotonFiltros => CantidadFiltrosActivos > 0
            ? $"Filtros ({CantidadFiltrosActivos})"
            : "Filtros";

        public string ResumenFiltrosActivos
        {
            get
            {
                var partes = new List<string>();

                if (!string.IsNullOrWhiteSpace(buscarAplicado))
                    partes.Add($"texto: {buscarAplicado}");
                if (!string.IsNullOrWhiteSpace(propietarioAplicado))
                    partes.Add($"propietario: {propietarioAplicado}");
                if (!string.IsNullOrWhiteSpace(departamentoAplicado))
                    partes.Add($"departamento: {departamentoAplicado}");
                if (!string.IsNullOrWhiteSpace(tipoFotografiaAplicado))
                    partes.Add("tipo de fotografía");
                if (!string.IsNullOrWhiteSpace(estadoAplicado))
                    partes.Add("estado");
                if (tecnicoAplicadoId is > 0)
                    partes.Add("técnico");
                if (usarFechaDesdeAplicada)
                    partes.Add($"desde {fechaDesdeAplicada:dd/MM/yyyy}");
                if (usarFechaHastaAplicada)
                    partes.Add($"hasta {fechaHastaAplicada:dd/MM/yyyy}");

                return partes.Count == 0
                    ? "Sin filtros aplicados"
                    : "Aplicados: " + string.Join(" · ", partes);
            }
        }

        public void AplicarModo(string? modo)
        {
            string normalizado = DiagnosticoIARoutes.NormalizarModo(modo);
            if (modoVista == normalizado && inicializado)
                return;

            CancelarOperaciones();
            modoVista = normalizado;
            inicializado = false;
            cargaInicialCompletada = false;
            paginaActual = 1;
            totalPaginas = 0;
            totalRegistros = 0;
            Solicitudes.Clear();

            // El catálogo se conserva únicamente dentro de esta instancia de
            // página. Una nueva Page crea un nuevo ViewModel y fuerza lectura.
            OnPropertyChanged(nameof(TituloPagina));
            NotificarListado();
            ActualizarComandos();
        }

        public void EstablecerTecnicoEscrito(int? tecnicoId)
        {
            int? nuevo = tecnicoId is > 0 ? tecnicoId : null;
            if (tecnicoEscritoId == nuevo)
                return;

            tecnicoEscritoId = nuevo;
            NotificarFiltrosEscritos();
        }

        public async Task InicializarAsync()
        {
            if (!paginaActiva)
                return;

            if (inicializado)
            {
                if (DiagnosticoIASolicitudVisitaService.ConsumirMutacion())
                    await CargarPaginaAsync(paginaActual, notificarScroll: false);
                return;
            }

            inicializado = true;
            // La primera carga de una nueva visita ya es fresca; consume una
            // invalidación anterior para que no provoque un segundo GET al
            // primer regreso desde un subflujo de solo lectura.
            DiagnosticoIASolicitudVisitaService.ConsumirMutacion();

            if (!ModoSesionService.EsOffline && !catalogoCargado)
            {
                cargaCts?.Cancel();
                cargaCts?.Dispose();
                cargaCts = new CancellationTokenSource();
                CancellationToken catalogoToken = cargaCts.Token;

                await CargarTiposFotografiaAsync(catalogoToken);
                if (catalogoToken.IsCancellationRequested || !paginaActiva)
                    return;
            }

            AplicarFiltrosEscritos();
            await CargarPaginaAsync(1, notificarScroll: false);
        }

        public void ActivarPagina() => paginaActiva = true;

        public void CancelarOperaciones()
        {
            paginaActiva = false;
            cargaCts?.Cancel();
        }

        private async Task BuscarAsync()
        {
            if (!await ValidarFechasAsync())
                return;

            AplicarFiltrosEscritos();
            FiltrosExpandidos = false;
            await CargarPaginaAsync(1, notificarScroll: true);
        }

        private async Task ActualizarAsync()
        {
            if (!cargaInicialCompletada)
                return;

            await CargarPaginaAsync(paginaActual, notificarScroll: false);
        }

        private async Task LimpiarFiltrosAsync()
        {
            BuscarInspeccion = string.Empty;
            PropietarioFiltro = string.Empty;
            DepartamentoFiltro = string.Empty;
            UsarFechaDesde = false;
            UsarFechaHasta = false;
            FechaDesde = FechaDesdePredeterminada;
            FechaHasta = DateTime.Today;
            TipoFotografiaFiltroSeleccionado = TiposFotografiaFiltro[0];
            EstadoFiltroSeleccionado = EstadosInspeccionFiltro[0];
            tecnicoEscritoId = null;
            bandejaContexto.EstablecerTecnicoContextual(modoVista, null);
            FiltrosExpandidos = false;

            AplicarFiltrosEscritos();
            await CargarPaginaAsync(1, notificarScroll: true);
        }

        private async Task CambiarPaginaAsync(int pagina)
        {
            if (pagina < 1 || pagina > totalPaginas || pagina == paginaActual)
                return;

            await CargarPaginaAsync(pagina, notificarScroll: true);
        }

        private async Task CargarPaginaAsync(
            int pagina,
            bool notificarScroll)
        {
            if (!paginaActiva || !ValidarEnLinea(false))
                return;

            if (!await cargaLock.WaitAsync(0))
                return;

            cargaCts?.Cancel();
            cargaCts?.Dispose();
            cargaCts = new CancellationTokenSource();
            CancellationToken cancellationToken = cargaCts.Token;

            IsBusy = true;
            MensajeEstado = "Buscando inspecciones...";
            ActualizarComandos();
            NotificarListado();

            try
            {
                var filtro = new InspeccionFitosanitariaBandejaFiltroV2
                {
                    Modo = modoVista,
                    Buscar = buscarAplicado,
                    Propietario = propietarioAplicado,
                    TecnicoId = tecnicoAplicadoId,
                    Departamento = departamentoAplicado,
                    TipoFotografia = tipoFotografiaAplicado,
                    Estado = estadoAplicado,
                    FechaDesde = usarFechaDesdeAplicada
                        ? fechaDesdeAplicada.Date
                        : null,
                    FechaHasta = usarFechaHastaAplicada
                        ? fechaHastaAplicada.Date
                        : null,
                    DesfaseHorarioMinutos =
                        (int)DateTimeOffset.Now.Offset.TotalMinutes,
                    TamanoPagina = TamanoPaginaPredeterminado
                };

                InspeccionFitosanitariaBandejaPaginaNumeradaV2 respuesta =
                    await api.ObtenerAsync(
                        filtro,
                        pagina,
                        cancellationToken);

                Solicitudes.Clear();
                foreach (InspeccionFitosanitariaBandejaItemV2 item
                         in respuesta.Items ?? [])
                {
                    Solicitudes.Add(item);
                }

                paginaActual = respuesta.TotalPaginas <= 0
                    ? 1
                    : Math.Clamp(respuesta.Pagina, 1, respuesta.TotalPaginas);
                totalPaginas = Math.Max(0, respuesta.TotalPaginas);
                totalRegistros = Math.Max(0, respuesta.Total);
                cargaInicialCompletada = true;
                MensajeEstado = string.Empty;

                NotificarListado();
                if (notificarScroll && paginaActiva)
                    PaginaCargada?.Invoke(this, EventArgs.Empty);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                MensajeEstado = string.Empty;
                if (paginaActiva)
                    await MostrarErrorAsync(ex);
            }
            finally
            {
                IsBusy = false;
                MensajeEstado = string.Empty;
                NotificarListado();
                ActualizarComandos();
                cargaLock.Release();
            }
        }

        private async Task CargarTiposFotografiaAsync(
            CancellationToken cancellationToken)
        {
            try
            {
                ApiResult<List<TipoFotografiaIAItem>> resultado =
                    await tiposFotografiaApi.ListarActivosAsync(
                        forzar: true,
                        cancellationToken: cancellationToken);

                if (!resultado.Success || resultado.Data == null)
                    return;

                catalogoCargado = true;

                string codigoSeleccionado =
                    TipoFotografiaFiltroSeleccionado.Codigo;

                while (TiposFotografiaFiltro.Count > 1)
                    TiposFotografiaFiltro.RemoveAt(TiposFotografiaFiltro.Count - 1);

                foreach (TipoFotografiaIAItem item in resultado.Data
                             .Where(item => item.Activo)
                             .OrderBy(item => item.Orden)
                             .ThenBy(item => item.NombreMostrar))
                {
                    TiposFotografiaFiltro.Add(
                        new FiltroCodigoOpcionV2(
                            item.Codigo,
                            item.NombreMostrar));
                }

                tipoFotografiaFiltroSeleccionado =
                    TiposFotografiaFiltro.FirstOrDefault(item =>
                        string.Equals(
                            item.Codigo,
                            codigoSeleccionado,
                            StringComparison.OrdinalIgnoreCase)) ??
                    TiposFotografiaFiltro[0];

                OnPropertyChanged(nameof(TipoFotografiaFiltroSeleccionado));
            }
            catch (OperationCanceledException)
            {
            }
        }

        private void AplicarFiltrosEscritos()
        {
            buscarAplicado = BuscarInspeccion.Trim();
            propietarioAplicado = PropietarioFiltro.Trim();
            departamentoAplicado = DepartamentoFiltro.Trim();
            usarFechaDesdeAplicada = UsarFechaDesde;
            usarFechaHastaAplicada = UsarFechaHasta;
            fechaDesdeAplicada = FechaDesde.Date;
            fechaHastaAplicada = FechaHasta.Date;
            tipoFotografiaAplicado =
                TipoFotografiaFiltroSeleccionado.Codigo?.Trim() ?? string.Empty;
            estadoAplicado =
                EstadoFiltroSeleccionado.Codigo?.Trim() ?? string.Empty;
            tecnicoAplicadoId = tecnicoEscritoId is > 0
                ? tecnicoEscritoId
                : null;

            OnPropertyChanged(nameof(ResumenFiltrosActivos));
        }

        private async Task<bool> ValidarFechasAsync()
        {
            DateTime hoy = DateTime.Today;

            if (UsarFechaDesde && FechaDesde > hoy)
            {
                await MostrarAlertaAsync(
                    "Fecha inicial no válida",
                    "La fecha inicial no puede estar en el futuro.");
                return false;
            }

            if (UsarFechaHasta && FechaHasta > hoy)
            {
                await MostrarAlertaAsync(
                    "Fecha final no válida",
                    "La fecha final no puede estar en el futuro.");
                return false;
            }

            if (UsarFechaDesde && UsarFechaHasta && FechaDesde > FechaHasta)
            {
                await MostrarAlertaAsync(
                    "Rango de fechas no válido",
                    "La fecha inicial debe ser anterior o igual a la fecha final.");
                return false;
            }

            return true;
        }

        private void AlternarFiltros()
        {
            if (!IsBusy)
                FiltrosExpandidos = !FiltrosExpandidos;
        }

        private async Task AbrirResultadoAsync(
            InspeccionFitosanitariaBandejaItemV2? item)
        {
            if (item == null || IsBusy)
                return;

            await GoToAsyncParameters(
                DiagnosticoIARoutes.CrearRutaResultado(
                    item.InspeccionId,
                    modoVista));
        }

        private async Task RegresarSolicitudAsync()
        {
            if (Shell.Current == null)
                return;

            string rutaAnterior =
                Shell.Current.CurrentState?.Location?.OriginalString ??
                string.Empty;

            try
            {
                await GoToAsyncParameters(AppRoutes.Regresar);
                await Task.Delay(100);
            }
            catch (InvalidOperationException)
            {
            }

            string rutaActual =
                Shell.Current.CurrentState?.Location?.OriginalString ??
                string.Empty;

            if (string.Equals(
                    rutaAnterior,
                    rutaActual,
                    StringComparison.OrdinalIgnoreCase))
            {
                await GoToAsyncParameters(DiagnosticoIARoutes.RutaModulo);
            }
        }

        private int ContarFiltrosEscritos()
        {
            int cantidad = 0;
            if (!string.IsNullOrWhiteSpace(BuscarInspeccion)) cantidad++;
            if (!string.IsNullOrWhiteSpace(PropietarioFiltro)) cantidad++;
            if (!string.IsNullOrWhiteSpace(DepartamentoFiltro)) cantidad++;
            if (!string.IsNullOrWhiteSpace(TipoFotografiaFiltroSeleccionado.Codigo)) cantidad++;
            if (!string.IsNullOrWhiteSpace(EstadoFiltroSeleccionado.Codigo)) cantidad++;
            if (tecnicoEscritoId is > 0) cantidad++;
            if (UsarFechaDesde) cantidad++;
            if (UsarFechaHasta) cantidad++;
            return cantidad;
        }

        private void CambiarTexto(
            ref string campo,
            string? valor,
            [CallerMemberName] string? nombrePropiedad = null)
        {
            string nuevo = valor ?? string.Empty;
            if (campo == nuevo)
                return;

            campo = nuevo;
            OnPropertyChanged(nombrePropiedad);
            NotificarFiltrosEscritos();
        }

        private static DateTime LimitarFecha(DateTime valor)
        {
            DateTime fecha = valor.Date;
            if (fecha < FechaMinimaPermitida)
                return FechaMinimaPermitida;
            if (fecha > DateTime.Today)
                return DateTime.Today;
            return fecha;
        }

        private void NotificarFiltrosEscritos()
        {
            OnPropertyChanged(nameof(CantidadFiltrosActivos));
            OnPropertyChanged(nameof(TieneFiltrosActivos));
            OnPropertyChanged(nameof(TextoBotonFiltros));
        }

        private void NotificarListado()
        {
            OnPropertyChanged(nameof(TieneSolicitudes));
            OnPropertyChanged(nameof(SinSolicitudes));
            OnPropertyChanged(nameof(PaginaActual));
            OnPropertyChanged(nameof(TotalPaginas));
            OnPropertyChanged(nameof(TotalRegistros));
            OnPropertyChanged(nameof(PuedePaginaAnterior));
            OnPropertyChanged(nameof(PuedePaginaSiguiente));
            OnPropertyChanged(nameof(TextoPaginacion));
            OnPropertyChanged(nameof(TextoResultadoListado));
            OnPropertyChanged(nameof(ResumenFiltrosActivos));
        }

        private void ActualizarComandos()
        {
            RegresarSolicitudCommand.ChangeCanExecute();
            BuscarInspeccionesCommand.ChangeCanExecute();
            LimpiarFiltrosCommand.ChangeCanExecute();
            ActualizarCommand.ChangeCanExecute();
            AlternarFiltrosCommand.ChangeCanExecute();
            PaginaAnteriorCommand.ChangeCanExecute();
            PaginaSiguienteCommand.ChangeCanExecute();
            AbrirResultadoCommand.ChangeCanExecute();
            CargarMasCommand.ChangeCanExecute();
        }
    }
}
