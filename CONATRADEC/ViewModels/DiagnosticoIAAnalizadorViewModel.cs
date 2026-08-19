using CONATRADEC.Models;
using CONATRADEC.Services;
using System.Collections.ObjectModel;

namespace CONATRADEC.ViewModels
{
    /// <summary>
    /// Bandeja operativa del analizador con paginación numérica.
    ///
    /// La visita conserva vista, filtro aplicado y página mientras el usuario
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

        private readonly InspeccionFitosanitariaBandejaApiService filtrosApi =
            InspeccionFitosanitariaBandejaApiService.Instance;
        private readonly InspeccionFitosanitariaBandejaOperativaNumeradaApiService api =
            new();
        private readonly InspeccionRevisionBloqueoApiService asignacionApi =
            new();

        private CancellationTokenSource? cargaCts;
        private bool paginaActiva;
        private bool inicializado;
        private bool catalogoTecnicosCargado;
        private bool cambiandoVista;
        private string vistaActual = VistaMis;
        private TecnicoInspeccionFiltroItem? tecnicoSeleccionado;
        private int? tecnicoAplicadoId;
        private int paginaActual = 1;
        private int totalPaginas;
        private int totalRegistros;

        public DiagnosticoIAAnalizadorViewModel()
        {
            ActualizarCommand = new Command(
                async () => await ActualizarAsync(),
                () => PuedeEjecutarAccion);

            AplicarFiltroTecnicoCommand = new Command(
                async () => await AplicarFiltroTecnicoAsync(),
                () => PuedeEjecutarAccion && FiltroTecnicoPendiente);

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

        public Command ActualizarCommand { get; }
        public Command AplicarFiltroTecnicoCommand { get; }
        public Command PaginaAnteriorCommand { get; }
        public Command PaginaSiguienteCommand { get; }
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
                OnPropertyChanged(nameof(FiltroTecnicoPendiente));
                OnPropertyChanged(nameof(TextoEstadoFiltroTecnico));
                ActualizarComandos();
            }
        }

        public string TecnicoFiltroTexto =>
            TecnicoSeleccionado?.TextoMostrar ?? "Todos los técnicos";

        public bool FiltroTecnicoPendiente =>
            ObtenerTecnicoId(TecnicoSeleccionado) != tecnicoAplicadoId;

        public string TextoEstadoFiltroTecnico
        {
            get
            {
                if (FiltroTecnicoPendiente)
                    return "Hay un cambio de técnico pendiente. Pulse Aplicar filtro para consultar.";

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
                "No hay expedientes disponibles para tomar con el filtro aplicado.",
            VistaRevisados =>
                "No hay inspecciones revisadas con el filtro aplicado.",
            _ =>
                "No tiene expedientes de análisis asignados con el filtro aplicado."
        };

        public string TextoAbrir => MostrandoRevisadas
            ? "Consultar revisión"
            : "Continuar revisión";

        public bool SinSolicitudes =>
            !IsBusy && Solicitudes.Count == 0;

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

            if (!catalogoTecnicosCargado)
            {
                cambiandoVista = true;
                ActualizarComandos();
                try
                {
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

        private async Task AplicarFiltroTecnicoAsync()
        {
            if (!PuedeEjecutarAccion || !FiltroTecnicoPendiente)
                return;

            tecnicoAplicadoId = ObtenerTecnicoId(TecnicoSeleccionado);
            NotificarFiltroTecnico();
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
                RestaurarFiltroEscritoDesdeAplicado();
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
                NotificarFiltroTecnico();
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
                InspeccionFitosanitariaBandejaPaginaNumeradaV2 pagina =
                    await api.ObtenerPaginaAsync(
                        ModoApiActual,
                        tecnicoAplicadoId,
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
                tecnicoAplicadoId = null;
                tecnicoSeleccionado = TecnicosFiltro.FirstOrDefault(itemFiltro =>
                    itemFiltro.UsuarioTecnicoId <= 0);
                catalogoTecnicosCargado = false;
                OnPropertyChanged(nameof(TecnicoSeleccionado));
                OnPropertyChanged(nameof(TecnicoFiltroTexto));
                NotificarFiltroTecnico();

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

            /*
             * Si otro analizador tomó el expediente al mismo tiempo, se vuelve
             * a leer únicamente la página visible de Disponibles para retirarlo.
             */
            if (!asignado && MostrandoDisponibles && paginaActiva)
                await CargarPaginaAsync(paginaActual);
        }

        private async Task AbrirAsync(
            InspeccionFitosanitariaBandejaItemV2? item)
        {
            if (item == null ||
                MostrandoDisponibles ||
                !PuedeEjecutarAccion)
            {
                return;
            }

            string origen = MostrandoRevisadas
                ? DiagnosticoIARoutes.ModoAnalizadorRevisadas
                : DiagnosticoIARoutes.ModoAnalizador;

            await GoToAsyncParameters(
                DiagnosticoIARoutes.CrearRutaResultado(
                    item.InspeccionId,
                    origen));
        }

        private void RestaurarFiltroEscritoDesdeAplicado()
        {
            tecnicoSeleccionado = tecnicoAplicadoId is > 0
                ? TecnicosFiltro.FirstOrDefault(item =>
                    item.UsuarioTecnicoId == tecnicoAplicadoId.Value)
                : TecnicosFiltro.FirstOrDefault(item =>
                    item.UsuarioTecnicoId <= 0);

            OnPropertyChanged(nameof(TecnicoSeleccionado));
            OnPropertyChanged(nameof(TecnicoFiltroTexto));
            NotificarFiltroTecnico();
        }

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

        private void NotificarFiltroTecnico()
        {
            OnPropertyChanged(nameof(FiltroTecnicoPendiente));
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
