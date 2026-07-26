using Microsoft.Maui.Networking;

namespace CONATRADEC.Services
{
    /// <summary>
    /// Mantiene el último resultado REAL de comunicación con la API.
    ///
    /// Connectivity.Current se conserva solamente como señal inicial del
    /// sistema. Una vez realizada una comprobación contra la API, ese resultado
    /// prevalece hasta la siguiente comprobación.
    /// </summary>
    public sealed class EstadoConexionService
    {
        private static readonly Lazy<EstadoConexionService> lazy =
            new(() => new EstadoConexionService());

        private readonly object syncRoot = new();

        private bool? servidorDisponible;
        private DateTime? ultimaComprobacionUtc;

        public static EstadoConexionService Instance => lazy.Value;

        public event Action<bool>? EstadoConexionCambiado;

        /// <summary>
        /// Solicita una comprobación real cuando el sistema detecta que
        /// reapareció una interfaz de red.
        /// </summary>
        public event Action? ConexionPotencialmenteRestablecida;

        public bool HayInternet
        {
            get
            {
                lock (syncRoot)
                {
                    if (servidorDisponible.HasValue)
                        return servidorDisponible.Value;
                }

                return Connectivity.Current.NetworkAccess !=
                       NetworkAccess.None;
            }
        }

        public bool? ServidorDisponibleConfirmado
        {
            get
            {
                lock (syncRoot)
                    return servidorDisponible;
            }
        }

        public DateTime? UltimaComprobacionUtc
        {
            get
            {
                lock (syncRoot)
                    return ultimaComprobacionUtc;
            }
        }

        private EstadoConexionService()
        {
            Connectivity.Current.ConnectivityChanged +=
                OnConnectivityChanged;
        }

        /// <summary>
        /// Cualquier respuesta HTTP demuestra que la API es accesible, incluso
        /// si la operación funcional devuelve 401, 403, 404 o 500.
        /// </summary>
        public void ReportarServidorDisponible() =>
            ActualizarResultadoServidor(disponible: true);

        /// <summary>
        /// Se utiliza únicamente ante fallos reales de transporte, DNS,
        /// conexión rechazada o timeout.
        /// </summary>
        public void ReportarServidorNoDisponible() =>
            ActualizarResultadoServidor(disponible: false);

        public void MarcarComoPendienteDeVerificacion()
        {
            lock (syncRoot)
            {
                servidorDisponible = null;
                ultimaComprobacionUtc = null;
            }
        }

        private void ActualizarResultadoServidor(bool disponible)
        {
            bool notificar;

            lock (syncRoot)
            {
                notificar =
                    !servidorDisponible.HasValue ||
                    servidorDisponible.Value != disponible;

                servidorDisponible = disponible;
                ultimaComprobacionUtc = DateTime.UtcNow;
            }

            if (notificar)
                EstadoConexionCambiado?.Invoke(disponible);
        }

        private void OnConnectivityChanged(
            object? sender,
            ConnectivityChangedEventArgs e)
        {
            if (e.NetworkAccess == NetworkAccess.None)
            {
                ReportarServidorNoDisponible();
                return;
            }

            /*
             * Tener Wi-Fi o cable no garantiza acceso a la API. El estado queda
             * pendiente hasta ejecutar la comprobación HTTP.
             */
            MarcarComoPendienteDeVerificacion();
            ConexionPotencialmenteRestablecida?.Invoke();
        }
    }
}
