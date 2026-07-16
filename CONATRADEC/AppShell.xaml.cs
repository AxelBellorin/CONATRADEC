using CONATRADEC.Services;
using CONATRADEC.Views;

namespace CONATRADEC
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            // Se conserva esta ruta para compatibilidad con cualquier
            // navegación que todavía utilice nameof(terrenoFormPage).
            Routing.RegisterRoute(
                AppRoutes.TerrenoFormularioRegistrado,
                typeof(terrenoFormPage));

            Routing.RegisterRoute(
                AppRoutes.MapaSeleccion,
                typeof(MapaSeleccionPage));

            Routing.RegisterRoute(
                AppRoutes.FotosTerrenoGaleria,
                typeof(FotosTerrenoGaleriaPage));
        }
    }
}
