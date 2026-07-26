using CONATRADEC.Models;
using Microsoft.Maui.Storage;
using SQLite;

namespace CONATRADEC.Services
{
    public sealed class ContenidoLocalDatabaseService
    {
        private static readonly Lazy<ContenidoLocalDatabaseService> lazy =
            new(() => new ContenidoLocalDatabaseService());

        private readonly SQLiteAsyncConnection database;
        private readonly SemaphoreSlim initializationLock = new(1, 1);
        private bool initialized;

        public static ContenidoLocalDatabaseService Instance => lazy.Value;
        public string DatabasePath { get; }

        private ContenidoLocalDatabaseService()
        {
            DatabasePath = Path.Combine(
                FileSystem.AppDataDirectory,
                "contenido_local.db3");

            database = new SQLiteAsyncConnection(
                DatabasePath,
                SQLiteOpenFlags.ReadWrite |
                SQLiteOpenFlags.Create |
                SQLiteOpenFlags.SharedCache);
        }

        public async Task<ContenidoRespuestaCacheEntity?>
            ObtenerRespuestaAsync(string cacheKey)
        {
            await InicializarAsync();

            return await database
                .Table<ContenidoRespuestaCacheEntity>()
                .Where(x => x.CacheKey == cacheKey)
                .FirstOrDefaultAsync();
        }

        public async Task GuardarRespuestaAsync(
            ContenidoRespuestaCacheEntity entity)
        {
            ArgumentNullException.ThrowIfNull(entity);
            await InicializarAsync();
            await database.InsertOrReplaceAsync(entity);
        }

        public async Task MarcarUsoRespuestaAsync(
            string cacheKey,
            DateTime fechaUtc)
        {
            await InicializarAsync();

            await database.ExecuteAsync(
                "UPDATE ContenidoRespuestaCache " +
                "SET UltimoUsoUtc = ? WHERE CacheKey = ?",
                fechaUtc,
                cacheKey);
        }

        public async Task EliminarRespuestaAsync(string cacheKey)
        {
            await InicializarAsync();

            await database.ExecuteAsync(
                "DELETE FROM ContenidoRespuestaCache " +
                "WHERE CacheKey = ?",
                cacheKey);
        }

        public async Task<int> EliminarRespuestasVersionAnteriorAsync(
            string usuarioId,
            string modulo,
            string versionVigente)
        {
            await InicializarAsync();

            return await database.ExecuteAsync(
                "DELETE FROM ContenidoRespuestaCache " +
                "WHERE UsuarioId = ? AND Modulo = ? " +
                "AND Version <> ?",
                usuarioId,
                modulo,
                versionVigente);
        }

        public async Task LimpiarModuloAsync(
            string usuarioId,
            string modulo)
        {
            await InicializarAsync();

            await database.ExecuteAsync(
                "DELETE FROM ContenidoRespuestaCache " +
                "WHERE UsuarioId = ? AND Modulo = ?",
                usuarioId,
                modulo);
        }

        public async Task GuardarEstadoAsync(
            ContenidoModuloEstadoEntity entity)
        {
            ArgumentNullException.ThrowIfNull(entity);
            await InicializarAsync();
            await database.InsertOrReplaceAsync(entity);
        }

        public async Task<ContenidoModuloEstadoEntity?>
            ObtenerEstadoAsync(string clave)
        {
            await InicializarAsync();

            return await database
                .Table<ContenidoModuloEstadoEntity>()
                .Where(x => x.Clave == clave)
                .FirstOrDefaultAsync();
        }

        public async Task GuardarImagenAsync(
            ContenidoImagenCacheEntity entity)
        {
            ArgumentNullException.ThrowIfNull(entity);
            await InicializarAsync();
            await database.InsertOrReplaceAsync(entity);
        }

        public async Task<List<ContenidoImagenCacheEntity>>
            ObtenerImagenesVersionAnteriorAsync(
                string usuarioId,
                string modulo,
                string versionVigente)
        {
            await InicializarAsync();

            return await database
                .Table<ContenidoImagenCacheEntity>()
                .Where(x =>
                    x.UsuarioId == usuarioId &&
                    x.Modulo == modulo &&
                    x.Version != versionVigente)
                .ToListAsync();
        }

        public async Task<List<ContenidoImagenCacheEntity>>
            ObtenerTodasImagenesAsync()
        {
            await InicializarAsync();

            return await database
                .Table<ContenidoImagenCacheEntity>()
                .OrderBy(x => x.UltimoUsoUtc)
                .ToListAsync();
        }

        public async Task EliminarImagenAsync(string clave)
        {
            await InicializarAsync();

            await database.ExecuteAsync(
                "DELETE FROM ContenidoImagenCache WHERE Clave = ?",
                clave);
        }

        public async Task EliminarImagenesPorRutaAsync(string rutaLocal)
        {
            await InicializarAsync();

            await database.ExecuteAsync(
                "DELETE FROM ContenidoImagenCache WHERE RutaLocal = ?",
                rutaLocal);
        }

        public async Task<int> ContarReferenciasImagenAsync(string rutaLocal)
        {
            await InicializarAsync();

            return await database.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM ContenidoImagenCache " +
                "WHERE RutaLocal = ?",
                rutaLocal);
        }

        public async Task<long> EncolarOperacionAsync(
            OperacionPendienteEntity entity)
        {
            ArgumentNullException.ThrowIfNull(entity);
            await InicializarAsync();
            await database.InsertAsync(entity);
            return entity.OperacionId;
        }

        public async Task<List<OperacionPendienteEntity>>
            ObtenerOperacionesPendientesAsync(
                string usuarioId,
                int limite = 100)
        {
            await InicializarAsync();

            return await database
                .Table<OperacionPendienteEntity>()
                .Where(x =>
                    x.UsuarioId == usuarioId &&
                    (x.Estado == "PENDIENTE" ||
                     x.Estado == "ERROR"))
                .OrderBy(x => x.FechaCreacionUtc)
                .Take(Math.Clamp(limite, 1, 500))
                .ToListAsync();
        }

        public async Task ActualizarOperacionAsync(
            OperacionPendienteEntity entity)
        {
            ArgumentNullException.ThrowIfNull(entity);
            await InicializarAsync();
            await database.UpdateAsync(entity);
        }

        public async Task<int> ContarOperacionesPendientesAsync(
            string usuarioId)
        {
            await InicializarAsync();

            return await database.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM OperacionPendiente " +
                "WHERE UsuarioId = ? " +
                "AND Estado IN ('PENDIENTE','ERROR')",
                usuarioId);
        }

        public async Task<ResumenCacheLocal> ObtenerResumenAsync(
            string usuarioId)
        {
            await InicializarAsync();

            int respuestas = await database.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM ContenidoRespuestaCache " +
                "WHERE UsuarioId = ?",
                usuarioId);

            int imagenes = await database.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM ContenidoImagenCache " +
                "WHERE UsuarioId = ?",
                usuarioId);

            long bytes = await database.ExecuteScalarAsync<long>(
                "SELECT COALESCE(SUM(TamanoBytes),0) " +
                "FROM ContenidoImagenCache WHERE UsuarioId = ?",
                usuarioId);

            int pendientes =
                await ContarOperacionesPendientesAsync(usuarioId);

            return new ResumenCacheLocal
            {
                TotalRespuestas = respuestas,
                TotalImagenes = imagenes,
                TamanoImagenesBytes = bytes,
                OperacionesPendientes = pendientes
            };
        }

        public async Task GuardarSeccionPaqueteAsync(
            CatalogoOfflineSeccionEntity entity)
        {
            ArgumentNullException.ThrowIfNull(entity);
            await InicializarAsync();
            await database.InsertOrReplaceAsync(entity);
        }

        public async Task<CatalogoOfflineSeccionEntity?>
            ObtenerSeccionPaqueteActivoAsync(
                string usuarioId,
                string seccion)
        {
            CatalogoOfflineEstadoEntity? estado =
                await ObtenerEstadoPaqueteAsync(usuarioId);

            if (estado == null ||
                string.IsNullOrWhiteSpace(estado.PaqueteActivoId))
            {
                return null;
            }

            await InicializarAsync();

            return await database
                .Table<CatalogoOfflineSeccionEntity>()
                .Where(x =>
                    x.UsuarioId == usuarioId &&
                    x.PaqueteId == estado.PaqueteActivoId &&
                    x.Seccion == seccion)
                .FirstOrDefaultAsync();
        }

        public async Task<CatalogoOfflineEstadoEntity?>
            ObtenerEstadoPaqueteAsync(string usuarioId)
        {
            await InicializarAsync();

            return await database
                .Table<CatalogoOfflineEstadoEntity>()
                .Where(x => x.Clave == usuarioId)
                .FirstOrDefaultAsync();
        }

        public async Task GuardarEstadoPaqueteAsync(
            CatalogoOfflineEstadoEntity entity)
        {
            ArgumentNullException.ThrowIfNull(entity);
            await InicializarAsync();
            await database.InsertOrReplaceAsync(entity);
        }

        public async Task ActivarPaqueteAsync(
            CatalogoOfflineEstadoEntity estado)
        {
            ArgumentNullException.ThrowIfNull(estado);
            await InicializarAsync();

            await database.RunInTransactionAsync(connection =>
            {
                connection.InsertOrReplace(estado);
            });

            await database.ExecuteAsync(
                "DELETE FROM CatalogoOfflineSeccion " +
                "WHERE UsuarioId = ? AND PaqueteId <> ?",
                estado.UsuarioId,
                estado.PaqueteActivoId);
        }

        public async Task EliminarPaqueteTemporalAsync(
            string usuarioId,
            string paqueteId)
        {
            await InicializarAsync();

            await database.ExecuteAsync(
                "DELETE FROM CatalogoOfflineSeccion " +
                "WHERE UsuarioId = ? AND PaqueteId = ?",
                usuarioId,
                paqueteId);
        }

        private async Task InicializarAsync()
        {
            if (initialized)
                return;

            await initializationLock.WaitAsync();

            try
            {
                if (initialized)
                    return;

                await database.CreateTableAsync<
                    ContenidoRespuestaCacheEntity>();

                await database.CreateTableAsync<
                    ContenidoModuloEstadoEntity>();

                await database.CreateTableAsync<
                    ContenidoImagenCacheEntity>();

                await database.CreateTableAsync<
                    OperacionPendienteEntity>();

                await database.CreateTableAsync<
                    CatalogoOfflineSeccionEntity>();

                await database.CreateTableAsync<
                    CatalogoOfflineEstadoEntity>();

                initialized = true;
            }
            finally
            {
                initializationLock.Release();
            }
        }
    }
}
