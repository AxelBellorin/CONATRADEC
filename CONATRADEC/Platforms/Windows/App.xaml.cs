// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using System.Runtime.InteropServices;

namespace CONATRADEC.WinUI
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : MauiWinUIApplication
    {
        /// <summary>
        /// Initializes the singleton application object. This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            InitializeComponent();

            /*
             * Captura las excepciones que llegan al dispatcher principal de WinUI.
             * Solamente se marcarán como controladas las COMException producidas
             * durante el cierre de la ventana.
             */
            UnhandledException += App_UnhandledException;
        }

        protected override MauiApp CreateMauiApp() =>
            MauiProgram.CreateMauiApp();

        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            base.OnLaunched(args);

            try
            {
                IntPtr hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
                var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
                var appWindow = AppWindow.GetFromWindowId(windowId);

                if (appWindow.Presenter is OverlappedPresenter presenter)
                    presenter.Maximize();
            }
            catch
            {
                // En caso extremo, ignorar la falla al maximizar la ventana.
            }
        }

        /// <summary>
        /// Evita que Visual Studio detenga la depuración cuando un control
        /// WinUI ya fue liberado como parte del cierre normal de la aplicación.
        /// No oculta excepciones COM mientras la aplicación continúa activa.
        /// </summary>
        private void App_UnhandledException(
            object sender,
            Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
        {
            if (!global::CONATRADEC.App.IsWindowClosing)
                return;

            if (!ContainsComException(e.Exception))
                return;

            e.Handled = true;
        }

        /// <summary>
        /// Recorre la excepción y sus excepciones internas para determinar
        /// si el origen fue una COMException de WinUI.
        /// </summary>
        private static bool ContainsComException(Exception? exception)
        {
            Exception? currentException = exception;

            while (currentException is not null)
            {
                if (currentException is COMException)
                    return true;

                currentException = currentException.InnerException;
            }

            return false;
        }
    }
}