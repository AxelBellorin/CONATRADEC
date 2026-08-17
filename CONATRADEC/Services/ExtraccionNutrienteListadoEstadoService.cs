using System.Threading;

namespace CONATRADEC.Services
{
    /// <summary>
    /// Estado de la visita administrativa de Extracción de nutrientes.
    /// La versión permite detectar CRUD o reactivaciones confirmadas sin
    /// convertir el estado de visita en una caché persistente.
    /// </summary>
    public static class ExtraccionNutrienteListadoEstadoService
    {
        private const string ClaveModulo =
            "extraccionNutrientePage";

        private static int versionActual;

        public static int VersionActual =>
            Volatile.Read(
                ref versionActual);

        public static int MarcarCambio() =>
            Interlocked.Increment(
                ref versionActual);

        /// <summary>
        /// Devuelve true únicamente cuando fue necesario crear una visita nueva.
        /// </summary>
        public static bool AsegurarVisita() =>
            InterfazVisitaCacheService.AsegurarVisita(
                ClaveModulo);

        public static bool EstaActiva =>
            InterfazVisitaCacheService.EstaActiva(
                ClaveModulo);

        public static void FinalizarVisita() =>
            InterfazVisitaCacheService.FinalizarVisita(
                ClaveModulo);

        public static int MarcarParaRecargar() =>
            MarcarCambio();
    }
}
