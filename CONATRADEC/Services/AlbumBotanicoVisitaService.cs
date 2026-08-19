using CONATRADEC.Models;

namespace CONATRADEC.Services
{
    /// <summary>
    /// Mantiene el alcance funcional de una visita al Álbum Botánico.
    /// Detalle, formularios, administración de fotografías y visor son
    /// subflujos internos; únicamente salir realmente del módulo libera los
    /// datos temporales de la visita.
    /// </summary>
    public static class AlbumBotanicoVisitaService
    {
        private const string Modulo = "album-botanico";
        private const string ClaveCategorias = "categorias";
        private const string ClaveSubcategorias = "subcategorias";

        private static bool navegacionSuscrita;

        public static bool AsegurarVisita()
        {
            bool nueva =
                InterfazVisitaCacheService.AsegurarVisita(Modulo);

            SuscribirNavegacion();
            return nueva;
        }

        public static bool EstaActiva =>
            InterfazVisitaCacheService.EstaActiva(Modulo);

        public static void FinalizarVisita()
        {
            InterfazVisitaCacheService.FinalizarVisita(Modulo);
            DesuscribirNavegacion();
        }

        public static void GuardarCatalogos(
            IEnumerable<CategoriaAlbumBotanicoResponse> categorias,
            IEnumerable<SubcategoriaAlbumBotanicoResponse> subcategorias)
        {
            if (!EstaActiva)
                return;

            InterfazVisitaCacheService.Guardar(
                Modulo,
                ClaveCategorias,
                categorias.Select(CopiarCategoria).ToList());

            InterfazVisitaCacheService.Guardar(
                Modulo,
                ClaveSubcategorias,
                subcategorias.Select(CopiarSubcategoria).ToList());
        }

        public static bool IntentarObtenerCategorias(
            out List<CategoriaAlbumBotanicoResponse> categorias)
        {
            categorias = [];

            if (!EstaActiva ||
                !InterfazVisitaCacheService.IntentarObtener(
                    Modulo,
                    ClaveCategorias,
                    out List<CategoriaAlbumBotanicoResponse> almacenadas))
            {
                return false;
            }

            categorias = almacenadas
                .Select(CopiarCategoria)
                .ToList();

            return true;
        }

        public static bool IntentarObtenerSubcategorias(
            out List<SubcategoriaAlbumBotanicoResponse> subcategorias)
        {
            subcategorias = [];

            if (!EstaActiva ||
                !InterfazVisitaCacheService.IntentarObtener(
                    Modulo,
                    ClaveSubcategorias,
                    out List<SubcategoriaAlbumBotanicoResponse> almacenadas))
            {
                return false;
            }

            subcategorias = almacenadas
                .Select(CopiarSubcategoria)
                .ToList();

            return true;
        }

        private static CategoriaAlbumBotanicoResponse CopiarCategoria(
            CategoriaAlbumBotanicoResponse item) =>
            new()
            {
                CategoriaAlbumBotanicoId = item.CategoriaAlbumBotanicoId,
                NombreCategoria = item.NombreCategoria,
                Descripcion = item.Descripcion,
                RutaImagenPortada = item.RutaImagenPortada,
                ImagenPortadaUrl = item.ImagenPortadaUrl,
                TotalRegistros = item.TotalRegistros,
                TotalRegistrosActivos = item.TotalRegistrosActivos,
                Activo = item.Activo
            };

        private static SubcategoriaAlbumBotanicoResponse CopiarSubcategoria(
            SubcategoriaAlbumBotanicoResponse item) =>
            new()
            {
                SubcategoriaAlbumBotanicoId =
                    item.SubcategoriaAlbumBotanicoId,
                CategoriaAlbumBotanicoId = item.CategoriaAlbumBotanicoId,
                Categoria = item.Categoria,
                NombreSubcategoria = item.NombreSubcategoria,
                Descripcion = item.Descripcion,
                Activo = item.Activo,
                TotalRegistros = item.TotalRegistros
            };

        private static void SuscribirNavegacion()
        {
            if (navegacionSuscrita || Shell.Current == null)
                return;

            Shell.Current.Navigating += Shell_Navigating;
            navegacionSuscrita = true;
        }

        private static void DesuscribirNavegacion()
        {
            if (!navegacionSuscrita || Shell.Current == null)
                return;

            Shell.Current.Navigating -= Shell_Navigating;
            navegacionSuscrita = false;
        }

        private static void Shell_Navigating(
            object? sender,
            ShellNavigatingEventArgs e)
        {
            if (!EstaActiva)
                return;

            string rutaDestino =
                e.Target?.Location?.OriginalString ?? string.Empty;

            if (string.IsNullOrWhiteSpace(rutaDestino))
                return;

            if (!EsRutaInterna(rutaDestino))
                FinalizarVisita();
        }

        private static bool EsRutaInterna(string ruta)
        {
            string rutaSinQuery = ruta;
            int indiceQuery = rutaSinQuery.IndexOf('?');

            if (indiceQuery >= 0)
                rutaSinQuery = rutaSinQuery[..indiceQuery];

            string rutaHoja = rutaSinQuery
                .Split(
                    '/',
                    StringSplitOptions.RemoveEmptyEntries)
                .LastOrDefault() ??
                string.Empty;

            string[] rutasInternas =
            [
                "albumFotosPage",
                "albumDetallePage",
                "categoriaAlbumFormPage",
                "albumRegistroFormPage",
                "albumFotosAdminPage",
                "albumFotoVisorPage"
            ];

            return rutasInternas.Any(nombre =>
                string.Equals(
                    rutaHoja,
                    nombre,
                    StringComparison.OrdinalIgnoreCase));
        }
    }
}
