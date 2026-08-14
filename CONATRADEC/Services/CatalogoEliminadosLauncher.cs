using CONATRADEC.Views;

namespace CONATRADEC.Services
{
    public static class CatalogoEliminadosLauncher
    {
        private static bool abriendo;

        public static async Task AbrirAsync(
            CatalogoEliminadoConfiguracion configuracion)
        {
            if (abriendo)
                return;

            abriendo = true;

            try
            {
                INavigation? navigation =
                    Shell.Current?
                        .Navigation;

                if (navigation == null)
                    return;

                var pagina =
                    new CatalogoEliminadosPage(
                        configuracion);

                await navigation.PushModalAsync(
                    new NavigationPage(pagina));

                /*
                 * La consulta se inicia únicamente después de que MAUI haya
                 * terminado de presentar el modal. De esta manera el relay de
                 * carga inicial ya forma parte de una Page visible antes de
                 * comenzar el GET y no se pierde durante la transición.
                 */
                await Task.Yield();
                await pagina.InicializarDespuesDeMostrarAsync();
            }
            finally
            {
                abriendo = false;
            }
        }
    }
}
