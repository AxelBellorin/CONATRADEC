using CONATRADEC.Views;

namespace CONATRADEC.Services
{
    /// <summary>
    /// Rutas de navegación utilizadas por la aplicación.
    /// Mantenerlas centralizadas evita diferencias de mayúsculas,
    /// errores de escritura y rutas duplicadas en los ViewModels.
    /// </summary>
    public static class AppRoutes
    {
        // Páginas principales declaradas en AppShell.xaml.
        public const string Login = "//LoginPage";
        public const string Principal = "//MainPage";
        public const string Usuarios = "//UserPage";
        public const string Roles = "//RolPage";
        public const string MatrizPermisos = "//MatrizPermisosPage";
        public const string Paises = "//PaisPage";
        public const string ElementosQuimicos = "//ElementoQuimicoPage";
        public const string Terrenos = "//TerrenoPage";
        public const string FuenteNutriente = "//FuenteNutrientePage";

        // Formularios declarados como ShellContent.
        public const string TerrenoFormulario = "//TerrenoFormPage";
        public const string FuenteNutrienteFormulario = "//FuenteNutrienteFormPage";

        // Pantallas secundarias que se abren sobre la pila actual.
        // Estas rutas se registran manualmente en AppShell.xaml.cs.
        public const string MapaSeleccion = nameof(MapaSeleccionPage);
        public const string FotosTerrenoGaleria = nameof(FotosTerrenoGaleriaPage);

        public const string Regresar = "..";
    }
}
