using CONATRADEC.Services;
using CONATRADEC.ViewModels;
using System.Collections.Specialized;
using System.Threading;

namespace CONATRADEC.Views
{
    public partial class fuenteNutrientePage : ContentPage
    {
        private const double AnchoMinimoTarjeta = 360;
        private const double EspaciadoTarjetas = 12;

        private readonly FuenteNutrienteViewModel
            viewModel = new();

        private int cantidadColumnasActual;
        private bool accionesFiltroCompactas;
        private bool navegacionShellSuscrita;
        private bool salidaExternaPendiente;
        private int desplazamientoEnCurso;

        public fuenteNutrientePage()
        {
            InitializeComponent();

            Shell.Current.FlyoutBehavior =
                FlyoutBehavior.Disabled;

            BindingContext = viewModel;

            viewModel.List.CollectionChanged +=
                OnListaCollectionChanged;

            viewModel.SolicitarDesplazamientoInicio +=
                OnSolicitarDesplazamientoInicio;

            FuentesCollectionView.SizeChanged +=
                OnListadoSizeChanged;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            salidaExternaPendiente = false;
            SuscribirNavegacionShell();

            viewModel.ActualizarPermisos();
            AjustarDiseno(Width, Height);

            if (!viewModel.CanView)
                return;

            bool nuevaVisita =
                FuenteNutrienteListadoEstadoService
                    .AsegurarVisita();

            if (nuevaVisita)
            {
                await viewModel.IniciarNuevaVisitaAsync();
                return;
            }

            await viewModel.InicializarAsync();
        }

        protected override void OnDisappearing()
        {
            if (salidaExternaPendiente)
            {
                FuenteNutrienteListadoEstadoService
                    .FinalizarVisita();

                salidaExternaPendiente = false;
            }

            DesuscribirNavegacionShell();
            viewModel.CancelarCargas();

            base.OnDisappearing();
        }

        protected override void OnSizeAllocated(
            double width,
            double height)
        {
            base.OnSizeAllocated(width, height);
            AjustarDiseno(width, height);
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
            AjustarDiseno(Width, Height);
        }

        private void AjustarDiseno(
            double width,
            double height)
        {
            if (width <= 0)
                return;

            AjustarPaddingContenido(width);
            AjustarCantidadColumnas(width);
            AjustarAccionesFiltro(width);
            AjustarPaginacion(width);
            AjustarMatrizComposicion(width, height);
        }

        private void AjustarPaddingContenido(
            double width)
        {
            if (ContenidoFuentes == null)
                return;

            ContenidoFuentes.Padding =
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
                FuentesGridLayout == null)
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
                FuentesGridLayout.Span == nuevasColumnas)
            {
                return;
            }

            cantidadColumnasActual = nuevasColumnas;
            FuentesGridLayout.Span = nuevasColumnas;
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
            if (FuentesCollectionView?.Width > 0)
                return FuentesCollectionView.Width;

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

        private void AjustarAccionesFiltro(
            double width)
        {
            if (width <= 0 ||
                FuenteFiltroAccionesGrid == null)
            {
                return;
            }

            bool compacto = width < 600;

            if (accionesFiltroCompactas == compacto &&
                FuenteFiltroAccionesGrid.RowDefinitions.Count > 0)
            {
                return;
            }

            accionesFiltroCompactas = compacto;

            FuenteFiltroAccionesGrid.ColumnDefinitions.Clear();
            FuenteFiltroAccionesGrid.RowDefinitions.Clear();

            if (compacto)
            {
                FuenteFiltroAccionesGrid.ColumnDefinitions.Add(
                    new ColumnDefinition(GridLength.Star));
                FuenteFiltroAccionesGrid.ColumnDefinitions.Add(
                    new ColumnDefinition(GridLength.Star));
                FuenteFiltroAccionesGrid.RowDefinitions.Add(
                    new RowDefinition(GridLength.Auto));
                FuenteFiltroAccionesGrid.RowDefinitions.Add(
                    new RowDefinition(GridLength.Auto));

                Grid.SetRow(ResumenFuentesLabel, 0);
                Grid.SetColumn(ResumenFuentesLabel, 0);
                Grid.SetColumnSpan(ResumenFuentesLabel, 2);

                Grid.SetRow(BuscarFuentesButton, 1);
                Grid.SetColumn(BuscarFuentesButton, 0);
                Grid.SetColumnSpan(BuscarFuentesButton, 1);

                Grid.SetRow(LimpiarFuentesButton, 1);
                Grid.SetColumn(LimpiarFuentesButton, 1);
                Grid.SetColumnSpan(LimpiarFuentesButton, 1);

                return;
            }

            FuenteFiltroAccionesGrid.ColumnDefinitions.Add(
                new ColumnDefinition(GridLength.Star));
            FuenteFiltroAccionesGrid.ColumnDefinitions.Add(
                new ColumnDefinition(GridLength.Auto));
            FuenteFiltroAccionesGrid.ColumnDefinitions.Add(
                new ColumnDefinition(GridLength.Auto));
            FuenteFiltroAccionesGrid.RowDefinitions.Add(
                new RowDefinition(GridLength.Auto));

            Grid.SetRow(ResumenFuentesLabel, 0);
            Grid.SetColumn(ResumenFuentesLabel, 0);
            Grid.SetColumnSpan(ResumenFuentesLabel, 1);

            Grid.SetRow(BuscarFuentesButton, 0);
            Grid.SetColumn(BuscarFuentesButton, 1);
            Grid.SetColumnSpan(BuscarFuentesButton, 1);

            Grid.SetRow(LimpiarFuentesButton, 0);
            Grid.SetColumn(LimpiarFuentesButton, 2);
            Grid.SetColumnSpan(LimpiarFuentesButton, 1);
        }

        private void AjustarPaginacion(
            double width)
        {
            if (PaginacionFuentes == null)
                return;

            PaginacionFuentes.WidthRequest =
                Math.Min(
                    560,
                    Math.Max(
                        0,
                        ObtenerAnchoUtilListado(width)));
        }

        /// <summary>
        /// Limita la altura visible de la matriz para que sus filas puedan
        /// desplazarse internamente sin extender indefinidamente el Header del
        /// CollectionView. El cálculo usa el tamaño real de la ventana, no el
        /// DeviceIdiom, por lo que también responde a ventanas WinUI estrechas.
        /// </summary>
        private void AjustarMatrizComposicion(
            double width,
            double height)
        {
            if (MatrizComposicionScrollView == null ||
                width <= 0 ||
                height <= 0)
            {
                return;
            }

            double alturaMaxima =
                width < 600
                    ? Math.Clamp(
                        height * 0.48,
                        260,
                        380)
                    : width < 900
                        ? Math.Clamp(
                            height * 0.52,
                            300,
                            440)
                        : Math.Clamp(
                            height * 0.62,
                            340,
                            520);

            MatrizComposicionScrollView.MaximumHeightRequest =
                alturaMaxima;
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
                if (FuentesCollectionView == null ||
                    viewModel.List.Count == 0)
                {
                    return;
                }

                await Task.Delay(60);

                FuentesCollectionView.ScrollTo(
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
                !EsRutaInternaFuentes(
                    rutaDestino);
        }

        private static bool EsRutaInternaFuentes(
            string ruta) =>
            ruta.Contains(
                "FuenteNutrientePage",
                StringComparison.OrdinalIgnoreCase) ||
            ruta.Contains(
                "FuenteNutrienteFormPage",
                StringComparison.OrdinalIgnoreCase) ||
            ruta.Contains(
                "FuenteNutrienteEliminadasPage",
                StringComparison.OrdinalIgnoreCase);
    }
}
