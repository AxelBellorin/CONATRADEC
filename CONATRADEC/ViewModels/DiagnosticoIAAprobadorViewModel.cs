using CONATRADEC.Models;
using CONATRADEC.Services;
using System.Collections.ObjectModel;

namespace CONATRADEC.ViewModels
{
    public sealed class DiagnosticoIAAprobadorViewModel : DiagnosticoIAViewModelBase
    {
        private const int TamanoPagina = 20;
        private const string VistaMis = "mis";
        private const string VistaDisponibles = "disponibles";
        private const string VistaRevisados = "revisados";
        private const string ModoAprobadorDisponibles = "aprobador-disponibles";
        private static readonly DateTime FechaMinimaPermitida = new(2000, 1, 1);

        private readonly InspeccionFitosanitariaBandejaApiService filtrosApi =
            InspeccionFitosanitariaBandejaApiService.Instance;
        private readonly InspeccionFitosanitariaBandejaOperativaApiService api =
            new();
        private readonly InspeccionRevisionBloqueoApiService asignacionApi =
            new();
        private readonly TipoFotografiaIAApiService tiposFotografiaApi =
            new();

        private bool catalogoTecnicosCargado;
        private bool cargandoMas;
        private string vistaActual = VistaMis;
        private bool cambiandoVista;
        private bool filtrosExpandidos;
        private bool usarFechaDesde;
        private bool usarFechaHasta;
        private TecnicoInspeccionFiltroItem? tecnicoSeleccionado;
        private FiltroCodigoOpcionV2? tipoFotografiaFiltroSeleccionado;
        private FiltroCodigoOpcionV2? estadoFiltroSeleccionado;
        private DateTime? siguienteFechaUtc;
        private int? siguienteId;
        private bool hayMas;
        private string buscarInspeccion = string.Empty;
        private string propietarioFiltro = string.Empty;
        private string departamentoFiltro = string.Empty;
        private DateTime fechaDesde = DateTime.Today.AddDays(-30);
        private DateTime fechaHasta = DateTime.Today;

        public DiagnosticoIAAprobadorViewModel()
        {
            tipoFotografiaFiltroSeleccionado =
                ObtenerFiltroTipoFotografiaPredeterminado();
            estadoFiltroSeleccionado = EstadosFiltro[0];

            ActualizarCommand = new Command(
                async () => await ActualizarAsync(),
                () => !IsBusy && !cargandoMas && !cambiandoVista);

            BuscarCommand = new Command(
                async () => await BuscarAsync(),
                () => !IsBusy && !cargandoMas && !cambiandoVista);

            LimpiarFiltrosCommand = new Command(
                async () => await LimpiarFiltrosAsync(),
                () => !IsBusy && !cargandoMas && !cambiandoVista);

            AlternarFiltrosCommand = new Command(
                () => FiltrosExpandidos = !FiltrosExpandidos,
                () => !IsBusy && !cargandoMas && !cambiandoVista);

            CargarMasCommand = new Command(
                async () => await CargarMasAsync(),
                () => !IsBusy && !cargandoMas && !cambiandoVista && HayMas);

            AbrirCommand = new Command<InspeccionFitosanitariaBandejaItemV2>(
                async item => await AbrirAsync(item),
                item => item != null &&
                    !MostrandoDisponibles &&
                    !IsBusy && !cargandoMas && !cambiandoVista);

            TomarCommand = new Command<InspeccionFitosanitariaBandejaItemV2>(
                async item => await TomarAsync(item),
                item => item != null &&
                    MostrandoDisponibles &&
                    PuedeTomarExpediente &&
                    !IsBusy && !cargandoMas && !cambiandoVista);

            VerMisCommand = new Command(
                async () => await CambiarVistaAsync(VistaMis),
                () => !MostrandoMis && !IsBusy && !cargandoMas && !cambiandoVista);

            VerDisponiblesCommand = new Command(
                async () => await CambiarVistaAsync(VistaDisponibles),
                () => !MostrandoDisponibles && !IsBusy && !cargandoMas && !cambiandoVista);

            VerRevisadasCommand = new Command(
                async () => await CambiarVistaAsync(VistaRevisados),
                () => !MostrandoRevisadas && !IsBusy && !cargandoMas && !cambiandoVista);
        }

        public ObservableCollection<InspeccionFitosanitariaBandejaItemV2>
            Solicitudes { get; } = [];

        public ObservableCollection<TecnicoInspeccionFiltroItem>
            TecnicosFiltro { get; } = [];

        /// <summary>
        /// El primer elemento es una opción exclusiva de interfaz. El resto se
        /// reconstruye desde el catálogo activo del backend para evitar que los
        /// filtros diverjan de Web o de la configuración administrativa.
        /// </summary>
        public ObservableCollection<FiltroCodigoOpcionV2>
            TiposFotografiaFiltro { get; } =
        [
            new(string.Empty, "Todos los tipos")
        ];

        public IReadOnlyList<FiltroCodigoOpcionV2> EstadosFiltro { get; } =
        [
            new(string.Empty, "Todos los estados"),
            new("PENDIENTE_APROBACION", "Pendiente de aprobación"),
            new("PENDIENTE_REVISION", "Devuelta / en revisión"),
            new("FINALIZADA", "Finalizada"),
            new("FINALIZADA_PARCIALMENTE", "Finalizada parcialmente")
        ];

        public Command ActualizarCommand { get; }
        public Command BuscarCommand { get; }
        public Command LimpiarFiltrosCommand { get; }
        public Command AlternarFiltrosCommand { get; }
        public Command CargarMasCommand { get; }
        public Command<InspeccionFitosanitariaBandejaItemV2> AbrirCommand { get; }
        public Command<InspeccionFitosanitariaBandejaItemV2> TomarCommand { get; }
        public Command VerMisCommand { get; }
        public Command VerDisponiblesCommand { get; }
        public Command VerRevisadasCommand { get; }

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

        public FiltroCodigoOpcionV2 TipoFotografiaFiltroSeleccionado
        {
            get => tipoFotografiaFiltroSeleccionado ??
                ObtenerFiltroTipoFotografiaPredeterminado();
            set
            {
                if (ReferenceEquals(tipoFotografiaFiltroSeleccionado, value))
                    return;
                tipoFotografiaFiltroSeleccionado = value;
                OnPropertyChanged();
                NotificarFiltros();
            }
        }

        public FiltroCodigoOpcionV2 EstadoFiltroSeleccionado
        {
            get => estadoFiltroSeleccionado ?? EstadosFiltro[0];
            set
            {
                if (ReferenceEquals(estadoFiltroSeleccionado, value))
                    return;
                estadoFiltroSeleccionado = value;
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
            set
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
                if (!string.IsNullOrWhiteSpace(BuscarInspeccion)) cantidad++;
                if (TecnicoSeleccionado?.UsuarioTecnicoId is > 0) cantidad++;
                if (!string.IsNullOrWhiteSpace(PropietarioFiltro)) cantidad++;
                if (!string.IsNullOrWhiteSpace(DepartamentoFiltro)) cantidad++;
                if (!string.IsNullOrWhiteSpace(TipoFotografiaFiltroSeleccionado.Codigo)) cantidad++;
                if (!string.IsNullOrWhiteSpace(EstadoFiltroSeleccionado.Codigo)) cantidad++;
                if (UsarFechaDesde) cantidad++;
                if (UsarFechaHasta) cantidad++;
                return cantidad;
            }
        }

        public string TextoBotonFiltros => FiltrosExpandidos
            ? "Ocultar filtros ▲"
            : CantidadFiltrosActivos == 0
                ? "Buscar y filtrar ▼"
                : $"Buscar y filtrar ({CantidadFiltrosActivos}) ▼";

        public string ResumenFiltrosActivos => CantidadFiltrosActivos == 0
            ? "Sin filtros adicionales"
            : CantidadFiltrosActivos == 1
                ? "1 filtro activo"
                : $"{CantidadFiltrosActivos} filtros activos";

        public bool MostrandoMis =>
            string.Equals(vistaActual, VistaMis, StringComparison.Ordinal);

        public bool MostrandoDisponibles =>
            string.Equals(vistaActual, VistaDisponibles, StringComparison.Ordinal);

        public bool MostrandoRevisadas =>
            string.Equals(vistaActual, VistaRevisados, StringComparison.Ordinal);

        public bool MostrarFiltroTecnico => true;

        public bool PuedeTomarExpediente =>
            PermissionService.Instance.HasUpdate(
                DiagnosticoIARoutes.InterfazAprobador);

        public string SubtituloVista => vistaActual switch
        {
            VistaDisponibles =>
                "Expedientes sin aprobador responsable y con fotografías listas para decisión.",
            VistaRevisados =>
                "Consulte decisiones ya emitidas y administre posteriormente la autorización o publicación de fotografías aprobadas.",
            _ =>
                "Expedientes asignados a usted con fotografías pendientes de aprobación."
        };

        public string TextoSinSolicitudes => vistaActual switch
        {
            VistaDisponibles =>
                "No hay expedientes disponibles para tomar como aprobador.",
            VistaRevisados =>
                "No hay inspecciones revisadas que coincidan con los filtros.",
            _ =>
                "No tiene expedientes asignados con fotografías pendientes de aprobación."
        };

        public bool SinSolicitudes =>
            !IsBusy && !cargandoMas && Solicitudes.Count == 0;

        public bool HayMas
        {
            get => hayMas;
            private set
            {
                if (hayMas == value)
                    return;
                hayMas = value;
                OnPropertyChanged();
                CargarMasCommand.ChangeCanExecute();
            }
        }

        public bool CargandoMas
        {
            get => cargandoMas;
            private set
            {
                if (cargandoMas == value)
                    return;
                cargandoMas = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TextoCargarMas));
                ActualizarComandos();
            }
        }

        public string TextoCargarMas => CargandoMas
            ? "Cargando..."
            : MostrandoRevisadas
                ? "Cargar más revisados"
                : MostrandoDisponibles
                    ? "Cargar más disponibles"
                    : "Cargar más expedientes";

        private string ModoApiActual => vistaActual switch
        {
            VistaDisponibles => ModoAprobadorDisponibles,
            VistaRevisados => DiagnosticoIARoutes.ModoAprobadorRevisadas,
            _ => DiagnosticoIARoutes.ModoAprobador
        };

        public async Task InicializarAsync()
        {
            await CargarTiposFotografiaAsync();
            await CargarTecnicosAsync();
            await CargarPrimeraPaginaAsync();
        }

        private async Task ActualizarAsync()
        {
            await CargarTiposFotografiaAsync(forzar: true);
            await CargarTecnicosAsync(forzar: true);
            await CargarPrimeraPaginaAsync();
        }

        private async Task CargarTiposFotografiaAsync(bool forzar = false)
        {
            string codigoSeleccionado =
                tipoFotografiaFiltroSeleccionado?.Codigo ?? string.Empty;

            ApiResult<List<TipoFotografiaIAItem>> resultado =
                await tiposFotografiaApi.ListarActivosAsync(forzar);

            if (!resultado.Success || resultado.Data is not { Count: > 0 })
            {
                if (TiposFotografiaFiltro.Count <= 1)
                {
                    await MostrarAlertaAsync(
                        "Tipos de fotografía",
                        string.IsNullOrWhiteSpace(resultado.Message)
                            ? "No fue posible cargar el catálogo activo de tipos de fotografía."
                            : resultado.Message);
                }

                return;
            }

            // Se conserva siempre la opción de interfaz "Todos los tipos".
            // Evitamos dejar la colección vacía temporalmente porque los
            // bindings de MAUI pueden reevaluar el getter durante un Clear().
            while (TiposFotografiaFiltro.Count > 1)
                TiposFotografiaFiltro.RemoveAt(TiposFotografiaFiltro.Count - 1);

            if (TiposFotografiaFiltro.Count == 0)
            {
                TiposFotografiaFiltro.Add(
                    new FiltroCodigoOpcionV2(
                        string.Empty,
                        "Todos los tipos"));
            }

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
                ObtenerFiltroTipoFotografiaPredeterminado();

            OnPropertyChanged(nameof(TipoFotografiaFiltroSeleccionado));
            NotificarFiltros();
        }

        private FiltroCodigoOpcionV2 ObtenerFiltroTipoFotografiaPredeterminado() =>
            TiposFotografiaFiltro.FirstOrDefault() ??
            new FiltroCodigoOpcionV2(
                string.Empty,
                "Todos los tipos");

        private async Task BuscarAsync()
        {
            if (!ValidarRangoFechas())
                return;

            await CargarPrimeraPaginaAsync();
            FiltrosExpandidos = false;
        }

        private async Task LimpiarFiltrosAsync()
        {
            BuscarInspeccion = string.Empty;
            PropietarioFiltro = string.Empty;
            DepartamentoFiltro = string.Empty;
            TecnicoSeleccionado = TecnicosFiltro.FirstOrDefault();
            TipoFotografiaFiltroSeleccionado =
                ObtenerFiltroTipoFotografiaPredeterminado();
            EstadoFiltroSeleccionado = EstadosFiltro[0];
            UsarFechaDesde = false;
            UsarFechaHasta = false;
            FechaDesde = DateTime.Today.AddDays(-30);
            FechaHasta = DateTime.Today;
            await CargarPrimeraPaginaAsync();
        }

        private async Task CambiarVistaAsync(string nuevaVista)
        {
            if (string.Equals(vistaActual, nuevaVista, StringComparison.Ordinal) ||
                cambiandoVista || IsBusy || CargandoMas)
            {
                return;
            }

            cambiandoVista = true;
            ActualizarComandos();

            try
            {
                EstablecerVista(nuevaVista);
                catalogoTecnicosCargado = false;
                await CargarTecnicosAsync(forzar: true);
                await CargarPrimeraPaginaAsync();
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

            OnPropertyChanged(nameof(MostrandoMis));
            OnPropertyChanged(nameof(MostrandoDisponibles));
            OnPropertyChanged(nameof(MostrandoRevisadas));
            OnPropertyChanged(nameof(SubtituloVista));
            OnPropertyChanged(nameof(TextoSinSolicitudes));
            OnPropertyChanged(nameof(TextoCargarMas));
            OnPropertyChanged(nameof(PuedeTomarExpediente));
            ActualizarComandos();
        }

        private async Task CargarTecnicosAsync(bool forzar = false)
        {
            if ((!forzar && catalogoTecnicosCargado) || !ValidarEnLinea(false))
                return;

            try
            {
                int seleccionadoId = tecnicoSeleccionado?.UsuarioTecnicoId ?? 0;
                TecnicoInspeccionFiltroRespuesta respuesta =
                    await filtrosApi.ObtenerTecnicosAsync(ModoApiActual);

                TecnicosFiltro.Clear();
                TecnicosFiltro.Add(TecnicoInspeccionFiltroItem.Todos());
                foreach (TecnicoInspeccionFiltroItem item in respuesta.Tecnicos)
                    TecnicosFiltro.Add(item);

                catalogoTecnicosCargado = true;
                tecnicoSeleccionado = TecnicosFiltro.FirstOrDefault(item =>
                    item.UsuarioTecnicoId == seleccionadoId) ?? TecnicosFiltro[0];
                OnPropertyChanged(nameof(TecnicoSeleccionado));
                OnPropertyChanged(nameof(TecnicoFiltroTexto));
                NotificarFiltros();
            }
            catch (Exception ex)
            {
                await MostrarErrorAsync(ex);
            }
        }

        private async Task CargarPrimeraPaginaAsync()
        {
            siguienteFechaUtc = null;
            siguienteId = null;
            HayMas = false;
            await CargarPaginaAsync(reemplazar: true);
        }

        private async Task CargarMasAsync()
        {
            if (!HayMas || CargandoMas || IsBusy || cambiandoVista)
                return;

            CargandoMas = true;
            try
            {
                await CargarPaginaAsync(reemplazar: false);
            }
            finally
            {
                CargandoMas = false;
            }
        }

        private async Task CargarPaginaAsync(bool reemplazar)
        {
            if ((IsBusy && reemplazar) || (cambiandoVista && !reemplazar) || !ValidarEnLinea(false))
                return;

            if (!ValidarRangoFechas())
                return;

            if (reemplazar)
            {
                IsBusy = true;
                MensajeEstado = vistaActual switch
                {
                    VistaDisponibles =>
                        "Cargando expedientes disponibles para aprobación...",
                    VistaRevisados =>
                        "Cargando inspecciones revisadas...",
                    _ =>
                        "Cargando mis expedientes de aprobación..."
                };
                ActualizarComandos();
            }

            try
            {
                var filtro = new InspeccionFitosanitariaBandejaFiltroV2
                {
                    Modo = ModoApiActual,
                    Buscar = BuscarInspeccion,
                    Propietario = PropietarioFiltro,
                    TecnicoId = TecnicoSeleccionado?.UsuarioTecnicoId is > 0
                        ? TecnicoSeleccionado.UsuarioTecnicoId
                        : null,
                    Departamento = DepartamentoFiltro,
                    TipoFotografia = TipoFotografiaFiltroSeleccionado.Codigo,
                    Estado = EstadoFiltroSeleccionado.Codigo,
                    FechaDesde = UsarFechaDesde ? FechaDesde : null,
                    FechaHasta = UsarFechaHasta ? FechaHasta : null,
                    DesfaseHorarioMinutos = (int)TimeZoneInfo.Local
                        .GetUtcOffset(DateTime.Now).TotalMinutes,
                    UltimaFechaUtc = reemplazar ? null : siguienteFechaUtc,
                    UltimoId = reemplazar ? null : siguienteId,
                    TamanoPagina = TamanoPagina
                };

                InspeccionFitosanitariaBandejaPaginaV2 pagina =
                    await api.ObtenerPaginaAsync(filtro);

                if (reemplazar)
                    Solicitudes.Clear();

                HashSet<int> existentes = Solicitudes
                    .Select(item => item.InspeccionId)
                    .ToHashSet();

                foreach (InspeccionFitosanitariaBandejaItemV2 item in pagina.Items)
                {
                    if (existentes.Add(item.InspeccionId))
                        Solicitudes.Add(item);
                }

                siguienteFechaUtc = pagina.SiguienteFechaUtc;
                siguienteId = pagina.SiguienteId;
                HayMas = pagina.HayMas;
                OnPropertyChanged(nameof(SinSolicitudes));
            }
            catch (Exception ex)
            {
                await MostrarErrorAsync(ex);
            }
            finally
            {
                if (reemplazar)
                {
                    MensajeEstado = string.Empty;
                    IsBusy = false;
                }

                OnPropertyChanged(nameof(SinSolicitudes));
                ActualizarComandos();
            }
        }

        private async Task TomarAsync(InspeccionFitosanitariaBandejaItemV2? item)
        {
            if (item == null ||
                !MostrandoDisponibles ||
                !PuedeTomarExpediente ||
                IsBusy || CargandoMas || cambiandoVista)
            {
                return;
            }

            IsBusy = true;
            MensajeEstado =
                $"Asignando el expediente #{item.InspeccionId} a su usuario...";
            ActualizarComandos();

            bool asignado = false;
            try
            {
                await asignacionApi.TomarAsync(
                    item.InspeccionId,
                    "aprobador");

                asignado = true;
                EstablecerVista(VistaMis);
                catalogoTecnicosCargado = false;

                await GoToAsyncParameters(
                    DiagnosticoIARoutes.CrearRutaResultado(
                        item.InspeccionId,
                        DiagnosticoIARoutes.ModoAprobador));
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

            if (!asignado && MostrandoDisponibles)
                await CargarPrimeraPaginaAsync();
        }

        private async Task AbrirAsync(InspeccionFitosanitariaBandejaItemV2? item)
        {
            if (item == null ||
                MostrandoDisponibles ||
                IsBusy || CargandoMas || cambiandoVista)
            {
                return;
            }

            string origen = MostrandoRevisadas
                ? DiagnosticoIARoutes.ModoAprobadorRevisadas
                : DiagnosticoIARoutes.ModoAprobador;

            await GoToAsyncParameters(
                DiagnosticoIARoutes.CrearRutaResultado(
                    item.InspeccionId,
                    origen));
        }

        private bool ValidarRangoFechas()
        {
            if (UsarFechaDesde && UsarFechaHasta && FechaDesde.Date > FechaHasta.Date)
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

        private void NotificarFiltros()
        {
            OnPropertyChanged(nameof(CantidadFiltrosActivos));
            OnPropertyChanged(nameof(TextoBotonFiltros));
            OnPropertyChanged(nameof(ResumenFiltrosActivos));
        }

        private void ActualizarComandos()
        {
            ActualizarCommand.ChangeCanExecute();
            BuscarCommand.ChangeCanExecute();
            LimpiarFiltrosCommand.ChangeCanExecute();
            AlternarFiltrosCommand.ChangeCanExecute();
            CargarMasCommand.ChangeCanExecute();
            AbrirCommand.ChangeCanExecute();
            TomarCommand.ChangeCanExecute();
            VerMisCommand.ChangeCanExecute();
            VerDisponiblesCommand.ChangeCanExecute();
            VerRevisadasCommand.ChangeCanExecute();
        }
    }
}
