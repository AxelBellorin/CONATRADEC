using System.Threading;

namespace CONATRADEC.Services
{
    /// <summary>
    /// Control de cambios por versión. Cada pantalla recuerda la versión que
    /// ya mostró, por lo que actualizar la administración no consume la
    /// actualización pendiente de la pantalla pública ni del detalle.
    /// </summary>
    public static class PublicacionListadoEstadoService
    {
        private static long versionActual;
        private static long versionConfirmadaGlobal;

        public static long VersionActual =>
            Interlocked.Read(ref versionActual);

        // Compatibilidad con las pantallas que todavía usan el contrato previo.
        public static bool HayActualizacionPendiente =>
            VersionActual >
            Interlocked.Read(ref versionConfirmadaGlobal);

        public static void MarcarActualizacion()
        {
            Interlocked.Increment(ref versionActual);
        }

        public static bool HayCambiosDesde(long versionAplicada) =>
            versionAplicada < VersionActual;

        public static void ConfirmarActualizacion()
        {
            Interlocked.Exchange(
                ref versionConfirmadaGlobal,
                VersionActual);
        }
    }
}
