using CONATRADEC.Services;
using CONATRADEC.Views;

namespace CONATRADEC
{
    public partial class App : Application
    {
        /// <summary>
        /// Indica que la ventana dejó de estar activa o está siendo destruida.
        /// En Windows se utiliza para ignorar únicamente las excepciones COM
        /// producidas por controles WinUI que ya fueron liberados durante el cierre.
        /// </summary>
        public static bool IsWindowClosing { get; private set; }

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
            {
                IsWindowClosing = false;
                SessionValidationService.Instance.Iniciar();
            };

            window.Resumed += (_, _) =>
            {
                IsWindowClosing = false;

                SessionValidationService.Instance.Iniciar();
                _ = DispositivoConexionService.Instance.ReanudarAsync();
            };

            window.Stopped += (_, _) =>
            {
                /*
                 * Se establece antes de detener los servicios. En Windows,
                 * los controles Entry pueden ejecutar Unfocused mientras la
                 * ventana nativa ya está comenzando a cerrarse.
                 */
                IsWindowClosing = true;

                SessionValidationService.Instance.Detener();
                _ = DispositivoConexionService.Instance.SuspenderAsync();
            };

            window.Destroying += (_, _) =>
            {
                /*
                 * Se mantiene activa la bandera durante toda la destrucción
                 * de los controles nativos de WinUI.
                 */
                IsWindowClosing = true;

                SessionValidationService.Instance.Detener();
                _ = DispositivoConexionService.Instance.DetenerAsync();
            };

            return window;
        }
    }
}