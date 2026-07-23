using CONATRADEC.Services;
using CONATRADEC.ViewModels;
using CONATRADEC.Views;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace CONATRADEC
{
    public partial class AppShell : Shell
    {
        private bool preparandoNuevoAnalisis;

        public AppShell()
        {
            InitializeComponent();

            /*
             * AlbumFotosPage ya no es ShellContent.
             * Cada navegación crea una instancia nueva de la página.
             */
            Routing.RegisterRoute(
                AppRoutes.AlbumFotos,
                typeof(albumFotosPage));

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
        }

        private async void AppShell_Navigating(
            object? sender,
            ShellNavigatingEventArgs e)
        {
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
