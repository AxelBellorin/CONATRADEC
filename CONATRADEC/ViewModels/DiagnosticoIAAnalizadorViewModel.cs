using CONATRADEC.Models;
using CONATRADEC.Services;
using System.Collections.ObjectModel;

namespace CONATRADEC.ViewModels
{
    /// <summary>
    /// Bandeja operativa del analizador.
    ///
    /// El flujo se divide en tres vistas:
    /// - Mis expedientes: asignados al usuario actual y todavía activos.
    /// - Disponibles: expedientes sin analizador que pueden tomarse.
    /// - Revisados: expedientes cuya revisión humana del usuario ya terminó.
    ///
    /// Tomar un expediente es una operación explícita y atómica en la API;
    /// ningún analizador normal puede quitarle un expediente a otro usuario.
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
        private readonly InspeccionFitosanitariaBandejaOperativaApiService api =
            new();
        private readonly InspeccionRevisionBloqueoApiService asignacionApi =
            new();

        private bool catalogoTecnicosCargado;
        private bool cargandoMas;
        private bool cambiandoVista;
        private string vistaActual = VistaMis;
        private TecnicoInspeccionFiltroItem? tecnicoSeleccionado;
        private DateTime? siguienteFechaUtc;
        private int? siguienteId;
        private bool hayMas;

        public DiagnosticoIAAnalizadorViewModel()
        {
            ActualizarCommand = new Command(
                async () => await ActualizarAsync(),
                () => !IsBusy && !cargandoMas && !cambiandoVista);

            CargarMasCommand = new Command(
                async () => await CargarMasAsync(),
                () => !IsBusy && !cargandoMas && !cambiandoVista && HayMas);

            AbrirCommand = new Command<InspeccionFitosanitariaBandejaItemV2>(
                async item => await AbrirAsync(item),
                item => item != null &&
                    !MostrandoDisponibles &&
                    !IsBusy &&
                    !cargandoMas &&
                    !cambiandoVista);

            TomarCommand = new Command<InspeccionFitosanitariaBandejaItemV2>(
                async item => await TomarAsync(item),
                item => item != null &&
                    MostrandoDisponibles &&
                    PuedeTomarExpediente &&
                    !IsBusy &&
                    !cargandoMas &&
                    !cambiandoVista);

            VerMisCommand = new Command(
                async () => await CambiarVistaAsync(VistaMis),
                () => !MostrandoMis &&
                    !IsBusy &&
                    !cargandoMas &&
                    !cambiandoVista);

            VerDisponiblesCommand = new Command(
                async () => await CambiarVistaAsync(VistaDisponibles),
                () => !MostrandoDisponibles &&
                    !IsBusy &&
                    !cargandoMas &&
                    !cambiandoVista);

            VerRevisadasCommand = new Command(
                async () => await CambiarVistaAsync(VistaRevisados),
                () => !MostrandoRevisadas &&
                    !IsBusy &&
                    !cargandoMas &&
                    !cambiandoVista);
        }

        public ObservableCollection<InspeccionFitosanitariaBandejaItemV2>
            Solicitudes { get; } = [];

        public ObservableCollection<TecnicoInspeccionFiltroItem>
            TecnicosFiltro { get; } = [];

        public Command ActualizarCommand { get; }
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

                if (catalogoTecnicosCargado && !IsBusy && !cambiandoVista)
                    _ = CargarPrimeraPaginaAsync();
            }
        }

        public string TecnicoFiltroTexto =>
            TecnicoSeleccionado?.TextoMostrar ?? "Todos los técnicos";

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
                "No hay expedientes disponibles para tomar en este momento.",
            VistaRevisados =>
                "Todavía no hay inspecciones revisadas por este analizador.",
            _ =>
                "No tiene expedientes de análisis asignados con trabajo activo."
        };

        public string TextoAbrir => MostrandoRevisadas
            ? "Consultar revisión"
            : "Continuar revisión";

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
            VistaDisponibles => ModoAnalizadorDisponibles,
            VistaRevisados => DiagnosticoIARoutes.ModoAnalizadorRevisadas,
            _ => DiagnosticoIARoutes.ModoAnalizador
        };

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
            OnPropertyChanged(nameof(TextoAbrir));
            OnPropertyChanged(nameof(PuedeTomarExpediente));
            ActualizarComandos();
        }

        private async Task CargarTecnicosAsync(bool forzar = false)
        {
            if ((!forzar && catalogoTecnicosCargado) ||
                !ValidarEnLinea(false))
            {
                return;
            }

            try
            {
                int tecnicoSeleccionadoId =
                    tecnicoSeleccionado?.UsuarioTecnicoId ?? 0;

                TecnicoInspeccionFiltroRespuesta respuesta =
                    await filtrosApi.ObtenerTecnicosAsync(ModoApiActual);

                TecnicosFiltro.Clear();
                TecnicosFiltro.Add(TecnicoInspeccionFiltroItem.Todos());

                foreach (TecnicoInspeccionFiltroItem item in respuesta.Tecnicos)
                    TecnicosFiltro.Add(item);

                catalogoTecnicosCargado = true;
                tecnicoSeleccionado = TecnicosFiltro.FirstOrDefault(item =>
                    item.UsuarioTecnicoId == tecnicoSeleccionadoId) ??
                    TecnicosFiltro[0];
                OnPropertyChanged(nameof(TecnicoSeleccionado));
                OnPropertyChanged(nameof(TecnicoFiltroTexto));
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
            if ((IsBusy && reemplazar) ||
                (cambiandoVista && !reemplazar) ||
                !ValidarEnLinea(false))
            {
                return;
            }

            if (reemplazar)
            {
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
            }

            try
            {
                InspeccionFitosanitariaBandejaPaginaV2 pagina =
                    await api.ObtenerPaginaAsync(
                        ModoApiActual,
                        TecnicoSeleccionado?.UsuarioTecnicoId is > 0
                            ? TecnicoSeleccionado.UsuarioTecnicoId
                            : null,
                        reemplazar ? null : siguienteFechaUtc,
                        reemplazar ? null : siguienteId,
                        TamanoPagina);

                if (reemplazar)
                    Solicitudes.Clear();

                HashSet<int> existentes = Solicitudes
                    .Select(item => item.InspeccionId)
                    .ToHashSet();

                foreach (InspeccionFitosanitariaBandejaItemV2 item
                         in pagina.Items)
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

        private async Task TomarAsync(
            InspeccionFitosanitariaBandejaItemV2? item)
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
                    "analizador");

                asignado = true;
                EstablecerVista(VistaMis);
                catalogoTecnicosCargado = false;

                await GoToAsyncParameters(
                    DiagnosticoIARoutes.CrearRutaResultado(
                        item.InspeccionId,
                        DiagnosticoIARoutes.ModoAnalizador));
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
             * a consultar Disponibles para retirarlo inmediatamente de la vista.
             */
            if (!asignado && MostrandoDisponibles)
                await CargarPrimeraPaginaAsync();
        }

        private async Task AbrirAsync(
            InspeccionFitosanitariaBandejaItemV2? item)
        {
            if (item == null ||
                MostrandoDisponibles ||
                IsBusy || CargandoMas || cambiandoVista)
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

        private void ActualizarComandos()
        {
            ActualizarCommand.ChangeCanExecute();
            CargarMasCommand.ChangeCanExecute();
            AbrirCommand.ChangeCanExecute();
            TomarCommand.ChangeCanExecute();
            VerMisCommand.ChangeCanExecute();
            VerDisponiblesCommand.ChangeCanExecute();
            VerRevisadasCommand.ChangeCanExecute();
        }
    }
}
