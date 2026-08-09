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
#endif

        public static MauiApp CreateMauiApp()
        {
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
