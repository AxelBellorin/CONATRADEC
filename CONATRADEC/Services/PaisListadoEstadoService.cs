using System.Threading;

namespace CONATRADEC.Services
{
    /// <summary>
    /// Permite recargar el listado de países únicamente cuando un registro
    /// fue creado, actualizado o eliminado.
    /// </summary>
    public static class PaisListadoEstadoService
    {
        private static int version;

        public static int VersionActual =>
            Volatile.Read(ref version);

        public static int MarcarCambio() =>
            Interlocked.Increment(ref version);
    }
}
