namespace CONATRADEC.Services
{
    /// <summary>
    /// Mantiene la visita funcional del módulo Análisis de suelo.
    ///
    /// Una visita inicia al entrar realmente al módulo y continúa mientras el
    /// usuario navega por formularios, resultados, cálculos y detalle. Al salir
    /// hacia otro módulo se descarta el estado de visita para que el próximo
    /// ingreso consulte información fresca.
    /// </summary>
    public static class AnalisisVisitaService
    {
        private const string Modulo = "analisis-suelo";

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
            string[] rutasInternas =
            [
                "MainPage",
                "NuevoAnalisisFormPage",
                "ResultadoAnalisisSueloPage",
                "MultiCalculoPage",
                "AnalisisGuardadoDetallePage",
                "EditarAnalisisGuardadoPage",
                "MapaSeleccionPage"
            ];

            return rutasInternas.Any(nombre =>
                ruta.Contains(
                    nombre,
                    StringComparison.OrdinalIgnoreCase));
        }
    }
}
