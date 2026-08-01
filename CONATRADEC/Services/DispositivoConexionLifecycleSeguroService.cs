using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace CONATRADEC.Services
{
    /// <summary>
    /// Protege los eventos del ciclo de vida de la ventana frente a errores
    /// temporales de WinRT al consultar red, dispositivo o geolocalización.
    ///
    /// DispositivoConexionService es telemetría auxiliar: una falla de ese
    /// servicio nunca debe cerrar, ocultar ni bloquear la aplicación.
    /// </summary>
    public sealed class DispositivoConexionLifecycleSeguroService
    {
        private static readonly Lazy<
            DispositivoConexionLifecycleSeguroService> instancia =
                new(() =>
                    new DispositivoConexionLifecycleSeguroService());

        private int reanudando;
        private int suspendiendo;
        private int deteniendo;

        private DispositivoConexionLifecycleSeguroService()
        {
        }

        public static
            DispositivoConexionLifecycleSeguroService Instance =>
                instancia.Value;

        public async Task ReanudarAsync()
        {
            if (Interlocked.Exchange(
                    ref reanudando,
                    1) == 1)
            {
                return;
            }

            try
            {
#if WINDOWS
                /*
                 * Window.Resumed puede dispararse antes de que Windows termine
                 * de reconstruir NetworkInformation/Connectivity. Esperar evita
                 * consultar un objeto WinRT en transición.
                 */
                await Task.Delay(500);
#endif

                await DispositivoConexionService.Instance
                    .ReanudarAsync();
            }
            catch (COMException ex)
            {
                Registrar(
                    "Reanudar",
                    ex);
            }
            catch (Exception ex)
            {
                Registrar(
                    "Reanudar",
                    ex);
            }
            finally
            {
                Interlocked.Exchange(
                    ref reanudando,
                    0);
            }
        }

        public async Task SuspenderAsync()
        {
            if (Interlocked.Exchange(
                    ref suspendiendo,
                    1) == 1)
            {
                return;
            }

            try
            {
                await DispositivoConexionService.Instance
                    .SuspenderAsync();
            }
            catch (COMException ex)
            {
                Registrar(
                    "Suspender",
                    ex);
            }
            catch (Exception ex)
            {
                Registrar(
                    "Suspender",
                    ex);
            }
            finally
            {
                Interlocked.Exchange(
                    ref suspendiendo,
                    0);
            }
        }

        public async Task DetenerAsync()
        {
            if (Interlocked.Exchange(
                    ref deteniendo,
                    1) == 1)
            {
                return;
            }

            try
            {
                await DispositivoConexionService.Instance
                    .DetenerAsync();
            }
            catch (COMException ex)
            {
                Registrar(
                    "Detener",
                    ex);
            }
            catch (Exception ex)
            {
                Registrar(
                    "Detener",
                    ex);
            }
            finally
            {
                Interlocked.Exchange(
                    ref deteniendo,
                    0);
            }
        }

        private static void Registrar(
            string operacion,
            Exception exception)
        {
            /*
             * Solo se registra en la salida de depuración. No se muestra alerta
             * al usuario porque el análisis, catálogos y sincronización principal
             * continúan funcionando independientemente de esta telemetría.
             */
            Debug.WriteLine(
                "[DispositivoConexionLifecycle] " +
                $"{operacion}: " +
                $"{exception.GetType().Name} - " +
                exception.Message);
        }
    }
}
