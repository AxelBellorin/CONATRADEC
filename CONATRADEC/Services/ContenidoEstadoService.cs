using CONATRADEC.Models;
using Microsoft.Maui.Storage;
using System.Collections.Concurrent;

namespace CONATRADEC.Services
{
    public sealed class ContenidoEstadoService
    {
        private static readonly Lazy<ContenidoEstadoService> lazy =
            new(() => new ContenidoEstadoService());

        private readonly ConcurrentDictionary<
            string,
            EstadoSincronizacionContenido> estados = new(
                StringComparer.OrdinalIgnoreCase);

        public static ContenidoEstadoService Instance => lazy.Value;

        public event EventHandler<
            EstadoSincronizacionContenidoEventArgs>? EstadoCambiado;

        private ContenidoEstadoService()
        {
        }

        public EstadoSincronizacionContenido Obtener(string modulo)
        {
            string clave = Normalizar(modulo);

            return estados.TryGetValue(
                clave,
                out EstadoSincronizacionContenido? estado)
                ? estado
                : new EstadoSincronizacionContenido
                {
                    Modulo = clave,
                    Tipo = TipoEstadoSincronizacionContenido.SinDatos,
                    Mensaje = "Sin copia local disponible",
                    Detalle =
                        "Origen: ninguno · conecte el dispositivo para sincronizar."
                };
        }

        public void Actualizar(
            string modulo,
            TipoEstadoSincronizacionContenido tipo,
            string mensaje,
            string detalle = "",
            string version = "",
            DateTime? ultimaSincronizacionUtc = null)
        {
            string clave = Normalizar(modulo);

            var estado = new EstadoSincronizacionContenido
            {
                Modulo = clave,
                Tipo = tipo,
                Mensaje = mensaje ?? string.Empty,
                Detalle = detalle ?? string.Empty,
                Version = version ?? string.Empty,
                UltimaSincronizacionUtc = ultimaSincronizacionUtc,
                ActualizadoUtc = DateTime.UtcNow
            };

            estados[clave] = estado;

            EstadoCambiado?.Invoke(
                this,
                new EstadoSincronizacionContenidoEventArgs(estado));
        }

        public async Task CargarPersistidoAsync(string modulo)
        {
            string claveModulo = Normalizar(modulo);
            string usuarioId = Preferences.Get(
                SessionKeys.KeyUserId,
                string.Empty);

            string clave = $"{usuarioId}|{claveModulo}";

            ContenidoModuloEstadoEntity? estado =
                await ContenidoLocalDatabaseService.Instance
                    .ObtenerEstadoAsync(clave);

            if (estado == null ||
                string.IsNullOrWhiteSpace(estado.Version))
            {
                Actualizar(
                    claveModulo,
                    TipoEstadoSincronizacionContenido.SinDatos,
                    "Sin copia local disponible",
                    "Origen: ninguno · conecte el dispositivo para sincronizar.");

                return;
            }

            Actualizar(
                claveModulo,
                TipoEstadoSincronizacionContenido.Local,
                "Datos sincronizados disponibles · verificando conexión",
                "Datos sincronizados anteriormente · " +
                ConstruirDetalleFecha(
                    estado.UltimaSincronizacionExitosaUtc),
                estado.Version,
                estado.UltimaSincronizacionExitosaUtc);
        }

        public static string ConstruirDetalleFecha(DateTime? fechaUtc)
        {
            if (!fechaUtc.HasValue)
                return "Sin sincronización completa registrada.";

            DateTime local = fechaUtc.Value.ToLocalTime();

            return $"Última sincronización: {local:dd/MM/yyyy h:mm tt}";
        }

        private static string Normalizar(string modulo) =>
            (modulo ?? string.Empty)
                .Trim()
                .ToLowerInvariant();
    }
}
