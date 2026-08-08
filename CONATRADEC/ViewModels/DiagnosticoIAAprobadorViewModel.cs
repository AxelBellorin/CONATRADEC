using CONATRADEC.Models;
using CONATRADEC.Services;
using System.Collections.ObjectModel;

namespace CONATRADEC.ViewModels
{
    public sealed class DiagnosticoIAAprobadorViewModel : DiagnosticoIAViewModelBase
    {
        private const int TamanoPagina = 20;
        private static readonly DateTime FechaMinimaPermitida = new(2000, 1, 1);

        private readonly InspeccionFitosanitariaBandejaApiService filtrosApi =
            InspeccionFitosanitariaBandejaApiService.Instance;
        private readonly InspeccionFitosanitariaBandejaOperativaApiService api =
            new();

        private bool catalogoTecnicosCargado;
        private bool cargandoMas;
        private bool mostrandoRevisadas;
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
            tipoFotografiaFiltroSeleccionado = TiposFotografiaFiltro[0];
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
                item => item != null && !IsBusy && !cargandoMas && !cambiandoVista);

            VerPendientesCommand = new Command(
                async () => await CambiarVistaAsync(revisadas: false),
                () => MostrandoRevisadas && !IsBusy && !cargandoMas && !cambiandoVista);

            VerRevisadasCommand = new Command(
                async () => await CambiarVistaAsync(revisadas: true),
                () => !MostrandoRevisadas && !IsBusy && !cargandoMas && !cambiandoVista);
        }

        public ObservableCollection<InspeccionFitosanitariaBandejaItemV2>
            Solicitudes { get; } = [];

        public ObservableCollection<TecnicoInspeccionFiltroItem>
            TecnicosFiltro { get; } = [];

        public IReadOnlyList<FiltroCodigoOpcionV2> TiposFotografiaFiltro { get; } =
        [
            new(string.Empty, "Todos los tipos"),
            new("EVIDENCIA", "Evidencia general"),
            new("HOJA", "Hoja"),
            new("FRUTO", "Fruto"),
            new("TALLO", "Tallo"),
            new("RAMA", "Rama"),
            new("PLANTA_COMPLETA", "Planta completa"),
            new("RAIZ", "Raíz"),
            new("OTRA", "Otra")
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
        public Command VerPendientesCommand { get; }
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
            get => tipoFotografiaFiltroSeleccionado ?? TiposFotografiaFiltro[0];
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

        public bool MostrandoRevisadas
        {
            get => mostrandoRevisadas;
            private set
            {
                if (mostrandoRevisadas == value)
                    return;

                mostrandoRevisadas = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(MostrandoPendientes));
                OnPropertyChanged(nameof(SubtituloVista));
                OnPropertyChanged(nameof(TextoSinSolicitudes));
                OnPropertyChanged(nameof(TextoCargarMas));
                ActualizarComandos();
            }
        }

        public bool MostrandoPendientes => !MostrandoRevisadas;

        public bool MostrarFiltroTecnico => true;

        public string SubtituloVista => MostrandoRevisadas
            ? "Consulte decisiones ya emitidas y administre posteriormente la autorización o publicación de fotografías aprobadas."
            : "Apruebe, devuelva, rechace o declare inconclusa cada fotografía pendiente de manera independiente.";

        public string TextoSinSolicitudes => MostrandoRevisadas
            ? "No hay inspecciones revisadas que coincidan con los filtros."
            : "No hay fotografías pendientes de aprobación que coincidan con los filtros.";

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
                ? "Cargar más revisadas"
                : "Cargar más inspecciones";

        public async Task InicializarAsync()
        {
            await CargarTecnicosAsync();
            await CargarPrimeraPaginaAsync();
        }

        private async Task ActualizarAsync()
        {
            await CargarTecnicosAsync(forzar: true);
            await CargarPrimeraPaginaAsync();
        }

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
            TipoFotografiaFiltroSeleccionado = TiposFotografiaFiltro[0];
            EstadoFiltroSeleccionado = EstadosFiltro[0];
            UsarFechaDesde = false;
            UsarFechaHasta = false;
            FechaDesde = DateTime.Today.AddDays(-30);
            FechaHasta = DateTime.Today;
            await CargarPrimeraPaginaAsync();
        }

        private async Task CambiarVistaAsync(bool revisadas)
        {
            if (MostrandoRevisadas == revisadas || cambiandoVista || IsBusy || CargandoMas)
                return;

            cambiandoVista = true;
            ActualizarComandos();

            try
            {
                MostrandoRevisadas = revisadas;
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

        private async Task CargarTecnicosAsync(bool forzar = false)
        {
            if ((!forzar && catalogoTecnicosCargado) || !ValidarEnLinea(false))
                return;

            try
            {
                int seleccionadoId = tecnicoSeleccionado?.UsuarioTecnicoId ?? 0;
                string modo = MostrandoRevisadas
                    ? DiagnosticoIARoutes.ModoAprobadorRevisadas
                    : DiagnosticoIARoutes.ModoAprobador;

                TecnicoInspeccionFiltroRespuesta respuesta =
                    await filtrosApi.ObtenerTecnicosAsync(modo);

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
                MensajeEstado = MostrandoRevisadas
                    ? "Cargando inspecciones revisadas..."
                    : "Cargando inspecciones pendientes de aprobación...";
                ActualizarComandos();
            }

            try
            {
                string modo = MostrandoRevisadas
                    ? DiagnosticoIARoutes.ModoAprobadorRevisadas
                    : DiagnosticoIARoutes.ModoAprobador;

                var filtro = new InspeccionFitosanitariaBandejaFiltroV2
                {
                    Modo = modo,
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

        private async Task AbrirAsync(InspeccionFitosanitariaBandejaItemV2? item)
        {
            if (item == null || IsBusy || CargandoMas || cambiandoVista)
                return;

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
            VerPendientesCommand.ChangeCanExecute();
            VerRevisadasCommand.ChangeCanExecute();
        }
    }
}
