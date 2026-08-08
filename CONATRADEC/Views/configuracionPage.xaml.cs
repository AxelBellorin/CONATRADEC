using CONATRADEC.Services;
using CONATRADEC.ViewModels;

namespace CONATRADEC.Views
{
    public partial class configuracionPage :
        ContentPage
    {
        private static bool rutasRegistradas;

        private readonly ConfiguracionViewModel
            viewModel = new();

        private int cantidadColumnasActual;
        private bool paginaVisible;

        public configuracionPage()
        {
            InitializeComponent();

            Shell.Current.FlyoutBehavior =
                FlyoutBehavior.Disabled;

            BindingContext =
                viewModel;

            RegistrarRutas();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            paginaVisible = true;

            AjustarCantidadColumnas(
                Width);

            ActualizarAccionesSistema();
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
            base.OnSizeAllocated(
                width,
                height);

            if (paginaVisible)
            {
                AjustarCantidadColumnas(
                    width);
            }
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

            if (cantidadColumnasActual ==
                nuevasColumnas)
            {
                return;
            }

            cantidadColumnasActual =
                nuevasColumnas;

            OpcionesGridLayout.Span =
                nuevasColumnas;
        }

        /// <summary>
        /// Datos sin conexión continúa respetando su permiso original.
        /// Cerrar sesión permanece siempre disponible dentro de Configuración.
        /// Si el usuario no puede trabajar sin conexión, la tarjeta de salida
        /// ocupa el ancho completo para no dejar un espacio vacío.
        /// </summary>
        private void ActualizarAccionesSistema()
        {
            if (DatosSinConexionCard == null ||
                CerrarSesionCard == null)
            {
                return;
            }

            bool mostrarSinConexion =
                DatosSinConexionPermisos.TienePermiso;

            DatosSinConexionCard.IsVisible =
                mostrarSinConexion;

            if (mostrarSinConexion)
            {
                Grid.SetColumn(
                    CerrarSesionCard,
                    1);

                Grid.SetColumnSpan(
                    CerrarSesionCard,
                    1);
            }
            else
            {
                Grid.SetColumn(
                    CerrarSesionCard,
                    0);

                Grid.SetColumnSpan(
                    CerrarSesionCard,
                    2);
            }
        }

        private async void DatosSinConexionCard_Tapped(
            object? sender,
            TappedEventArgs e)
        {
            if (!DatosSinConexionPermisos.TienePermiso)
            {
                DatosSinConexionCard.IsVisible = false;
                ActualizarAccionesSistema();
                return;
            }

            await viewModel.GoToAsyncParameters(
                "//DatosSinConexionPage");
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

            Routing.RegisterRoute(
                AppRoutes.ConfiguracionUnidades,
                typeof(configuracionUnidadesPage));

            /*
             * El catálogo de motivos se muestra como una opción normal dentro
             * del grupo Inteligencia artificial. La ruta se registra una sola
             * vez antes de que el usuario pueda abrir la tarjeta.
             */
            MotivoDevolucionTecnicoRoutes.AsegurarRegistro();

            rutasRegistradas = true;
        }
    }
}
