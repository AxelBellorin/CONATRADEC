using CommunityToolkit.Maui;                     // Importa CommunityToolkit para MAUI (Snackbar, Toast, Popup, etc.)
using CONATRADEC.Behaviors;                      // Comportamientos globales para controles MAUI
using CONATRADEC.Services;                       // Espacio de nombres de los servicios de la aplicación
using CONATRADEC.ViewModels;                     // Espacio de nombres de los ViewModels
using CONATRADEC.Views;                          // Espacio de nombres de las vistas/pages
using Microsoft.Extensions.DependencyInjection;  // Permite registrar servicios y ViewModels por inyección de dependencias
using Microsoft.Extensions.Logging;              // Habilita logging para depuración
using Microsoft.Maui.Controls;                   // Controles principales de MAUI
using Microsoft.Maui.LifecycleEvents;            // Permite configurar eventos del ciclo de vida de la app

#if WINDOWS
using Microsoft.UI;                              // API de interfaz de usuario de Windows
using Microsoft.UI.Windowing;                    // Control de ventana nativa en WinUI
using Microsoft.UI.Xaml;                         // Permite forzar tema claro en Windows
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

                // ======================================================
                // Habilita la librería CommunityToolkit.Maui
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
            // Habilita globalmente el clic derecho en los SwipeView.
            // En Windows muestra un menú contextual con las mismas acciones
            // Editar/Eliminar. En Android no modifica el swipe táctil.
            // ==========================================================
            SwipeViewRightClick.Register();

            // ==========================================================
            // Logging solo en modo DEBUG
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
                        // ==================================================
                        // Fuerza tema claro en la ventana nativa de Windows
                        // Esto evita que el modo oscuro de Windows cambie
                        // colores internos de controles WinUI.
                        // ==================================================
                        if (window.Content is FrameworkElement rootElement)
                        {
                            rootElement.RequestedTheme = ElementTheme.Light;
                        }

                        // ==================================================
                        // Obtiene el identificador nativo de la ventana
                        // ==================================================
                        IntPtr nativeWindowHandle = WinRT.Interop.WindowNative.GetWindowHandle(window);
                        WindowId win32WindowsId = Win32Interop.GetWindowIdFromWindow(nativeWindowHandle);
                        AppWindow winuiAppWindow = AppWindow.GetFromWindowId(win32WindowsId);

                        // ==================================================
                        // Maximiza la ventana si el modo lo permite
                        // ==================================================
                        if (winuiAppWindow.Presenter is OverlappedPresenter p)
                            p.Maximize();
                        else
                        {
                            // ==============================================
                            // Si no puede maximizarse, se define tamaño manual
                            // y se centra aproximadamente en pantalla.
                            // ==============================================
                            const int width = 1200;
                            const int height = 800;

                            winuiAppWindow.MoveAndResize(new RectInt32(
                                1920 / 2 - width / 2,
                                1080 / 2 - height / 2,
                                width,
                                height));
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
