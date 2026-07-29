namespace CONATRADEC.Services
{
    public sealed record CatalogoEliminadoConfiguracion(
        string Codigo,
        string Titulo,
        string Singular,
        string Descripcion,
        string Interfaz);

    /// <summary>
    /// Catálogos que comparten el flujo de eliminación lógica y reactivación.
    /// Fuente de Nutriente se excluye porque conserva su pantalla especializada.
    /// </summary>
    public static class CatalogoEliminadoCodigos
    {
        public const string Pais = "pais";
        public const string Departamento = "departamento";
        public const string Municipio = "municipio";
        public const string Rol = "rol";
        public const string ElementoQuimico = "elemento-quimico";
        public const string TipoCultivo = "tipo-cultivo";
        public const string TipoAnalisis = "tipo-analisis";
        public const string Usuario = "usuario";
        public const string Terreno = "terreno";
        public const string ExtraccionNutriente = "extraccion-nutriente";
        public const string RangoNutriente = "rango-nutriente";
        public const string CategoriaPublicacion = "categoria-publicacion";
        public const string CategoriaAlbum = "categoria-album";

        private static readonly IReadOnlyDictionary<
            string,
            CatalogoEliminadoConfiguracion>
            Configuraciones =
                new Dictionary<
                    string,
                    CatalogoEliminadoConfiguracion>(
                    StringComparer.OrdinalIgnoreCase)
                {
                    [Pais] = new(
                        Pais,
                        "Países eliminados",
                        "país",
                        "Reactive países conservando su identificador y sus relaciones históricas.",
                        "paisPage"),

                    [Departamento] = new(
                        Departamento,
                        "Departamentos eliminados",
                        "departamento",
                        "El país relacionado debe estar activo antes de restaurar el departamento.",
                        "departamentoPage"),

                    [Municipio] = new(
                        Municipio,
                        "Municipios eliminados",
                        "municipio",
                        "El departamento y el país deben estar activos antes de restaurar el municipio.",
                        "municipioPage"),

                    [Rol] = new(
                        Rol,
                        "Roles eliminados",
                        "rol",
                        "La reactivación conserva el mismo identificador y su historial de permisos.",
                        "rolPage"),

                    [ElementoQuimico] = new(
                        ElementoQuimico,
                        "Elementos químicos eliminados",
                        "elemento químico",
                        "La reactivación conserva las relaciones con fuentes, rangos y análisis anteriores.",
                        "elementoQuimicoPage"),

                    [TipoCultivo] = new(
                        TipoCultivo,
                        "Tipos de cultivo eliminados",
                        "tipo de cultivo",
                        "La reactivación conserva rangos nutricionales y análisis históricos.",
                        "tipoCultivoPage"),

                    [TipoAnalisis] = new(
                        TipoAnalisis,
                        "Tipos de análisis eliminados",
                        "tipo de análisis",
                        "Los códigos técnicos internos se mantienen al reactivar.",
                        "tipoAnalisisSueloPage"),

                    [Usuario] = new(
                        Usuario,
                        "Usuarios inactivos",
                        "usuario",
                        "La reactivación conserva procedencia, rol e historial. No cambia la contraseña.",
                        "userPage"),

                    [Terreno] = new(
                        Terreno,
                        "Terrenos eliminados",
                        "terreno",
                        "Se conserva el código único, las fotografías y las relaciones históricas.",
                        "terrenoPage"),

                    [ExtraccionNutriente] = new(
                        ExtraccionNutriente,
                        "Parámetros de extracción eliminados",
                        "parámetro de extracción",
                        "El elemento químico relacionado debe permanecer activo.",
                        "extraccionNutrientePage"),

                    [RangoNutriente] = new(
                        RangoNutriente,
                        "Rangos nutricionales eliminados",
                        "rango nutricional",
                        "El cultivo y el elemento químico deben estar activos antes de restaurar.",
                        "rangoNutrientePage"),

                    [CategoriaPublicacion] = new(
                        CategoriaPublicacion,
                        "Tipos de publicación eliminados",
                        "tipo de publicación",
                        "La reactivación conserva sus publicaciones relacionadas.",
                        "categoriaPublicacionPage"),

                    [CategoriaAlbum] = new(
                        CategoriaAlbum,
                        "Categorías del álbum eliminadas",
                        "categoría del álbum",
                        "La reactivación conserva sus registros e imágenes relacionadas.",
                        "categoriaAlbumPage")
                };

        /// <summary>
        /// Rutas de creación que serán atendidas por el nuevo flujo común.
        /// Las demás operaciones continúan utilizando sus endpoints actuales.
        /// </summary>
        private static readonly IReadOnlyDictionary<string, string>
            RutasCreacion =
                new Dictionary<string, string>(
                    StringComparer.OrdinalIgnoreCase)
                {
                    ["api/pais/crearPais"] = Pais,
                    ["api/departamento/crear"] = Departamento,
                    ["api/municipio/crear"] = Municipio,
                    ["api/Rol/crearRol"] = Rol,
                    ["api/elemento-quimico/crear"] = ElementoQuimico,
                    ["api/configuracion/tipos-cultivo"] = TipoCultivo,
                    ["api/configuracion/tipos-analisis-suelo"] =
                        TipoAnalisis,
                    ["api/configuracion/extraccion-nutrientes"] =
                        ExtraccionNutriente,
                    ["api/configuracion/rangos-nutrientes"] =
                        RangoNutriente,
                    ["api/configuracion/categorias-publicacion"] =
                        CategoriaPublicacion
                };

        public static bool TryGet(
            string? codigo,
            out CatalogoEliminadoConfiguracion configuracion)
        {
            if (!string.IsNullOrWhiteSpace(codigo) &&
                Configuraciones.TryGetValue(
                    codigo.Trim(),
                    out CatalogoEliminadoConfiguracion? encontrada))
            {
                configuracion = encontrada;
                return true;
            }

            configuracion = null!;
            return false;
        }

        public static bool TryGetPorTitulo(
            string? titulo,
            out CatalogoEliminadoConfiguracion configuracion)
        {
            string valor = Normalizar(titulo);

            /*
             * La pantalla principal "Rangos nutricionales" no muestra rangos
             * individuales: muestra tarjetas de tipos de cultivo.
             *
             * Por eso, el botón Eliminados de esa pantalla debe abrir
             * "Tipos de cultivo eliminados". El botón rojo de sus tarjetas
             * también desactiva un TipoCultivo, no un rango individual.
             */
            if (string.Equals(
                    valor,
                    "rangos nutricionales",
                    StringComparison.Ordinal))
            {
                return TryGet(
                    TipoCultivo,
                    out configuracion);
            }

            /*
             * En la pantalla de detalle el título es "Rangos de <cultivo>".
             * Allí sí se administran ParametroRangoNutrienteCultivo y el
             * botón Eliminados debe consultar los rangos individuales.
             */
            if (valor.StartsWith(
                    "rangos de ",
                    StringComparison.Ordinal))
            {
                return TryGet(
                    RangoNutriente,
                    out configuracion);
            }

            string? codigo =
                valor.Contains("departamento")
                    ? Departamento
                    : valor.Contains("municipio")
                        ? Municipio
                        : valor.Contains("pais") ||
                          valor.Contains("paises")
                            ? Pais
                            : valor.Contains("elemento") &&
                              valor.Contains("quim")
                                ? ElementoQuimico
                                : valor.Contains("tipo") &&
                                  valor.Contains("cultivo")
                                    ? TipoCultivo
                                    : valor.Contains("tipo") &&
                                      valor.Contains("analisis")
                                        ? TipoAnalisis
                                        : valor.Contains("usuario")
                                            ? Usuario
                                            : valor.Contains("terreno")
                                                ? Terreno
                                                : valor.Contains("extraccion")
                                                    ? ExtraccionNutriente
                                                    : valor.Contains("rango") &&
                                                      valor.Contains("nutri")
                                                        ? RangoNutriente
                                                        : valor.Contains("publicacion") &&
                                                          (valor.Contains("tipo") ||
                                                           valor.Contains("categoria"))
                                                            ? CategoriaPublicacion
                                                            : valor.Contains("categoria") &&
                                                              (valor.Contains("album") ||
                                                               valor.Contains("botan"))
                                                                ? CategoriaAlbum
                                                                : valor == "roles" ||
                                                                  valor.Contains("rol")
                                                                    ? Rol
                                                                    : null;

            return TryGet(
                codigo,
                out configuracion);
        }

        public static bool TryGetCodigoCreacion(
            HttpMethod method,
            string? route,
            out string codigo)
        {
            codigo = string.Empty;

            if (method != HttpMethod.Post ||
                string.IsNullOrWhiteSpace(route))
            {
                return false;
            }

            string limpia =
                route
                    .Split('?', 2)[0]
                    .Trim()
                    .TrimStart('/')
                    .TrimEnd('/');

            return RutasCreacion.TryGetValue(
                limpia,
                out codigo!);
        }

        private static string Normalizar(string? valor)
        {
            string texto =
                (valor ?? string.Empty)
                    .Trim()
                    .ToLowerInvariant()
                    .Normalize(
                        System.Text.NormalizationForm.FormD);

            return new string(
                texto
                    .Where(caracter =>
                        System.Globalization.CharUnicodeInfo
                            .GetUnicodeCategory(caracter) !=
                        System.Globalization.UnicodeCategory
                            .NonSpacingMark)
                    .ToArray());
        }
    }
}
