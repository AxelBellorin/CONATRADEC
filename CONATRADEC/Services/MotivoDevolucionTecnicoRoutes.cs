using CONATRADEC.Views;

namespace CONATRADEC.Services
{
    public static class MotivoDevolucionTecnicoRoutes
    {
        public const string Pagina = nameof(MotivoDevolucionTecnicoPage);
        public const string Formulario = nameof(MotivoDevolucionTecnicoFormPage);

        private static bool registrado;

        public static void AsegurarRegistro()
        {
            if (registrado)
                return;

            Routing.RegisterRoute(Pagina, typeof(MotivoDevolucionTecnicoPage));
            Routing.RegisterRoute(
                Formulario,
                typeof(MotivoDevolucionTecnicoFormPage));
            registrado = true;
        }

        public static string CrearRutaFormulario(int? id = null) =>
            id is > 0
                ? $"{Formulario}?id={id.Value}"
                : Formulario;
    }
}
