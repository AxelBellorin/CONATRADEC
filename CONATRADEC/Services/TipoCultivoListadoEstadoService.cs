namespace CONATRADEC.Services
{
    /// <summary>
    /// Estado del listado administrativo de Tipos de cultivo.
    ///
    /// La versión detecta cambios confirmados por CRUD o reactivación.
    /// La visita distingue entre volver desde una pantalla interna y entrar
    /// nuevamente desde otro módulo.
    /// </summary>
    public static class TipoCultivoListadoEstadoService
    {
        private const string ClaveModulo =
            "tipoCultivoPage";

        private static int versionActual;

        public static int VersionActual =>
            Volatile.Read(
                ref versionActual);

        public static int MarcarCambio() =>
            Interlocked.Increment(
                ref versionActual);

        /// <summary>
        /// Devuelve true solamente cuando acaba de iniciar una visita nueva.
        /// </summary>
        public static bool AsegurarVisita() =>
            InterfazVisitaCacheService
                .AsegurarVisita(
                    ClaveModulo);

        public static bool EstaActiva =>
            InterfazVisitaCacheService
                .EstaActiva(
                    ClaveModulo);

        public static void FinalizarVisita() =>
            InterfazVisitaCacheService
                .FinalizarVisita(
                    ClaveModulo);
    }
}
