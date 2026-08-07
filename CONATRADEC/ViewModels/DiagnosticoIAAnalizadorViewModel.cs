using CONATRADEC.Models;
using CONATRADEC.Services;
using System.Collections.ObjectModel;

namespace CONATRADEC.ViewModels
{
    public sealed class DiagnosticoIAAnalizadorViewModel : DiagnosticoIAViewModelBase
    {
        private const int TamanoPagina = 20;

        private readonly InspeccionFitosanitariaBandejaApiService filtrosApi =
            InspeccionFitosanitariaBandejaApiService.Instance;
        private readonly InspeccionFitosanitariaBandejaOperativaApiService api =
            new();

        private bool catalogoTecnicosCargado;
        private bool cargandoMas;
        private TecnicoInspeccionFiltroItem? tecnicoSeleccionado;
        private DateTime? siguienteFechaUtc;
        private int? siguienteId;
        private bool hayMas;

        public DiagnosticoIAAnalizadorViewModel()
        {
            ActualizarCommand = new Command(
                async () => await ActualizarAsync(),
                () => !IsBusy && !cargandoMas);

            CargarMasCommand = new Command(
                async () => await CargarMasAsync(),
                () => !IsBusy && !cargandoMas && HayMas);

            AbrirCommand = new Command<InspeccionFitosanitariaBandejaItemV2>(
                async item => await AbrirAsync(item),
                item => item != null && !IsBusy && !cargandoMas);
        }

        public ObservableCollection<InspeccionFitosanitariaBandejaItemV2>
            Solicitudes { get; } = [];

        public ObservableCollection<TecnicoInspeccionFiltroItem>
            TecnicosFiltro { get; } = [];

        public Command ActualizarCommand { get; }
        public Command CargarMasCommand { get; }
        public Command<InspeccionFitosanitariaBandejaItemV2> AbrirCommand { get; }

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

                if (catalogoTecnicosCargado && !IsBusy)
                    _ = CargarPrimeraPaginaAsync();
            }
        }

        public string TecnicoFiltroTexto =>
            TecnicoSeleccionado?.TextoMostrar ?? "Todos los técnicos";

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

        public string TextoCargarMas =>
            CargandoMas ? "Cargando..." : "Cargar más inspecciones";

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
                        DiagnosticoIARoutes.ModoAnalizador);

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
            if (!HayMas || CargandoMas || IsBusy)
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
            if ((IsBusy && reemplazar) || !ValidarEnLinea(false))
                return;

            if (reemplazar)
            {
                IsBusy = true;
                MensajeEstado = "Cargando inspecciones pendientes del analizador...";
                ActualizarComandos();
            }

            try
            {
                InspeccionFitosanitariaBandejaPaginaV2 pagina =
                    await api.ObtenerPaginaAsync(
                        DiagnosticoIARoutes.ModoAnalizador,
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

        private async Task AbrirAsync(
            InspeccionFitosanitariaBandejaItemV2? item)
        {
            if (item == null || IsBusy || CargandoMas)
                return;

            await GoToAsyncParameters(
                DiagnosticoIARoutes.CrearRutaResultado(
                    item.InspeccionId,
                    DiagnosticoIARoutes.ModoAnalizador));
        }

        private void ActualizarComandos()
        {
            ActualizarCommand.ChangeCanExecute();
            CargarMasCommand.ChangeCanExecute();
            AbrirCommand.ChangeCanExecute();
        }
    }
}
