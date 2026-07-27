using Microsoft.Maui.Networking;

namespace CONATRADEC.Services
{
    /// <summary>
    /// Mantiene el último resultado real de comunicación con la API.
    ///
    /// En una sesión offline informa siempre que el servidor no está
    /// disponible y descarta los eventos de reconexión del sistema. Esto evita
    /// que componentes heredados inicien verificaciones o reintentos mientras
    /// el técnico permanece horas en una zona sin cobertura.
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
        public event Action? ConexionPotencialmenteRestablecida;

        public bool HayInternet
        {
            get
            {
                if (ModoSesionService.EsOffline)
                    return false;

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
                if (ModoSesionService.EsOffline)
                    return false;

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

        public void ReportarServidorDisponible()
        {
            if (ModoSesionService.EsOffline)
            {
                ActualizarResultadoServidor(
                    disponible: false,
                    notificar: false);
                return;
            }

            ActualizarResultadoServidor(
                disponible: true,
                notificar: true);
        }

        public void ReportarServidorNoDisponible() =>
            ActualizarResultadoServidor(
                disponible: false,
                notificar: true);

        public void MarcarComoPendienteDeVerificacion()
        {
            if (ModoSesionService.EsOffline)
            {
                ActualizarResultadoServidor(
                    disponible: false,
                    notificar: false);
                return;
            }

            lock (syncRoot)
            {
                servidorDisponible = null;
                ultimaComprobacionUtc = null;
            }
        }

        private void ActualizarResultadoServidor(
            bool disponible,
            bool notificar)
        {
            bool cambio;

            lock (syncRoot)
            {
                cambio =
                    !servidorDisponible.HasValue ||
                    servidorDisponible.Value != disponible;

                servidorDisponible = disponible;
                ultimaComprobacionUtc = DateTime.UtcNow;
            }

            if (notificar && cambio)
                EstadoConexionCambiado?.Invoke(disponible);
        }

        private void OnConnectivityChanged(
            object? sender,
            ConnectivityChangedEventArgs e)
        {
            if (ModoSesionService.EsOffline)
            {
                /*
                 * El modo fue elegido en el login. Recuperar Wi-Fi no modifica
                 * la sesión ni dispara verificaciones contra la API.
                 */
                ActualizarResultadoServidor(
                    disponible: false,
                    notificar: false);
                return;
            }

            if (e.NetworkAccess == NetworkAccess.None)
            {
                ReportarServidorNoDisponible();
                return;
            }

            MarcarComoPendienteDeVerificacion();
            ConexionPotencialmenteRestablecida?.Invoke();
        }
    }
}
