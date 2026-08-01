using CONATRADEC.Models;
using CONATRADEC.Services;
using CONATRADEC.ViewModels;
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

        /// <summary>
        /// Vincula todos los servicios que dependen de la instancia actual del
        /// AppShell. Debe ejecutarse tanto al iniciar la aplicación como cuando
        /// la sesión reconstruye el Shell después de expirar por inactividad.
        /// </summary>
        public static void VincularServiciosShell(
            AppShell shell)
        {
            ArgumentNullException.ThrowIfNull(shell);

            ModoOfflineNavigationService.Instance
                .VincularShell(shell);

            AnalisisEdicionUbicacionService.Instance
                .VincularShell(shell);

            /*
             * La restauración de los cálculos guardados se ejecuta desde
             * MultiCalculoPage después de que Balance, Enmienda y Mixta hayan
             * terminado su inicialización.
             *
             * Se retiraron los dos observadores globales de navegación porque
             * ambos intentaban restaurar las mismas pestañas al mismo tiempo y
             * podían competir con la carga interna de Fertilización Mixta.
             */

            TerrenoPropietarioEdicionService.Instance
                .VincularShell(shell);

            DispositivoConexionService.Instance
                .VincularShell(shell);
        }

        protected override Window CreateWindow(
            IActivationState? activationState)
        {
            var shell = new AppShell();
            var window = new Window(shell);

#if WINDOWS
            window.Title = "ConatraCafé Soil";
#endif

            /*
             * Recupera un estado de descarga que haya quedado guardado como
             * "Sincronizando" después de cerrar la aplicación de forma inesperada.
             */
            SincronizacionOfflineEstadoRecuperacionService
                .RecuperarSiInterrumpida();

            VincularServiciosShell(shell);

            DispositivoConexionService.Instance.Iniciar();

            window.Created += (_, _) =>
            {
                IsWindowClosing = false;

                SessionValidationService.Instance
                    .Iniciar();

                SesionInactividadService.Instance
                    .ReanudarSesion();

                WindowsWindowRecoveryService
                    .RestaurarYActivar();
            };

            window.Resumed += (_, _) =>
            {
                IsWindowClosing = false;

                SessionValidationService.Instance
                    .Iniciar();

                SesionInactividadService.Instance
                    .ReanudarSesion();

                /*
                 * En Windows el evento Resumed puede ejecutarse mientras WinRT
                 * todavía reactiva los perfiles de red. La envoltura segura
                 * retrasa brevemente la consulta y observa cualquier excepción,
                 * evitando que una función secundaria de telemetría cierre la app.
                 */
                _ = DispositivoConexionLifecycleSeguroService.Instance
                    .ReanudarAsync();

                WindowsWindowRecoveryService
                    .RestaurarYActivar();
            };

            window.Stopped += (_, _) =>
            {
                /*
                 * Se establece antes de detener los servicios. En Windows,
                 * los controles Entry pueden ejecutar Unfocused mientras la
                 * ventana nativa ya está comenzando a cerrarse.
                 */
                IsWindowClosing = true;

                SessionValidationService.Instance
                    .Detener();

                SesionInactividadService.Instance
                    .Pausar();

                _ = DispositivoConexionLifecycleSeguroService.Instance
                    .SuspenderAsync();
            };

            window.Destroying += (_, _) =>
            {
                IsWindowClosing = true;

                SessionValidationService.Instance
                    .Detener();

                SesionInactividadService.Instance
                    .Pausar();

                _ = DispositivoConexionLifecycleSeguroService.Instance
                    .DetenerAsync();
            };

            return window;
        }
    }
}
