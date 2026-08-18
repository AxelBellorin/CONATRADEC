using CONATRADEC.Views;

namespace CONATRADEC.Services
{
    public static class TipoFotografiaIARoutes
    {
        public const string InterfazConfiguracion =
            "diagnosticoIAConfiguracionPage";

        public static readonly string Pagina =
            nameof(TipoFotografiaIAPage);

        public static readonly string PaginaFormulario =
            nameof(TipoFotografiaIAFormPage);

        public static readonly string PaginaEliminados =
            nameof(TipoFotografiaIAEliminadosPage);

        static TipoFotografiaIARoutes()
        {
            Routing.RegisterRoute(
                Pagina,
                typeof(TipoFotografiaIAPage));

            Routing.RegisterRoute(
                PaginaFormulario,
                typeof(TipoFotografiaIAFormPage));

            Routing.RegisterRoute(
                PaginaEliminados,
                typeof(TipoFotografiaIAEliminadosPage));
        }
    }
}
