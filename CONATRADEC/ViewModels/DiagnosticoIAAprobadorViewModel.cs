using CONATRADEC.Models;
using CONATRADEC.Services;
using System.Collections.ObjectModel;

namespace CONATRADEC.ViewModels
{
    public sealed class DiagnosticoIAAprobadorViewModel : DiagnosticoIAViewModelBase
    {
        private const int TamanoPagina = 20;
        private const string ModoRevisadas = "aprobador-revisadas";

        private readonly InspeccionFitosanitariaBandejaApiService filtrosApi =
            InspeccionFitosanitariaBandejaApiService.Instance;
        private readonly InspeccionFitosanitariaBandejaOperativaApiService api =
            new();

        private bool catalogoTecnicosCargado;
        private bool cargandoMas;
        private bool mostrandoRevisadas;
        private bool cambiandoVista;
        private TecnicoInspeccionFiltroItem? tecnicoSeleccionado;
        private DateTime? siguienteFechaUtc;
        private int? siguienteId;
        private bool hayMas;

        public DiagnosticoIAAprobadorViewModel()
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
                    !IsBusy &&
                    !cargandoMas &&
                    !cambiandoVista);

            VerPendientesCommand = new Command(
                async () => await CambiarVistaAsync(revisadas: false),
                () => MostrandoRevisadas &&
                    !IsBusy &&
                    !cargandoMas &&
                    !cambiandoVista);

            VerRevisadasCommand = new Command(
                async () => await CambiarVistaAsync(revisadas: true),
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

                if (catalogoTecnicosCargado &&
                    !IsBusy &&
                    !MostrandoRevisadas)
                {
                    _ = CargarPrimeraPaginaAsync();
                }
            }
        }

        public string TecnicoFiltroTexto =>
            TecnicoSeleccionado?.TextoMostrar ?? "Todos los técnicos";

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
                OnPropertyChanged(nameof(MostrarFiltroTecnico));
                OnPropertyChanged(nameof(SubtituloVista));
                OnPropertyChanged(nameof(TextoSinSolicitudes));
                OnPropertyChanged(nameof(TextoCargarMas));
                ActualizarComandos();
            }
        }

        public bool MostrandoPendientes => !MostrandoRevisadas;

        public bool MostrarFiltroTecnico => !MostrandoRevisadas;

        public string SubtituloVista => MostrandoRevisadas
            ? "Consulte las inspecciones que ya revisó y administre posteriormente la autorización o publicación de sus fotografías aprobadas."
            : "Apruebe, devuelva, rechace o declare inconclusa cada fotografía pendiente de manera independiente.";

        public string TextoSinSolicitudes => MostrandoRevisadas
            ? "Todavía no hay inspecciones revisadas por este aprobador."
            : "No hay fotografías pendientes de aprobación para el técnico seleccionado.";

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
            if (!MostrandoRevisadas)
                await CargarTecnicosAsync(forzar: true);

            await CargarPrimeraPaginaAsync();
        }

        private async Task CambiarVistaAsync(bool revisadas)
        {
            if (MostrandoRevisadas == revisadas ||
                cambiandoVista ||
                IsBusy ||
                CargandoMas)
            {
                return;
            }

            cambiandoVista = true;
            ActualizarComandos();

            try
            {
                MostrandoRevisadas = revisadas;

                if (revisadas && TecnicosFiltro.Count > 0)
                {
                    tecnicoSeleccionado = TecnicosFiltro[0];
                    OnPropertyChanged(nameof(TecnicoSeleccionado));
                    OnPropertyChanged(nameof(TecnicoFiltroTexto));
                }

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
                    await filtrosApi.ObtenerTecnicosAsync(
                        DiagnosticoIARoutes.ModoAprobador);

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
                cambiandoVista && !reemplazar ||
                !ValidarEnLinea(false))
            {
                return;
            }

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
                    ? ModoRevisadas
                    : DiagnosticoIARoutes.ModoAprobador;

                int? tecnicoId = !MostrandoRevisadas &&
                    TecnicoSeleccionado?.UsuarioTecnicoId is > 0
                        ? TecnicoSeleccionado.UsuarioTecnicoId
                        : null;

                InspeccionFitosanitariaBandejaPaginaV2 pagina =
                    await api.ObtenerPaginaAsync(
                        modo,
                        tecnicoId,
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

        private async Task AbrirAsync(
            InspeccionFitosanitariaBandejaItemV2? item)
        {
            if (item == null || IsBusy || CargandoMas || cambiandoVista)
                return;

            await GoToAsyncParameters(
                DiagnosticoIARoutes.CrearRutaResultado(
                    item.InspeccionId,
                    DiagnosticoIARoutes.ModoAprobador));
        }

        private void ActualizarComandos()
        {
            ActualizarCommand.ChangeCanExecute();
            CargarMasCommand.ChangeCanExecute();
            AbrirCommand.ChangeCanExecute();
            VerPendientesCommand.ChangeCanExecute();
            VerRevisadasCommand.ChangeCanExecute();
        }
    }
}
