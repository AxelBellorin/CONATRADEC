using CONATRADEC.Services;
using CONATRADEC.Views;

namespace CONATRADEC
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            // Pantallas secundarias.
            Routing.RegisterRoute(
                AppRoutes.MapaSeleccion,
                typeof(MapaSeleccionPage));

            Routing.RegisterRoute(
                AppRoutes.FotosTerrenoGaleria,
                typeof(FotosTerrenoGaleriaPage));

            Routing.RegisterRoute(
                AppRoutes.AnalisisGuardadoDetalle,
                typeof(AnalisisGuardadoDetallePage));

            Routing.RegisterRoute(
                AppRoutes.EditarAnalisisGuardado,
                typeof(EditarAnalisisGuardadoPage));
        }

        /// <summary>
        /// Evita que el botón físico o gesto de retroceso de Android
        /// cierre la aplicación o cambie de página accidentalmente.
        /// Los botones internos de la aplicación continúan funcionando.
        /// </summary>
        protected override bool OnBackButtonPressed()
        {
#if ANDROID
            // true significa que el evento fue controlado
            // y que Android no debe realizar la navegación atrás.
            return true;
#else
            return base.OnBackButtonPressed();
#endif
        }
    }
}