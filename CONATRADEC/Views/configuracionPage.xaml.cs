using CONATRADEC.Services;
using CONATRADEC.ViewModels;

namespace CONATRADEC.Views
{
    public partial class configuracionPage : ContentPage
    {
        private static bool rutasRegistradas;
        private readonly ConfiguracionViewModel viewModel = new();

        private int cantidadColumnasActual;

        public configuracionPage()
        {
            InitializeComponent();

            Shell.Current.FlyoutBehavior =
                FlyoutBehavior.Disabled;

            BindingContext = viewModel;

            RegistrarRutas();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            AjustarCantidadColumnas(Width);
            viewModel.ActualizarOpciones();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            viewModel.CancelarBusqueda();
        }

        protected override void OnSizeAllocated(
            double width,
            double height)
        {
            base.OnSizeAllocated(width, height);
            AjustarCantidadColumnas(width);
        }

        private void AjustarCantidadColumnas(double width)
        {
            if (width <= 0)
                return;

            int nuevasColumnas =
                width >= 1200
                    ? 3
                    : width >= 700
                        ? 2
                        : 1;

            if (cantidadColumnasActual == nuevasColumnas)
                return;

            cantidadColumnasActual = nuevasColumnas;

            viewModel.ConfigurarColumnas(
                nuevasColumnas);
        }

        private static void RegistrarRutas()
        {
            if (rutasRegistradas)
                return;

            Routing.RegisterRoute(
                AppRoutes.Bitacora,
                typeof(bitacoraPage));

            Routing.RegisterRoute(
                AppRoutes.BitacoraDetalle,
                typeof(bitacoraDetallePage));

            rutasRegistradas = true;
        }
    }
}
