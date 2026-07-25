namespace CONATRADEC.Services
{
    /// <summary>
    /// Permite recargar el listado únicamente cuando ocurrió un cambio
    /// real en el catálogo.
    /// </summary>
    public static class TipoCultivoListadoEstadoService
    {
        private static int versionActual;

        public static int VersionActual =>
            Volatile.Read(
                ref versionActual);

        public static int MarcarCambio() =>
            Interlocked.Increment(
                ref versionActual);
    }
}
