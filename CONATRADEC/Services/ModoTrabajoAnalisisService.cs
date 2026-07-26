using CONATRADEC.Models;

namespace CONATRADEC.Services
{
    /// <summary>
    /// Mantiene el modo elegido para el análisis que está en curso.
    ///
    /// Una vez que el análisis cambia automáticamente a modo local por pérdida
    /// de conexión, no vuelve a modo en línea hasta que el usuario lo seleccione
    /// expresamente o comience un nuevo análisis.
    /// </summary>
    public sealed class ModoTrabajoAnalisisService
    {
        private static readonly Lazy<ModoTrabajoAnalisisService> lazy =
            new(() => new ModoTrabajoAnalisisService());

        private readonly SemaphoreSlim stateLock = new(1, 1);

        private string versionPaqueteActual =
            string.Empty;

        private ModoTrabajoAnalisisEstado estado =
            new()
            {
                Modo = ModoTrabajoAnalisis.EnLinea,
                InternetDisponible = true,
                PaqueteLocalDisponible = false,
                Mensaje = "Seleccione el modo de trabajo del análisis."
            };

        public static ModoTrabajoAnalisisService Instance =>
            lazy.Value;

        public event EventHandler<ModoTrabajoAnalisisEventArgs>?
            EstadoCambiado;

        private ModoTrabajoAnalisisService()
        {
        }

        public ModoTrabajoAnalisisEstado EstadoActual =>
            estado;

        public async Task<ModoTrabajoAnalisisEstado>
            PrepararNuevoAnalisisAsync(
                CancellationToken cancellationToken = default)
        {
            await stateLock.WaitAsync(cancellationToken);

            try
            {
                bool internet =
                    EstadoConexionService.Instance.HayInternet;

                bool localDisponible =
                    DatosSinConexionPermisos.TienePermiso &&
                    await MotorCalculoPaqueteService.Instance
                        .TienePaqueteValidoAsync(
                            cancellationToken);

                versionPaqueteActual =
                    localDisponible
                        ? (
                            await MotorCalculoPaqueteService
                                .Instance
                                .ObtenerPaqueteActivoAsync(
                                    cancellationToken)
                          )?.VersionPaquete ??
                          string.Empty
                        : string.Empty;

                ModoTrabajoAnalisis modo =
                    !internet &&
                    localDisponible
                        ? ModoTrabajoAnalisis.SinConexion
                        : ModoTrabajoAnalisis.EnLinea;

                estado =
                    CrearEstado(
                        modo,
                        internet,
                        localDisponible,
                        cambioAutomatico:
                            !internet &&
                            localDisponible);

                Notificar();
                return estado;
            }
            finally
            {
                stateLock.Release();
            }
        }

        public async Task<ModoTrabajoAnalisisEstado>
            SeleccionarModoAsync(
                ModoTrabajoAnalisis modo,
                CancellationToken cancellationToken = default)
        {
            await stateLock.WaitAsync(cancellationToken);

            try
            {
                bool internet =
                    EstadoConexionService.Instance.HayInternet;

                bool localDisponible =
                    DatosSinConexionPermisos.TienePermiso &&
                    await MotorCalculoPaqueteService.Instance
                        .TienePaqueteValidoAsync(
                            cancellationToken);

                versionPaqueteActual =
                    localDisponible
                        ? (
                            await MotorCalculoPaqueteService
                                .Instance
                                .ObtenerPaqueteActivoAsync(
                                    cancellationToken)
                          )?.VersionPaquete ??
                          string.Empty
                        : string.Empty;

                if (modo == ModoTrabajoAnalisis.SinConexion &&
                    !localDisponible)
                {
                    estado =
                        CrearEstado(
                            ModoTrabajoAnalisis.EnLinea,
                            internet,
                            localDisponible,
                            cambioAutomatico: false,
                            mensajePersonalizado:
                                "No existe un motor local válido. Use Descargar todo con conexión.");

                    Notificar();
                    return estado;
                }

                if (modo == ModoTrabajoAnalisis.EnLinea &&
                    !internet)
                {
                    if (localDisponible)
                    {
                        estado =
                            CrearEstado(
                                ModoTrabajoAnalisis.SinConexion,
                                internet,
                                localDisponible,
                                cambioAutomatico: true);
                    }
                    else
                    {
                        estado =
                            CrearEstado(
                                ModoTrabajoAnalisis.EnLinea,
                                internet,
                                localDisponible,
                                cambioAutomatico: false,
                                mensajePersonalizado:
                                    "No hay conexión y este dispositivo no tiene un motor local válido.");
                    }

                    Notificar();
                    return estado;
                }

                estado =
                    CrearEstado(
                        modo,
                        internet,
                        localDisponible,
                        cambioAutomatico: false);

                Notificar();
                return estado;
            }
            finally
            {
                stateLock.Release();
            }
        }

        public async Task<ModoTrabajoAnalisisEstado>
            AsegurarModoDisponibleAsync(
                CancellationToken cancellationToken = default)
        {
            bool internet =
                EstadoConexionService.Instance.HayInternet;

            if (internet)
            {
                await ActualizarDisponibilidadAsync(
                    cancellationToken);

                return estado;
            }

            return await CambiarAOfflinePorCaidaAsync(
                cancellationToken);
        }

        public async Task<ModoTrabajoAnalisisEstado>
            CambiarAOfflinePorCaidaAsync(
                CancellationToken cancellationToken = default)
        {
            await stateLock.WaitAsync(cancellationToken);

            try
            {
                bool localDisponible =
                    DatosSinConexionPermisos.TienePermiso &&
                    await MotorCalculoPaqueteService.Instance
                        .TienePaqueteValidoAsync(
                            cancellationToken);

                versionPaqueteActual =
                    localDisponible
                        ? (
                            await MotorCalculoPaqueteService
                                .Instance
                                .ObtenerPaqueteActivoAsync(
                                    cancellationToken)
                          )?.VersionPaquete ??
                          string.Empty
                        : string.Empty;

                estado =
                    localDisponible
                        ? CrearEstado(
                            ModoTrabajoAnalisis.SinConexion,
                            internet: false,
                            localDisponible: true,
                            cambioAutomatico: true)
                        : CrearEstado(
                            estado.Modo,
                            internet: false,
                            localDisponible: false,
                            cambioAutomatico: false,
                            mensajePersonalizado:
                                "Se perdió la conexión y no existe un motor local válido. El formulario puede conservarse, pero no puede calcularse.");

                Notificar();
                return estado;
            }
            finally
            {
                stateLock.Release();
            }
        }

        public async Task ActualizarDisponibilidadAsync(
            CancellationToken cancellationToken = default)
        {
            await stateLock.WaitAsync(cancellationToken);

            try
            {
                bool internet =
                    EstadoConexionService.Instance.HayInternet;

                bool localDisponible =
                    DatosSinConexionPermisos.TienePermiso &&
                    await MotorCalculoPaqueteService.Instance
                        .TienePaqueteValidoAsync(
                            cancellationToken);

                versionPaqueteActual =
                    localDisponible
                        ? (
                            await MotorCalculoPaqueteService
                                .Instance
                                .ObtenerPaqueteActivoAsync(
                                    cancellationToken)
                          )?.VersionPaquete ??
                          string.Empty
                        : string.Empty;

                ModoTrabajoAnalisis modo =
                    estado.Modo;

                if (!internet &&
                    localDisponible)
                {
                    modo =
                        ModoTrabajoAnalisis.SinConexion;
                }

                estado =
                    CrearEstado(
                        modo,
                        internet,
                        localDisponible,
                        cambioAutomatico:
                            !internet &&
                            localDisponible);

                Notificar();
            }
            finally
            {
                stateLock.Release();
            }
        }

        private ModoTrabajoAnalisisEstado CrearEstado(
            ModoTrabajoAnalisis modo,
            bool internet,
            bool localDisponible,
            bool cambioAutomatico,
            string? mensajePersonalizado = null)
        {
            string version =
                versionPaqueteActual;

            string mensaje =
                mensajePersonalizado ??
                CrearMensaje(
                    modo,
                    internet,
                    localDisponible,
                    version,
                    cambioAutomatico);

            return new ModoTrabajoAnalisisEstado
            {
                Modo = modo,
                InternetDisponible = internet,
                PaqueteLocalDisponible =
                    localDisponible,
                VersionPaquete = version,
                Mensaje = mensaje,
                CambioAutomatico =
                    cambioAutomatico
            };
        }

        private static string CrearMensaje(
            ModoTrabajoAnalisis modo,
            bool internet,
            bool localDisponible,
            string version,
            bool cambioAutomatico)
        {
            if (modo ==
                ModoTrabajoAnalisis.SinConexion)
            {
                string prefijo =
                    cambioAutomatico
                        ? "Se activó automáticamente el modo sin conexión."
                        : "El análisis utilizará únicamente los datos descargados.";

                return string.IsNullOrWhiteSpace(version)
                    ? prefijo
                    : $"{prefijo} Motor: {version}.";
            }

            if (!internet)
            {
                return
                    "No hay conexión disponible. Descargue el motor antes de trabajar fuera de línea.";
            }

            if (localDisponible)
            {
                return
                    "El análisis se calculará en el servidor. Si se pierde la señal, podrá continuar con el motor local.";
            }

            return
                "El análisis se calculará en el servidor. Este dispositivo todavía no tiene respaldo local.";
        }

        private void Notificar()
        {
            EstadoCambiado?.Invoke(
                this,
                new ModoTrabajoAnalisisEventArgs(
                    estado));
        }
    }
}
