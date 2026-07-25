using CONATRADEC.ViewModels;

namespace CONATRADEC.Views
{
    public partial class fuenteNutrientePage : ContentPage
    {
        private readonly FuenteNutrienteViewModel
            viewModel = new();

        private int cantidadColumnasActual;
        private bool accionesFiltroCompactas;

        public fuenteNutrientePage()
        {
            InitializeComponent();

            Shell.Current.FlyoutBehavior =
                FlyoutBehavior.Disabled;

            BindingContext = viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            viewModel.ActualizarPermisos();
            AjustarDiseno(Width);

            await viewModel.InicializarAsync();
        }

        protected override void OnDisappearing()
        {
            viewModel.CancelarCargas();

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
            AjustarCantidadColumnas(width);
            AjustarAccionesFiltro(width);
        }

        private void AjustarCantidadColumnas(double width)
        {
            if (width <= 0 ||
                FuentesGridLayout == null)
            {
                return;
            }

            int nuevasColumnas =
                width >= 1280
                    ? 3
                    : width >= 700
                        ? 2
                        : 1;

            if (cantidadColumnasActual == nuevasColumnas)
                return;

            cantidadColumnasActual = nuevasColumnas;
            FuentesGridLayout.Span = nuevasColumnas;
        }

        /// <summary>
        /// En teléfono, el total ocupa una fila y Buscar/Limpiar comparten
        /// una segunda fila. En tablet y escritorio se usa una sola fila.
        /// </summary>
        private void AjustarAccionesFiltro(double width)
        {
            if (width <= 0 ||
                FuenteFiltroAccionesGrid == null)
            {
                return;
            }

            bool compacto = width < 600;

            if (accionesFiltroCompactas == compacto)
                return;

            accionesFiltroCompactas = compacto;

            FuenteFiltroAccionesGrid.ColumnDefinitions.Clear();
            FuenteFiltroAccionesGrid.RowDefinitions.Clear();

            View resumen = (View)FuenteFiltroAccionesGrid.Children[0];
            View buscar = (View)FuenteFiltroAccionesGrid.Children[1];
            View limpiar = (View)FuenteFiltroAccionesGrid.Children[2];

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

                Grid.SetRow(resumen, 0);
                Grid.SetColumn(resumen, 0);
                Grid.SetColumnSpan(resumen, 2);

                Grid.SetRow(buscar, 1);
                Grid.SetColumn(buscar, 0);
                Grid.SetColumnSpan(buscar, 1);

                Grid.SetRow(limpiar, 1);
                Grid.SetColumn(limpiar, 1);
                Grid.SetColumnSpan(limpiar, 1);

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

            Grid.SetRow(resumen, 0);
            Grid.SetColumn(resumen, 0);
            Grid.SetColumnSpan(resumen, 1);

            Grid.SetRow(buscar, 0);
            Grid.SetColumn(buscar, 1);
            Grid.SetColumnSpan(buscar, 1);

            Grid.SetRow(limpiar, 0);
            Grid.SetColumn(limpiar, 2);
            Grid.SetColumnSpan(limpiar, 1);
        }
    }
}
