using CONATRADEC.Models;
using Microsoft.Maui.Storage;
using SQLite;
using System.Text.Json;

namespace CONATRADEC.Services
{
    /// <summary>
    /// Almacena los análisis históricos descargados del servidor.
    ///
    /// La descarga completa usa un paquete temporal. La copia anterior continúa
    /// activa hasta que todos los encabezados, detalles y datos de reporte se
    /// hayan guardado correctamente.
    /// </summary>
    public sealed class AnalisisHistorialLocalService
    {
        private const string PaqueteConsultas = "consultas-online";

        private static readonly Lazy<AnalisisHistorialLocalService> lazy =
            new(() => new AnalisisHistorialLocalService());

        private readonly SemaphoreSlim initializationLock = new(1, 1);
        private readonly SemaphoreSlim writeLock = new(1, 1);

        private readonly JsonSerializerOptions jsonOptions =
            new(JsonSerializerDefaults.Web)
            {
                PropertyNameCaseInsensitive = true
            };

        private SQLiteAsyncConnection? database;

        public static AnalisisHistorialLocalService Instance =>
            lazy.Value;

        public string DatabasePath =>
            Path.Combine(
                FileSystem.AppDataDirectory,
                "analisis_historial_local.db3");

        private AnalisisHistorialLocalService()
        {
        }

        public string CrearPaqueteTemporal() =>
            Guid.NewGuid().ToString("N");

        public async Task GuardarTemporalAsync(
            string paqueteId,
            AnalisisGuardadoResumen resumen,
            string detalleJson,
            string reporteJson)
        {
            ArgumentNullException.ThrowIfNull(resumen);

            string usuarioId = ObtenerUsuarioId();
            if (usuarioId == "0")
                throw new InvalidOperationException(
                    "No existe una sesión válida para guardar el historial.");

            await InicializarAsync();

            var entity = new AnalisisHistorialLocalEntity
            {
                Clave = CrearClave(
                    usuarioId,
                    paqueteId,
                    resumen.AnalisisSueloCalculoId),
                UsuarioId = usuarioId,
                AnalisisSueloCalculoId =
                    resumen.AnalisisSueloCalculoId,
                AnalisisSueloId = resumen.AnalisisSueloId,
                PaqueteId = paqueteId,
                Activo = false,
                ResumenJson = JsonSerializer.Serialize(
                    resumen,
                    jsonOptions),
                DetalleJson = detalleJson ?? string.Empty,
                ReporteJson = reporteJson ?? string.Empty,
                GuardadoUtc = DateTime.UtcNow,
                UltimoUsoUtc = DateTime.UtcNow
            };

            await database!.InsertOrReplaceAsync(entity);
        }

        public async Task ActivarPaqueteAsync(
            string paqueteId,
            string usuariosFiltroJson)
        {
            string usuarioId = ObtenerUsuarioId();
            if (usuarioId == "0")
                throw new InvalidOperationException(
                    "No existe una sesión válida para activar el historial.");

            await InicializarAsync();
            await writeLock.WaitAsync();

            try
            {
                await database!.RunInTransactionAsync(connection =>
                {
                    int total = connection.ExecuteScalar<int>(
                        "SELECT COUNT(*) FROM AnalisisHistorialLocal " +
                        "WHERE UsuarioId = ? AND PaqueteId = ?",
                        usuarioId,
                        paqueteId);

                    if (total < 0)
                        throw new InvalidOperationException(
                            "No fue posible validar el paquete de análisis.");

                    connection.Execute(
                        "UPDATE AnalisisHistorialLocal SET Activo = 0 " +
                        "WHERE UsuarioId = ?",
                        usuarioId);

                    connection.Execute(
                        "UPDATE AnalisisHistorialLocal SET Activo = 1 " +
                        "WHERE UsuarioId = ? AND PaqueteId = ?",
                        usuarioId,
                        paqueteId);

                    int detalles = connection.ExecuteScalar<int>(
                        "SELECT COUNT(*) FROM AnalisisHistorialLocal " +
                        "WHERE UsuarioId = ? AND PaqueteId = ? " +
                        "AND LENGTH(DetalleJson) > 0",
                        usuarioId,
                        paqueteId);

                    int reportes = connection.ExecuteScalar<int>(
                        "SELECT COUNT(*) FROM AnalisisHistorialLocal " +
                        "WHERE UsuarioId = ? AND PaqueteId = ? " +
                        "AND LENGTH(ReporteJson) > 0",
                        usuarioId,
                        paqueteId);

                    connection.InsertOrReplace(
                        new AnalisisHistorialEstadoEntity
                        {
                            UsuarioId = usuarioId,
                            PaqueteActivoId = paqueteId,
                            TotalAnalisis = total,
                            TotalDetalles = detalles,
                            TotalReportes = reportes,
                            UsuariosFiltroJson =
                                usuariosFiltroJson ?? string.Empty,
                            UltimaDescargaCompletaUtc = DateTime.UtcNow,
                            TamanoBytes = 0
                        });

                    connection.Execute(
                        "DELETE FROM AnalisisHistorialLocal " +
                        "WHERE UsuarioId = ? AND PaqueteId <> ?",
                        usuarioId,
                        paqueteId);
                });

                await ActualizarTamanoEstadoAsync(usuarioId);
            }
            finally
            {
                writeLock.Release();
            }
        }

        public async Task CancelarPaqueteAsync(string paqueteId)
        {
            string usuarioId = ObtenerUsuarioId();
            if (usuarioId == "0")
                return;

            await InicializarAsync();

            await database!.ExecuteAsync(
                "DELETE FROM AnalisisHistorialLocal " +
                "WHERE UsuarioId = ? AND PaqueteId = ? AND Activo = 0",
                usuarioId,
                paqueteId);
        }

        /// <summary>
        /// En una sesión online actualiza silenciosamente los encabezados que el
        /// usuario va consultando. No elimina el paquete preparado manualmente.
        /// </summary>
        public async Task GuardarResumenConsultadoAsync(
            AnalisisGuardadoResumen resumen)
        {
            ArgumentNullException.ThrowIfNull(resumen);

            string usuarioId = ObtenerUsuarioId();
            if (usuarioId == "0")
                return;

            await InicializarAsync();

            AnalisisHistorialLocalEntity? entity =
                await ObtenerActivoInternoAsync(
                    usuarioId,
                    resumen.AnalisisSueloCalculoId);

            if (entity == null)
            {
                entity = new AnalisisHistorialLocalEntity
                {
                    Clave = CrearClave(
                        usuarioId,
                        PaqueteConsultas,
                        resumen.AnalisisSueloCalculoId),
                    UsuarioId = usuarioId,
                    AnalisisSueloCalculoId =
                        resumen.AnalisisSueloCalculoId,
                    AnalisisSueloId = resumen.AnalisisSueloId,
                    PaqueteId = PaqueteConsultas,
                    Activo = true,
                    GuardadoUtc = DateTime.UtcNow
                };
            }

            entity.ResumenJson = JsonSerializer.Serialize(
                resumen,
                jsonOptions);
            entity.AnalisisSueloId = resumen.AnalisisSueloId;
            entity.UltimoUsoUtc = DateTime.UtcNow;

            await database!.InsertOrReplaceAsync(entity);
        }

        public async Task GuardarDetalleConsultadoAsync(
            int analisisSueloCalculoId,
            string detalleJson)
        {
            await ActualizarContenidoConsultadoAsync(
                analisisSueloCalculoId,
                detalleJson,
                esReporte: false);
        }

        public async Task GuardarReporteConsultadoAsync(
            int analisisSueloCalculoId,
            string reporteJson)
        {
            await ActualizarContenidoConsultadoAsync(
                analisisSueloCalculoId,
                reporteJson,
                esReporte: true);
        }

        public async Task<List<AnalisisGuardadoResumen>>
            ListarAsync()
        {
            string usuarioId = ObtenerUsuarioId();
            if (usuarioId == "0")
                return new List<AnalisisGuardadoResumen>();

            await InicializarAsync();

            List<AnalisisHistorialLocalEntity> entities =
                await database!
                    .Table<AnalisisHistorialLocalEntity>()
                    .Where(item =>
                        item.UsuarioId == usuarioId &&
                        item.Activo)
                    .ToListAsync();

            var result = new List<AnalisisGuardadoResumen>();

            foreach (AnalisisHistorialLocalEntity entity in entities)
            {
                AnalisisGuardadoResumen? resumen =
                    Deserializar<AnalisisGuardadoResumen>(
                        entity.ResumenJson);

                if (resumen != null)
                    result.Add(resumen);
            }

            return result
                .OrderByDescending(item =>
                    item.FechaRegistroValor ??
                    item.FechaCalculoValor ??
                    item.FechaAnalisisValor)
                .ToList();
        }

        public async Task<string?> ObtenerDetalleJsonAsync(
            int analisisSueloCalculoId)
        {
            return await ObtenerContenidoAsync(
                analisisSueloCalculoId,
                esReporte: false);
        }

        public async Task<string?> ObtenerReporteJsonAsync(
            int analisisSueloCalculoId)
        {
            return await ObtenerContenidoAsync(
                analisisSueloCalculoId,
                esReporte: true);
        }

        public async Task GuardarUsuariosFiltroConsultadosAsync(
            string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return;

            string usuarioId = ObtenerUsuarioId();
            if (usuarioId == "0")
                return;

            await InicializarAsync();

            AnalisisHistorialEstadoEntity? estado =
                await database!
                    .Table<AnalisisHistorialEstadoEntity>()
                    .FirstOrDefaultAsync(item =>
                        item.UsuarioId == usuarioId);

            estado ??= new AnalisisHistorialEstadoEntity
            {
                UsuarioId = usuarioId,
                PaqueteActivoId = PaqueteConsultas
            };

            estado.UsuariosFiltroJson = json;
            await database.InsertOrReplaceAsync(estado);
        }

        public async Task<string> ObtenerUsuariosFiltroJsonAsync()
        {
            string usuarioId = ObtenerUsuarioId();
            if (usuarioId == "0")
                return string.Empty;

            await InicializarAsync();

            AnalisisHistorialEstadoEntity? estado =
                await database!
                    .Table<AnalisisHistorialEstadoEntity>()
                    .FirstOrDefaultAsync(item =>
                        item.UsuarioId == usuarioId);

            return estado?.UsuariosFiltroJson ?? string.Empty;
        }

        public async Task<AnalisisHistorialEstadoEntity?>
            ObtenerEstadoAsync()
        {
            string usuarioId = ObtenerUsuarioId();
            if (usuarioId == "0")
                return null;

            await InicializarAsync();

            return await database!
                .Table<AnalisisHistorialEstadoEntity>()
                .FirstOrDefaultAsync(item =>
                    item.UsuarioId == usuarioId);
        }

        public async Task<bool> TieneHistorialCompletoAsync()
        {
            AnalisisHistorialEstadoEntity? estado =
                await ObtenerEstadoAsync();

            return estado != null &&
                   !string.IsNullOrWhiteSpace(
                       estado.PaqueteActivoId) &&
                   estado.TotalAnalisis ==
                       estado.TotalDetalles &&
                   estado.TotalAnalisis ==
                       estado.TotalReportes;
        }

        public long ObtenerTamanoBytes()
        {
            try
            {
                return File.Exists(DatabasePath)
                    ? new FileInfo(DatabasePath).Length
                    : 0;
            }
            catch
            {
                return 0;
            }
        }

        private async Task ActualizarContenidoConsultadoAsync(
            int analisisSueloCalculoId,
            string json,
            bool esReporte)
        {
            if (analisisSueloCalculoId <= 0 ||
                string.IsNullOrWhiteSpace(json))
            {
                return;
            }

            string usuarioId = ObtenerUsuarioId();
            if (usuarioId == "0")
                return;

            await InicializarAsync();

            AnalisisHistorialLocalEntity? entity =
                await ObtenerActivoInternoAsync(
                    usuarioId,
                    analisisSueloCalculoId);

            if (entity == null)
            {
                entity = new AnalisisHistorialLocalEntity
                {
                    Clave = CrearClave(
                        usuarioId,
                        PaqueteConsultas,
                        analisisSueloCalculoId),
                    UsuarioId = usuarioId,
                    AnalisisSueloCalculoId =
                        analisisSueloCalculoId,
                    PaqueteId = PaqueteConsultas,
                    Activo = true,
                    GuardadoUtc = DateTime.UtcNow
                };
            }

            if (esReporte)
                entity.ReporteJson = json;
            else
                entity.DetalleJson = json;

            entity.UltimoUsoUtc = DateTime.UtcNow;
            await database!.InsertOrReplaceAsync(entity);
        }

        private async Task<string?> ObtenerContenidoAsync(
            int analisisSueloCalculoId,
            bool esReporte)
        {
            string usuarioId = ObtenerUsuarioId();
            if (usuarioId == "0" ||
                analisisSueloCalculoId <= 0)
            {
                return null;
            }

            await InicializarAsync();

            AnalisisHistorialLocalEntity? entity =
                await ObtenerActivoInternoAsync(
                    usuarioId,
                    analisisSueloCalculoId);

            if (entity == null)
                return null;

            entity.UltimoUsoUtc = DateTime.UtcNow;
            await database!.UpdateAsync(entity);

            string value = esReporte
                ? entity.ReporteJson
                : entity.DetalleJson;

            return string.IsNullOrWhiteSpace(value)
                ? null
                : value;
        }

        private async Task<AnalisisHistorialLocalEntity?>
            ObtenerActivoInternoAsync(
                string usuarioId,
                int analisisSueloCalculoId)
        {
            return await database!
                .Table<AnalisisHistorialLocalEntity>()
                .Where(item =>
                    item.UsuarioId == usuarioId &&
                    item.AnalisisSueloCalculoId ==
                        analisisSueloCalculoId &&
                    item.Activo)
                .OrderByDescending(item => item.GuardadoUtc)
                .FirstOrDefaultAsync();
        }

        private async Task ActualizarTamanoEstadoAsync(
            string usuarioId)
        {
            AnalisisHistorialEstadoEntity? estado =
                await database!
                    .Table<AnalisisHistorialEstadoEntity>()
                    .FirstOrDefaultAsync(item =>
                        item.UsuarioId == usuarioId);

            if (estado == null)
                return;

            estado.TamanoBytes = ObtenerTamanoBytes();
            await database.UpdateAsync(estado);
        }

        private async Task InicializarAsync()
        {
            if (database != null)
                return;

            await initializationLock.WaitAsync();

            try
            {
                if (database != null)
                    return;

                database = new SQLiteAsyncConnection(
                    DatabasePath,
                    SQLiteOpenFlags.ReadWrite |
                    SQLiteOpenFlags.Create |
                    SQLiteOpenFlags.SharedCache);

                await database.CreateTableAsync<
                    AnalisisHistorialLocalEntity>();

                await database.CreateTableAsync<
                    AnalisisHistorialEstadoEntity>();
            }
            finally
            {
                initializationLock.Release();
            }
        }

        private T? Deserializar<T>(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return default;

            try
            {
                return JsonSerializer.Deserialize<T>(
                    json,
                    jsonOptions);
            }
            catch
            {
                return default;
            }
        }

        private static string CrearClave(
            string usuarioId,
            string paqueteId,
            int analisisSueloCalculoId) =>
            $"{usuarioId}|{paqueteId}|{analisisSueloCalculoId}";

        private static string ObtenerUsuarioId()
        {
            string value = Preferences.Get(
                SessionKeys.KeyUserId,
                "0");

            return string.IsNullOrWhiteSpace(value)
                ? "0"
                : value.Trim();
        }
    }
}
