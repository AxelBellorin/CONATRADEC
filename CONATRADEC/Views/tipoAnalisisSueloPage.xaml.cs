using CONATRADEC.Services;
using CONATRADEC.ViewModels;

namespace CONATRADEC.Views
{
    public partial class tipoAnalisisSueloPage : ContentPage
    {
        private readonly TipoAnalisisSueloViewModel
            viewModel = new();

        private int cantidadColumnasActual;
        private bool navegacionShellSuscrita;
        private bool salidaExternaPendiente;

        public tipoAnalisisSueloPage()
        {
            InitializeComponent();

            Shell.Current.FlyoutBehavior =
                FlyoutBehavior.Disabled;

            BindingContext =
                viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            salidaExternaPendiente =
                false;

            SuscribirNavegacionShell();

            viewModel.ActualizarPermisos();
            AjustarDiseno(Width);

            if (!viewModel.CanView)
                return;

            bool nuevaVisita =
                TipoAnalisisSueloListadoEstadoService
                    .AsegurarVisita();

            if (nuevaVisita)
            {
                await viewModel
                    .IniciarNuevaVisitaAsync();

                return;
            }

            /*
             * Crear, Ver, Editar y el modal de Eliminados pertenecen a la misma
             * visita. El ViewModel conserva filtros/página y solo recarga cuando
             * la versión del catálogo cambió por una operación confirmada.
             */
            await viewModel.InicializarAsync();
        }

        protected override void OnDisappearing()
        {
            if (salidaExternaPendiente)
            {
                viewModel.FinalizarVisita();
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

        private void AjustarDiseno(
            double width)
        {
            if (width <= 0)
                return;

            AjustarPaddingContenido(width);
            AjustarCantidadColumnas(width);
            AjustarPaginacion(width);
        }

        private void AjustarCantidadColumnas(
            double width)
        {
            if (TiposAnalisisGridLayout == null)
                return;

            double anchoUtil =
                ObtenerAnchoUtilListado(width);

            int nuevasColumnas =
                anchoUtil >= 1150
                    ? 3
                    : anchoUtil >= 680
                        ? 2
                        : 1;

            if (cantidadColumnasActual ==
                nuevasColumnas)
            {
                return;
            }

            cantidadColumnasActual =
                nuevasColumnas;

            TiposAnalisisGridLayout.Span =
                nuevasColumnas;
        }

        private void AjustarPaddingContenido(
            double width)
        {
            if (ContenidoTiposAnalisis == null)
                return;

            /*
             * Una ventana WinUI estrecha continúa reportándose como Desktop.
             * El padding responde al ancho real para conservar espacio útil.
             */
            ContenidoTiposAnalisis.Padding =
                width < 600
                    ? new Thickness(14, 14, 14, 22)
                    : width < 900
                        ? new Thickness(20, 18, 20, 26)
                        : new Thickness(26, 22, 26, 28);
        }

        private void AjustarPaginacion(
            double width)
        {
            if (PaginacionTiposAnalisis == null)
                return;

            double anchoDisponible =
                ObtenerAnchoUtilListado(width);

            PaginacionTiposAnalisis.WidthRequest =
                Math.Min(
                    560,
                    Math.Max(0, anchoDisponible));
        }

        private static double ObtenerAnchoUtilListado(
            double width)
        {
            double paddingHorizontal =
                width < 600
                    ? 28
                    : width < 900
                        ? 40
                        : 52;

            return Math.Max(
                0,
                width - paddingHorizontal);
        }

        /// <summary>
        /// Después de cambiar de página espera a que termine la consulta y
        /// posiciona el primer registro de la nueva página al inicio visible.
        /// El Command continúa siendo el único responsable de la paginación.
        /// </summary>
        private async void PaginacionTiposAnalisis_Clicked(
            object? sender,
            EventArgs e)
        {
            int paginaAnterior =
                viewModel.PaginaActual;

            bool operacionDetectada =
                false;

            for (int intento = 0;
                 intento < 240;
                 intento++)
            {
                if (viewModel.IsBusy ||
                    viewModel.PaginaActual != paginaAnterior)
                {
                    operacionDetectada =
                        true;
                }

                if (operacionDetectada &&
                    !viewModel.IsBusy)
                {
                    if (viewModel.PaginaActual != paginaAnterior &&
                        viewModel.List.Count > 0)
                    {
                        await DesplazarListadoAlInicioAsync();
                    }

                    return;
                }

                await Task.Delay(50);
            }
        }

        private async Task DesplazarListadoAlInicioAsync()
        {
            if (TiposAnalisisCollectionView == null ||
                viewModel.List.Count == 0)
            {
                return;
            }

            // Permite que CollectionView materialice la página nueva.
            await Task.Delay(60);

            TiposAnalisisCollectionView.ScrollTo(
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

            navegacionShellSuscrita =
                true;
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

            navegacionShellSuscrita =
                false;
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
             * El formulario pertenece al flujo interno. El modal genérico de
             * Eliminados no navega por Shell, por lo que tampoco finaliza visita.
             */
            salidaExternaPendiente =
                !EsRutaInternaTipoAnalisis(
                    rutaDestino);
        }

        private static bool EsRutaInternaTipoAnalisis(
            string ruta) =>
            ruta.Contains(
                "tipoAnalisisSuelo",
                StringComparison.OrdinalIgnoreCase) ||
            ruta.Contains(
                "TiposAnalisisSuelo",
                StringComparison.OrdinalIgnoreCase);
    }
}
