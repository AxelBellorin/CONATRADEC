using Microsoft.Maui.ApplicationModel;

namespace CONATRADEC.Services
{
    /// <summary>
    /// Bloquea antes de navegar las pantallas que necesitan conexión real con
    /// el servidor. La validación se realiza en Shell para cubrir el menú
    /// lateral, la pantalla de configuración y cualquier navegación directa.
    /// </summary>
    public sealed class ModoOfflineNavigationService
    {
        private static readonly Lazy<ModoOfflineNavigationService> lazy =
            new(() => new ModoOfflineNavigationService());

        private Shell? shellVinculado;
        private int mostrandoMensaje;

        public static ModoOfflineNavigationService Instance =>
            lazy.Value;

        private ModoOfflineNavigationService()
        {
        }

        public void VincularShell(Shell shell)
        {
            ArgumentNullException.ThrowIfNull(shell);

            if (ReferenceEquals(shellVinculado, shell))
                return;

            if (shellVinculado != null)
            {
                shellVinculado.Navigating -=
                    Shell_Navigating;
            }

            shellVinculado = shell;
            shellVinculado.Navigating +=
                Shell_Navigating;
        }

        private async void Shell_Navigating(
            object? sender,
            ShellNavigatingEventArgs e)
        {
            string destino =
                Uri.UnescapeDataString(
                    e.Target?.Location?.OriginalString ??
                    string.Empty);

            bool esDatosOffline =
                ContieneRuta(
                    destino,
                    "datosSinConexionPage");

            bool esBitacora =
                ContieneRuta(
                    destino,
                    "bitacoraPage") ||
                ContieneRuta(
                    destino,
                    "bitacoraDetallePage");

            if (!esDatosOffline && !esBitacora)
                return;

            if (esDatosOffline)
            {
                /*
                 * Se ejecuta también al ingresar nuevamente con el mismo usuario,
                 * por si una descarga anterior quedó interrumpida.
                 */
                SincronizacionOfflineEstadoRecuperacionService
                    .RecuperarSiInterrumpida();
            }

            string? mensaje = null;

            if (ModoSesionService.EsOffline)
            {
                mensaje = esDatosOffline
                    ? "La preparación y búsqueda de actualizaciones solamente " +
                      "están disponibles durante una sesión en línea. Cierre " +
                      "sesión e ingrese en modo En línea."
                    : "La bitácora consulta información central del servidor y " +
                      "solamente está disponible durante una sesión en línea.";
            }
            else if (!EstadoConexionService.Instance.HayInternet)
            {
                mensaje = esDatosOffline
                    ? "No hay conexión disponible para descargar o buscar " +
                      "actualizaciones. Revise la red e intente nuevamente."
                    : "No hay conexión disponible para consultar la bitácora. " +
                      "Revise la red e intente nuevamente.";
            }

            if (string.IsNullOrWhiteSpace(mensaje))
                return;

            e.Cancel();

            if (Interlocked.Exchange(
                    ref mostrandoMensaje,
                    1) == 1)
            {
                return;
            }

            try
            {
                await MainThread.InvokeOnMainThreadAsync(
                    async () =>
                        await GlobalService.MostrarAdvertenciaAsync(
                            mensaje));
            }
            finally
            {
                Interlocked.Exchange(
                    ref mostrandoMensaje,
                    0);
            }
        }

        private static bool ContieneRuta(
            string destino,
            string nombrePagina) =>
            destino.Contains(
                nombrePagina,
                StringComparison.OrdinalIgnoreCase);
    }
}
