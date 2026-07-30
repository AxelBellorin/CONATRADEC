using CONATRADEC.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

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
        private UIElement? raizActividad;

        public App()
        {
            InitializeComponent();

            UnhandledException += App_UnhandledException;

            AppDomain.CurrentDomain.UnhandledException +=
                CurrentDomain_UnhandledException;

            TaskScheduler.UnobservedTaskException +=
                TaskScheduler_UnobservedTaskException;
        }

        protected override MauiApp CreateMauiApp()
        {
            return MauiProgram.CreateMauiApp();
        }

        protected override void OnLaunched(
            LaunchActivatedEventArgs args)
        {
            base.OnLaunched(args);

            Microsoft.Maui.Controls.Window? mauiWindow =
                Microsoft.Maui.Controls.Application
                    .Current?
                    .Windows
                    .FirstOrDefault();

            if (mauiWindow == null)
                return;

            mauiWindow.HandlerChanged +=
                (_, _) => VincularActividad(mauiWindow);

            VincularActividad(mauiWindow);
        }

        private void VincularActividad(
            Microsoft.Maui.Controls.Window mauiWindow)
        {
            if (mauiWindow.Handler?.PlatformView
                    is not Microsoft.UI.Xaml.Window nativeWindow ||
                nativeWindow.Content is not UIElement nuevaRaiz ||
                ReferenceEquals(
                    raizActividad,
                    nuevaRaiz))
            {
                return;
            }

            if (raizActividad != null)
            {
                raizActividad.PointerPressed -=
                    AlRegistrarActividadPuntero;

                raizActividad.PointerWheelChanged -=
                    AlRegistrarActividadPuntero;

                raizActividad.KeyDown -=
                    AlRegistrarActividadTeclado;
            }

            raizActividad = nuevaRaiz;

            raizActividad.PointerPressed +=
                AlRegistrarActividadPuntero;

            raizActividad.PointerWheelChanged +=
                AlRegistrarActividadPuntero;

            raizActividad.KeyDown +=
                AlRegistrarActividadTeclado;
        }

        private static void AlRegistrarActividadPuntero(
            object sender,
            PointerRoutedEventArgs e)
        {
            SesionInactividadService.Instance
                .RegistrarActividad();
        }

        private static void AlRegistrarActividadTeclado(
            object sender,
            KeyRoutedEventArgs e)
        {
            SesionInactividadService.Instance
                .RegistrarActividad();
        }

        private void App_UnhandledException(
            object sender,
            WinUiUnhandledExceptionEventArgs e)
        {
            RegistrarExcepcion(
                "WinUI.UnhandledException",
                e.Exception);

            if (!global::CONATRADEC.App.IsWindowClosing)
                return;

            if (!ContainsComException(e.Exception))
                return;

            e.Handled = true;
        }

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

        private static void TaskScheduler_UnobservedTaskException(
            object? sender,
            UnobservedTaskExceptionEventArgs e)
        {
            RegistrarExcepcion(
                "TaskScheduler.UnobservedTaskException",
                e.Exception);

            e.SetObserved();
        }

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

                if (!string.IsNullOrWhiteSpace(
                        detalleAdicional))
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
