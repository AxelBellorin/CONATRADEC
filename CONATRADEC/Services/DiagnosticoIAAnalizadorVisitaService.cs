using System.Threading;

namespace CONATRADEC.Services
{
    /// <summary>
    /// Señal mínima de invalidación para la visita de Analizador. No conserva
    /// filtros ni páginas globalmente; únicamente indica que un subflujo hizo
    /// una modificación real y que la página visible debe refrescarse al volver.
    /// </summary>
    public static class DiagnosticoIAAnalizadorVisitaService
    {
        private static int mutacionPendiente;

        public static void MarcarMutacion() =>
            Interlocked.Exchange(ref mutacionPendiente, 1);

        public static bool ConsumirMutacion() =>
            Interlocked.Exchange(ref mutacionPendiente, 0) == 1;

        public static void Limpiar() =>
            Interlocked.Exchange(ref mutacionPendiente, 0);
    }
}
