using CONATRADEC.Views;

namespace CONATRADEC.Services
{
    /// <summary>
    /// Rutas de navegación utilizadas por la aplicación.
    /// Centralizarlas evita errores por escribir rutas diferentes
    /// en páginas y ViewModels.
    /// </summary>
    public static class AppRoutes
    {
        public const string Login = "//LoginPage";
        public const string Principal = "//MainPage";

        public const string Terrenos = "//TerrenoPage";
        public const string TerrenoFormulario = "//TerrenoFormPage";

        public const string FuenteNutriente = "//FuenteNutrientePage";
        public const string FuenteNutrienteFormulario = "//FuenteNutrienteFormPage";

        // Rutas registradas manualmente en AppShell.xaml.cs.
        // Se conservan porque actualmente algunas pantallas navegan
        // mediante rutas relativas y no mediante rutas absolutas de Shell.
        public const string TerrenoFormularioRegistrado = nameof(terrenoFormPage);
        public const string MapaSeleccion = nameof(MapaSeleccionPage);
        public const string FotosTerrenoGaleria = nameof(FotosTerrenoGaleriaPage);
    }
}
