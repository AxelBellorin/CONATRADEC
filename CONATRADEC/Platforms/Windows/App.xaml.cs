using Microsoft.UI.Xaml;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

// Alias para evitar la ambigüedad entre System y WinUI.
using SystemUnhandledExceptionEventArgs =
    System.UnhandledExceptionEventArgs;

using WinUiUnhandledExceptionEventArgs =
    Microsoft.UI.Xaml.UnhandledExceptionEventArgs;

namespace CONATRADEC.WinUI
{
    /// <summary>
    /// Punto de entrada específico de Windows
    /// para ConatraCafé Soil.
    /// </summary>
    public partial class App : MauiWinUIApplication
    {
        public App()
        {
            InitializeComponent();

            /*
             * Captura excepciones no controladas provenientes
             * del dispatcher principal de WinUI.
             */
            UnhandledException += App_UnhandledException;

            /*
             * Captura errores generales no controlados
             * provenientes del entorno de .NET.
             */
            AppDomain.CurrentDomain.UnhandledException +=
                CurrentDomain_UnhandledException;

            /*
             * Captura errores de tareas que finalizaron
             * sin que su excepción fuera observada.
             */
            TaskScheduler.UnobservedTaskException +=
                TaskScheduler_UnobservedTaskException;
        }

        /// <summary>
        /// Crea y configura la aplicación MAUI.
        /// </summary>
        protected override MauiApp CreateMauiApp()
        {
            return MauiProgram.CreateMauiApp();
        }

        /// <summary>
        /// Captura excepciones provenientes del dispatcher
        /// principal de WinUI.
        /// </summary>
        private void App_UnhandledException(
            object sender,
            WinUiUnhandledExceptionEventArgs e)
        {
            RegistrarExcepcion(
                "WinUI.UnhandledException",
                e.Exception);

            /*
             * Durante el cierre normal algunos controles WinUI
             * pueden intentar acceder a objetos COM que ya fueron
             * liberados.
             *
             * Solamente se ignoran esas excepciones cuando
             * la ventana realmente se está cerrando.
             */
            if (!global::CONATRADEC.App.IsWindowClosing)
                return;

            if (!ContainsComException(e.Exception))
                return;

            e.Handled = true;
        }

        /// <summary>
        /// Captura excepciones generales no controladas
        /// por el entorno de .NET.
        /// </summary>
        private static void CurrentDomain_UnhandledException(
            object sender,
            SystemUnhandledExceptionEventArgs e)
        {
            Exception? exception =
                e.ExceptionObject as Exception;

            RegistrarExcepcion(
                "AppDomain.UnhandledException",
                exception,
                $"Finalizando proceso: {e.IsTerminating}");
        }

        /// <summary>
        /// Captura excepciones de tareas que no fueron observadas.
        /// </summary>
        private static void TaskScheduler_UnobservedTaskException(
            object? sender,
            UnobservedTaskExceptionEventArgs e)
        {
            RegistrarExcepcion(
                "TaskScheduler.UnobservedTaskException",
                e.Exception);

            /*
             * Marca la excepción como observada para evitar
             * que una tarea secundaria afecte la aplicación.
             */
            e.SetObserved();
        }

        /// <summary>
        /// Determina si la excepción o alguna excepción interna
        /// corresponde a una COMException.
        /// </summary>
        private static bool ContainsComException(
            Exception? exception)
        {
            Exception? currentException = exception;

            while (currentException is not null)
            {
                if (currentException is COMException)
                    return true;

                currentException =
                    currentException.InnerException;
            }

            return false;
        }

        /// <summary>
        /// Guarda la información de una excepción en un archivo
        /// local para diagnosticar cierres de la aplicación.
        /// </summary>
        private static void RegistrarExcepcion(
            string origen,
            Exception? exception,
            string? detalleAdicional = null)
        {
            try
            {
                string carpetaLogs = Path.Combine(
                    Environment.GetFolderPath(
                        Environment.SpecialFolder.LocalApplicationData),
                    "CONATRADEC",
                    "Logs");

                Directory.CreateDirectory(carpetaLogs);

                string rutaLog = Path.Combine(
                    carpetaLogs,
                    "crash-windows.log");

                var contenido = new StringBuilder();

                contenido.AppendLine(
                    "==================================================");

                contenido.AppendLine(
                    $"Fecha local: {DateTime.Now:dd/MM/yyyy HH:mm:ss}");

                contenido.AppendLine(
                    $"Fecha UTC: {DateTime.UtcNow:O}");

                contenido.AppendLine(
                    $"Origen: {origen}");

                if (!string.IsNullOrWhiteSpace(detalleAdicional))
                {
                    contenido.AppendLine(
                        $"Detalle: {detalleAdicional}");
                }

                if (exception is null)
                {
                    contenido.AppendLine(
                        "No fue posible obtener el objeto Exception.");
                }
                else
                {
                    contenido.AppendLine(
                        $"Tipo: {exception.GetType().FullName}");

                    contenido.AppendLine(
                        $"Mensaje: {exception.Message}");

                    contenido.AppendLine(
                        $"HResult: 0x{exception.HResult:X8}");

                    contenido.AppendLine(
                        "Excepción completa:");

                    contenido.AppendLine(
                        exception.ToString());
                }

                contenido.AppendLine();

                File.AppendAllText(
                    rutaLog,
                    contenido.ToString(),
                    Encoding.UTF8);

                Debug.WriteLine(
                    contenido.ToString());
            }
            catch
            {
                /*
                 * El registro de errores nunca debe provocar
                 * una excepción adicional.
                 */
            }
        }
    }
}