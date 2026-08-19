using CONATRADEC.ViewModels;
using System.Diagnostics;

namespace CONATRADEC.Views
{
    public partial class albumEliminadosPage : ContentPage
    {
        private const double BusquedaCompactaBreakpoint = 600;
        private const double DosColumnasBreakpoint = 900;
        private const double TresColumnasBreakpoint = 1280;

        private readonly AlbumEliminadosViewModel viewModel = new();
        private bool inicializando;
        private bool inicializada;
        private bool? busquedaCompacta;
        private int spanActual = -1;

        public albumEliminadosPage()
        {
            InitializeComponent();
            BindingContext = viewModel;

            Loaded += (_, _) => AplicarDisenoResponsivo();
            SizeChanged += (_, _) => AplicarDisenoResponsivo();
            EliminadosCollection.SizeChanged +=
                (_, _) => AplicarDisenoResponsivo();
        }

        public async Task InicializarDespuesDeMostrarAsync()
        {
            if (inicializada || inicializando)
                return;

            inicializando = true;
            try
            {
                viewModel.ActualizarPermisos();

                if (!viewModel.CanView)
                {
                    await DisplayAlert(
                        "Permiso denegado",
                        "No tiene permisos para consultar los elementos eliminados del Álbum Botánico.",
                        "Aceptar");

                    if (Navigation.ModalStack.Count > 0)
                        await Navigation.PopModalAsync();
                    return;
                }

                await viewModel.InicializarAsync();
                inicializada = true;
                AplicarDisenoResponsivo();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error al cargar eliminados del álbum: {ex}");
                await DisplayAlert(
                    "No fue posible",
                    "No fue posible cargar los elementos eliminados.",
                    "Aceptar");
            }
            finally
            {
                inicializando = false;
            }
        }


        protected override async void OnAppearing()
        {
            base.OnAppearing();

            if (inicializada &&
                viewModel.RequiereRecargaPorCambios &&
                !viewModel.IsBusy)
            {
                await viewModel.RecargarPaginaActualAsync();
                AplicarDisenoResponsivo();
            }
        }

        protected override void OnDisappearing()
        {
            viewModel.CancelarCarga();
            base.OnDisappearing();
        }

        private void AplicarDisenoResponsivo()
        {
            double ancho = Width;
            if (ancho <= 0)
                return;

            Thickness margenLista = ancho < 600
                ? new Thickness(12, 12, 12, 24)
                : ancho < 950
                    ? new Thickness(20, 16, 20, 28)
                    : new Thickness(28, 20, 28, 32);

            /*
             * El encabezado forma parte del mismo CollectionView. El margen
             * exterior pertenece al listado completo para que encabezado,
             * tarjetas y paginación conserven la misma alineación responsive.
             */
            EncabezadoEliminados.Margin = new Thickness(0, 0, 0, 14);
            EliminadosCollection.Margin = margenLista;

            AplicarBusqueda();
            AplicarColumnas();

            PaginacionEliminados.WidthRequest = Math.Min(
                580,
                Math.Max(0, EliminadosCollection.Width - 12));
        }

        private void AplicarBusqueda()
        {
            double ancho = BusquedaEliminadosGrid.Width > 0
                ? BusquedaEliminadosGrid.Width
                : Width;

            bool compacta = ancho < BusquedaCompactaBreakpoint;
            if (busquedaCompacta == compacta)
                return;

            busquedaCompacta = compacta;
            BusquedaEliminadosGrid.RowDefinitions.Clear();
            BusquedaEliminadosGrid.ColumnDefinitions.Clear();

            if (compacta)
            {
                BusquedaEliminadosGrid.RowDefinitions.Add(
                    new RowDefinition(GridLength.Auto));
                BusquedaEliminadosGrid.RowDefinitions.Add(
                    new RowDefinition(GridLength.Auto));
                BusquedaEliminadosGrid.ColumnDefinitions.Add(
                    new ColumnDefinition(GridLength.Star));

                Grid.SetRow(BusquedaEliminados, 0);
                Grid.SetColumn(BusquedaEliminados, 0);
                Grid.SetRow(LimpiarEliminadosButton, 1);
                Grid.SetColumn(LimpiarEliminadosButton, 0);
            }
            else
            {
                BusquedaEliminadosGrid.RowDefinitions.Add(
                    new RowDefinition(GridLength.Auto));
                BusquedaEliminadosGrid.ColumnDefinitions.Add(
                    new ColumnDefinition(GridLength.Star));
                BusquedaEliminadosGrid.ColumnDefinitions.Add(
                    new ColumnDefinition(GridLength.Auto));

                Grid.SetRow(BusquedaEliminados, 0);
                Grid.SetColumn(BusquedaEliminados, 0);
                Grid.SetRow(LimpiarEliminadosButton, 0);
                Grid.SetColumn(LimpiarEliminadosButton, 1);
            }
        }

        private void AplicarColumnas()
        {
            double ancho = EliminadosCollection.Width > 0
                ? EliminadosCollection.Width
                : Width;

            int span = ancho >= TresColumnasBreakpoint
                ? 3
                : ancho >= DosColumnasBreakpoint
                    ? 2
                    : 1;

            if (spanActual == span && EliminadosGrid.Span == span)
                return;

            spanActual = span;
            EliminadosGrid.Span = span;
        }

        private void OnBuscarPressed(object? sender, EventArgs e)
        {
            if (viewModel.BuscarCommand.CanExecute(null))
                viewModel.BuscarCommand.Execute(null);
        }

        private async void OnPaginaAnteriorClicked(object? sender, EventArgs e)
        {
            if (await viewModel.IrPaginaAnteriorAsync())
                DesplazarAlInicio();
        }

        private async void OnPaginaSiguienteClicked(object? sender, EventArgs e)
        {
            if (await viewModel.IrPaginaSiguienteAsync())
                DesplazarAlInicio();
        }

        private void DesplazarAlInicio()
        {
            if (viewModel.Registros.Count == 0)
                return;

            EliminadosCollection.ScrollTo(
                0,
                position: ScrollToPosition.Start,
                animate: false);
        }
    }
}
