using System.Threading;

namespace CONATRADEC.Services
{
    /// <summary>
    /// Permite conservar la galería en memoria al consultar detalles y
    /// recargarla únicamente después de una modificación real.
    /// </summary>
    public static class AlbumBotanicoRefreshState
    {
        private static long version;

        public static long VersionActual =>
            Interlocked.Read(ref version);

        public static long MarcarCambio() =>
            Interlocked.Increment(ref version);
    }
}
