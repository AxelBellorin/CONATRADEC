using CONATRADEC.Views;

namespace CONATRADEC.Services
{
    /// <summary>
    /// Abre la administración de publicaciones eliminadas como un flujo modal
    /// independiente, igual que las demás pantallas de registros eliminados.
    /// </summary>
    public static class PublicacionesEliminadasLauncher
    {
        private static bool abriendo;

        public static async Task AbrirAsync()
        {
            if (abriendo)
                return;

            abriendo = true;

            try
            {
                INavigation? navigation =
                    Shell.Current?.Navigation;

                if (navigation == null)
                    return;

                var pagina =
                    new publicacionesEliminadasPage();

                await navigation.PushModalAsync(
                    new NavigationPage(pagina));

                /*
                 * La consulta comienza después de presentar el modal para que
                 * el indicador de carga sea visible antes de iniciar el GET.
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
