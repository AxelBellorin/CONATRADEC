using System.Threading;

namespace CONATRADEC.Services
{
    /// <summary>
    /// Señal mínima de invalidación para la visita de Solicitudes.
    /// No conserva listados ni filtros globalmente: únicamente indica que un
    /// subflujo modificó un expediente y que la página actual debe refrescarse
    /// al volver a la bandeja.
    /// </summary>
    public static class DiagnosticoIASolicitudVisitaService
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
