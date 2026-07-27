using CommunityToolkit.Maui;
using CONATRADEC.Behaviors;
using CONATRADEC.Services;
using CONATRADEC.ViewModels;
using CONATRADEC.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Controls;
using Microsoft.Maui.LifecycleEvents;

#if WINDOWS
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Windows.Graphics;
#endif

namespace CONATRADEC
{
    public static class MauiProgram
    {
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

            SwipeViewRightClick.Register();
            KeyboardDismissBehavior.Register();
            LoginTabletResponsiveMapper.Register();
            NuevoAnalisisInfoResponsiveMapper.Register();

            /* Selector global En línea / Sin conexión dentro del login. */
            ModoSesionLoginMapper.Register();

            /*
             * Conserva únicamente la navegación segura de Nuevo análisis.
             * Ya no agrega indicadores ni verificaciones automáticas.
             */
            OfflineContenidoPageMapper.Register();

#if DEBUG
            builder.Logging.AddDebug();
#endif

#if WINDOWS
            builder.ConfigureLifecycleEvents(events =>
            {
                events.AddWindows(wndLifeCycleBuilder =>
                {
                    wndLifeCycleBuilder.OnWindowCreated(window =>
                    {
                        if (window.Content is FrameworkElement rootElement)
                        {
                            rootElement.RequestedTheme =
                                ElementTheme.Light;
                        }

                        IntPtr nativeWindowHandle =
                            WinRT.Interop.WindowNative
                                .GetWindowHandle(window);

                        WindowId win32WindowsId =
                            Win32Interop.GetWindowIdFromWindow(
                                nativeWindowHandle);

                        AppWindow winuiAppWindow =
                            AppWindow.GetFromWindowId(
                                win32WindowsId);

                        if (winuiAppWindow.Presenter
                            is OverlappedPresenter presenter)
                        {
                            presenter.Maximize();
                        }
                        else
                        {
                            const int width = 1200;
                            const int height = 800;

                            winuiAppWindow.MoveAndResize(
                                new RectInt32(
                                    1920 / 2 - width / 2,
                                    1080 / 2 - height / 2,
                                    width,
                                    height));
                        }
                    });
                });
            });
#endif

            return builder.Build();
        }
    }
}
