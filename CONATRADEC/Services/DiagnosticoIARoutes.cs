using CONATRADEC.Views;
using Microsoft.Maui.Controls;

namespace CONATRADEC.Services
{
    /// <summary>
    /// Registra el módulo sin modificar AppShell ni AppRoutes.
    /// </summary>
    public static class DiagnosticoIARoutes
    {
        public const string Interfaz =
            "diagnosticoIAPage";

        public static readonly string Pagina =
            RegistrarRuta();

        private static string RegistrarRuta()
        {
            string ruta = nameof(DiagnosticoIAPage);

            Routing.RegisterRoute(
                ruta,
                typeof(DiagnosticoIAPage));

            return ruta;
        }
    }
}
