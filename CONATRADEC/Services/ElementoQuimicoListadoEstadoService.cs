namespace CONATRADEC.Services
{
    /// <summary>
    /// Permite que el listado se recargue únicamente cuando un formulario
    /// haya creado, editado o eliminado un elemento químico.
    /// </summary>
    public static class ElementoQuimicoListadoEstadoService
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
