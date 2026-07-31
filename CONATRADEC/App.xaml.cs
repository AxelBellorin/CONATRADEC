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

            /*
             * Impide abrir pantallas que necesitan servidor durante una sesión
             * iniciada expresamente en modo Sin conexión.
             */
            ModoOfflineNavigationService.Instance
                .VincularShell(shell);

            /*
             * Restaura País, Departamento y Municipio del terreno cuando un
             * análisis se abre por primera vez en modo edición.
             */
            AnalisisEdicionUbicacionService.Instance
                .VincularShell(shell);

            /*
             * Restaura de forma determinista la selección de elementos, las
             * fuentes y los resultados guardados de Balance y Mixta.
             */
            AnalisisEdicionCalculosDeterministaService.Instance
                .VincularShell(shell);

            /*
             * Si el listado del terreno solo trae PropietarioId, recupera los
             * datos completos del propietario al abrir el formulario de edición.
             */
            TerrenoPropietarioEdicionService.Instance
                .VincularShell(shell);

            DispositivoConexionService.Instance.VincularShell(shell);
            DispositivoConexionService.Instance.Iniciar();

            window.Created += (_, _) =>
            {
                IsWindowClosing = false;

                SessionValidationService.Instance
                    .Iniciar();

                SesionInactividadService.Instance
                    .ReanudarSesion();
            };

            window.Resumed += (_, _) =>
            {
                IsWindowClosing = false;

                SessionValidationService.Instance
                    .Iniciar();

                SesionInactividadService.Instance
                    .ReanudarSesion();

                _ = DispositivoConexionService.Instance
                    .ReanudarAsync();
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

                _ = DispositivoConexionService.Instance
                    .SuspenderAsync();
            };

            window.Destroying += (_, _) =>
            {
                IsWindowClosing = true;

                SessionValidationService.Instance
                    .Detener();

                SesionInactividadService.Instance
                    .Pausar();

                _ = DispositivoConexionService.Instance
                    .DetenerAsync();
            };

            return window;
        }
    }
}
