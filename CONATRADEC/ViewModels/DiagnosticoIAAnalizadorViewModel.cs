using CONATRADEC.Models;
using CONATRADEC.Services;
using System.Collections.ObjectModel;

namespace CONATRADEC.ViewModels
{
    public sealed class DiagnosticoIAAnalizadorViewModel :
        DiagnosticoIAViewModelBase
    {
        private readonly InspeccionFitosanitariaBandejaApiService filtrosApi =
            InspeccionFitosanitariaBandejaApiService.Instance;
        private readonly InspeccionFitosanitariaBandejaOperativaApiService api =
            new();

        private bool catalogoTecnicosCargado;
        private TecnicoInspeccionFiltroItem? tecnicoSeleccionado;

        public DiagnosticoIAAnalizadorViewModel()
        {
            ActualizarCommand = new Command(
                async () => await ActualizarAsync(),
                () => !IsBusy);

            AbrirCommand = new Command<InspeccionFitosanitariaListaItemV2>(
                async item => await AbrirAsync(item),
                item => item != null && !IsBusy);
        }

        public ObservableCollection<InspeccionFitosanitariaListaItemV2>
            Solicitudes { get; } = [];

        public ObservableCollection<TecnicoInspeccionFiltroItem>
            TecnicosFiltro { get; } = [];

        public Command ActualizarCommand { get; }
        public Command<InspeccionFitosanitariaListaItemV2> AbrirCommand { get; }

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
                    _ = CargarAsync();
            }
        }

        public string TecnicoFiltroTexto =>
            TecnicoSeleccionado?.TextoMostrar ?? "Todos los técnicos";

        public bool SinSolicitudes =>
            !IsBusy && Solicitudes.Count == 0;

        public async Task InicializarAsync()
        {
            await CargarTecnicosAsync();
            await CargarAsync();
        }

        private async Task ActualizarAsync()
        {
            await CargarTecnicosAsync(forzar: true);
            await CargarAsync();
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

        private async Task CargarAsync()
        {
            if (IsBusy || !ValidarEnLinea(false))
                return;

            IsBusy = true;
            MensajeEstado =
                "Cargando fotografías pendientes del analizador...";
            ActualizarComandos();

            try
            {
                List<InspeccionFitosanitariaListaItemV2> items =
                    await api.ObtenerAsync(
                        DiagnosticoIARoutes.ModoAnalizador,
                        TecnicoSeleccionado?.UsuarioTecnicoId is > 0
                            ? TecnicoSeleccionado.UsuarioTecnicoId
                            : null);

                Solicitudes.Clear();
                foreach (InspeccionFitosanitariaListaItemV2 item in items)
                    Solicitudes.Add(item);

                OnPropertyChanged(nameof(SinSolicitudes));
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
                ActualizarComandos();
            }
        }

        private async Task AbrirAsync(
            InspeccionFitosanitariaListaItemV2? item)
        {
            if (item == null || IsBusy)
                return;

            await GoToAsyncParameters(
                DiagnosticoIARoutes.CrearRutaResultado(
                    item.InspeccionId,
                    DiagnosticoIARoutes.ModoAnalizador));
        }

        private void ActualizarComandos()
        {
            ActualizarCommand.ChangeCanExecute();
            AbrirCommand.ChangeCanExecute();
        }
    }
}
