using CONATRADEC.Services;
using CONATRADEC.ViewModels;
using System.Collections.Specialized;
using System.Threading;

namespace CONATRADEC.Views
{
    public partial class extraccionNutrientePage : ContentPage
    {
        private const double AnchoMinimoTarjeta = 360;
        private const double EspaciadoTarjetas = 12;

        private readonly ExtraccionNutrienteViewModel
            viewModel = new();

        private int cantidadColumnasActual;
        private bool navegacionShellSuscrita;
        private bool salidaExternaPendiente;
        private int desplazamientoEnCurso;

        public extraccionNutrientePage()
        {
            InitializeComponent();

            Shell.Current.FlyoutBehavior =
                FlyoutBehavior.Disabled;

            BindingContext = viewModel;

            viewModel.List.CollectionChanged +=
                OnListaCollectionChanged;

            viewModel.SolicitarDesplazamientoInicio +=
                OnSolicitarDesplazamientoInicio;

            ExtraccionesCollectionView.SizeChanged +=
                OnListadoSizeChanged;
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

            bool nuevaVisita =
                ExtraccionNutrienteListadoEstadoService
                    .AsegurarVisita();

            if (nuevaVisita)
            {
                await viewModel
                    .IniciarNuevaVisitaAsync();
                return;
            }

            await viewModel.InicializarAsync();
        }

        protected override void OnDisappearing()
        {
            /*
             * Crear, Ver, Editar y Eliminados pertenecen a la misma visita.
             * Solo una navegación real hacia otro módulo finaliza el estado.
             */
            if (salidaExternaPendiente)
            {
                ExtraccionNutrienteListadoEstadoService
                    .FinalizarVisita();

                viewModel.FinalizarVisita();
                salidaExternaPendiente = false;
            }
            else
            {
                viewModel.CancelarCarga();
            }

            DesuscribirNavegacionShell();
            base.OnDisappearing();
        }

        protected override void OnSizeAllocated(
            double width,
            double height)
        {
            base.OnSizeAllocated(width, height);
            AjustarDiseno(width);
        }

        private void OnListaCollectionChanged(
            object? sender,
            NotifyCollectionChangedEventArgs e)
        {
            AjustarCantidadColumnas(Width);
        }

        private void OnListadoSizeChanged(
            object? sender,
            EventArgs e)
        {
            AjustarDiseno(Width);
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

        private void AjustarPaddingContenido(
            double width)
        {
            if (ContenidoExtracciones == null)
                return;

            ContenidoExtracciones.Padding =
                width < 600
                    ? new Thickness(12, 12, 12, 20)
                    : width < 900
                        ? new Thickness(18, 16, 18, 24)
                        : new Thickness(24, 20, 24, 28);
        }

        private void AjustarCantidadColumnas(
            double width)
        {
            if (width <= 0 ||
                ExtraccionesGridLayout == null)
            {
                return;
            }

            double anchoUtil =
                ObtenerAnchoUtilListado(width);

            int nuevasColumnas =
                viewModel.List.Count == 0
                    ? 1
                    : CalcularColumnas(anchoUtil);

            if (cantidadColumnasActual == nuevasColumnas &&
                ExtraccionesGridLayout.Span == nuevasColumnas)
            {
                return;
            }

            cantidadColumnasActual = nuevasColumnas;
            ExtraccionesGridLayout.Span = nuevasColumnas;
        }

        private static int CalcularColumnas(
            double anchoUtil)
        {
            if (anchoUtil <= 0)
                return 1;

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

        private double ObtenerAnchoUtilListado(
            double width)
        {
            if (ExtraccionesCollectionView?.Width > 0)
                return ExtraccionesCollectionView.Width;

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

        private void AjustarPaginacion(
            double width)
        {
            if (PaginacionExtracciones == null)
                return;

            double anchoDisponible =
                ObtenerAnchoUtilListado(width);

            PaginacionExtracciones.WidthRequest =
                Math.Min(
                    560,
                    Math.Max(0, anchoDisponible));
        }

        private async void OnSolicitarDesplazamientoInicio(
            object? sender,
            EventArgs e)
        {
            if (Interlocked.CompareExchange(
                    ref desplazamientoEnCurso,
                    1,
                    0) != 0)
            {
                return;
            }

            try
            {
                if (ExtraccionesCollectionView == null ||
                    viewModel.List.Count == 0)
                {
                    return;
                }

                // Permite que CollectionView materialice la página recibida.
                await Task.Delay(60);

                ExtraccionesCollectionView.ScrollTo(
                    0,
                    position: ScrollToPosition.Start,
                    animate: false);
            }
            finally
            {
                Interlocked.Exchange(
                    ref desplazamientoEnCurso,
                    0);
            }
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
                !EsRutaInternaExtraccion(
                    rutaDestino);
        }

        private static bool EsRutaInternaExtraccion(
            string ruta) =>
            ruta.Contains(
                "ExtraccionNutrientePage",
                StringComparison.OrdinalIgnoreCase) ||
            ruta.Contains(
                "ExtraccionNutrienteFormPage",
                StringComparison.OrdinalIgnoreCase);
    }
}
