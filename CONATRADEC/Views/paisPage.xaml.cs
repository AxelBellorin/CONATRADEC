using CONATRADEC.Services;
using CONATRADEC.ViewModels;

namespace CONATRADEC.Views
{
    public partial class paisPage : ContentPage
    {
        private const double AnchoMinimoTarjeta = 380;
        private const double EspaciadoTarjetas = 12;

        private readonly PaisViewModel viewModel = new();
        private int columnasActuales;
        private bool navegacionShellSuscrita;
        private bool salidaExternaPendiente;
        private int paginaAntesCambio = -1;

        public paisPage()
        {
            InitializeComponent();
            BindingContext = viewModel;
            Shell.Current.FlyoutBehavior = FlyoutBehavior.Disabled;

            viewModel.List.CollectionChanged +=
                (_, _) => AjustarDiseno(Width);
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            salidaExternaPendiente = false;
            SuscribirNavegacionShell();

            viewModel.ActualizarPermisos();
            AjustarDiseno(Width);

            if (!viewModel.CanView)
                return;

            bool nuevaVisita = UbicacionVisitaService.AsegurarVisita();

            if (nuevaVisita)
            {
                await viewModel.IniciarNuevaVisitaAsync();
                return;
            }

            if (UbicacionVisitaService.ConsumirRecargaPaises())
            {
                await viewModel.RecargarPaginaActualAsync();
                return;
            }

            bool requiereGet = viewModel.AplicarCambiosPendientes();

            if (requiereGet)
            {
                await viewModel.RecargarPaginaActualAsync();
                return;
            }

            if (!viewModel.TienePaginaCargada)
                await viewModel.InicializarAsync();
        }

        protected override void OnDisappearing()
        {
            if (salidaExternaPendiente)
            {
                UbicacionVisitaService.FinalizarVisita();
                salidaExternaPendiente = false;
            }

            DesuscribirNavegacionShell();
            viewModel.CancelarCarga();
            base.OnDisappearing();
        }

        protected override void OnSizeAllocated(double width, double height)
        {
            base.OnSizeAllocated(width, height);
            AjustarDiseno(width);
        }

        protected override bool OnBackButtonPressed()
        {
            if (viewModel.RegresarConfiguracionCommand.CanExecute(null))
                viewModel.RegresarConfiguracionCommand.Execute(null);

            return true;
        }

        private void AjustarDiseno(double width)
        {
            if (width <= 0)
                return;

            AjustarPadding(width);
            AjustarColumnas(width);
            AjustarPaginacion(width);
        }

        private void AjustarPadding(double width)
        {
            if (ContenidoPaises == null)
                return;

            ContenidoPaises.Padding = width < 600
                ? new Thickness(12, 12, 12, 20)
                : width < 900
                    ? new Thickness(18, 16, 18, 24)
                    : new Thickness(24, 20, 24, 28);
        }

        private void AjustarColumnas(double width)
        {
            if (PaisesGridLayout == null)
                return;

            double anchoUtil = ObtenerAnchoUtil(width);
            int columnas = viewModel.List.Count == 0
                ? 1
                : CalcularColumnas(anchoUtil);

            if (columnasActuales == columnas &&
                PaisesGridLayout.Span == columnas)
            {
                return;
            }

            columnasActuales = columnas;
            PaisesGridLayout.Span = columnas;
        }

        private static int CalcularColumnas(double anchoUtil)
        {
            double tres =
                (AnchoMinimoTarjeta * 3) +
                (EspaciadoTarjetas * 2);

            if (anchoUtil >= tres)
                return 3;

            double dos =
                (AnchoMinimoTarjeta * 2) +
                EspaciadoTarjetas;

            return anchoUtil >= dos ? 2 : 1;
        }

        private void AjustarPaginacion(double width)
        {
            if (PaginacionPaises == null)
                return;

            PaginacionPaises.WidthRequest = Math.Min(
                560,
                Math.Max(0, ObtenerAnchoUtil(width)));
        }

        private static double ObtenerAnchoUtil(double width)
        {
            double paddingHorizontal = width < 600
                ? 24
                : width < 900
                    ? 36
                    : 48;

            return Math.Max(0, width - paddingHorizontal);
        }

        private void PaginacionPaises_Pressed(object? sender, EventArgs e)
        {
            paginaAntesCambio = viewModel.PaginaActual;
        }

        private async void PaginacionPaises_Clicked(object? sender, EventArgs e)
        {
            int paginaOrigen = paginaAntesCambio > 0
                ? paginaAntesCambio
                : viewModel.PaginaActual;

            bool operacionDetectada = false;

            for (int intento = 0; intento < 240; intento++)
            {
                if (viewModel.IsBusy ||
                    viewModel.PaginaActual != paginaOrigen)
                {
                    operacionDetectada = true;
                }

                if (operacionDetectada && !viewModel.IsBusy)
                {
                    if (viewModel.PaginaActual != paginaOrigen &&
                        viewModel.List.Count > 0)
                    {
                        await DesplazarAlInicioAsync();
                    }

                    paginaAntesCambio = -1;
                    return;
                }

                await Task.Delay(50);
            }

            paginaAntesCambio = -1;
        }

        private async Task DesplazarAlInicioAsync()
        {
            if (PaisesCollectionView == null || viewModel.List.Count == 0)
                return;

            await Task.Delay(60);
            PaisesCollectionView.ScrollTo(
                0,
                position: ScrollToPosition.Start,
                animate: false);
        }

        private void SuscribirNavegacionShell()
        {
            if (navegacionShellSuscrita || Shell.Current == null)
                return;

            Shell.Current.Navigating += Shell_Navigating;
            navegacionShellSuscrita = true;
        }

        private void DesuscribirNavegacionShell()
        {
            if (!navegacionShellSuscrita || Shell.Current == null)
                return;

            Shell.Current.Navigating -= Shell_Navigating;
            navegacionShellSuscrita = false;
        }

        private void Shell_Navigating(
            object? sender,
            ShellNavigatingEventArgs e)
        {
            string ruta = e.Target?.Location?.OriginalString ?? string.Empty;
            if (string.IsNullOrWhiteSpace(ruta))
                return;

            salidaExternaPendiente = !EsRutaInternaUbicaciones(ruta);
        }

        internal static bool EsRutaInternaUbicaciones(string ruta) =>
            ruta.Contains("PaisPage", StringComparison.OrdinalIgnoreCase) ||
            ruta.Contains("PaisFormPage", StringComparison.OrdinalIgnoreCase) ||
            ruta.Contains("DepartamentoPage", StringComparison.OrdinalIgnoreCase) ||
            ruta.Contains("DepartamentoFormPage", StringComparison.OrdinalIgnoreCase) ||
            ruta.Contains("MunicipioPage", StringComparison.OrdinalIgnoreCase) ||
            ruta.Contains("MunicipioFormPage", StringComparison.OrdinalIgnoreCase);
    }
}
