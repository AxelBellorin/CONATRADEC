using Microsoft.Maui.Storage;

namespace CONATRADEC.Services
{
    /// <summary>
    /// Controla la inactividad local y registra únicamente interacciones reales
    /// provenientes de Android o Windows.
    /// </summary>
    public sealed class SesionInactividadService
    {
        private static readonly Lazy<SesionInactividadService> instancia =
            new(() => new SesionInactividadService());

        private static readonly TimeSpan IntervaloPersistencia =
            TimeSpan.FromSeconds(10);

        private CancellationTokenSource? cancellationTokenSource;

        private long ultimaActividadUtcTicks;
        private long ultimaPersistenciaUtcTicks;
        private long versionActividad;
        private long versionActividadConfirmada;

        private int minutosInactividad = 15;
        private int expirando;

        private SesionInactividadService()
        {
        }

        public static SesionInactividadService Instance =>
            instancia.Value;

        public void IniciarNuevaSesion(
            int minutos)
        {
            minutosInactividad =
                Math.Clamp(
                    minutos,
                    1,
                    1440);

            Preferences.Set(
                SessionKeys.KeyInactivityMinutes,
                minutosInactividad);

            Interlocked.Exchange(
                ref versionActividad,
                0);

            Interlocked.Exchange(
                ref versionActividadConfirmada,
                0);

            Interlocked.Exchange(
                ref expirando,
                0);

            RegistrarActividad(
                persistirAhora: true);

            IniciarTemporizador();
        }

        public void ReanudarSesion()
        {
            if (!ExisteUsuarioActivo())
            {
                Detener();
                return;
            }

            minutosInactividad =
                Math.Clamp(
                    Preferences.Get(
                        SessionKeys.KeyInactivityMinutes,
                        15),
                    1,
                    1440);

            long ahoraTicks =
                DateTime.UtcNow.Ticks;

            long guardadoTicks =
                Preferences.Get(
                    SessionKeys.KeyLastActivityUtcTicks,
                    0L);

            if (guardadoTicks <= 0 ||
                guardadoTicks > ahoraTicks)
            {
                guardadoTicks = ahoraTicks;
            }

            Interlocked.Exchange(
                ref ultimaActividadUtcTicks,
                guardadoTicks);

            Interlocked.Exchange(
                ref ultimaPersistenciaUtcTicks,
                guardadoTicks);

            if (SuperoTiempo(
                    ahoraTicks,
                    guardadoTicks))
            {
                NotificarExpiracion();
                return;
            }

            Interlocked.Exchange(
                ref expirando,
                0);

            IniciarTemporizador();
        }

        public void RegistrarActividad()
        {
            RegistrarActividad(
                persistirAhora: false);
        }

        /// <summary>
        /// Devuelve la versión de actividad que todavía no fue confirmada por
        /// una respuesta HTTP del servidor.
        /// </summary>
        public long ObtenerVersionActividadPendienteServidor()
        {
            long version =
                Interlocked.Read(
                    ref versionActividad);

            long confirmada =
                Interlocked.Read(
                    ref versionActividadConfirmada);

            return version > confirmada
                ? version
                : 0;
        }

        public void ConfirmarActividadEnviada(
            long versionEnviada)
        {
            if (versionEnviada <= 0)
                return;

            while (true)
            {
                long actual =
                    Interlocked.Read(
                        ref versionActividadConfirmada);

                if (actual >= versionEnviada)
                    return;

                if (Interlocked.CompareExchange(
                        ref versionActividadConfirmada,
                        versionEnviada,
                        actual) == actual)
                {
                    return;
                }
            }
        }

        public void Pausar()
        {
            PersistirUltimaActividad();
            Detener();
        }

        public void Detener()
        {
            CancellationTokenSource? anterior =
                Interlocked.Exchange(
                    ref cancellationTokenSource,
                    null);

            if (anterior == null)
                return;

            try
            {
                anterior.Cancel();
            }
            catch (ObjectDisposedException)
            {
            }
            finally
            {
                anterior.Dispose();
            }
        }

        public void Limpiar()
        {
            Detener();

            Interlocked.Exchange(
                ref ultimaActividadUtcTicks,
                0);

            Interlocked.Exchange(
                ref ultimaPersistenciaUtcTicks,
                0);

            Interlocked.Exchange(
                ref versionActividad,
                0);

            Interlocked.Exchange(
                ref versionActividadConfirmada,
                0);

            Interlocked.Exchange(
                ref expirando,
                0);

            Preferences.Remove(
                SessionKeys.KeyLastActivityUtcTicks);

            Preferences.Remove(
                SessionKeys.KeyInactivityMinutes);
        }

        private void RegistrarActividad(
            bool persistirAhora)
        {
            if (!ExisteUsuarioActivo())
                return;

            long ahoraTicks =
                DateTime.UtcNow.Ticks;

            Interlocked.Exchange(
                ref ultimaActividadUtcTicks,
                ahoraTicks);

            Interlocked.Increment(
                ref versionActividad);

            long ultimaPersistencia =
                Interlocked.Read(
                    ref ultimaPersistenciaUtcTicks);

            if (!persistirAhora &&
                ahoraTicks - ultimaPersistencia <
                    IntervaloPersistencia.Ticks)
            {
                return;
            }

            Preferences.Set(
                SessionKeys.KeyLastActivityUtcTicks,
                ahoraTicks);

            Interlocked.Exchange(
                ref ultimaPersistenciaUtcTicks,
                ahoraTicks);
        }

        private void PersistirUltimaActividad()
        {
            long ticks =
                Interlocked.Read(
                    ref ultimaActividadUtcTicks);

            if (ticks <= 0)
                return;

            Preferences.Set(
                SessionKeys.KeyLastActivityUtcTicks,
                ticks);

            Interlocked.Exchange(
                ref ultimaPersistenciaUtcTicks,
                ticks);
        }

        private void IniciarTemporizador()
        {
            Detener();

            cancellationTokenSource =
                new CancellationTokenSource();

            _ = EjecutarAsync(
                cancellationTokenSource.Token);
        }

        private async Task EjecutarAsync(
            CancellationToken cancellationToken)
        {
            using var timer =
                new PeriodicTimer(
                    TimeSpan.FromSeconds(1));

            try
            {
                while (await timer.WaitForNextTickAsync(
                           cancellationToken))
                {
                    if (!ExisteUsuarioActivo())
                    {
                        Detener();
                        return;
                    }

                    long ahoraTicks =
                        DateTime.UtcNow.Ticks;

                    long ultimaActividad =
                        Interlocked.Read(
                            ref ultimaActividadUtcTicks);

                    if (SuperoTiempo(
                            ahoraTicks,
                            ultimaActividad))
                    {
                        NotificarExpiracion();
                        return;
                    }
                }
            }
            catch (OperationCanceledException)
            {
            }
        }

        private bool SuperoTiempo(
            long ahoraTicks,
            long ultimaActividad) =>
            ultimaActividad > 0 &&
            ahoraTicks - ultimaActividad >=
                TimeSpan.FromMinutes(
                    minutosInactividad).Ticks;

        private void NotificarExpiracion()
        {
            if (Interlocked.Exchange(
                    ref expirando,
                    1) == 1)
            {
                return;
            }

            Detener();

            SessionValidationService.Instance
                .NotificarSesionInactiva();
        }

        private static bool ExisteUsuarioActivo()
        {
            string usuarioId =
                Preferences.Get(
                    SessionKeys.KeyUserId,
                    string.Empty);

            return int.TryParse(
                       usuarioId,
                       out int id) &&
                   id > 0;
        }
    }
}
