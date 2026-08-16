using CONATRADEC.Views;
using Microsoft.Maui.Controls;

namespace CONATRADEC.Services
{
    public static class AppRoutes
    {
        /*
         * Las rutas de propietarios, Roles y Diagnóstico IA son dinámicas.
         * El constructor estático garantiza su registro antes de navegar.
         */
        static AppRoutes()
        {
            Routing.RegisterRoute(
                Propietarios,
                typeof(propietariosPage));

            Routing.RegisterRoute(
                PropietarioFormulario,
                typeof(propietarioFormPage));

            Routing.RegisterRoute(
                PropietarioTerrenos,
                typeof(propietarioTerrenosPage));

            /*
             * RolFormPage también existe como ShellContent histórico. Se usa
             * una ruta interna con nombre diferente para apilar el formulario
             * sobre RolPage y conservar la misma visita al regresar.
             */
            Routing.RegisterRoute(
                RolFormularioInterno,
                typeof(rolFormPage));
        }

        public const string Login =
            "//LoginPage";

        public const string SinPermisos =
            "//SinPermisosPage";

        public const string Principal =
            "//MainPage";

        public const string Configuracion =
            "//ConfiguracionPage";

        public const string NoticiasPrincipal =
            "//noticiasPage";

        public const string AlbumFotos =
            nameof(albumFotosPage);

        public const string Usuarios =
            "//UserPage";

        public const string Roles =
            "//RolPage";

        public static readonly string RolFormularioInterno =
            "RolFormularioInterno";

        public const string MatrizPermisos =
            "//MatrizPermisosPage";

        public const string Paises =
            "//PaisPage";

        public const string ElementosQuimicos =
            "//ElementoQuimicoPage";

        public const string Terrenos =
            "//TerrenoPage";

        public const string FuenteNutriente =
            "//FuenteNutrientePage";

        public const string TiposCultivo =
            "//TipoCultivoPage";

        public const string TiposAnalisisSuelo =
            "//TipoAnalisisSueloPage";

        public const string ExtraccionNutrientes =
            "//ExtraccionNutrientePage";

        public const string RangosNutrientes =
            "//RangoNutrientePage";

        public const string TerrenoFormulario =
            "//TerrenoFormPage";

        public const string FuenteNutrienteFormulario =
            "//FuenteNutrienteFormPage";

        public const string TipoCultivoFormulario =
            "//TipoCultivoFormPage";

        public const string TipoAnalisisSueloFormulario =
            "//TipoAnalisisSueloFormPage";

        public const string ExtraccionNutrienteFormulario =
            "//ExtraccionNutrienteFormPage";

        public const string RangoNutrienteFormulario =
            "RangoNutrienteAporteFormulario";

        public const string RangoNutrienteDetalle =
            nameof(rangoNutrienteDetallePage);

        public const string
            RangoNutrienteCategoriaFormulario =
                nameof(
                    rangoNutrienteCategoriaFormPage);

        public const string AlbumDetalle =
            nameof(albumDetallePage);

        public const string CategoriaAlbumFormulario =
            nameof(categoriaAlbumFormPage);

        public const string AlbumRegistroFormulario =
            nameof(albumRegistroFormPage);

        public const string AlbumFotosAdministrar =
            nameof(albumFotosAdminPage);

        public const string AlbumFotoVisor =
            nameof(albumFotoVisorPage);

        public const string MapaSeleccion =
            nameof(MapaSeleccionPage);

        public const string FotosTerrenoGaleria =
            nameof(FotosTerrenoGaleriaPage);

        public const string AnalisisGuardadoDetalle =
            nameof(AnalisisGuardadoDetallePage);

        /*
         * Esta ruta sigue siendo necesaria al regresar desde Resultado
         * durante la edición de un análisis histórico. Aunque la edición
         * principal se realiza en NuevoAnalisisFormPage, el flujo de retorno
         * todavía utiliza EditarAnalisisGuardadoPage para conservar el
         * análisis seleccionado y su navegación histórica.
         */
        public const string EditarAnalisisGuardado =
            nameof(EditarAnalisisGuardadoPage);

        public const string Bitacora =
            nameof(bitacoraPage);

        public const string BitacoraDetalle =
            nameof(bitacoraDetallePage);

        public const string Noticias =
            nameof(noticiasPage);

        public const string NoticiaDetalle =
            nameof(noticiaDetallePage);

        public const string PublicacionesAdmin =
            nameof(publicacionesAdminPage);

        public const string PublicacionFormulario =
            nameof(publicacionFormPage);

        public const string CategoriasPublicacion =
            nameof(categoriaPublicacionPage);

        public const string
            CategoriaPublicacionFormulario =
                nameof(
                    categoriaPublicacionFormPage);

        public const string ConfiguracionUnidades =
            nameof(configuracionUnidadesPage);

        public const string ActualizacionAplicacion =
            nameof(ActualizacionAplicacionPage);

        /*
         * Son static readonly para que el primer acceso ejecute el
         * constructor estático y registre las páginas dinámicas.
         */
        public static readonly string InspeccionFitosanitaria =
            DiagnosticoIARoutes.Pagina;

        public static readonly string DiagnosticoIASolicitud =
            DiagnosticoIARoutes.PaginaSolicitud;

        public static readonly string DiagnosticoIAAnalizador =
            DiagnosticoIARoutes.PaginaAnalizador;

        public static readonly string DiagnosticoIAResultado =
            DiagnosticoIARoutes.PaginaResultado;

        public static readonly string DiagnosticoIAAprobador =
            DiagnosticoIARoutes.PaginaAprobador;

        public static readonly string DiagnosticoIAConfiguracion =
            DiagnosticoIARoutes.PaginaConfiguracion;

        /*
         * Son static readonly para forzar la inicialización de la clase y
         * registrar las páginas antes de usarlas.
         */
        public static readonly string Propietarios =
            nameof(propietariosPage);

        public static readonly string PropietarioFormulario =
            nameof(propietarioFormPage);

        public static readonly string PropietarioTerrenos =
            nameof(propietarioTerrenosPage);

        public const string Regresar =
            "..";
    }
}
