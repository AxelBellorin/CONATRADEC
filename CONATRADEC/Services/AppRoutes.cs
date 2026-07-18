using CONATRADEC.Views;

namespace CONATRADEC.Services
{
    public static class AppRoutes
    {
        public const string Login = "//LoginPage";
        public const string Principal = "//MainPage";
        public const string Usuarios = "//UserPage";
        public const string Roles = "//RolPage";
        public const string MatrizPermisos = "//MatrizPermisosPage";
        public const string Paises = "//PaisPage";
        public const string ElementosQuimicos = "//ElementoQuimicoPage";
        public const string Terrenos = "//TerrenoPage";
        public const string FuenteNutriente = "//FuenteNutrientePage";

        public const string TerrenoFormulario = "//TerrenoFormPage";
        public const string FuenteNutrienteFormulario = "//FuenteNutrienteFormPage";

        public const string MapaSeleccion = nameof(MapaSeleccionPage);
        public const string FotosTerrenoGaleria = nameof(FotosTerrenoGaleriaPage);
        public const string AnalisisGuardadoDetalle = nameof(AnalisisGuardadoDetallePage);
        public const string EditarAnalisisGuardado = nameof(EditarAnalisisGuardadoPage);

        public const string Regresar = "..";
    }
}
