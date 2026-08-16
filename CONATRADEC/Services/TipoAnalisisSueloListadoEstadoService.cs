namespace CONATRADEC.Services
{
    /// <summary>
    /// Mantiene el estado mínimo de la visita al módulo y una versión global
    /// para detectar cambios confirmados por el servidor durante esa visita.
    /// </summary>
    public static class TipoAnalisisSueloListadoEstadoService
    {
        private const string Modulo =
            "tipos-analisis-suelo";

        private static int versionActual;

        public static int VersionActual =>
            Volatile.Read(
                ref versionActual);

        /// <summary>
        /// Devuelve true únicamente cuando el usuario está entrando al módulo
        /// desde otra interfaz y debe iniciar una visita nueva.
        /// </summary>
        public static bool AsegurarVisita() =>
            InterfazVisitaCacheService.AsegurarVisita(
                Modulo);

        public static void FinalizarVisita() =>
            InterfazVisitaCacheService.FinalizarVisita(
                Modulo);

        public static int MarcarCambio() =>
            Interlocked.Increment(
                ref versionActual);
    }
}
