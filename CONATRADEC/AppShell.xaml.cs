using CONATRADEC.Services;
using CONATRADEC.Views;

namespace CONATRADEC
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            // TerrenoFormPage ya está declarada como ShellContent
            // dentro de AppShell.xaml, por lo que no necesita
            // registrarse nuevamente mediante Routing.RegisterRoute.

            // Estas páginas se abren como pantallas secundarias
            // sobre la pila actual y permiten regresar con "..".
            Routing.RegisterRoute(
                AppRoutes.MapaSeleccion,
                typeof(MapaSeleccionPage));

            Routing.RegisterRoute(
                AppRoutes.FotosTerrenoGaleria,
                typeof(FotosTerrenoGaleriaPage));
        }
    }
}
