using CONATRADEC.Services;
using CONATRADEC.ViewModels;

namespace CONATRADEC.Views
{
    public partial class configuracionPage : ContentPage
    {
        private static bool rutasRegistradas;

        private readonly ConfiguracionViewModel
            viewModel = new();

        private int cantidadColumnasActual;
        private bool paginaVisible;
        private bool redireccionando;

        public configuracionPage()
        {
            InitializeComponent();

            Shell.Current.FlyoutBehavior =
                FlyoutBehavior.Disabled;

            BindingContext = viewModel;

            RegistrarRutas();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            if (!NavigationPermissionService
                    .PuedeVerConfiguracion())
            {
                if (redireccionando)
                    return;

                redireccionando = true;

                try
                {
                    string rutaPermitida =
                        NavigationPermissionService
                            .ObtenerRutaInicialPermitida();

                    await Shell.Current.GoToAsync(
                        rutaPermitida,
                        false);
                }
                finally
                {
                    redireccionando = false;
                }

                return;
            }

            paginaVisible = true;

            /*
             * El Span se cambia directamente en el layout.
             * Ya no reconstruye toda la colección.
             */
            AjustarCantidadColumnas(Width);

            /*
             * Una sola carga al entrar.
             */
            viewModel.ActualizarOpciones();
        }

        protected override void OnDisappearing()
        {
            paginaVisible = false;
            viewModel.CancelarBusqueda();

            base.OnDisappearing();
        }

        protected override void OnSizeAllocated(
            double width,
            double height)
        {
            base.OnSizeAllocated(width, height);

            if (paginaVisible)
                AjustarCantidadColumnas(width);
        }

        private void AjustarCantidadColumnas(
            double width)
        {
            if (width <= 0 ||
                OpcionesGridLayout == null)
            {
                return;
            }

            int nuevasColumnas =
                width >= 1180
                    ? 3
                    : width >= 680
                        ? 2
                        : 1;

            if (cantidadColumnasActual == nuevasColumnas)
                return;

            cantidadColumnasActual = nuevasColumnas;
            OpcionesGridLayout.Span = nuevasColumnas;
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
