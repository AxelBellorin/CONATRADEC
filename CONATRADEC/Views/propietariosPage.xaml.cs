using CONATRADEC.Services;
using CONATRADEC.ViewModels;

namespace CONATRADEC.Views
{
    public partial class propietariosPage : ContentPage
    {
        private const double AnchoMinimoTarjeta = 420;
        private const double EspaciadoTarjetas = 12;

        private readonly PropietariosViewModel viewModel = new();

        private int columnasActuales;
        private bool navegacionShellSuscrita;
        private bool salidaExternaPendiente;
        private int paginaAntesCambio = -1;

        public propietariosPage()
        {
            InitializeComponent();

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

            BindingContext = viewModel;

            viewModel.Propietarios.CollectionChanged +=
                (_, _) => AjustarColumnas(Width);

            PropietariosCollectionView.SizeChanged +=
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

            if (!viewModel.PuedeConsultarListado)
                return;

            bool nuevaVisita =
                PropietarioVisitaService.AsegurarVisita(
                    viewModel.EsModoSeleccion);

            if (nuevaVisita)
            {
                await viewModel.IniciarNuevaVisitaAsync();
                return;
            }

            if (PropietarioVisitaService
                .ConsumirRecargaListado(
                    viewModel.EsModoSeleccion))
            {
                await viewModel.RecargarPaginaActualAsync();
                return;
            }

            if (!viewModel.TienePaginaCargada)
                await viewModel.InicializarAsync();
        }

        protected override void OnDisappearing()
        {
            /*
             * Formulario y Terrenos del propietario son pantallas internas.
             * Al navegar a otro módulo se cierra la visita para que el próximo
             * ingreso comience en página 1 con datos actuales del servidor.
             */
            if (salidaExternaPendiente)
            {
                PropietarioVisitaService.FinalizarVisita(
                    viewModel.EsModoSeleccion);

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
            base.OnSizeAllocated(
                width,
                height);

            AjustarDiseno(width);
        }

        protected override bool OnBackButtonPressed()
        {
            if (viewModel.RegresarCommand.CanExecute(null))
                viewModel.RegresarCommand.Execute(null);

            return true;
        }

        private void AjustarDiseno(
            double width)
        {
            if (width <= 0)
                return;

            AjustarPaddingContenido(width);
            AjustarColumnas(width);
            AjustarPaginacion(width);
        }

        private void AjustarPaddingContenido(
            double width)
        {
            if (ContenidoPropietarios == null)
                return;

            ContenidoPropietarios.Padding =
                width < 600
                    ? new Thickness(12, 12, 12, 20)
                    : width < 900
                        ? new Thickness(18, 16, 18, 24)
                        : new Thickness(24, 20, 24, 28);
        }

        private void AjustarColumnas(
            double width)
        {
            if (PropietariosGridLayout == null)
                return;

            double anchoUtil =
                ObtenerAnchoUtilListado(width);

            int columnas =
                viewModel.Propietarios.Count == 0
                    ? 1
                    : CalcularColumnas(anchoUtil);

            if (columnasActuales == columnas &&
                PropietariosGridLayout.Span == columnas)
            {
                return;
            }

            columnasActuales = columnas;
            PropietariosGridLayout.Span = columnas;
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

        private void AjustarPaginacion(
            double width)
        {
            if (PaginacionPropietarios == null)
                return;

            double anchoDisponible =
                ObtenerAnchoUtilListado(width);

            PaginacionPropietarios.WidthRequest =
                Math.Min(
                    560,
                    Math.Max(
                        0,
                        anchoDisponible));
        }

        private static double ObtenerAnchoUtilListado(
            double width)
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

        private void PaginacionPropietarios_Pressed(
            object? sender,
            EventArgs e)
        {
            paginaAntesCambio =
                viewModel.PaginaActual;
        }

        /// <summary>
        /// Regla común del proyecto: cambiar página, terminar la consulta y
        /// mostrar el primer registro de la nueva página al inicio del listado.
        /// </summary>
        private async void PaginacionPropietarios_Clicked(
            object? sender,
            EventArgs e)
        {
            int paginaOrigen =
                paginaAntesCambio > 0
                    ? paginaAntesCambio
                    : viewModel.PaginaActual;

            bool operacionDetectada = false;

            for (int intento = 0;
                 intento < 240;
                 intento++)
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
                        viewModel.Propietarios.Count > 0)
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
            if (PropietariosCollectionView == null ||
                viewModel.Propietarios.Count == 0)
            {
                return;
            }

            await Task.Delay(60);

            PropietariosCollectionView.ScrollTo(
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

            if (string.IsNullOrWhiteSpace(
                    rutaDestino))
            {
                return;
            }

            /*
             * El modal de Propietarios eliminados no utiliza navegación Shell.
             * Formulario y Terrenos del propietario pertenecen a la misma visita.
             */
            salidaExternaPendiente =
                !EsRutaInternaPropietarios(
                    rutaDestino);
        }

        private static bool EsRutaInternaPropietarios(
            string ruta) =>
            ruta.Contains(
                "propietariosPage",
                StringComparison.OrdinalIgnoreCase) ||
            ruta.Contains(
                "propietarioFormPage",
                StringComparison.OrdinalIgnoreCase) ||
            ruta.Contains(
                "propietarioTerrenosPage",
                StringComparison.OrdinalIgnoreCase);
    }
}
