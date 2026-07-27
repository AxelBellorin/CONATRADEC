using CONATRADEC.Models;

namespace CONATRADEC.Services
{
    /// <summary>
    /// Adaptador de compatibilidad para las pantallas que todavía consultan el
    /// antiguo modo por análisis. El valor ahora proviene exclusivamente del
    /// modo global confirmado en el login y nunca cambia por pérdida de señal.
    /// </summary>
    public sealed class ModoTrabajoAnalisisService
    {
        private static readonly Lazy<ModoTrabajoAnalisisService> lazy =
            new(() => new ModoTrabajoAnalisisService());

        private readonly SemaphoreSlim stateLock = new(1, 1);

        private ModoTrabajoAnalisisEstado estado =
            CrearEstadoInicial();

        public static ModoTrabajoAnalisisService Instance =>
            lazy.Value;

        public event EventHandler<ModoTrabajoAnalisisEventArgs>?
            EstadoCambiado;

        private ModoTrabajoAnalisisService()
        {
            ModoSesionService.Instance.ModoCambiado +=
                (_, _) => _ = ActualizarDisponibilidadAsync();
        }

        public ModoTrabajoAnalisisEstado EstadoActual => estado;

        public Task<ModoTrabajoAnalisisEstado>
            PrepararNuevoAnalisisAsync(
                CancellationToken cancellationToken = default) =>
            RefrescarAsync(cancellationToken);

        public Task<ModoTrabajoAnalisisEstado>
            SeleccionarModoAsync(
                ModoTrabajoAnalisis modo,
                CancellationToken cancellationToken = default) =>
            RefrescarAsync(cancellationToken);

        public Task<ModoTrabajoAnalisisEstado>
            AsegurarModoDisponibleAsync(
                CancellationToken cancellationToken = default) =>
            RefrescarAsync(cancellationToken);

        /// <summary>
        /// Se conserva por compatibilidad. Ya no cambia el modo global.
        /// </summary>
        public Task<ModoTrabajoAnalisisEstado>
            CambiarAOfflinePorCaidaAsync(
                CancellationToken cancellationToken = default) =>
            RefrescarAsync(cancellationToken);

        public async Task ActualizarDisponibilidadAsync(
            CancellationToken cancellationToken = default)
        {
            await RefrescarAsync(cancellationToken);
        }

        private async Task<ModoTrabajoAnalisisEstado> RefrescarAsync(
            CancellationToken cancellationToken)
        {
            await stateLock.WaitAsync(cancellationToken);

            try
            {
                bool paqueteDisponible =
                    DatosSinConexionPermisos.TienePermiso &&
                    await MotorCalculoPaqueteService.Instance
                        .TienePaqueteValidoAsync(
                            cancellationToken);

                MotorCalculoPaquete? paquete =
                    paqueteDisponible
                        ? await MotorCalculoPaqueteService.Instance
                            .ObtenerPaqueteActivoAsync(
                                cancellationToken)
                        : null;

                bool offline = ModoSesionService.EsOffline;

                estado = new ModoTrabajoAnalisisEstado
                {
                    Modo = offline
                        ? ModoTrabajoAnalisis.SinConexion
                        : ModoTrabajoAnalisis.EnLinea,

                    /*
                     * La propiedad histórica representa aquí disponibilidad
                     * lógica del servidor, no conectividad física.
                     */
                    InternetDisponible = !offline,
                    PaqueteLocalDisponible = paqueteDisponible,
                    VersionPaquete =
                        paquete?.VersionPaquete ?? string.Empty,
                    CambioAutomatico = false,
                    Mensaje = offline
                        ? paqueteDisponible
                            ? "La sesión utiliza exclusivamente el motor y los datos descargados."
                            : "La sesión está sin conexión, pero falta descargar un motor válido."
                        : "La sesión utiliza exclusivamente la API."
                };

                EstadoCambiado?.Invoke(
                    this,
                    new ModoTrabajoAnalisisEventArgs(estado));

                return estado;
            }
            finally
            {
                stateLock.Release();
            }
        }

        private static ModoTrabajoAnalisisEstado
            CrearEstadoInicial()
        {
            bool offline = ModoSesionService.EsOffline;

            return new ModoTrabajoAnalisisEstado
            {
                Modo = offline
                    ? ModoTrabajoAnalisis.SinConexion
                    : ModoTrabajoAnalisis.EnLinea,
                InternetDisponible = !offline,
                PaqueteLocalDisponible = false,
                CambioAutomatico = false,
                Mensaje = offline
                    ? "Sesión sin conexión."
                    : "Sesión en línea."
            };
        }
    }
}
