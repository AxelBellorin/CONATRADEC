using CONATRADEC.Services;
using CONATRADEC.ViewModels;
using CONATRADEC.Views;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;

namespace CONATRADEC
{
    public partial class AppShell : Shell
    {
        private bool preparandoNuevoAnalisis;
        private bool corrigiendoFlyoutNativo;

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
             * explícitamente el formulario y el cálculo temporal anterior.
             */
            Navigating += AppShell_Navigating;

            /*
             * Después de cada navegación se agrega una flecha fija a todos
             * los formularios. Esto evita que el usuario tenga que desplazarse
             * hasta el final para encontrar Cancelar o Regresar.
             */
            Navigated += AppShell_Navigated;
        }

        private void AppShell_Navigated(
            object? sender,
            ShellNavigatedEventArgs e)
        {
            ConfigurarFlyoutNativo();

            FormNavigationHeaderService
                .AsegurarEnPaginaActual();
        }

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

                await CalculoAnalisisTemporalService.Instance
                    .LimpiarTodoAsync();

                NuevoAnalisisFormPage? pagina =
                    BuscarPaginaNuevoAnalisis();

                if (pagina?.BindingContext
                    is NuevoAnalisisFormEdicionViewModel viewModel)
                {
                    for (int intento = 0;
                         intento < 200 && viewModel.IsBusy;
                         intento++)
                    {
                        await Task.Delay(50);
                    }

                    await viewModel
                        .InicializarPaginaAsync(true);
                }
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

        private NuevoAnalisisFormPage?
            BuscarPaginaNuevoAnalisis()
        {
            foreach (ShellItem item in Items)
            {
                foreach (ShellSection seccion in item.Items)
                {
                    foreach (ShellContent contenido in seccion.Items)
                    {
                        if (!string.Equals(
                                contenido.Route,
                                "NuevoAnalisisFormPage",
                                StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        return
                            ((IShellContentController)contenido)
                                .GetOrCreateContent()
                            as NuevoAnalisisFormPage;
                    }
                }
            }

            return null;
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
