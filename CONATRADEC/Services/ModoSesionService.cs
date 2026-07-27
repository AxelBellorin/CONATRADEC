using CONATRADEC.Models;
using Microsoft.Maui.Storage;

namespace CONATRADEC.Services
{
    /// <summary>
    /// Mantiene el modo global seleccionado en el login.
    ///
    /// En línea:
    /// - todas las operaciones funcionales van al backend;
    /// - las respuestas se copian silenciosamente al almacenamiento local.
    ///
    /// Sin conexión:
    /// - ningún request puede alcanzar la red;
    /// - toda la sesión usa SQLite, archivos y el motor local.
    ///
    /// El modo solamente se confirma al autenticar al usuario. Para cambiarlo
    /// se debe cerrar la sesión y volver al login.
    /// </summary>
    public sealed class ModoSesionService
    {
        private const string KeyModoSolicitado =
            "sesion.modo_solicitado";

        private const string KeyModoActivo =
            "sesion.modo_activo";

        private const string KeySesionConfirmada =
            "sesion.modo_confirmado";

        private static readonly Lazy<ModoSesionService> lazy =
            new(() => new ModoSesionService());

        private readonly object stateLock = new();

        private ModoSesion modoSolicitado;
        private ModoSesion modoActivo;
        private bool sesionConfirmada;

        public static ModoSesionService Instance => lazy.Value;

        public event EventHandler<ModoSesionEventArgs>? ModoCambiado;

        private ModoSesionService()
        {
            modoSolicitado = LeerModo(
                Preferences.Get(
                    KeyModoSolicitado,
                    ModoSesion.EnLinea.ToString()));

            modoActivo = LeerModo(
                Preferences.Get(
                    KeyModoActivo,
                    ModoSesion.EnLinea.ToString()));

            sesionConfirmada =
                Preferences.Get(KeySesionConfirmada, false);
        }

        public ModoSesion ModoSolicitado
        {
            get
            {
                lock (stateLock)
                    return modoSolicitado;
            }
        }

        public ModoSesion ModoActual
        {
            get
            {
                lock (stateLock)
                {
                    return sesionConfirmada
                        ? modoActivo
                        : modoSolicitado;
                }
            }
        }

        public bool SesionConfirmada
        {
            get
            {
                lock (stateLock)
                    return sesionConfirmada;
            }
        }

        public static bool EsEnLinea =>
            Instance.ModoActual == ModoSesion.EnLinea;

        public static bool EsOffline =>
            Instance.ModoActual == ModoSesion.SinConexion;

        public void SeleccionarParaLogin(ModoSesion modo)
        {
            lock (stateLock)
            {
                if (sesionConfirmada)
                    return;

                modoSolicitado = modo;
                Preferences.Set(
                    KeyModoSolicitado,
                    modo.ToString());
            }

            Notificar(modo);
        }

        public void ConfirmarSesion(ModoSesion modo)
        {
            lock (stateLock)
            {
                modoSolicitado = modo;
                modoActivo = modo;
                sesionConfirmada = true;

                Preferences.Set(
                    KeyModoSolicitado,
                    modo.ToString());

                Preferences.Set(
                    KeyModoActivo,
                    modo.ToString());

                Preferences.Set(
                    KeySesionConfirmada,
                    true);

                /*
                 * Se conserva esta llave histórica porque otros componentes de
                 * versiones anteriores todavía pueden consultarla.
                 */
                Preferences.Set(
                    "sesion.modo_offline",
                    modo == ModoSesion.SinConexion);
            }

            AnalisisOfflineSincronizacionService.Instance
                .ReiniciarSesion();

            if (modo == ModoSesion.SinConexion)
            {
                /*
                 * Los servicios heredados que solo consultan esta bandera
                 * regresan inmediatamente sin iniciar verificaciones HTTP.
                 */
                EstadoConexionService.Instance
                    .ReportarServidorNoDisponible();
            }
            else
            {
                EstadoConexionService.Instance
                    .ReportarServidorDisponible();
            }

            Notificar(modo);
        }

        /// <summary>
        /// Se ejecuta al mostrar nuevamente el login. Permite elegir el modo
        /// de la próxima sesión sin modificar datos ya descargados.
        /// </summary>
        public void PrepararNuevoLogin()
        {
            lock (stateLock)
            {
                sesionConfirmada = false;
                modoSolicitado =
                    ModoSesion.EnLinea;

                Preferences.Set(
                    KeySesionConfirmada,
                    false);

                Preferences.Set(
                    KeyModoSolicitado,
                    ModoSesion.EnLinea.ToString());
            }

            /*
             * El login siempre comienza en línea. La opción offline solo se
             * vuelve visible después de validar al usuario escrito.
             */
            Notificar(ModoSesion.EnLinea);
        }

        public void CerrarSesion()
        {
            lock (stateLock)
            {
                sesionConfirmada = false;
                Preferences.Set(KeySesionConfirmada, false);
            }

            AnalisisOfflineSincronizacionService.Instance
                .ReiniciarSesion();
        }

        private static ModoSesion LeerModo(string? value) =>
            Enum.TryParse(
                value,
                ignoreCase: true,
                out ModoSesion modo)
                    ? modo
                    : ModoSesion.EnLinea;

        private void Notificar(ModoSesion modo)
        {
            ModoCambiado?.Invoke(
                this,
                new ModoSesionEventArgs(modo));
        }
    }
}
