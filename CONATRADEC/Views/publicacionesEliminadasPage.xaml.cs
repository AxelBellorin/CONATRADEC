using CONATRADEC.ViewModels;
using System.Collections.Specialized;

namespace CONATRADEC.Views
{
    public partial class publicacionesEliminadasPage : ContentPage
    {
        private const double AccionesCompactasBreakpoint = 560;
        private const double DosColumnasBreakpoint = 900;
        private const double TresColumnasBreakpoint = 1280;

        private readonly PublicacionesEliminadasViewModel viewModel = new();
        private int spanActual = -1;
        private bool? accionesCompactas;
        private bool inicializacionSolicitada;
        private int paginaAntesCambio = -1;

        public publicacionesEliminadasPage()
        {
            InitializeComponent();
            BindingContext = viewModel;

            viewModel.Registros.CollectionChanged +=
                OnRegistrosCollectionChanged;

            Loaded += (_, _) => AplicarDisenoResponsivo();
            SizeChanged += (_, _) => AplicarDisenoResponsivo();
            RegistrosCollection.SizeChanged +=
                (_, _) => AplicarDisenoResponsivo();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            AplicarDisenoResponsivo();
        }

        protected override void OnDisappearing()
        {
            viewModel.CancelarCarga();
            base.OnDisappearing();
        }

        public async Task InicializarDespuesDeMostrarAsync()
        {
            if (inicializacionSolicitada)
                return;

            inicializacionSolicitada = true;
            await viewModel.InicializarAsync();
            AplicarDisenoResponsivo();
        }

        protected override bool OnBackButtonPressed()
        {
#if ANDROID
            return true;
#else
            if (viewModel.CerrarCommand.CanExecute(null))
            {
                viewModel.CerrarCommand.Execute(null);
                return true;
            }

            return base.OnBackButtonPressed();
#endif
        }

        protected override void OnSizeAllocated(
            double width,
            double height)
        {
            base.OnSizeAllocated(width, height);
            AplicarDisenoResponsivo();
        }

        private void OnRegistrosCollectionChanged(
            object? sender,
            NotifyCollectionChangedEventArgs e)
        {
            AplicarColumnas();
        }

        private void AplicarDisenoResponsivo()
        {
            AjustarPadding();
            AplicarColumnas();
            AplicarAccionesBusqueda();
            AplicarPaginacion();
        }

        private void AjustarPadding()
        {
            double ancho = Width;

            if (ContenidoPublicacionesEliminadas == null ||
                ancho <= 0)
            {
                return;
            }

            ContenidoPublicacionesEliminadas.Padding =
                ancho < 600
                    ? new Thickness(12, 12, 12, 18)
                    : ancho < 900
                        ? new Thickness(18, 16, 18, 22)
                        : new Thickness(24, 20, 24, 26);
        }

        private void AplicarColumnas()
        {
            double ancho =
                RegistrosCollection.Width > 0
                    ? RegistrosCollection.Width
                    : Width;

            if (ancho <= 0)
                return;

            int nuevoSpan =
                viewModel.Registros.Count == 0
                    ? 1
                    : ancho >= TresColumnasBreakpoint
                        ? 3
                        : ancho >= DosColumnasBreakpoint
                            ? 2
                            : 1;

            if (spanActual == nuevoSpan &&
                RegistrosGrid.Span == nuevoSpan)
            {
                return;
            }

            spanActual = nuevoSpan;
            RegistrosGrid.Span = nuevoSpan;
        }

        private void AplicarAccionesBusqueda()
        {
            double ancho =
                RegistrosCollection.Width > 0
                    ? RegistrosCollection.Width
                    : Width;

            if (ancho <= 0)
                return;

            bool compacto =
                ancho < AccionesCompactasBreakpoint;

            if (accionesCompactas == compacto)
                return;

            accionesCompactas = compacto;

            AccionesBusquedaGrid.RowDefinitions.Clear();
            AccionesBusquedaGrid.ColumnDefinitions.Clear();

            if (compacto)
            {
                AccionesBusquedaGrid.RowDefinitions.Add(
                    new RowDefinition(GridLength.Auto));
                AccionesBusquedaGrid.RowDefinitions.Add(
                    new RowDefinition(GridLength.Auto));

                AccionesBusquedaGrid.ColumnDefinitions.Add(
                    new ColumnDefinition(GridLength.Star));
                AccionesBusquedaGrid.ColumnDefinitions.Add(
                    new ColumnDefinition(GridLength.Star));

                Grid.SetRow(ResumenLabel, 0);
                Grid.SetColumn(ResumenLabel, 0);
                Grid.SetColumnSpan(ResumenLabel, 2);

                Grid.SetRow(BuscarButton, 1);
                Grid.SetColumn(BuscarButton, 0);
                Grid.SetColumnSpan(BuscarButton, 1);

                Grid.SetRow(LimpiarButton, 1);
                Grid.SetColumn(LimpiarButton, 1);
                Grid.SetColumnSpan(LimpiarButton, 1);
            }
            else
            {
                AccionesBusquedaGrid.RowDefinitions.Add(
                    new RowDefinition(GridLength.Auto));

                AccionesBusquedaGrid.ColumnDefinitions.Add(
                    new ColumnDefinition(GridLength.Star));
                AccionesBusquedaGrid.ColumnDefinitions.Add(
                    new ColumnDefinition(GridLength.Auto));
                AccionesBusquedaGrid.ColumnDefinitions.Add(
                    new ColumnDefinition(GridLength.Auto));

                Grid.SetRow(ResumenLabel, 0);
                Grid.SetColumn(ResumenLabel, 0);
                Grid.SetColumnSpan(ResumenLabel, 1);

                Grid.SetRow(BuscarButton, 0);
                Grid.SetColumn(BuscarButton, 1);
                Grid.SetColumnSpan(BuscarButton, 1);

                Grid.SetRow(LimpiarButton, 0);
                Grid.SetColumn(LimpiarButton, 2);
                Grid.SetColumnSpan(LimpiarButton, 1);
            }

            BuscarButton.MinimumWidthRequest = 0;
            LimpiarButton.MinimumWidthRequest = 0;
        }

        private void AplicarPaginacion()
        {
            if (PaginacionEliminados == null)
                return;

            double ancho =
                RegistrosCollection.Width > 0
                    ? RegistrosCollection.Width
                    : Width;

            if (ancho <= 0)
                return;

            double margenHorizontal =
                ancho < 480
                    ? 8
                    : ancho < 800
                        ? 20
                        : 32;

            PaginacionEliminados.WidthRequest =
                Math.Min(
                    560,
                    Math.Max(0, ancho - margenHorizontal));
        }

        private void PaginacionEliminados_Pressed(
            object? sender,
            EventArgs e)
        {
            paginaAntesCambio =
                viewModel.PaginaActual;
        }

        private async void PaginacionEliminados_Clicked(
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
                        viewModel.Registros.Count > 0)
                    {
                        await DesplazarRegistrosAlInicioAsync();
                    }

                    paginaAntesCambio = -1;
                    return;
                }

                await Task.Delay(50);
            }

            paginaAntesCambio = -1;
        }

        private async Task DesplazarRegistrosAlInicioAsync()
        {
            if (RegistrosCollection == null ||
                viewModel.Registros.Count == 0)
            {
                return;
            }

            await Task.Delay(60);

            RegistrosCollection.ScrollTo(
                0,
                position: ScrollToPosition.Start,
                animate: false);
        }
    }
}
