using CONATRADEC.Views;

namespace CONATRADEC.Services
{
    public static class AppRoutes
    {
        public const string Login = "//LoginPage";
        public const string Principal = "//MainPage";
        public const string Configuracion = "//ConfiguracionPage";

        public const string AlbumFotos = nameof(albumFotosPage);

        public const string Usuarios = "//UserPage";
        public const string Roles = "//RolPage";
        public const string MatrizPermisos = "//MatrizPermisosPage";
        public const string Paises = "//PaisPage";
        public const string ElementosQuimicos = "//ElementoQuimicoPage";
        public const string Terrenos = "//TerrenoPage";
        public const string FuenteNutriente = "//FuenteNutrientePage";

        public const string TiposCultivo = "//TipoCultivoPage";
        public const string TiposAnalisisSuelo = "//TipoAnalisisSueloPage";
        public const string ExtraccionNutrientes = "//ExtraccionNutrientePage";
        public const string RangosNutrientes = "//RangoNutrientePage";

        public const string TerrenoFormulario = "//TerrenoFormPage";
        public const string FuenteNutrienteFormulario = "//FuenteNutrienteFormPage";
        public const string TipoCultivoFormulario = "//TipoCultivoFormPage";
        public const string TipoAnalisisSueloFormulario = "//TipoAnalisisSueloFormPage";
        public const string ExtraccionNutrienteFormulario = "//ExtraccionNutrienteFormPage";
        public const string RangoNutrienteFormulario = "//RangoNutrienteFormPage";

        public const string AlbumDetalle = nameof(albumDetallePage);
        public const string CategoriaAlbumFormulario =
            nameof(categoriaAlbumFormPage);
        public const string AlbumRegistroFormulario =
            nameof(albumRegistroFormPage);
        public const string AlbumFotosAdministrar =
            nameof(albumFotosAdminPage);
        public const string AlbumFotoVisor = nameof(albumFotoVisorPage);

        public const string MapaSeleccion = nameof(MapaSeleccionPage);
        public const string FotosTerrenoGaleria =
            nameof(FotosTerrenoGaleriaPage);
        public const string AnalisisGuardadoDetalle =
            nameof(AnalisisGuardadoDetallePage);
        public const string EditarAnalisisGuardado =
            nameof(EditarAnalisisGuardadoPage);

        public const string Bitacora = nameof(bitacoraPage);
        public const string BitacoraDetalle = nameof(bitacoraDetallePage);

        public const string Regresar = "..";
    }
}
