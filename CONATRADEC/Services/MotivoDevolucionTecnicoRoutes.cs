using CONATRADEC.Views;

namespace CONATRADEC.Services
{
    public static class MotivoDevolucionTecnicoRoutes
    {
        public const string InterfazConfiguracion =
            "diagnosticoIAConfiguracionPage";

        public const string Pagina = nameof(MotivoDevolucionTecnicoPage);
        public const string Formulario = nameof(MotivoDevolucionTecnicoFormPage);
        public const string Eliminados = nameof(MotivoDevolucionTecnicoEliminadosPage);

        private static bool registrado;

        public static void AsegurarRegistro()
        {
            if (registrado)
                return;

            Routing.RegisterRoute(Pagina, typeof(MotivoDevolucionTecnicoPage));
            Routing.RegisterRoute(Formulario, typeof(MotivoDevolucionTecnicoFormPage));
            Routing.RegisterRoute(Eliminados, typeof(MotivoDevolucionTecnicoEliminadosPage));
            registrado = true;
        }

        public static string CrearRutaFormulario(int? id = null) =>
            id is > 0
                ? $"{Formulario}?id={id.Value}"
                : Formulario;
    }
}
