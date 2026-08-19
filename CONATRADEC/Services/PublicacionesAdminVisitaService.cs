using CONATRADEC.Models;

namespace CONATRADEC.Services
{
    /// <summary>
    /// Mantiene el alcance funcional de una visita a la administración de
    /// publicaciones. Crear, editar y consultar eliminadas son subflujos de la
    /// misma visita; salir realmente del módulo libera su caché temporal.
    /// </summary>
    public static class PublicacionesAdminVisitaService
    {
        private const string Modulo =
            "publicaciones-administracion";

        private const string ClaveCategorias =
            "categorias-publicacion";

        private static bool navegacionSuscrita;

        public static bool AsegurarVisita()
        {
            bool nuevaVisita =
                InterfazVisitaCacheService.AsegurarVisita(Modulo);

            SuscribirNavegacion();
            return nuevaVisita;
        }

        public static bool EstaActiva =>
            InterfazVisitaCacheService.EstaActiva(Modulo);

        public static void FinalizarVisita()
        {
            InterfazVisitaCacheService.FinalizarVisita(Modulo);
            DesuscribirNavegacion();
        }

        /// <summary>
        /// Guarda una copia desacoplada del catálogo obtenido al iniciar la
        /// visita. El formulario puede reutilizarla sin repetir el GET.
        /// </summary>
        public static void GuardarCategorias(
            IEnumerable<CategoriaPublicacionResponse> categorias)
        {
            if (!EstaActiva)
                return;

            List<CategoriaPublicacionResponse> copia =
                categorias
                    .Where(item => item.CategoriaPublicacionId > 0)
                    .Select(CopiarCategoria)
                    .ToList();

            InterfazVisitaCacheService.Guardar(
                Modulo,
                ClaveCategorias,
                copia);
        }

        public static bool IntentarObtenerCategorias(
            out List<CategoriaPublicacionResponse> categorias)
        {
            categorias = new List<CategoriaPublicacionResponse>();

            if (!EstaActiva ||
                !InterfazVisitaCacheService.IntentarObtener(
                    Modulo,
                    ClaveCategorias,
                    out List<CategoriaPublicacionResponse> almacenadas))
            {
                return false;
            }

            categorias = almacenadas
                .Select(CopiarCategoria)
                .ToList();

            return true;
        }

        private static CategoriaPublicacionResponse CopiarCategoria(
            CategoriaPublicacionResponse item) =>
            new()
            {
                CategoriaPublicacionId = item.CategoriaPublicacionId,
                Nombre = item.Nombre,
                Descripcion = item.Descripcion,
                ColorHex = item.ColorHex,
                Orden = item.Orden
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
                "publicacionesAdminPage",
                "publicacionFormPage"
            ];

            return rutasInternas.Any(nombre =>
                string.Equals(
                    rutaHoja,
                    nombre,
                    StringComparison.OrdinalIgnoreCase));
        }
    }
}
