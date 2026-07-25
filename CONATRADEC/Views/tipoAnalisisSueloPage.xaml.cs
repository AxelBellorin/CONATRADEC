using CONATRADEC.ViewModels;

namespace CONATRADEC.Views
{
    public partial class tipoAnalisisSueloPage : ContentPage
    {
        private readonly TipoAnalisisSueloViewModel
            viewModel = new();

        private int cantidadColumnasActual;

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

            viewModel.ActualizarPermisos();
            AjustarCantidadColumnas(Width);

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
            base.OnSizeAllocated(
                width,
                height);

            AjustarCantidadColumnas(
                width);
        }

        private void AjustarCantidadColumnas(
            double width)
        {
            if (width <= 0 ||
                TiposAnalisisGridLayout == null)
            {
                return;
            }

            int nuevasColumnas =
                width >= 1280
                    ? 3
                    : width >= 760
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
    }
}
