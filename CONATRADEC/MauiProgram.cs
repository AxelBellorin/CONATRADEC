using CommunityToolkit.Maui;                     // Importa la extensión CommunityToolkit para MAUI (Snackbar, Toast, etc.)
using CONATRADEC.Services;                       // Espacio de nombres de los servicios de la aplicación
using CONATRADEC.ViewModels;                     // Espacio de nombres de los ViewModels
using CONATRADEC.Views;                          // Espacio de nombres de las vistas (Pages)
using Microsoft.Extensions.DependencyInjection;  // Permite registrar servicios y ViewModels (Inyección de dependencias)
using Microsoft.Extensions.Logging;              // Habilita el sistema de logging para depuración
using Microsoft.Maui.LifecycleEvents;            // Permite configurar eventos del ciclo de vida (Android, Windows, iOS)
using Microsoft.Maui.Controls.Maps;



#if WINDOWS
using Microsoft.UI;                              // API de interfaz de usuario de Windows
using Microsoft.UI.Windowing;                    // Control de ventana nativa en WinUI
using Windows.Graphics;                          // Permite manejar tamaños y coordenadas de ventana
#endif

namespace CONATRADEC
{
    public static class MauiProgram
    {
        /// <summary>
        /// Punto de configuración inicial del proyecto .NET MAUI.
        /// Se ejecuta una única vez al iniciar la aplicación.
        /// </summary>
        public static MauiApp CreateMauiApp()
        {
            // ==========================================================
            // Crea el constructor base del aplicativo MAUI
            // ==========================================================
            var builder = MauiApp.CreateBuilder();

            // ==========================================================
            // Registra la clase principal App.xaml.cs
            // ==========================================================
            builder
                .UseMauiApp<App>()
#if !WINDOWS
        .UseMauiMaps()
#endif
                // ======================================================
                // Habilita la librería CommunityToolkit.Maui
                //    (para usar DisplaySnackbar, Popup, Alert, etc.)
                // ======================================================
                .UseMauiCommunityToolkit(options =>
                {
                    // Permite usar Snackbars en entorno Windows
                    options.SetShouldEnableSnackbarOnWindows(true);
                })

                // ======================================================
                // Configura fuentes personalizadas de la aplicación
                // ======================================================
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                    fonts.AddFont("Montserrat-Bold.ttf", "MontserratBold");
                    fonts.AddFont("Montserrat-Medium.ttf", "MontserratMedium");
                });

            // ==========================================================
            // Logging (solo en modo DEBUG)
            // ==========================================================
#if DEBUG
            builder.Logging.AddDebug();
#endif

            // ==========================================================
            // Configuración especial para entorno Windows
            // ==========================================================
#if WINDOWS
            builder.ConfigureLifecycleEvents(events =>
            {
                events.AddWindows(wndLifeCycleBuilder =>
                {
                    wndLifeCycleBuilder.OnWindowCreated(window =>
                    {
                        // Obtiene el identificador nativo de la ventana
                        IntPtr nativeWindowHandle = WinRT.Interop.WindowNative.GetWindowHandle(window);
                        WindowId win32WindowsId = Win32Interop.GetWindowIdFromWindow(nativeWindowHandle);
                        AppWindow winuiAppWindow = AppWindow.GetFromWindowId(win32WindowsId);

                        // Si el modo de presentación lo permite, maximiza la ventana
                        if (winuiAppWindow.Presenter is OverlappedPresenter p)
                            p.Maximize();
                        else
                        {
                            // De lo contrario, define un tamaño y posición manual centrada
                            const int width = 1200;
                            const int height = 800;
                            winuiAppWindow.MoveAndResize(new RectInt32(
                                1920 / 2 - width / 2,   // Centra horizontalmente
                                1080 / 2 - height / 2,  // Centra verticalmente
                                width, height));        // Define dimensiones
                        }
                    });
                });
            });
#endif       
            // ==========================================================
            // Retorna la instancia final configurada del aplicativo
            // ==========================================================
            return builder.Build();
        }
    }
}
