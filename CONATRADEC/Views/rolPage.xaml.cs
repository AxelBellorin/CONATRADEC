using CONATRADEC.Services;
using CONATRADEC.ViewModels;

namespace CONATRADEC.Views
{
    public partial class rolPage : ContentPage
    {
        private const double AnchoMinimoTarjeta = 380;
        private const double EspaciadoTarjetas = 12;

        private readonly RolViewModel viewModel = new();

        private int columnasActuales;
        private bool navegacionShellSuscrita;
        private bool salidaExternaPendiente;
        private int paginaAntesCambio = -1;

        public rolPage()
        {
            InitializeComponent();
            BindingContext = viewModel;

            Shell.SetNavBarIsVisible(
                this,
                false);

            Shell.SetBackButtonBehavior(
                this,
                new BackButtonBehavior
                {
                    IsVisible = false,
                    IsEnabled = false
                });

            Shell.Current.FlyoutBehavior =
                FlyoutBehavior.Disabled;

            viewModel.List.CollectionChanged +=
                (_, _) => AjustarColumnas(Width);

            RolesCollectionView.SizeChanged +=
                (_, _) => AjustarDiseno(Width);
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            Shell.SetNavBarIsVisible(
                this,
                false);

            salidaExternaPendiente = false;
            SuscribirNavegacionShell();

            viewModel.ActualizarPermisos();
            AjustarDiseno(Width);

            if (!viewModel.CanView)
                return;

            bool nuevaVisita =
                RolVisitaService.AsegurarVisita();

            if (nuevaVisita)
            {
                await viewModel.IniciarNuevaVisitaAsync();
                return;
            }

            if (RolVisitaService.ConsumirRecargaListado())
            {
                RolVisitaService.DescartarMutacion();
                await viewModel.RecargarPaginaActualAsync();
                return;
            }

            if (RolVisitaService.ConsumirMutacion(
                    out RolMutacionListado mutacion))
            {
                bool requiereGet =
                    viewModel.AplicarMutacionPendiente(
                        mutacion);

                if (requiereGet)
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
                RolVisitaService.FinalizarVisita();
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
            AjustarDiseno(width);
        }

        protected override bool OnBackButtonPressed()
        {
            if (viewModel.RegresarConfiguracionCommand.CanExecute(null))
            {
                viewModel.RegresarConfiguracionCommand.Execute(null);
            }

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
            if (ContenidoRoles == null)
                return;

            ContenidoRoles.Padding =
                width < 600
                    ? new Thickness(12, 12, 12, 20)
                    : width < 900
                        ? new Thickness(18, 16, 18, 24)
                        : new Thickness(24, 20, 24, 28);
        }

        private void AjustarColumnas(double width)
        {
            if (width <= 0 || RolesGridLayout == null)
                return;

            double anchoUtil =
                ObtenerAnchoUtil(width);

            int columnas =
                viewModel.List.Count == 0
                    ? 1
                    : CalcularColumnas(anchoUtil);

            if (columnasActuales == columnas &&
                RolesGridLayout.Span == columnas)
            {
                return;
            }

            columnasActuales = columnas;
            RolesGridLayout.Span = columnas;
        }

        private static int CalcularColumnas(
            double anchoUtil)
        {
            double requeridoTres =
                (AnchoMinimoTarjeta * 3) +
                (EspaciadoTarjetas * 2);

            if (anchoUtil >= requeridoTres)
                return 3;

            double requeridoDos =
                (AnchoMinimoTarjeta * 2) +
                EspaciadoTarjetas;

            return anchoUtil >= requeridoDos
                ? 2
                : 1;
        }

        private void AjustarPaginacion(double width)
        {
            if (PaginacionRoles == null)
                return;

            double anchoDisponible =
                ObtenerAnchoUtil(width);

            PaginacionRoles.WidthRequest =
                Math.Min(
                    560,
                    Math.Max(0, anchoDisponible));
        }

        private static double ObtenerAnchoUtil(double width)
        {
            double paddingHorizontal =
                width < 600
                    ? 24
                    : width < 900
                        ? 36
                        : 48;

            return Math.Max(
                0,
                width - paddingHorizontal);
        }

        private void PaginacionRoles_Pressed(
            object? sender,
            EventArgs e)
        {
            paginaAntesCambio =
                viewModel.PaginaActual;
        }

        /// <summary>
        /// Después de cambiar página espera la respuesta del servidor y lleva
        /// el CollectionView al primer registro de la nueva página.
        /// </summary>
        private async void PaginacionRoles_Clicked(
            object? sender,
            EventArgs e)
        {
            int paginaOrigen =
                paginaAntesCambio > 0
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

                if (operacionDetectada &&
                    !viewModel.IsBusy)
                {
                    if (viewModel.PaginaActual != paginaOrigen &&
                        viewModel.List.Count > 0)
                    {
                        await DesplazarListadoAlInicioAsync();
                    }

                    paginaAntesCambio = -1;
                    return;
                }

                await Task.Delay(50);
            }

            paginaAntesCambio = -1;
        }

        private async Task DesplazarListadoAlInicioAsync()
        {
            if (RolesCollectionView == null ||
                viewModel.List.Count == 0)
            {
                return;
            }

            await Task.Delay(60);

            RolesCollectionView.ScrollTo(
                0,
                position: ScrollToPosition.Start,
                animate: false);
        }

        private void SuscribirNavegacionShell()
        {
            if (navegacionShellSuscrita ||
                Shell.Current == null)
            {
                return;
            }

            Shell.Current.Navigating +=
                Shell_Navigating;

            navegacionShellSuscrita = true;
        }

        private void DesuscribirNavegacionShell()
        {
            if (!navegacionShellSuscrita ||
                Shell.Current == null)
            {
                return;
            }

            Shell.Current.Navigating -=
                Shell_Navigating;

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
                !EsRutaInternaRoles(rutaDestino);
        }

        private static bool EsRutaInternaRoles(
            string ruta) =>
            ruta.Contains(
                "RolPage",
                StringComparison.OrdinalIgnoreCase) ||
            ruta.Contains(
                AppRoutes.RolFormularioInterno,
                StringComparison.OrdinalIgnoreCase) ||
            ruta.Contains(
                "RolFormPage",
                StringComparison.OrdinalIgnoreCase);
    }
}
