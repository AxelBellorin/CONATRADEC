using CONATRADEC.Services;
using CONATRADEC.Views;

namespace CONATRADEC
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            // Fuerza la app a usar tema claro aunque el sistema esté en modo oscuro
            UserAppTheme = AppTheme.Light;
        }

        protected override Window CreateWindow(
            IActivationState? activationState)
        {
            var shell = new AppShell();
            var window = new Window(shell);

#if WINDOWS
            window.Title = "ConatraCafé Soil";
#endif

            DispositivoConexionService.Instance.VincularShell(shell);
            DispositivoConexionService.Instance.Iniciar();

            // Los eventos de Window funcionan en Android y Windows. El cierre
            // explícito se reporta cuando el sistema operativo lo permite; si
            // no lo permite, el servidor aplica la tolerancia de dos minutos.
            window.Resumed += (_, _) =>
                _ = DispositivoConexionService.Instance.ReanudarAsync();

            window.Stopped += (_, _) =>
                _ = DispositivoConexionService.Instance.SuspenderAsync();

            window.Destroying += (_, _) =>
                _ = DispositivoConexionService.Instance.DetenerAsync();

            return window;
        }
    }
}
