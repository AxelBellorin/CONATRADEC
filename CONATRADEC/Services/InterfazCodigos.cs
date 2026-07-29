using System.Text;

namespace CONATRADEC.Services
{
    /// <summary>
    /// Centraliza los códigos internos y sus nombres visibles.
    /// La autorización siempre utiliza el código interno y la interfaz
    /// presenta únicamente un nombre amigable.
    /// </summary>
    public static class InterfazCodigos
    {
        public const string AnalisisSuelo = "MainPage";
        public const string Usuarios = "userPage";
        public const string Roles = "rolPage";
        public const string MatrizPermisos = "matrizPermisosPage";
        public const string Paises = "paisPage";
        public const string Departamentos = "departamentoPage";
        public const string Municipios = "municipioPage";
        public const string ElementosQuimicos = "elementoQuimicoPage";
        public const string Terrenos = "terrenoPage";
        public const string FuentesNutrientes = "fuenteNutrientePage";
        public const string TiposCultivo = "tipoCultivoPage";
        public const string TiposAnalisisSuelo =
            "tipoAnalisisSueloPage";
        public const string ExtraccionNutrientes =
            "extraccionNutrientePage";
        public const string RangosNutrientes = "rangoNutrientePage";
        public const string AlbumFotos = "albumFotosPage";
        public const string Noticias = "noticiasPage";
        public const string CategoriasPublicacion =
            "categoriaPublicacionPage";
        public const string Bitacora = "bitacoraPage";
        public const string DatosSinConexion =
            "datosSinConexionPage";

        // Permiso de lectura para abrir el centro de actualizaciones en la app.
        public const string Actualizaciones =
            "ActualizacionAplicacionPage";

        private static readonly IReadOnlyDictionary<string, string>
            NombresAmigables =
                new Dictionary<string, string>(
                    StringComparer.OrdinalIgnoreCase)
                {
                    [AnalisisSuelo] = "Análisis de suelo",
                    [Usuarios] = "Usuarios",
                    [Roles] = "Roles",
                    [MatrizPermisos] = "Matriz de permisos",
                    [Paises] = "Países",
                    [Departamentos] = "Departamentos",
                    [Municipios] = "Municipios",
                    [ElementosQuimicos] = "Elementos químicos",
                    [Terrenos] = "Terrenos",
                    [FuentesNutrientes] = "Fuentes de nutrientes",
                    [TiposCultivo] = "Tipos de cultivo",
                    [TiposAnalisisSuelo] =
                        "Tipos de análisis de suelo",
                    [ExtraccionNutrientes] =
                        "Extracción de nutrientes",
                    [RangosNutrientes] = "Rangos nutricionales",
                    [AlbumFotos] = "Álbum de fotos",
                    [Noticias] = "Noticias e intereses",
                    [CategoriasPublicacion] =
                        "Tipos de publicación",
                    [Bitacora] = "Bitácora del sistema",
                    [DatosSinConexion] = "Datos sin conexión",
                    [Actualizaciones] = "Actualizaciones de la aplicación"
                };

        private static readonly IReadOnlyDictionary<string, string>
            Alias =
                new Dictionary<string, string>(
                    StringComparer.OrdinalIgnoreCase)
                {
                    ["UserFormPage"] = Usuarios,
                    ["RolFormPage"] = Roles,
                    ["PaisFormPage"] = Paises,
                    ["DepartamentoFormPage"] = Departamentos,
                    ["MunicipioFormPage"] = Municipios,
                    ["ElementoQuimicoFormPage"] =
                        ElementosQuimicos,
                    ["FuenteNutrienteFormPage"] =
                        FuentesNutrientes,
                    ["TipoCultivoFormPage"] = TiposCultivo,
                    ["TipoAnalisisSueloFormPage"] =
                        TiposAnalisisSuelo,
                    ["ExtraccionNutrienteFormPage"] =
                        ExtraccionNutrientes,
                    ["RangoNutrienteFormPage"] =
                        RangosNutrientes,
                    ["RangoNutrienteAporteFormulario"] =
                        RangosNutrientes,
                    ["RangoNutrienteDetallePage"] =
                        RangosNutrientes,
                    ["RangoNutrienteCategoriaFormPage"] =
                        RangosNutrientes,
                    ["TerrenoFormPage"] = Terrenos,
                    ["MapaSeleccionPage"] = Terrenos,
                    ["FotosTerrenoGaleriaPage"] = Terrenos,
                    ["AlbumDetallePage"] = AlbumFotos,
                    ["CategoriaAlbumFormPage"] = AlbumFotos,
                    ["AlbumRegistroFormPage"] = AlbumFotos,
                    ["AlbumFotosAdminPage"] = AlbumFotos,
                    ["AlbumFotoVisorPage"] = AlbumFotos,
                    ["NoticiaDetallePage"] = Noticias,
                    ["PublicacionesAdminPage"] = Noticias,
                    ["PublicacionFormPage"] = Noticias,
                    ["CategoriaPublicacionFormPage"] =
                        CategoriasPublicacion,
                    ["BitacoraDetallePage"] = Bitacora,
                    ["NuevoAnalisisFormPage"] = AnalisisSuelo,
                    ["ResultadoAnalisisSueloPage"] =
                        AnalisisSuelo,
                    ["ResultadosAnalisisPage"] = AnalisisSuelo,
                    ["BalanceFormulaPage"] = AnalisisSuelo,
                    ["BalanceFormulasPage"] = AnalisisSuelo,
                    ["EnmiendasCalcareasPage"] = AnalisisSuelo,
                    ["FertilizacionMixtaPage"] = AnalisisSuelo,
                    ["MultiCalculoPage"] = AnalisisSuelo,
                    ["AnalisisGuardadoDetallePage"] =
                        AnalisisSuelo,
                    ["EditarAnalisisGuardadoPage"] =
                        AnalisisSuelo,
                    ["DatosSinConexionPage"] =
                        DatosSinConexion,
                    ["ActualizacionesPage"] =
                        Actualizaciones
                };

        public static string Normalizar(string? codigo)
        {
            string valor =
                (codigo ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(valor))
                return string.Empty;

            return Alias.TryGetValue(
                    valor,
                    out string? canonico)
                ? canonico
                : valor;
        }

        public static string ObtenerNombreAmigable(
            string? codigo,
            string? nombreApi = null)
        {
            string codigoNormalizado =
                Normalizar(codigo);

            string nombreRecibido =
                (nombreApi ?? string.Empty).Trim();

            bool nombreApiValido =
                !string.IsNullOrWhiteSpace(nombreRecibido) &&
                !string.Equals(
                    nombreRecibido,
                    codigo,
                    StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(
                    nombreRecibido,
                    codigoNormalizado,
                    StringComparison.OrdinalIgnoreCase) &&
                !PareceCodigoTecnico(nombreRecibido);

            if (nombreApiValido)
                return nombreRecibido;

            if (NombresAmigables.TryGetValue(
                    codigoNormalizado,
                    out string? nombreCatalogo))
            {
                return nombreCatalogo;
            }

            return HumanizarCodigo(codigoNormalizado);
        }

        private static bool PareceCodigoTecnico(string valor)
        {
            string texto = valor.Trim();

            return texto.EndsWith(
                       "Page",
                       StringComparison.OrdinalIgnoreCase) ||
                   texto.EndsWith(
                       "FormPage",
                       StringComparison.OrdinalIgnoreCase) ||
                   texto.EndsWith(
                       "Formulario",
                       StringComparison.OrdinalIgnoreCase);
        }

        private static string HumanizarCodigo(string codigo)
        {
            string valor =
                (codigo ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(valor))
                return "Interfaz";

            string[] sufijos =
            {
                "FormPage",
                "Page",
                "Formulario"
            };

            foreach (string sufijo in sufijos)
            {
                if (!valor.EndsWith(
                        sufijo,
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                valor = valor[..^sufijo.Length];
                break;
            }

            var resultado = new StringBuilder();

            for (int indice = 0;
                 indice < valor.Length;
                 indice++)
            {
                char actual = valor[indice];

                bool agregarEspacio =
                    indice > 0 &&
                    char.IsUpper(actual) &&
                    (
                        char.IsLower(valor[indice - 1]) ||
                        (
                            indice + 1 < valor.Length &&
                            char.IsLower(valor[indice + 1])
                        )
                    );

                if (agregarEspacio)
                    resultado.Append(' ');

                resultado.Append(actual);
            }

            string texto =
                resultado.ToString().Trim();

            if (string.IsNullOrWhiteSpace(texto))
                return "Interfaz";

            return char.ToUpperInvariant(texto[0]) +
                   texto[1..];
        }
    }
}
