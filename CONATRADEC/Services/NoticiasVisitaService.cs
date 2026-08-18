namespace CONATRADEC.Services
{
    /// <summary>
    /// Mantiene la visita funcional del módulo público de Noticias.
    ///
    /// La visita inicia al entrar a Noticias desde otra interfaz y continúa
    /// mientras el usuario consulta el detalle o edita una publicación desde
    /// ese detalle. La administración general de publicaciones es un módulo
    /// independiente y, por lo tanto, finaliza la visita pública.
    /// </summary>
    public static class NoticiasVisitaService
    {
        private const string Modulo = "noticias-publico";

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
                "noticiasPage",
                "noticiaDetallePage",
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
