using CONATRADEC.Services;
using CONATRADEC.ViewModels;

namespace CONATRADEC.Views
{
    public partial class MainPage : ContentPage
    {
        private readonly MainPageViewModel viewModel = new();
        private bool filtrosCompactos;
        private bool fechasCompactas;

        public MainPage()
        {
            Shell.Current.FlyoutBehavior = FlyoutBehavior.Disabled;
            InitializeComponent();
            BindingContext = viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            viewModel.LoadPagePermissions("MainPage");
            viewModel.PrepararPantalla();

            ContenidoPrincipal.IsVisible = viewModel.CanView;
            ContenidoSinPermiso.IsVisible = !viewModel.CanView;

            if (!viewModel.CanView)
            {
                AnalisisVisitaService.FinalizarVisita();
                viewModel.CancelarCarga();
                viewModel.IsBusy = false;
                return;
            }

            AjustarDiseno(Width);

            bool nuevaVisita =
                AnalisisVisitaService.AsegurarVisita();

            await Task.Yield();

            if (nuevaVisita)
            {
                await viewModel.IniciarNuevaVisitaAsync();

                if (viewModel.UltimaCargaExitosa &&
                    AnalisisListadoEstadoService.HayActualizacionPendiente)
                {
                    AnalisisListadoEstadoService.ConfirmarActualizacion();
                }

                return;
            }

            /*
             * Regresar desde Ver no consulta el listado. Crear/Editar marcan
             * explícitamente una actualización pendiente y solamente entonces
             * se renueva la página actual con los filtros aplicados.
             */
            if (viewModel.SeHaListado &&
                AnalisisListadoEstadoService.HayActualizacionPendiente &&
                !viewModel.IsBusy &&
                !viewModel.CargandoListado)
            {
                await viewModel.RecargarPaginaActualAsync();

                if (viewModel.UltimaCargaExitosa)
                {
                    AnalisisListadoEstadoService
                        .ConfirmarActualizacion();
                }

                return;
            }

            if (!viewModel.SeHaListado &&
                !viewModel.CargandoListado)
            {
                await viewModel.IniciarNuevaVisitaAsync();
            }
        }

        protected override void OnDisappearing()
        {
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

        private void AjustarDiseno(double width)
        {
            if (width <= 0)
                return;

            Thickness margen =
                width < 600
                    ? new Thickness(12, 12, 12, 8)
                    : width < 900
                        ? new Thickness(18, 16, 18, 10)
                        : new Thickness(22, 18, 22, 12);

            EncabezadoPrincipal.Margin = margen;

            AnalisisCollectionView.Margin =
                width < 600
                    ? new Thickness(12, 0, 12, 0)
                    : width < 900
                        ? new Thickness(18, 0, 18, 0)
                        : new Thickness(22, 0, 22, 0);

            if (PaginacionAnalisis != null)
            {
                double paddingHorizontal =
                    width < 600
                        ? 24
                        : width < 900
                            ? 36
                            : 44;

                PaginacionAnalisis.WidthRequest =
                    Math.Min(
                        560,
                        Math.Max(0, width - paddingHorizontal));
            }

            AjustarCabeceraFiltros(width);
            AjustarRangoFechas(width);
        }

        private void AjustarCabeceraFiltros(double width)
        {
            bool compacto = width < 620;

            if (filtrosCompactos == compacto ||
                FiltrosCabeceraGrid == null)
            {
                return;
            }

            filtrosCompactos = compacto;

            FiltrosCabeceraGrid.ColumnDefinitions.Clear();
            FiltrosCabeceraGrid.RowDefinitions.Clear();

            if (compacto)
            {
                FiltrosCabeceraGrid.ColumnDefinitions.Add(
                    new ColumnDefinition(GridLength.Star));
                FiltrosCabeceraGrid.ColumnDefinitions.Add(
                    new ColumnDefinition(GridLength.Star));
                FiltrosCabeceraGrid.RowDefinitions.Add(
                    new RowDefinition(GridLength.Auto));
                FiltrosCabeceraGrid.RowDefinitions.Add(
                    new RowDefinition(GridLength.Auto));

                Grid.SetRow(FiltrosTituloContenedor, 0);
                Grid.SetColumn(FiltrosTituloContenedor, 0);
                Grid.SetColumnSpan(FiltrosTituloContenedor, 2);

                View buscar =
                    (View)FiltrosCabeceraGrid.Children[1];
                View limpiar =
                    (View)FiltrosCabeceraGrid.Children[2];

                Grid.SetRow(buscar, 1);
                Grid.SetColumn(buscar, 0);
                Grid.SetRow(limpiar, 1);
                Grid.SetColumn(limpiar, 1);
            }
            else
            {
                FiltrosCabeceraGrid.ColumnDefinitions.Add(
                    new ColumnDefinition(GridLength.Star));
                FiltrosCabeceraGrid.ColumnDefinitions.Add(
                    new ColumnDefinition(GridLength.Auto));
                FiltrosCabeceraGrid.ColumnDefinitions.Add(
                    new ColumnDefinition(GridLength.Auto));
                FiltrosCabeceraGrid.RowDefinitions.Add(
                    new RowDefinition(GridLength.Auto));

                Grid.SetRow(FiltrosTituloContenedor, 0);
                Grid.SetColumn(FiltrosTituloContenedor, 0);
                Grid.SetColumnSpan(FiltrosTituloContenedor, 1);

                View buscar =
                    (View)FiltrosCabeceraGrid.Children[1];
                View limpiar =
                    (View)FiltrosCabeceraGrid.Children[2];

                Grid.SetRow(buscar, 0);
                Grid.SetColumn(buscar, 1);
                Grid.SetRow(limpiar, 0);
                Grid.SetColumn(limpiar, 2);
            }
        }

        private void AjustarRangoFechas(double width)
        {
            bool compacto = width < 520;

            if (fechasCompactas == compacto ||
                RangoFechasGrid == null)
            {
                return;
            }

            fechasCompactas = compacto;

            RangoFechasGrid.ColumnDefinitions.Clear();
            RangoFechasGrid.RowDefinitions.Clear();

            if (compacto)
            {
                RangoFechasGrid.ColumnDefinitions.Add(
                    new ColumnDefinition(GridLength.Star));
                RangoFechasGrid.RowDefinitions.Add(
                    new RowDefinition(GridLength.Auto));
                RangoFechasGrid.RowDefinitions.Add(
                    new RowDefinition(GridLength.Auto));

                Grid.SetRow(FechaDesdeContenedor, 0);
                Grid.SetColumn(FechaDesdeContenedor, 0);
                Grid.SetRow(FechaHastaContenedor, 1);
                Grid.SetColumn(FechaHastaContenedor, 0);
            }
            else
            {
                RangoFechasGrid.ColumnDefinitions.Add(
                    new ColumnDefinition(GridLength.Star));
                RangoFechasGrid.ColumnDefinitions.Add(
                    new ColumnDefinition(GridLength.Star));
                RangoFechasGrid.RowDefinitions.Add(
                    new RowDefinition(GridLength.Auto));

                Grid.SetRow(FechaDesdeContenedor, 0);
                Grid.SetColumn(FechaDesdeContenedor, 0);
                Grid.SetRow(FechaHastaContenedor, 0);
                Grid.SetColumn(FechaHastaContenedor, 1);
            }
        }

        /// <summary>
        /// Espera el reemplazo real de la página y luego posiciona el primer
        /// análisis al inicio, igual que Usuarios y Terrenos.
        /// </summary>
        private async void PaginacionAnalisis_Clicked(
            object? sender,
            EventArgs e)
        {
            int paginaAnterior = viewModel.PaginaActual;
            bool operacionDetectada = false;

            for (int intento = 0; intento < 240; intento++)
            {
                if (viewModel.CargandoListado ||
                    viewModel.PaginaActual != paginaAnterior)
                {
                    operacionDetectada = true;
                }

                if (operacionDetectada &&
                    !viewModel.CargandoListado)
                {
                    if (viewModel.PaginaActual != paginaAnterior &&
                        viewModel.AnalisisGuardados.Count > 0)
                    {
                        await DesplazarAnalisisAlInicioAsync();
                    }

                    return;
                }

                await Task.Delay(50);
            }
        }

        private async Task DesplazarAnalisisAlInicioAsync()
        {
            if (AnalisisCollectionView == null ||
                viewModel.AnalisisGuardados.Count == 0)
            {
                return;
            }

            await Task.Delay(60);

            AnalisisCollectionView.ScrollTo(
                0,
                position: ScrollToPosition.Start,
                animate: false);
        }
    }
}
