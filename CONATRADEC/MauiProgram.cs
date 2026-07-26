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
            // Habilita globalmente el cierre del teclado al finalizar
            // campos de entrada o ejecutar búsquedas.
            // El toque fuera del campo se controla desde MainActivity.
            // ==========================================================
            KeyboardDismissBehavior.Register();

            // ==========================================================
            // Adapta el login para tabletas Android.
            // ==========================================================
            LoginTabletResponsiveMapper.Register();

            // ==========================================================
            // Evita que el texto informativo del formulario de análisis
            // se corte en teléfonos con pantalla estrecha.
            // ==========================================================
            NuevoAnalisisInfoResponsiveMapper.Register();

            // ==========================================================
            // Agrega el estado de sincronización a Noticias y Álbum sin
            // modificar sus archivos XAML o code-behind.
            // ==========================================================
            OfflineContenidoPageMapper.Register();

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
                        // Fuerza tema claro en la ventana nativa de Windows.
                        if (window.Content is FrameworkElement rootElement)
                        {
                            rootElement.RequestedTheme = ElementTheme.Light;
                        }

                        IntPtr nativeWindowHandle =
                            WinRT.Interop.WindowNative.GetWindowHandle(window);

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
