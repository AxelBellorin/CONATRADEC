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

                await navigation.PushModalAsync(
                    new NavigationPage(
                        new CatalogoEliminadosPage(
                            configuracion)));
            }
            finally
            {
                abriendo = false;
            }
        }
    }
}
