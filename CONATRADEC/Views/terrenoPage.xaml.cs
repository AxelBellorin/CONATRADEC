using CONATRADEC.Services;
using CONATRADEC.ViewModels;
using System.Windows.Input;

namespace CONATRADEC.Views
{
    public partial class terrenoPage : ContentPage
    {
        private readonly TerrenoViewModel viewModel = new();

        private bool accionesCompactas;
        private bool navegandoAConfiguracion;

        public terrenoPage()
        {
            /*
             * El comando debe existir antes de InitializeComponent().
             *
             * El encabezado responsive utiliza x:Reference para enlazar
             * esta propiedad durante la carga del XAML. Si se asigna
             * después, el botón queda visible pero recibe Command = null.
             */
            RegresarConfiguracionCommand =
                new Command(
                    async () =>
                        await RegresarConfiguracionAsync(),
                    () => !navegandoAConfiguracion);

            InitializeComponent();

            BindingContext = viewModel;

            Shell.Current.FlyoutBehavior =
                FlyoutBehavior.Disabled;
        }

        public ICommand RegresarConfiguracionCommand { get; }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            navegandoAConfiguracion = false;
            ActualizarComandoRegresar();

            viewModel.ActualizarPermisos();

            ContenidoPrincipal.IsVisible =
                viewModel.CanView;

            ContenidoSinPermiso.IsVisible =
                !viewModel.CanView;

            if (!viewModel.CanView)
                return;

            AjustarAccionesBusqueda(Width);

            await viewModel.InicializarAsync();
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

            AjustarAccionesBusqueda(width);
        }

        private async Task RegresarConfiguracionAsync()
        {
            if (navegandoAConfiguracion)
                return;

            try
            {
                navegandoAConfiguracion = true;
                ActualizarComandoRegresar();

                await Shell.Current.GoToAsync(
                    AppRoutes.Configuracion,
                    true);
            }
            catch (Exception ex)
            {
                navegandoAConfiguracion = false;
                ActualizarComandoRegresar();

                await DisplayAlert(
                    "Navegación",
                    $"No fue posible regresar a Configuración. " +
                    $"{ex.Message}",
                    "Aceptar");
            }
        }

        private void ActualizarComandoRegresar()
        {
            if (RegresarConfiguracionCommand is Command command)
                command.ChangeCanExecute();
        }

        /// <summary>
        /// En teléfono, Buscar ocupa toda la primera fila y las acciones
        /// Filtros/Limpiar comparten una segunda fila.
        /// </summary>
        private void AjustarAccionesBusqueda(double width)
        {
            if (width <= 0 ||
                BusquedaAccionesGrid == null)
            {
                return;
            }

            bool compacto = width < 600;

            if (accionesCompactas == compacto)
                return;

            accionesCompactas = compacto;

            BusquedaAccionesGrid
                .ColumnDefinitions
                .Clear();

            BusquedaAccionesGrid
                .RowDefinitions
                .Clear();

            if (BusquedaAccionesGrid.Children.Count < 3)
                return;

            View buscar =
                (View)BusquedaAccionesGrid.Children[0];

            View filtros =
                (View)BusquedaAccionesGrid.Children[1];

            View limpiar =
                (View)BusquedaAccionesGrid.Children[2];

            if (compacto)
            {
                BusquedaAccionesGrid.ColumnDefinitions.Add(
                    new ColumnDefinition(GridLength.Star));

                BusquedaAccionesGrid.ColumnDefinitions.Add(
                    new ColumnDefinition(GridLength.Star));

                BusquedaAccionesGrid.RowDefinitions.Add(
                    new RowDefinition(GridLength.Auto));

                BusquedaAccionesGrid.RowDefinitions.Add(
                    new RowDefinition(GridLength.Auto));

                Grid.SetRow(buscar, 0);
                Grid.SetColumn(buscar, 0);
                Grid.SetColumnSpan(buscar, 2);

                Grid.SetRow(filtros, 1);
                Grid.SetColumn(filtros, 0);
                Grid.SetColumnSpan(filtros, 1);

                Grid.SetRow(limpiar, 1);
                Grid.SetColumn(limpiar, 1);
                Grid.SetColumnSpan(limpiar, 1);

                return;
            }

            BusquedaAccionesGrid.ColumnDefinitions.Add(
                new ColumnDefinition(GridLength.Star));

            BusquedaAccionesGrid.ColumnDefinitions.Add(
                new ColumnDefinition(GridLength.Auto));

            BusquedaAccionesGrid.ColumnDefinitions.Add(
                new ColumnDefinition(GridLength.Auto));

            BusquedaAccionesGrid.RowDefinitions.Add(
                new RowDefinition(GridLength.Auto));

            Grid.SetRow(buscar, 0);
            Grid.SetColumn(buscar, 0);
            Grid.SetColumnSpan(buscar, 1);

            Grid.SetRow(filtros, 0);
            Grid.SetColumn(filtros, 1);
            Grid.SetColumnSpan(filtros, 1);

            Grid.SetRow(limpiar, 0);
            Grid.SetColumn(limpiar, 2);
            Grid.SetColumnSpan(limpiar, 1);
        }
    }
}
