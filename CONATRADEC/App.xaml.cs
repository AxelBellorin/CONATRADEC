using CONATRADEC.Services;
using CONATRADEC.Views;

namespace CONATRADEC
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            // Fuerza la app a usar tema claro aunque el sistema esté en modo oscuro.
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

            /*
             * La validación debe arrancar también al abrir una sesión que ya
             * estaba guardada. Antes solo comenzaba al hacer un login nuevo,
             * por eso una sesión anterior podía conservar permisos viejos.
             */
            window.Created += (_, _) =>
                SessionValidationService.Instance.Iniciar();

            window.Resumed += (_, _) =>
            {
                SessionValidationService.Instance.Iniciar();
                _ = DispositivoConexionService.Instance.ReanudarAsync();
            };

            window.Stopped += (_, _) =>
            {
                SessionValidationService.Instance.Detener();
                _ = DispositivoConexionService.Instance.SuspenderAsync();
            };

            window.Destroying += (_, _) =>
            {
                SessionValidationService.Instance.Detener();
                _ = DispositivoConexionService.Instance.DetenerAsync();
            };

            return window;
        }
    }
}
