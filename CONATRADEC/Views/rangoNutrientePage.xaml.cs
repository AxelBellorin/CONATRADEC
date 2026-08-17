using CONATRADEC.Services;
using CONATRADEC.ViewModels;

namespace CONATRADEC.Views
{
    public partial class rangoNutrientePage : ContentPage
    {
        private readonly RangoNutrienteViewModel
            viewModel = new();

        private int cantidadColumnasActual;
        private bool navegacionShellSuscrita;
        private bool salidaExternaPendiente;

        public rangoNutrientePage()
        {
            InitializeComponent();

            Shell.Current.FlyoutBehavior =
                FlyoutBehavior.Disabled;

            BindingContext = viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            salidaExternaPendiente = false;
            SuscribirNavegacionShell();

            viewModel.ActualizarPermisos();
            AjustarCantidadColumnas(Width);

            if (!viewModel.CanView)
                return;

            bool nuevaVisita =
                RangoNutrienteVisitaService.AsegurarVisita();

            if (nuevaVisita)
            {
                await viewModel.IniciarNuevaVisitaAsync();
                return;
            }

            /*
             * Detalle y formularios pertenecen a la misma visita. Solo se
             * consulta nuevamente cuando una operación confirmada cambió la
             * composición o el orden del listado principal.
             */
            if (RangoNutrienteVisitaService
                .ConsumirRecargaListadoPrincipal())
            {
                await viewModel.RecargarVentanaActualAsync();
                return;
            }

            if (!viewModel.TienePaginaCargada)
                await viewModel.InicializarAsync();
        }

        protected override void OnDisappearing()
        {
            if (salidaExternaPendiente)
            {
                RangoNutrienteVisitaService.FinalizarVisita();
                salidaExternaPendiente = false;
            }

            DesuscribirNavegacionShell();
            viewModel.CancelarCarga();
            base.OnDisappearing();
        }

        protected override void OnSizeAllocated(
            double width,
            double height)
        {
            base.OnSizeAllocated(width, height);
            AjustarCantidadColumnas(width);
        }

        private void AjustarCantidadColumnas(double width)
        {
            if (width <= 0 ||
                CultivosGridLayout == null)
            {
                return;
            }

            int nuevasColumnas =
                width >= 1200
                    ? 3
                    : width >= 700
                        ? 2
                        : 1;

            if (cantidadColumnasActual == nuevasColumnas)
                return;

            cantidadColumnasActual = nuevasColumnas;
            CultivosGridLayout.Span = nuevasColumnas;
        }

        private void SuscribirNavegacionShell()
        {
            if (navegacionShellSuscrita ||
                Shell.Current == null)
            {
                return;
            }

            Shell.Current.Navigating += Shell_Navigating;
            navegacionShellSuscrita = true;
        }

        private void DesuscribirNavegacionShell()
        {
            if (!navegacionShellSuscrita ||
                Shell.Current == null)
            {
                return;
            }

            Shell.Current.Navigating -= Shell_Navigating;
            navegacionShellSuscrita = false;
        }

        private void Shell_Navigating(
            object? sender,
            ShellNavigatingEventArgs e)
        {
            string rutaDestino =
                e.Target?
                    .Location?
                    .OriginalString ??
                string.Empty;

            if (string.IsNullOrWhiteSpace(rutaDestino))
                return;

            salidaExternaPendiente =
                !EsRutaInternaRangos(rutaDestino);
        }

        private static bool EsRutaInternaRangos(
            string ruta)
        {
            return
                ruta.Contains(
                    "RangoNutrientePage",
                    StringComparison.OrdinalIgnoreCase) ||
                ruta.Contains(
                    nameof(rangoNutrienteDetallePage),
                    StringComparison.OrdinalIgnoreCase) ||
                ruta.Contains(
                    nameof(rangoNutrienteCategoriaFormPage),
                    StringComparison.OrdinalIgnoreCase) ||
                ruta.Contains(
                    "RangoNutrienteAporteFormulario",
                    StringComparison.OrdinalIgnoreCase) ||
                ruta.Contains(
                    nameof(rangoNutrienteFormPage),
                    StringComparison.OrdinalIgnoreCase);
        }
    }
}
