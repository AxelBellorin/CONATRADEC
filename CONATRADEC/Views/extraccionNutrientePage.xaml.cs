using CONATRADEC.ViewModels;

namespace CONATRADEC.Views
{
    public partial class extraccionNutrientePage : ContentPage
    {
        private readonly ExtraccionNutrienteViewModel viewModel = new();
        private int cantidadColumnasActual;

        public extraccionNutrientePage()
        {
            InitializeComponent();
            Shell.Current.FlyoutBehavior = FlyoutBehavior.Disabled;
            BindingContext = viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            viewModel.ActualizarPermisos();
            AjustarCantidadColumnas(Width);

            await viewModel.InicializarAsync();
        }

        protected override void OnDisappearing()
        {
            viewModel.CancelarCarga();
            base.OnDisappearing();
        }

        protected override void OnSizeAllocated(double width, double height)
        {
            base.OnSizeAllocated(width, height);
            AjustarCantidadColumnas(width);
        }

        private void AjustarCantidadColumnas(double width)
        {
            if (width <= 0 || ExtraccionesGridLayout == null)
                return;

            int nuevasColumnas = width >= 1200
                ? 3
                : width >= 700
                    ? 2
                    : 1;

            if (cantidadColumnasActual == nuevasColumnas)
                return;

            cantidadColumnasActual = nuevasColumnas;
            ExtraccionesGridLayout.Span = nuevasColumnas;
        }
    }
}
