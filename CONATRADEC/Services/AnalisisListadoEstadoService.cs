using System.Threading;

namespace CONATRADEC.Services
{
    public static class AnalisisListadoEstadoService
    {
        private static int actualizacionPendiente;

        public static bool HayActualizacionPendiente =>
            Volatile.Read(ref actualizacionPendiente) == 1;

        public static void MarcarActualizacionPendiente()
        {
            Interlocked.Exchange(
                ref actualizacionPendiente,
                1);
        }

        public static void ConfirmarActualizacion()
        {
            Interlocked.Exchange(
                ref actualizacionPendiente,
                0);
        }
    }
}
