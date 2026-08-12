using CommunityToolkit.Maui;
using CONATRADEC.Behaviors;
using CONATRADEC.Services;
using CONATRADEC.ViewModels;
using CONATRADEC.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Controls;
using Microsoft.Maui.LifecycleEvents;
using System.Diagnostics;

#if WINDOWS
using Microsoft.UI.Xaml;
using System.Runtime.InteropServices;
#endif

namespace CONATRADEC
{
    public static class MauiProgram
    {
#if WINDOWS
        /*
         * Constante de Win32 utilizada para maximizar la ventana.
         * Se evita Microsoft.UI.Windowing.AppWindow porque estaba
         * provocando una excepción COM "Clase no registrada".
         */
        private const int SW_MAXIMIZE = 3;

        [DllImport(
            "user32.dll",
            SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool ShowWindow(
            IntPtr hWnd,
            int nCmdShow);

        /// <summary>
        /// WebView2 crea por defecto su carpeta de datos junto al ejecutable
        /// en aplicaciones Windows no empaquetadas. Cuando el programa se
        /// instala en una carpeta protegida, esa ubicación puede no permitir
        /// escritura y la creación del WebView puede fallar.
        ///
        /// Se fuerza una carpeta por usuario dentro de LocalAppData antes de
        /// que MAUI cree cualquier WebView. El cambio aplica solamente a
        /// Windows y conserva intacto el comportamiento de Android.
        /// </summary>
        private static void ConfigurarDatosWebView2()
        {
            try
            {
                string localAppData =
                    Environment.GetFolderPath(
                        Environment.SpecialFolder.LocalApplicationData);

                if (string.IsNullOrWhiteSpace(localAppData))
                    return;

                string userDataFolder =
                    Path.Combine(
                        localAppData,
                        "CONATRADEC",
                        "WebView2");

                Directory.CreateDirectory(userDataFolder);

                Environment.SetEnvironmentVariable(
                    "WEBVIEW2_USER_DATA_FOLDER",
                    userDataFolder,
                    EnvironmentVariableTarget.Process);
            }
            catch (Exception ex)
            {
                /*
                 * La configuración de WebView2 no debe impedir el arranque.
                 * Se registra para diagnóstico si Windows rechazara la ruta.
                 */
                Debug.WriteLine(
                    "No fue posible configurar la carpeta de datos " +
                    $"de WebView2: {ex}");
            }
        }
#endif

        public static MauiApp CreateMauiApp()
        {
#if WINDOWS
            /*
             * Debe ejecutarse antes de crear handlers o controles WebView.
             */
            ConfigurarDatosWebView2();
#endif

            var builder = MauiApp.CreateBuilder();

            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit(options =>
                {
                    options.SetShouldEnableSnackbarOnWindows(true);
                })
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont(
                        "OpenSans-Regular.ttf",
                        "OpenSansRegular");

                    fonts.AddFont(
                        "OpenSans-Semibold.ttf",
                        "OpenSansSemibold");

                    fonts.AddFont(
                        "Montserrat-Bold.ttf",
                        "MontserratBold");

                    fonts.AddFont(
                        "Montserrat-Medium.ttf",
                        "MontserratMedium");
                });

            /*
             * Registros globales de controles y comportamientos.
             */
            SwipeViewRightClick.Register();
            KeyboardDismissBehavior.Register();
            LoginTabletResponsiveMapper.Register();
            LoginPhoneViewportMapper.Register();
            NuevoAnalisisInfoResponsiveMapper.Register();
            DecimalAnalysisEntryMapper.Register();

            /*
             * Windows: las rutas absolutas guardadas en AppDataDirectory
             * se abren mediante StreamImageSource. Esto evita que WinUI
             * las trate como recursos incluidos dentro del instalable.
             * En Android el registro no realiza ninguna modificación.
             */
            WindowsLocalImageMapper.Register();

            /*
             * Selector global En línea / Sin conexión
             * dentro del inicio de sesión.
             */
            ModoSesionLoginMapper.Register();

            /*
             * Conserva la navegación segura correspondiente al análisis
             * y a la preparación de datos sin conexión.
             */
            OfflineContenidoPageMapper.Register();

            /*
             * Restablece la regla global de escritura offline:
             * solamente el módulo de análisis puede agregar, editar o eliminar
             * mientras la sesión fue iniciada en modo Sin conexión.
             */
            OfflineCrudPageMapper.Register();

#if DEBUG
            builder.Logging.AddDebug();
#endif

#if WINDOWS
            builder.ConfigureLifecycleEvents(events =>
            {
                events.AddWindows(windows =>
                {
                    windows.OnWindowCreated(window =>
                    {
                        /*
                         * Fuerza el tema claro únicamente en Windows.
                         */
                        try
                        {
                            if (window.Content
                                is FrameworkElement rootElement)
                            {
                                rootElement.RequestedTheme =
                                    ElementTheme.Light;
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine(
                                "No fue posible establecer el tema " +
                                $"de Windows: {ex}");
                        }

                        /*
                         * Maximiza la ventana mediante Win32.
                         *
                         * No utiliza AppWindow, WindowId ni
                         * OverlappedPresenter, evitando la activación
                         * COM que estaba ocasionando el error
                         * 0x80040154: Clase no registrada.
                         */
                        try
                        {
                            IntPtr windowHandle =
                                WinRT.Interop.WindowNative
                                    .GetWindowHandle(window);

                            if (windowHandle != IntPtr.Zero)
                            {
                                ShowWindow(
                                    windowHandle,
                                    SW_MAXIMIZE);
                            }
                        }
                        catch (Exception ex)
                        {
                            /*
                             * Maximizar es solamente una mejora visual.
                             * Una falla aquí nunca debe cerrar la aplicación.
                             */
                            Debug.WriteLine(
                                "No fue posible maximizar la ventana " +
                                $"de Windows: {ex}");
                        }
                    });
                });
            });
#endif

            return builder.Build();
        }
    }
}
