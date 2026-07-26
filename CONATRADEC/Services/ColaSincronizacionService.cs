using CONATRADEC.Models;
using Microsoft.Maui.Storage;
using System.Text.Json;

namespace CONATRADEC.Services
{
    /// <summary>
    /// Base reusable para guardar operaciones realizadas sin conexión.
    /// En esta fase todavía no se procesa automáticamente; será utilizada al
    /// llevar Terrenos y Análisis de suelo a modo offline.
    /// </summary>
    public sealed class ColaSincronizacionService
    {
        public const string Pendiente = "PENDIENTE";
        public const string Sincronizando = "SINCRONIZANDO";
        public const string Sincronizado = "SINCRONIZADO";
        public const string Error = "ERROR";
        public const string Conflicto = "CONFLICTO";

        private static readonly Lazy<ColaSincronizacionService> lazy =
            new(() => new ColaSincronizacionService());

        private static readonly JsonSerializerOptions JsonOptions =
            new(JsonSerializerDefaults.Web);

        public static ColaSincronizacionService Instance => lazy.Value;

        private ColaSincronizacionService()
        {
        }

        public async Task<long> EncolarAsync<T>(
            string modulo,
            string tipoOperacion,
            string entidadLocalId,
            T payload,
            int? entidadServidorId = null)
        {
            string usuarioId = Preferences.Get(
                SessionKeys.KeyUserId,
                string.Empty);

            var entity = new OperacionPendienteEntity
            {
                UsuarioId = usuarioId,
                Modulo = (modulo ?? string.Empty).Trim(),
                TipoOperacion = (tipoOperacion ?? string.Empty).Trim(),
                EntidadLocalId = entidadLocalId ?? string.Empty,
                EntidadServidorId = entidadServidorId,
                JsonPayload = JsonSerializer.Serialize(
                    payload,
                    JsonOptions),
                Estado = Pendiente,
                Intentos = 0,
                FechaCreacionUtc = DateTime.UtcNow
            };

            return await ContenidoLocalDatabaseService.Instance
                .EncolarOperacionAsync(entity);
        }

        public Task<List<OperacionPendienteEntity>>
            ObtenerPendientesAsync(int limite = 100)
        {
            string usuarioId = Preferences.Get(
                SessionKeys.KeyUserId,
                string.Empty);

            return ContenidoLocalDatabaseService.Instance
                .ObtenerOperacionesPendientesAsync(
                    usuarioId,
                    limite);
        }

        public async Task MarcarErrorAsync(
            OperacionPendienteEntity entity,
            string error)
        {
            entity.Estado = Error;
            entity.Intentos++;
            entity.UltimoError = error ?? string.Empty;
            entity.FechaUltimoIntentoUtc = DateTime.UtcNow;

            await ContenidoLocalDatabaseService.Instance
                .ActualizarOperacionAsync(entity);
        }

        public async Task MarcarSincronizadaAsync(
            OperacionPendienteEntity entity,
            int? entidadServidorId = null)
        {
            entity.Estado = Sincronizado;
            entity.EntidadServidorId =
                entidadServidorId ?? entity.EntidadServidorId;
            entity.UltimoError = string.Empty;
            entity.FechaUltimoIntentoUtc = DateTime.UtcNow;
            entity.FechaSincronizacionUtc = DateTime.UtcNow;

            await ContenidoLocalDatabaseService.Instance
                .ActualizarOperacionAsync(entity);
        }
    }
}
