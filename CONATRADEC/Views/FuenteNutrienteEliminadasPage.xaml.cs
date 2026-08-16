using CONATRADEC.ViewModels;
using Microsoft.Maui.ApplicationModel;

namespace CONATRADEC.Views
{
    public partial class FuenteNutrienteEliminadasPage : ContentPage
    {
        private readonly FuenteNutrienteEliminadasViewModel
            viewModel = new();

        private int cantidadColumnasActual;
        private bool filtroCompacto;
        private bool paginacionCompacta;

        public FuenteNutrienteEliminadasPage()
        {
            InitializeComponent();
            BindingContext = viewModel;

            viewModel.SolicitarDesplazamientoInicio +=
                OnSolicitarDesplazamientoInicio;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            AjustarDiseno(Width);
            await viewModel.IniciarAsync();
        }

        protected override void OnDisappearing()
        {
            viewModel.CancelarOperaciones();
            base.OnDisappearing();
        }

        /// <summary>
        /// La ventana se cierra únicamente con los controles de la aplicación
        /// en Android para no perder accidentalmente el contexto modal.
        /// </summary>
        protected override bool OnBackButtonPressed()
        {
#if ANDROID
            return true;
#else
            return base.OnBackButtonPressed();
#endif
        }

        protected override void OnSizeAllocated(
            double width,
            double height)
        {
            base.OnSizeAllocated(width, height);
            AjustarDiseno(width);
        }

        private void AjustarDiseno(double width)
        {
            if (width <= 0)
                return;

            if (ContenidoEliminadas != null)
            {
                ContenidoEliminadas.Padding =
                    width < 600
                        ? new Thickness(12, 12, 12, 18)
                        : width < 900
                            ? new Thickness(18, 16, 18, 22)
                            : new Thickness(24, 20, 24, 26);
            }

            AjustarCantidadColumnas(width);
            AjustarFiltro(width);
            AjustarPaginacion(width);
        }

        private void AjustarCantidadColumnas(double width)
        {
            if (FuentesEliminadasGrid == null)
                return;

            double anchoUtil = Math.Max(280, width - 32);
            int nuevasColumnas =
                anchoUtil >= 1180
                    ? 3
                    : anchoUtil >= 720
                        ? 2
                        : 1;

            if (cantidadColumnasActual == nuevasColumnas)
                return;

            cantidadColumnasActual = nuevasColumnas;
            FuentesEliminadasGrid.Span = nuevasColumnas;
        }

        private void AjustarFiltro(double width)
        {
            if (FiltroEliminadasGrid == null ||
                ResumenEliminadas == null ||
                BuscarEliminadasButton == null ||
                LimpiarEliminadasButton == null)
            {
                return;
            }

            bool compacto = width < 600;

            if (filtroCompacto == compacto &&
                FiltroEliminadasGrid.RowDefinitions.Count > 0)
            {
                return;
            }

            filtroCompacto = compacto;
            FiltroEliminadasGrid.ColumnDefinitions.Clear();
            FiltroEliminadasGrid.RowDefinitions.Clear();

            if (compacto)
            {
                FiltroEliminadasGrid.ColumnDefinitions.Add(
                    new ColumnDefinition(GridLength.Star));
                FiltroEliminadasGrid.ColumnDefinitions.Add(
                    new ColumnDefinition(GridLength.Star));
                FiltroEliminadasGrid.RowDefinitions.Add(
                    new RowDefinition(GridLength.Auto));
                FiltroEliminadasGrid.RowDefinitions.Add(
                    new RowDefinition(GridLength.Auto));

                Grid.SetRow(ResumenEliminadas, 0);
                Grid.SetColumn(ResumenEliminadas, 0);
                Grid.SetColumnSpan(ResumenEliminadas, 2);
                Grid.SetRow(BuscarEliminadasButton, 1);
                Grid.SetColumn(BuscarEliminadasButton, 0);
                Grid.SetColumnSpan(BuscarEliminadasButton, 1);
                Grid.SetRow(LimpiarEliminadasButton, 1);
                Grid.SetColumn(LimpiarEliminadasButton, 1);
                Grid.SetColumnSpan(LimpiarEliminadasButton, 1);
            }
            else
            {
                FiltroEliminadasGrid.ColumnDefinitions.Add(
                    new ColumnDefinition(GridLength.Star));
                FiltroEliminadasGrid.ColumnDefinitions.Add(
                    new ColumnDefinition(GridLength.Auto));
                FiltroEliminadasGrid.ColumnDefinitions.Add(
                    new ColumnDefinition(GridLength.Auto));
                FiltroEliminadasGrid.RowDefinitions.Add(
                    new RowDefinition(GridLength.Auto));

                Grid.SetRow(ResumenEliminadas, 0);
                Grid.SetColumn(ResumenEliminadas, 0);
                Grid.SetColumnSpan(ResumenEliminadas, 1);
                Grid.SetRow(BuscarEliminadasButton, 0);
                Grid.SetColumn(BuscarEliminadasButton, 1);
                Grid.SetColumnSpan(BuscarEliminadasButton, 1);
                Grid.SetRow(LimpiarEliminadasButton, 0);
                Grid.SetColumn(LimpiarEliminadasButton, 2);
                Grid.SetColumnSpan(LimpiarEliminadasButton, 1);
            }
        }

        private void AjustarPaginacion(double width)
        {
            if (PaginacionEliminadas == null ||
                AnteriorEliminadasButton == null ||
                TextoPaginacionEliminadas == null ||
                SiguienteEliminadasButton == null)
            {
                return;
            }

            bool compacto = width < 560;

            if (paginacionCompacta == compacto &&
                PaginacionEliminadas.RowDefinitions.Count > 0)
            {
                return;
            }

            paginacionCompacta = compacto;
            PaginacionEliminadas.ColumnDefinitions.Clear();
            PaginacionEliminadas.RowDefinitions.Clear();

            if (compacto)
            {
                PaginacionEliminadas.ColumnDefinitions.Add(
                    new ColumnDefinition(GridLength.Star));
                PaginacionEliminadas.ColumnDefinitions.Add(
                    new ColumnDefinition(GridLength.Star));
                PaginacionEliminadas.RowDefinitions.Add(
                    new RowDefinition(GridLength.Auto));
                PaginacionEliminadas.RowDefinitions.Add(
                    new RowDefinition(GridLength.Auto));

                Grid.SetRow(TextoPaginacionEliminadas, 0);
                Grid.SetColumn(TextoPaginacionEliminadas, 0);
                Grid.SetColumnSpan(TextoPaginacionEliminadas, 2);
                Grid.SetRow(AnteriorEliminadasButton, 1);
                Grid.SetColumn(AnteriorEliminadasButton, 0);
                Grid.SetColumnSpan(AnteriorEliminadasButton, 1);
                Grid.SetRow(SiguienteEliminadasButton, 1);
                Grid.SetColumn(SiguienteEliminadasButton, 1);
                Grid.SetColumnSpan(SiguienteEliminadasButton, 1);
            }
            else
            {
                PaginacionEliminadas.ColumnDefinitions.Add(
                    new ColumnDefinition(GridLength.Auto));
                PaginacionEliminadas.ColumnDefinitions.Add(
                    new ColumnDefinition(GridLength.Star));
                PaginacionEliminadas.ColumnDefinitions.Add(
                    new ColumnDefinition(GridLength.Auto));
                PaginacionEliminadas.RowDefinitions.Add(
                    new RowDefinition(GridLength.Auto));

                Grid.SetRow(AnteriorEliminadasButton, 0);
                Grid.SetColumn(AnteriorEliminadasButton, 0);
                Grid.SetColumnSpan(AnteriorEliminadasButton, 1);
                Grid.SetRow(TextoPaginacionEliminadas, 0);
                Grid.SetColumn(TextoPaginacionEliminadas, 1);
                Grid.SetColumnSpan(TextoPaginacionEliminadas, 1);
                Grid.SetRow(SiguienteEliminadasButton, 0);
                Grid.SetColumn(SiguienteEliminadasButton, 2);
                Grid.SetColumnSpan(SiguienteEliminadasButton, 1);
            }
        }

        private void OnSolicitarDesplazamientoInicio(
            object? sender,
            EventArgs e)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (FuentesEliminadasCollectionView?.ItemsSource == null ||
                    viewModel.Fuentes.Count == 0)
                {
                    return;
                }

                FuentesEliminadasCollectionView.ScrollTo(
                    viewModel.Fuentes[0],
                    position: ScrollToPosition.Start,
                    animate: false);
            });
        }
    }
}
