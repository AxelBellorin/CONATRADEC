using CONATRADEC.Models;
using CONATRADEC.Services;
using System.Collections.ObjectModel;

namespace CONATRADEC.ViewModels
{
    /// <summary>
    /// Bandeja operativa del analizador con paginación numérica y filtros
    /// explícitos. Los valores escritos nunca consultan el servidor hasta que
    /// el usuario pulsa Buscar/Aplicar filtro.
    ///
    /// La visita conserva vista, filtros aplicados y página mientras el usuario
    /// entra a un expediente y regresa. Solo una mutación real del subflujo
    /// invalida la página visible; una consulta de solo lectura no la recarga.
    /// </summary>
    public sealed class DiagnosticoIAAnalizadorViewModel : DiagnosticoIAViewModelBase
    {
        private const int TamanoPagina = 20;
        private const string VistaMis = "mis";
        private const string VistaDisponibles = "disponibles";
        private const string VistaRevisados = "revisados";
        private const string ModoAnalizadorDisponibles = "analizador-disponibles";
        private static readonly DateTime FechaMinimaPermitida = new(2000, 1, 1);

        private readonly InspeccionFitosanitariaBandejaApiService filtrosApi =
            InspeccionFitosanitariaBandejaApiService.Instance;
        private readonly InspeccionFitosanitariaBandejaOperativaNumeradaApiService api =
            new();
        private readonly InspeccionRevisionBloqueoApiService asignacionApi = new();
        private readonly TipoFotografiaIAApiService tiposFotografiaApi = new();

        private CancellationTokenSource? cargaCts;
        private bool paginaActiva;
        private bool inicializado;
        private bool catalogoTecnicosCargado;
        private bool catalogoTiposCargado;
        private bool cambiandoVista;
        private bool filtrosExpandidos;
        private string vistaActual = VistaMis;

        // Filtros escritos: cambiar estos valores no ejecuta HTTP.
        private string buscarInspeccion = string.Empty;
        private string propietarioFiltro = string.Empty;
        private string departamentoFiltro = string.Empty;
        private TecnicoInspeccionFiltroItem? tecnicoSeleccionado;
        private FiltroCodigoOpcionV2? tipoFotografiaFiltroSeleccionado;
        private FiltroCodigoOpcionV2? estadoFiltroSeleccionado;
        private bool usarFechaDesde;
        private bool usarFechaHasta;
        private DateTime fechaDesde = DateTime.Today.AddDays(-30);
        private DateTime fechaHasta = DateTime.Today;

        // Filtros aplicados: estos son los únicos utilizados por la consulta.
        private string buscarAplicado = string.Empty;
        private string propietarioAplicado = string.Empty;
        private string departamentoAplicado = string.Empty;
        private int? tecnicoAplicadoId;
        private string tipoFotografiaAplicado = string.Empty;
        private string estadoAplicado = string.Empty;
        private DateTime? fechaDesdeAplicada;
        private DateTime? fechaHastaAplicada;

        private int paginaActual = 1;
        private int totalPaginas;
        private int totalRegistros;

        public DiagnosticoIAAnalizadorViewModel()
        {
            tipoFotografiaFiltroSeleccionado = TiposFotografiaFiltro[0];
            estadoFiltroSeleccionado = EstadosFiltro[0];

            ActualizarCommand = new Command(
                async () => await ActualizarAsync(),
                () => PuedeEjecutarAccion);

            BuscarCommand = new Command(
                async () => await BuscarAsync(),
                () => PuedeEjecutarAccion);

            LimpiarFiltrosCommand = new Command(
                async () => await LimpiarFiltrosAsync(),
                () => PuedeEjecutarAccion);

            AlternarFiltrosCommand = new Command(
                () => FiltrosExpandidos = !FiltrosExpandidos,
                () => PuedeEjecutarAccion);

            // Se conserva para compatibilidad con el selector histórico de
            // técnico. Ahora aplica todos los filtros escritos en una sola GET.
            AplicarFiltroTecnicoCommand = new Command(
                async () => await BuscarAsync(),
                () => PuedeEjecutarAccion && FiltrosPendientesDeAplicar);

            PaginaAnteriorCommand = new Command(
                async () => await CambiarPaginaAsync(paginaActual - 1),
                () => PuedeEjecutarAccion && PuedePaginaAnterior);

            PaginaSiguienteCommand = new Command(
                async () => await CambiarPaginaAsync(paginaActual + 1),
                () => PuedeEjecutarAccion && PuedePaginaSiguiente);

            AbrirCommand = new Command<InspeccionFitosanitariaBandejaItemV2>(
                async item => await AbrirAsync(item),
                item => item != null &&
                    !MostrandoDisponibles &&
                    PuedeEjecutarAccion);

            TomarCommand = new Command<InspeccionFitosanitariaBandejaItemV2>(
                async item => await TomarAsync(item),
                item => item != null &&
                    MostrandoDisponibles &&
                    PuedeTomarExpediente &&
                    PuedeEjecutarAccion);

            VerMisCommand = new Command(
                async () => await CambiarVistaAsync(VistaMis),
                () => !MostrandoMis && PuedeEjecutarAccion);

            VerDisponiblesCommand = new Command(
                async () => await CambiarVistaAsync(VistaDisponibles),
                () => !MostrandoDisponibles && PuedeEjecutarAccion);

            VerRevisadasCommand = new Command(
                async () => await CambiarVistaAsync(VistaRevisados),
                () => !MostrandoRevisadas && PuedeEjecutarAccion);
        }

        public event EventHandler? PaginaCargada;

        public ObservableCollection<InspeccionFitosanitariaBandejaItemV2>
            Solicitudes { get; } = [];

        public ObservableCollection<TecnicoInspeccionFiltroItem>
            TecnicosFiltro { get; } = [];

        public ObservableCollection<FiltroCodigoOpcionV2>
            TiposFotografiaFiltro { get; } =
        [
            new(string.Empty, "Todos los tipos")
        ];

        public IReadOnlyList<FiltroCodigoOpcionV2> EstadosFiltro { get; } =
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

        public Command ActualizarCommand { get; }
        public Command BuscarCommand { get; }
        public Command LimpiarFiltrosCommand { get; }
        public Command AlternarFiltrosCommand { get; }
        public Command AplicarFiltroTecnicoCommand { get; }
        public Command PaginaAnteriorCommand { get; }
        public Command PaginaSiguienteCommand { get; }
        public Command<InspeccionFitosanitariaBandejaItemV2> AbrirCommand { get; }
        public Command<InspeccionFitosanitariaBandejaItemV2> TomarCommand { get; }
        public Command VerMisCommand { get; }
        public Command VerDisponiblesCommand { get; }
        public Command VerRevisadasCommand { get; }

        public string BuscarInspeccion
        {
            get => buscarInspeccion;
            set
            {
                string nuevo = value ?? string.Empty;
                if (buscarInspeccion == nuevo)
                    return;

                buscarInspeccion = nuevo;
                OnPropertyChanged();
                NotificarFiltros();
            }
        }

        public string PropietarioFiltro
        {
            get => propietarioFiltro;
            set
            {
                string nuevo = value ?? string.Empty;
                if (propietarioFiltro == nuevo)
                    return;

                propietarioFiltro = nuevo;
                OnPropertyChanged();
                NotificarFiltros();
            }
        }

        public string DepartamentoFiltro
        {
            get => departamentoFiltro;
            set
            {
                string nuevo = value ?? string.Empty;
                if (departamentoFiltro == nuevo)
                    return;

                departamentoFiltro = nuevo;
                OnPropertyChanged();
                NotificarFiltros();
            }
        }

        public TecnicoInspeccionFiltroItem? TecnicoSeleccionado
        {
            get => tecnicoSeleccionado;
            set
            {
                if (ReferenceEquals(tecnicoSeleccionado, value))
                    return;

                tecnicoSeleccionado = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TecnicoFiltroTexto));
                NotificarFiltros();
            }
        }

        public string TecnicoFiltroTexto =>
            TecnicoSeleccionado?.TextoMostrar ?? "Todos los técnicos";

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
                NotificarFiltros();
            }
        }

        public FiltroCodigoOpcionV2 EstadoFiltroSeleccionado
        {
            get => estadoFiltroSeleccionado ?? EstadosFiltro[0];
            set
            {
                FiltroCodigoOpcionV2 nuevo = value ?? EstadosFiltro[0];
                if (ReferenceEquals(estadoFiltroSeleccionado, nuevo))
                    return;

                estadoFiltroSeleccionado = nuevo;
                OnPropertyChanged();
                NotificarFiltros();
            }
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
                NotificarFiltros();
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
                NotificarFiltros();
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
                NotificarFiltros();
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
                NotificarFiltros();
            }
        }

        public DateTime FechaMinimaFiltro => FechaMinimaPermitida;
        public DateTime FechaMaximaFiltro => DateTime.Today;

        public bool FiltrosExpandidos
        {
            get => filtrosExpandidos;
            private set
            {
                if (filtrosExpandidos == value)
                    return;

                filtrosExpandidos = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TextoBotonFiltros));
            }
        }

        public int CantidadFiltrosActivos
        {
            get
            {
                int cantidad = 0;
                if (!string.IsNullOrWhiteSpace(buscarAplicado)) cantidad++;
                if (tecnicoAplicadoId is > 0) cantidad++;
                if (!string.IsNullOrWhiteSpace(propietarioAplicado)) cantidad++;
                if (!string.IsNullOrWhiteSpace(departamentoAplicado)) cantidad++;
                if (!string.IsNullOrWhiteSpace(tipoFotografiaAplicado)) cantidad++;
                if (!string.IsNullOrWhiteSpace(estadoAplicado)) cantidad++;
                if (fechaDesdeAplicada.HasValue) cantidad++;
                if (fechaHastaAplicada.HasValue) cantidad++;
                return cantidad;
            }
        }

        public bool FiltrosPendientesDeAplicar =>
            !FiltrosEscritosCoincidenConAplicados();

        public bool FiltroTecnicoPendiente =>
            ObtenerTecnicoId(TecnicoSeleccionado) != tecnicoAplicadoId;

        public string TextoBotonFiltros => FiltrosExpandidos
            ? "Ocultar filtros ▲"
            : CantidadFiltrosActivos == 0
                ? "Buscar y filtrar ▼"
                : $"Buscar y filtrar ({CantidadFiltrosActivos}) ▼";

        public string ResumenFiltrosActivos
        {
            get
            {
                string aplicados = CantidadFiltrosActivos switch
                {
                    0 => "Sin filtros aplicados",
                    1 => "1 filtro aplicado",
                    _ => $"{CantidadFiltrosActivos} filtros aplicados"
                };

                return FiltrosPendientesDeAplicar
                    ? aplicados + " · cambios pendientes de aplicar"
                    : aplicados;
            }
        }

        public string TextoEstadoFiltroTecnico
        {
            get
            {
                if (FiltrosPendientesDeAplicar)
                    return "Hay cambios pendientes. Pulse Buscar para consultar.";

                if (tecnicoAplicadoId is not > 0)
                    return "Filtro aplicado: todos los técnicos.";

                TecnicoInspeccionFiltroItem? aplicado = TecnicosFiltro
                    .FirstOrDefault(item =>
                        item.UsuarioTecnicoId == tecnicoAplicadoId.Value);

                return aplicado == null
                    ? "Filtro de técnico aplicado."
                    : $"Filtro aplicado: {aplicado.TextoMostrar}.";
            }
        }

        public bool MostrandoMis =>
            string.Equals(vistaActual, VistaMis, StringComparison.Ordinal);

        public bool MostrandoDisponibles =>
            string.Equals(vistaActual, VistaDisponibles, StringComparison.Ordinal);

        public bool MostrandoRevisadas =>
            string.Equals(vistaActual, VistaRevisados, StringComparison.Ordinal);

        public bool PuedeTomarExpediente =>
            PermissionService.Instance.HasUpdate(
                DiagnosticoIARoutes.InterfazAnalizador);

        public string SubtituloVista => vistaActual switch
        {
            VistaDisponibles =>
                "Expedientes sin analizador responsable. Puede tomar uno para asumir su revisión.",
            VistaRevisados =>
                "Consulte las inspecciones cuya revisión humana ya terminó para este analizador.",
            _ =>
                "Expedientes asignados a usted. Las nuevas fotografías enviadas por el técnico permanecen bajo su responsabilidad."
        };

        public string TextoSinSolicitudes => vistaActual switch
        {
            VistaDisponibles =>
                "No hay expedientes disponibles para tomar con los filtros aplicados.",
            VistaRevisados =>
                "No hay inspecciones revisadas con los filtros aplicados.",
            _ =>
                "No tiene expedientes de análisis asignados con los filtros aplicados."
        };

        public string TextoAbrir => MostrandoRevisadas
            ? "Consultar revisión"
            : "Continuar revisión";

        public bool SinSolicitudes => !IsBusy && Solicitudes.Count == 0;
        public bool TieneResultados => Solicitudes.Count > 0;

        public int PaginaActual => paginaActual;
        public int TotalPaginas => totalPaginas;
        public int TotalRegistros => totalRegistros;
        public bool PuedePaginaAnterior => paginaActual > 1;
        public bool PuedePaginaSiguiente =>
            totalPaginas > 0 && paginaActual < totalPaginas;

        public string TextoPagina => totalPaginas <= 0
            ? "Página 1 de 1"
            : $"Página {paginaActual:N0} de {totalPaginas:N0}";

        public string ResumenResultados => totalRegistros == 1
            ? "1 expediente"
            : $"{totalRegistros:N0} expedientes";

        private bool PuedeEjecutarAccion =>
            paginaActiva && !IsBusy && !cambiandoVista;

        private string ModoApiActual => vistaActual switch
        {
            VistaDisponibles => ModoAnalizadorDisponibles,
            VistaRevisados => DiagnosticoIARoutes.ModoAnalizadorRevisadas,
            _ => DiagnosticoIARoutes.ModoAnalizador
        };

        public void ActivarPagina()
        {
            paginaActiva = true;

            if (cargaCts == null || cargaCts.IsCancellationRequested)
            {
                cargaCts?.Dispose();
                cargaCts = new CancellationTokenSource();
            }

            ActualizarComandos();
        }

        public void CancelarOperaciones()
        {
            paginaActiva = false;
            cargaCts?.Cancel();
            cargaCts?.Dispose();
            cargaCts = null;
            ActualizarComandos();
        }

        public async Task InicializarOReanudarAsync()
        {
            if (!paginaActiva || !ValidarEnLinea())
                return;

            if (!inicializado)
            {
                DiagnosticoIAAnalizadorVisitaService.Limpiar();
                cambiandoVista = true;
                ActualizarComandos();

                try
                {
                    await CargarTiposFotografiaAsync(forzar: true);
                    await CargarTecnicosAsync(forzar: true);
                    bool cargaInicialExitosa = await CargarPaginaAsync(
                        1,
                        permitirDuranteCambioVista: true);
                    inicializado = cargaInicialExitosa;
                }
                finally
                {
                    cambiandoVista = false;
                    ActualizarComandos();
                }

                return;
            }

            if (!DiagnosticoIAAnalizadorVisitaService.ConsumirMutacion())
                return;

            bool necesitaCatalogos =
                !catalogoTecnicosCargado || !catalogoTiposCargado;

            if (necesitaCatalogos)
            {
                cambiandoVista = true;
                ActualizarComandos();
                try
                {
                    if (!catalogoTiposCargado)
                        await CargarTiposFotografiaAsync(forzar: true);
                    if (!catalogoTecnicosCargado)
                        await CargarTecnicosAsync(forzar: true);

                    bool refrescoExitoso = await CargarPaginaAsync(
                        paginaActual,
                        permitirDuranteCambioVista: true);
                    if (!refrescoExitoso)
                        DiagnosticoIAAnalizadorVisitaService.MarcarMutacion();
                }
                finally
                {
                    cambiandoVista = false;
                    ActualizarComandos();
                }

                return;
            }

            bool refrescoPaginaExitoso = await CargarPaginaAsync(paginaActual);
            if (!refrescoPaginaExitoso)
                DiagnosticoIAAnalizadorVisitaService.MarcarMutacion();
        }

        private async Task ActualizarAsync()
        {
            if (!PuedeEjecutarAccion || !ValidarEnLinea())
                return;

            cambiandoVista = true;
            ActualizarComandos();

            try
            {
                await CargarTiposFotografiaAsync(forzar: true);
                await CargarTecnicosAsync(forzar: true);
                await CargarPaginaAsync(
                    paginaActual,
                    permitirDuranteCambioVista: true);
            }
            finally
            {
                cambiandoVista = false;
                ActualizarComandos();
            }
        }

        private async Task BuscarAsync()
        {
            if (!PuedeEjecutarAccion || !ValidarRangoFechasEscritas())
                return;

            CapturarFiltrosAplicados();
            NotificarFiltros();
            await CargarPaginaAsync(1);
            FiltrosExpandidos = false;
        }

        private async Task LimpiarFiltrosAsync()
        {
            if (!PuedeEjecutarAccion)
                return;

            BuscarInspeccion = string.Empty;
            PropietarioFiltro = string.Empty;
            DepartamentoFiltro = string.Empty;
            TecnicoSeleccionado = TecnicosFiltro.FirstOrDefault(item =>
                item.UsuarioTecnicoId <= 0);
            TipoFotografiaFiltroSeleccionado = TiposFotografiaFiltro[0];
            EstadoFiltroSeleccionado = EstadosFiltro[0];
            UsarFechaDesde = false;
            UsarFechaHasta = false;
            FechaDesde = DateTime.Today.AddDays(-30);
            FechaHasta = DateTime.Today;

            LimpiarFiltrosAplicados();
            NotificarFiltros();
            FiltrosExpandidos = false;
            await CargarPaginaAsync(1);
        }

        private async Task CambiarPaginaAsync(int nuevaPagina)
        {
            if (!PuedeEjecutarAccion ||
                nuevaPagina < 1 ||
                (totalPaginas > 0 && nuevaPagina > totalPaginas) ||
                nuevaPagina == paginaActual)
            {
                return;
            }

            await CargarPaginaAsync(nuevaPagina);
        }

        private async Task CambiarVistaAsync(string nuevaVista)
        {
            if (string.Equals(vistaActual, nuevaVista, StringComparison.Ordinal) ||
                !PuedeEjecutarAccion)
            {
                return;
            }

            cambiandoVista = true;
            ActualizarComandos();

            try
            {
                // Un cambio escrito pendiente no se aplica silenciosamente al
                // cambiar de vista. Se restaura lo que realmente estaba activo.
                RestaurarFiltrosEscritosDesdeAplicados();
                EstablecerVista(nuevaVista);
                catalogoTecnicosCargado = false;

                await CargarTecnicosAsync(forzar: true);
                await CargarPaginaAsync(1, permitirDuranteCambioVista: true);
            }
            finally
            {
                cambiandoVista = false;
                ActualizarComandos();
            }
        }

        private void EstablecerVista(string nuevaVista)
        {
            vistaActual = nuevaVista switch
            {
                VistaDisponibles => VistaDisponibles,
                VistaRevisados => VistaRevisados,
                _ => VistaMis
            };

            paginaActual = 1;
            totalPaginas = 0;
            totalRegistros = 0;

            OnPropertyChanged(nameof(MostrandoMis));
            OnPropertyChanged(nameof(MostrandoDisponibles));
            OnPropertyChanged(nameof(MostrandoRevisadas));
            OnPropertyChanged(nameof(SubtituloVista));
            OnPropertyChanged(nameof(TextoSinSolicitudes));
            OnPropertyChanged(nameof(TextoAbrir));
            OnPropertyChanged(nameof(PuedeTomarExpediente));
            NotificarPaginacion();
            ActualizarComandos();
        }

        private async Task CargarTiposFotografiaAsync(bool forzar = false)
        {
            if ((!forzar && catalogoTiposCargado) ||
                !paginaActiva ||
                !ValidarEnLinea(false))
            {
                return;
            }

            CancellationToken token = ObtenerTokenActivo();
            string codigoSeleccionado =
                tipoFotografiaFiltroSeleccionado?.Codigo ?? string.Empty;
            string codigoAplicadoAnterior = tipoFotografiaAplicado;

            try
            {
                ApiResult<List<TipoFotografiaIAItem>> resultado =
                    await tiposFotografiaApi.ListarActivosAsync(
                        forzar,
                        token);

                token.ThrowIfCancellationRequested();
                if (!resultado.Success || resultado.Data == null)
                    return;

                while (TiposFotografiaFiltro.Count > 1)
                    TiposFotografiaFiltro.RemoveAt(TiposFotografiaFiltro.Count - 1);

                foreach (TipoFotografiaIAItem item in resultado.Data
                             .Where(item => item.Activo)
                             .OrderBy(item => item.Orden)
                             .ThenBy(item => item.NombreMostrar))
                {
                    TiposFotografiaFiltro.Add(
                        new FiltroCodigoOpcionV2(item.Codigo, item.NombreMostrar));
                }

                catalogoTiposCargado = true;

                tipoFotografiaFiltroSeleccionado =
                    TiposFotografiaFiltro.FirstOrDefault(item =>
                        string.Equals(
                            item.Codigo,
                            codigoSeleccionado,
                            StringComparison.OrdinalIgnoreCase)) ??
                    TiposFotografiaFiltro[0];

                if (!string.IsNullOrWhiteSpace(codigoAplicadoAnterior) &&
                    TiposFotografiaFiltro.All(item =>
                        !string.Equals(
                            item.Codigo,
                            codigoAplicadoAnterior,
                            StringComparison.OrdinalIgnoreCase)))
                {
                    tipoFotografiaAplicado = string.Empty;
                }

                OnPropertyChanged(nameof(TipoFotografiaFiltroSeleccionado));
                NotificarFiltros();
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                await MostrarErrorAsync(ex);
            }
        }

        private async Task CargarTecnicosAsync(bool forzar = false)
        {
            if ((!forzar && catalogoTecnicosCargado) ||
                !paginaActiva ||
                !ValidarEnLinea(false))
            {
                return;
            }

            CancellationToken token = ObtenerTokenActivo();

            try
            {
                int tecnicoSeleccionadoId =
                    ObtenerTecnicoId(TecnicoSeleccionado) ?? 0;
                int tecnicoAplicadoAnterior = tecnicoAplicadoId ?? 0;

                TecnicoInspeccionFiltroRespuesta respuesta =
                    await filtrosApi.ObtenerTecnicosAsync(
                        ModoApiActual,
                        token);

                token.ThrowIfCancellationRequested();

                TecnicosFiltro.Clear();
                TecnicosFiltro.Add(TecnicoInspeccionFiltroItem.Todos());

                foreach (TecnicoInspeccionFiltroItem item in respuesta.Tecnicos)
                    TecnicosFiltro.Add(item);

                catalogoTecnicosCargado = true;

                tecnicoSeleccionado = TecnicosFiltro.FirstOrDefault(item =>
                    item.UsuarioTecnicoId == tecnicoSeleccionadoId) ??
                    TecnicosFiltro[0];

                if (tecnicoAplicadoAnterior > 0 &&
                    TecnicosFiltro.All(item =>
                        item.UsuarioTecnicoId != tecnicoAplicadoAnterior))
                {
                    tecnicoAplicadoId = null;
                }

                OnPropertyChanged(nameof(TecnicoSeleccionado));
                OnPropertyChanged(nameof(TecnicoFiltroTexto));
                NotificarFiltros();
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                await MostrarErrorAsync(ex);
            }
        }

        private async Task<bool> CargarPaginaAsync(
            int paginaSolicitada,
            bool permitirDuranteCambioVista = false)
        {
            if (!paginaActiva ||
                IsBusy ||
                (!permitirDuranteCambioVista && cambiandoVista) ||
                !ValidarEnLinea(false))
            {
                return false;
            }

            CancellationToken token = ObtenerTokenActivo();
            bool exito = false;

            IsBusy = true;
            MensajeEstado = vistaActual switch
            {
                VistaDisponibles =>
                    "Cargando expedientes disponibles para el analizador...",
                VistaRevisados =>
                    "Cargando inspecciones revisadas por el analizador...",
                _ =>
                    "Cargando mis expedientes de análisis..."
            };
            ActualizarComandos();

            try
            {
                var filtro = new InspeccionFitosanitariaBandejaFiltroV2
                {
                    Modo = ModoApiActual,
                    Buscar = buscarAplicado,
                    Propietario = propietarioAplicado,
                    TecnicoId = tecnicoAplicadoId,
                    Departamento = departamentoAplicado,
                    TipoFotografia = tipoFotografiaAplicado,
                    Estado = estadoAplicado,
                    FechaDesde = fechaDesdeAplicada,
                    FechaHasta = fechaHastaAplicada,
                    DesfaseHorarioMinutos =
                        (int)DateTimeOffset.Now.Offset.TotalMinutes,
                    TamanoPagina = TamanoPagina
                };

                InspeccionFitosanitariaBandejaPaginaNumeradaV2 pagina =
                    await api.ObtenerPaginaAsync(
                        filtro,
                        paginaSolicitada,
                        TamanoPagina,
                        token);

                token.ThrowIfCancellationRequested();

                Solicitudes.Clear();
                foreach (InspeccionFitosanitariaBandejaItemV2 item in pagina.Items)
                    Solicitudes.Add(item);

                paginaActual = Math.Max(1, pagina.Pagina);
                totalPaginas = Math.Max(0, pagina.TotalPaginas);
                totalRegistros = Math.Max(0, pagina.Total);

                OnPropertyChanged(nameof(SinSolicitudes));
                OnPropertyChanged(nameof(TieneResultados));
                NotificarPaginacion();
                PaginaCargada?.Invoke(this, EventArgs.Empty);
                exito = true;
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                await MostrarErrorAsync(ex);
            }
            finally
            {
                MensajeEstado = string.Empty;
                IsBusy = false;
                OnPropertyChanged(nameof(SinSolicitudes));
                OnPropertyChanged(nameof(TieneResultados));
                ActualizarComandos();
            }

            return exito;
        }

        private async Task TomarAsync(
            InspeccionFitosanitariaBandejaItemV2? item)
        {
            if (item == null ||
                !MostrandoDisponibles ||
                !PuedeTomarExpediente ||
                !PuedeEjecutarAccion)
            {
                return;
            }

            CancellationToken token = ObtenerTokenActivo();
            IsBusy = true;
            MensajeEstado =
                $"Asignando el expediente #{item.InspeccionId} a su usuario...";
            ActualizarComandos();

            bool asignado = false;
            try
            {
                await asignacionApi.TomarAsync(
                    item.InspeccionId,
                    "analizador",
                    token);

                token.ThrowIfCancellationRequested();
                asignado = true;

                EstablecerVista(VistaMis);
                catalogoTecnicosCargado = false;

                await GoToAsyncParameters(
                    DiagnosticoIARoutes.CrearRutaResultado(
                        item.InspeccionId,
                        DiagnosticoIARoutes.ModoAnalizador));
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                await MostrarErrorAsync(ex);
            }
            finally
            {
                MensajeEstado = string.Empty;
                IsBusy = false;
                ActualizarComandos();
            }

            // Si otro analizador lo tomó simultáneamente, se vuelve a leer solo
            // la página visible para retirarlo de Disponibles.
            if (!asignado && MostrandoDisponibles && paginaActiva)
                await CargarPaginaAsync(paginaActual);
        }

        private async Task AbrirAsync(
            InspeccionFitosanitariaBandejaItemV2? item)
        {
            if (item == null || MostrandoDisponibles || !PuedeEjecutarAccion)
                return;

            string origen = MostrandoRevisadas
                ? DiagnosticoIARoutes.ModoAnalizadorRevisadas
                : DiagnosticoIARoutes.ModoAnalizador;

            await GoToAsyncParameters(
                DiagnosticoIARoutes.CrearRutaResultado(
                    item.InspeccionId,
                    origen));
        }

        private void CapturarFiltrosAplicados()
        {
            buscarAplicado = NormalizarTexto(BuscarInspeccion);
            propietarioAplicado = NormalizarTexto(PropietarioFiltro);
            departamentoAplicado = NormalizarTexto(DepartamentoFiltro);
            tecnicoAplicadoId = ObtenerTecnicoId(TecnicoSeleccionado);
            tipoFotografiaAplicado =
                TipoFotografiaFiltroSeleccionado.Codigo?.Trim() ?? string.Empty;
            estadoAplicado =
                EstadoFiltroSeleccionado.Codigo?.Trim() ?? string.Empty;
            fechaDesdeAplicada = UsarFechaDesde ? FechaDesde.Date : null;
            fechaHastaAplicada = UsarFechaHasta ? FechaHasta.Date : null;
        }

        private void LimpiarFiltrosAplicados()
        {
            buscarAplicado = string.Empty;
            propietarioAplicado = string.Empty;
            departamentoAplicado = string.Empty;
            tecnicoAplicadoId = null;
            tipoFotografiaAplicado = string.Empty;
            estadoAplicado = string.Empty;
            fechaDesdeAplicada = null;
            fechaHastaAplicada = null;
        }

        private void RestaurarFiltrosEscritosDesdeAplicados()
        {
            buscarInspeccion = buscarAplicado;
            propietarioFiltro = propietarioAplicado;
            departamentoFiltro = departamentoAplicado;
            tecnicoSeleccionado = tecnicoAplicadoId is > 0
                ? TecnicosFiltro.FirstOrDefault(item =>
                    item.UsuarioTecnicoId == tecnicoAplicadoId.Value)
                : TecnicosFiltro.FirstOrDefault(item =>
                    item.UsuarioTecnicoId <= 0);
            tipoFotografiaFiltroSeleccionado =
                TiposFotografiaFiltro.FirstOrDefault(item =>
                    string.Equals(
                        item.Codigo,
                        tipoFotografiaAplicado,
                        StringComparison.OrdinalIgnoreCase)) ??
                TiposFotografiaFiltro[0];
            estadoFiltroSeleccionado = EstadosFiltro.FirstOrDefault(item =>
                string.Equals(
                    item.Codigo,
                    estadoAplicado,
                    StringComparison.OrdinalIgnoreCase)) ?? EstadosFiltro[0];
            usarFechaDesde = fechaDesdeAplicada.HasValue;
            usarFechaHasta = fechaHastaAplicada.HasValue;
            fechaDesde = fechaDesdeAplicada ?? DateTime.Today.AddDays(-30);
            fechaHasta = fechaHastaAplicada ?? DateTime.Today;

            OnPropertyChanged(nameof(BuscarInspeccion));
            OnPropertyChanged(nameof(PropietarioFiltro));
            OnPropertyChanged(nameof(DepartamentoFiltro));
            OnPropertyChanged(nameof(TecnicoSeleccionado));
            OnPropertyChanged(nameof(TecnicoFiltroTexto));
            OnPropertyChanged(nameof(TipoFotografiaFiltroSeleccionado));
            OnPropertyChanged(nameof(EstadoFiltroSeleccionado));
            OnPropertyChanged(nameof(UsarFechaDesde));
            OnPropertyChanged(nameof(UsarFechaHasta));
            OnPropertyChanged(nameof(FechaDesde));
            OnPropertyChanged(nameof(FechaHasta));
            NotificarFiltros();
        }

        private bool FiltrosEscritosCoincidenConAplicados()
        {
            DateTime? desdeEscrita = UsarFechaDesde ? FechaDesde.Date : null;
            DateTime? hastaEscrita = UsarFechaHasta ? FechaHasta.Date : null;

            return string.Equals(
                       NormalizarTexto(BuscarInspeccion),
                       buscarAplicado,
                       StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(
                       NormalizarTexto(PropietarioFiltro),
                       propietarioAplicado,
                       StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(
                       NormalizarTexto(DepartamentoFiltro),
                       departamentoAplicado,
                       StringComparison.OrdinalIgnoreCase) &&
                   ObtenerTecnicoId(TecnicoSeleccionado) == tecnicoAplicadoId &&
                   string.Equals(
                       TipoFotografiaFiltroSeleccionado.Codigo?.Trim() ?? string.Empty,
                       tipoFotografiaAplicado,
                       StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(
                       EstadoFiltroSeleccionado.Codigo?.Trim() ?? string.Empty,
                       estadoAplicado,
                       StringComparison.OrdinalIgnoreCase) &&
                   desdeEscrita == fechaDesdeAplicada &&
                   hastaEscrita == fechaHastaAplicada;
        }

        private bool ValidarRangoFechasEscritas()
        {
            if (UsarFechaDesde && UsarFechaHasta &&
                FechaDesde.Date > FechaHasta.Date)
            {
                _ = MostrarAlertaAsync(
                    "Filtros de fecha",
                    "La fecha inicial debe ser anterior o igual a la fecha final.");
                return false;
            }

            return true;
        }

        private static DateTime LimitarFecha(DateTime value)
        {
            DateTime fecha = value.Date;
            if (fecha < FechaMinimaPermitida)
                return FechaMinimaPermitida;
            if (fecha > DateTime.Today)
                return DateTime.Today;
            return fecha;
        }

        private static string NormalizarTexto(string? valor) =>
            string.IsNullOrWhiteSpace(valor)
                ? string.Empty
                : valor.Trim();

        private static int? ObtenerTecnicoId(
            TecnicoInspeccionFiltroItem? tecnico) =>
            tecnico?.UsuarioTecnicoId is > 0
                ? tecnico.UsuarioTecnicoId
                : null;

        private CancellationToken ObtenerTokenActivo()
        {
            if (cargaCts == null || cargaCts.IsCancellationRequested)
            {
                cargaCts?.Dispose();
                cargaCts = new CancellationTokenSource();
            }

            return cargaCts.Token;
        }

        private void NotificarFiltros()
        {
            OnPropertyChanged(nameof(CantidadFiltrosActivos));
            OnPropertyChanged(nameof(FiltrosPendientesDeAplicar));
            OnPropertyChanged(nameof(FiltroTecnicoPendiente));
            OnPropertyChanged(nameof(TextoBotonFiltros));
            OnPropertyChanged(nameof(ResumenFiltrosActivos));
            OnPropertyChanged(nameof(TextoEstadoFiltroTecnico));
            ActualizarComandos();
        }

        private void NotificarPaginacion()
        {
            OnPropertyChanged(nameof(PaginaActual));
            OnPropertyChanged(nameof(TotalPaginas));
            OnPropertyChanged(nameof(TotalRegistros));
            OnPropertyChanged(nameof(PuedePaginaAnterior));
            OnPropertyChanged(nameof(PuedePaginaSiguiente));
            OnPropertyChanged(nameof(TextoPagina));
            OnPropertyChanged(nameof(ResumenResultados));
        }

        private void ActualizarComandos()
        {
            ActualizarCommand.ChangeCanExecute();
            BuscarCommand.ChangeCanExecute();
            LimpiarFiltrosCommand.ChangeCanExecute();
            AlternarFiltrosCommand.ChangeCanExecute();
            AplicarFiltroTecnicoCommand.ChangeCanExecute();
            PaginaAnteriorCommand.ChangeCanExecute();
            PaginaSiguienteCommand.ChangeCanExecute();
            AbrirCommand.ChangeCanExecute();
            TomarCommand.ChangeCanExecute();
            VerMisCommand.ChangeCanExecute();
            VerDisponiblesCommand.ChangeCanExecute();
            VerRevisadasCommand.ChangeCanExecute();
        }
    }
}
