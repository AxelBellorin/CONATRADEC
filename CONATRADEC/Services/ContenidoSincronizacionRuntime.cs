using System.Collections.Concurrent;

namespace CONATRADEC.Services
{
    /// <summary>
    /// Permite invalidar la comprobación en memoria cuando el usuario pulsa
    /// Sincronizar. No borra SQLite; solamente obliga a consultar nuevamente
    /// la versión del servidor.
    /// </summary>
    public static class ContenidoSincronizacionRuntime
    {
        private static readonly ConcurrentDictionary<string, long>
            invalidaciones = new(
                StringComparer.OrdinalIgnoreCase);

        public static long ObtenerVersionInvalidacion(string modulo) =>
            invalidaciones.TryGetValue(
                Normalizar(modulo),
                out long version)
                ? version
                : 0;

        public static void Invalidar(string modulo)
        {
            string clave = Normalizar(modulo);

            invalidaciones.AddOrUpdate(
                clave,
                1,
                (_, actual) => actual + 1);
        }

        private static string Normalizar(string modulo) =>
            (modulo ?? string.Empty)
                .Trim()
                .ToLowerInvariant();
    }
}
