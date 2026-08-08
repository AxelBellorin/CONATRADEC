using CONATRADEC.Models;
using CONATRADEC.Services;
using CONATRADEC.Views;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace CONATRADEC
{
    public partial class AppShell : Shell
    {
        private bool preparandoNuevoAnalisis;
        private bool corrigiendoFlyoutNativo;
        private bool actualizacionComprobadaEnSesion;
        private bool esperandoComprobacionDespuesLogin = true;
        private int comprobacionActualizacionEnCurso;

        public AppShell()
        {
            InitializeComponent();

            /*
             * CONATRADEC utiliza sus propios botones y menús de navegación.
             * El Flyout nativo de Shell no contiene opciones visibles, por lo
             * que debe permanecer desactivado para evitar el panel celeste vacío.
             */
            ConfigurarFlyoutNativo();

            /*
             * Algunas páginas antiguas intentan cambiar Shell.Current.FlyoutBehavior.
             * Este evento evita que el Flyout nativo vuelva a habilitarse.
             */
            PropertyChanged += AppShell_PropertyChanged;

            // Inicio, Noticias y Configuración están declarados como
            // ShellContent. Álbum conserva una ruta dinámica para crear una
            // instancia nueva y mantener estable su navegación interna.
            Routing.RegisterRoute(
                AppRoutes.AlbumFotos,
                typeof(albumFotosPage));

            // ================================================
            // CENTRO DE NOTICIAS E INTERESES
            // ================================================
            // noticiasPage está declarada como ShellContent en AppShell.xaml.
            // Las páginas secundarias continúan registradas dinámicamente.
            Routing.RegisterRoute(
                AppRoutes.NoticiaDetalle,
                typeof(noticiaDetallePage));

            Routing.RegisterRoute(
                AppRoutes.PublicacionesAdmin,
                typeof(publicacionesAdminPage));

            Routing.RegisterRoute(
                AppRoutes.PublicacionFormulario,
                typeof(publicacionFormPage));

            // Catálogo de tipos de publicación.
            Routing.RegisterRoute(
                AppRoutes.CategoriasPublicacion,
                typeof(categoriaPublicacionPage));

            Routing.RegisterRoute(
                AppRoutes.CategoriaPublicacionFormulario,
                typeof(categoriaPublicacionFormPage));

            // Descarga e instalación de actualizaciones Android y Windows.
            Routing.RegisterRoute(
                AppRoutes.ActualizacionAplicacion,
                typeof(ActualizacionAplicacionPage));

            // Pantallas secundarias.
            Routing.RegisterRoute(
                AppRoutes.MapaSeleccion,
                typeof(MapaSeleccionPage));

            Routing.RegisterRoute(
                AppRoutes.FotosTerrenoGaleria,
                typeof(FotosTerrenoGaleriaPage));

            Routing.RegisterRoute(
                AppRoutes.AnalisisGuardadoDetalle,
                typeof(AnalisisGuardadoDetallePage));

            Routing.RegisterRoute(
                AppRoutes.EditarAnalisisGuardado,
                typeof(EditarAnalisisGuardadoPage));

            // Pantallas secundarias del álbum botánico.
            Routing.RegisterRoute(
                AppRoutes.AlbumDetalle,
                typeof(albumDetallePage));

            Routing.RegisterRoute(
                AppRoutes.CategoriaAlbumFormulario,
                typeof(categoriaAlbumFormPage));

            Routing.RegisterRoute(
                AppRoutes.AlbumRegistroFormulario,
                typeof(albumRegistroFormPage));

            Routing.RegisterRoute(
                AppRoutes.AlbumFotosAdministrar,
                typeof(albumFotosAdminPage));

            Routing.RegisterRoute(
                AppRoutes.AlbumFotoVisor,
                typeof(albumFotoVisorPage));

            /*
             * Pantallas maestro-detalle de rangos nutricionales.
             * Se registran como rutas secundarias para que cada navegación
             * cree una página nueva y no conserve datos de otra categoría.
             */
            Routing.RegisterRoute(
                AppRoutes.RangoNutrienteDetalle,
                typeof(rangoNutrienteDetallePage));

            Routing.RegisterRoute(
                AppRoutes.RangoNutrienteCategoriaFormulario,
                typeof(rangoNutrienteCategoriaFormPage));

            Routing.RegisterRoute(
                AppRoutes.RangoNutrienteFormulario,
                typeof(rangoNutrienteFormPage));

            /*
             * Las páginas declaradas como ShellContent conservan su instancia.
             * Antes de volver a NuevoAnalisisFormPage desde MainPage se limpia
             * explícitamente el contexto y el cálculo temporal anterior.
             *
             * La carga visual del formulario se realiza exclusivamente en
             * NuevoAnalisisFormPage.OnAppearing. De esta forma no se inicializan
             * los mismos catálogos dos veces antes y después de navegar.
             */
            Navigating += AppShell_Navigating;

            /*
             * Después de cada navegación se agrega una flecha fija a todos
             * los formularios. El mismo evento detecta exclusivamente la
             * navegación que ocurre después de un inicio de sesión exitoso y
             * comprueba la versión sin bloquear el ingreso a la aplicación.
             */
            Navigated += AppShell_Navigated;
        }

        private async void AppShell_Navigated(
            object? sender,
            ShellNavigatedEventArgs e)
        {
            ConfigurarFlyoutNativo();

            FormNavigationHeaderService
                .AsegurarEnPaginaActual();

            await ComprobarActualizacionDespuesDelLoginAsync(e);
        }

        /// <summary>
        /// Consulta una sola vez por sesión, únicamente después de pasar desde
        /// LoginPage hacia una pantalla autenticada. La navegación ya terminó,
        /// por lo que esta operación no retrasa ni bloquea el inicio de sesión.
        /// </summary>
        private async Task ComprobarActualizacionDespuesDelLoginAsync(
            ShellNavigatedEventArgs e)
        {
            string rutaActual =
                e.Current?.Location?.OriginalString ??
                string.Empty;

            /*
             * Al cerrar sesión se prepara la próxima comprobación. No se hace
             * ninguna llamada HTTP mientras se muestra el login.
             */
            if (EsRutaLogin(rutaActual))
            {
                actualizacionComprobadaEnSesion = false;
                esperandoComprobacionDespuesLogin = true;
                Interlocked.Exchange(
                    ref comprobacionActualizacionEnCurso,
                    0);
                return;
            }

            bool acabaDeIniciarSesion =
                esperandoComprobacionDespuesLogin;

            esperandoComprobacionDespuesLogin = false;

            if (!acabaDeIniciarSesion ||
                actualizacionComprobadaEnSesion ||
                !ModoSesionService.EsEnLinea ||
                !ActualizacionAplicacionService.Instance
                    .PlataformaCompatible)
            {
                return;
            }

            if (Interlocked.Exchange(
                    ref comprobacionActualizacionEnCurso,
                    1) == 1)
            {
                return;
            }

            /*
             * Se marca antes de consultar para impedir llamadas duplicadas si
             * MAUI dispara otra navegación mientras la API está respondiendo.
             */
            actualizacionComprobadaEnSesion = true;

            try
            {
                // Primero permite que MainPage termine de dibujarse.
                await Task.Delay(650);

                if (!ModoSesionService.EsEnLinea ||
                    EsRutaLogin(ObtenerRutaActual()))
                {
                    return;
                }

                using var timeoutCts =
                    new CancellationTokenSource(
                        TimeSpan.FromSeconds(12));

                ActualizacionDisponible? actualizacion =
                    await ActualizacionAplicacionService
                        .Instance
                        .ComprobarActualizacionAsync(
                            timeoutCts.Token);

                if (actualizacion is null ||
                    EsRutaLogin(ObtenerRutaActual()))
                {
                    return;
                }

                bool abrirActualizador;

                if (actualizacion.Obligatoria)
                {
                    await DisplayAlert(
                        "Actualización obligatoria",
                        "Debe instalar ConatraCafé Soil " +
                        $"{actualizacion.VersionNombre} para continuar.",
                        "Actualizar ahora");

                    abrirActualizador = true;
                }
                else
                {
                    abrirActualizador = await DisplayAlert(
                        "Nueva versión disponible",
                        "ConatraCafé Soil " +
                        $"{actualizacion.VersionNombre} está disponible. " +
                        "¿Desea descargarla ahora?",
                        "Actualizar",
                        "Más tarde");
                }

                if (!abrirActualizador ||
                    EsRutaLogin(ObtenerRutaActual()))
                {
                    return;
                }

                await GoToAsync(
                    AppRoutes.ActualizacionAplicacion,
                    false,
                    new Dictionary<string, object>
                    {
                        ["Actualizacion"] = actualizacion
                    });
            }
            catch (OperationCanceledException)
            {
                /*
                 * Una respuesta lenta no afecta el uso de la aplicación y no
                 * muestra mensajes innecesarios al usuario.
                 */
                Debug.WriteLine(
                    "La comprobación de actualizaciones superó el tiempo permitido.");
            }
            catch (Exception ex)
            {
                /*
                 * La actualización es una comprobación auxiliar. Una falla de
                 * red o del servidor nunca debe impedir el inicio de sesión.
                 */
                Debug.WriteLine(
                    $"No fue posible comprobar actualizaciones: {ex}");
            }
            finally
            {
                Interlocked.Exchange(
                    ref comprobacionActualizacionEnCurso,
                    0);
            }
        }

        private static bool EsRutaLogin(string? ruta) =>
            !string.IsNullOrWhiteSpace(ruta) &&
            ruta.Contains(
                "LoginPage",
                StringComparison.OrdinalIgnoreCase);

        private string ObtenerRutaActual() =>
            CurrentState?
                .Location?
                .OriginalString ??
            string.Empty;

        private void AppShell_PropertyChanged(
            object? sender,
            PropertyChangedEventArgs e)
        {
            if (corrigiendoFlyoutNativo ||
                !string.Equals(
                    e.PropertyName,
                    nameof(FlyoutBehavior),
                    StringComparison.Ordinal))
            {
                return;
            }

            if (FlyoutBehavior ==
                Microsoft.Maui.FlyoutBehavior.Disabled)
            {
                return;
            }

            corrigiendoFlyoutNativo = true;

            try
            {
                ConfigurarFlyoutNativo();
            }
            finally
            {
                corrigiendoFlyoutNativo = false;
            }
        }

        private void ConfigurarFlyoutNativo()
        {
            /*
             * Cierra primero el panel por si ya fue abierto mediante un gesto.
             */
            if (FlyoutIsPresented)
                FlyoutIsPresented = false;

            if (FlyoutBehavior !=
                Microsoft.Maui.FlyoutBehavior.Disabled)
            {
                FlyoutBehavior =
                    Microsoft.Maui.FlyoutBehavior.Disabled;
            }
        }

        private async void AppShell_Navigating(
            object? sender,
            ShellNavigatingEventArgs e)
        {
            /*
             * Se vuelve a comprobar en cada navegación para impedir que una
             * página deje habilitado el panel lateral vacío.
             */
            ConfigurarFlyoutNativo();

            if (preparandoNuevoAnalisis ||
                !EsNavegacionHaciaNuevoAnalisis(e))
            {
                return;
            }

            var deferral = e.GetDeferral();
            if (deferral == null)
                return;

            preparandoNuevoAnalisis = true;

            try
            {
                AnalisisEdicionService.Instance.Limpiar();

                /*
                 * Solo se limpia el cálculo temporal antes de navegar.
                 * NuevoAnalisisFormPage es la única responsable de inicializar
                 * sus catálogos en OnAppearing. Antes se ejecutaba aquí una
                 * segunda InicializarPaginaAsync(true), incluida una espera de
                 * hasta 10 segundos si el ViewModel estaba ocupado.
                 */
                await CalculoAnalisisTemporalService.Instance
                    .LimpiarTodoAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    "No fue posible preparar el formulario de un nuevo " +
                    $"análisis: {ex}");
            }
            finally
            {
                preparandoNuevoAnalisis = false;
                deferral.Complete();
            }
        }

        private static bool EsNavegacionHaciaNuevoAnalisis(
            ShellNavigatingEventArgs e)
        {
            string rutaActual =
                e.Current?.Location?.OriginalString ??
                string.Empty;

            string rutaDestino =
                e.Target?.Location?.OriginalString ??
                string.Empty;

            bool vieneDePrincipal =
                rutaActual.Contains(
                    "MainPage",
                    StringComparison.OrdinalIgnoreCase);

            bool vaAlFormulario =
                rutaDestino.Contains(
                    "NuevoAnalisisFormPage",
                    StringComparison.OrdinalIgnoreCase);

            return
                vieneDePrincipal &&
                vaAlFormulario &&
                !AnalisisEdicionService.Instance.EsModoEdicion;
        }

        /// <summary>
        /// Respaldo de MAUI para impedir la navegación con el botón o gesto
        /// nativo de retroceso de Android.
        /// </summary>
        protected override bool OnBackButtonPressed()
        {
#if ANDROID
            return true;
#else
            return base.OnBackButtonPressed();
#endif
        }
    }
}
