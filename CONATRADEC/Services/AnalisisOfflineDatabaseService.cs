using CONATRADEC.Models;
using Microsoft.Maui.Storage;
using SQLite;
using System.Text.Json;

namespace CONATRADEC.Services
{
    public static class AnalisisOfflineEstados
    {
        public const string Pendiente = "PENDIENTE";
        public const string Sincronizando = "SINCRONIZANDO";
        public const string Sincronizado = "SINCRONIZADO";
        public const string Error = "ERROR";
        public const string RequiereRevision = "REQUIERE_REVISION";
    }

    [Table("analisisOfflineLocal")]
    public sealed class AnalisisOfflineLocalEntity
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [Indexed(Unique = true)]
        public string OperacionLocalId { get; set; } =
            string.Empty;

        [Indexed]
        public string UsuarioId { get; set; } =
            string.Empty;

        public string TipoOperacion { get; set; } =
            "CREAR";

        public int? AnalisisSueloCalculoIdServidor { get; set; }

        public int? AnalisisSueloIdServidor { get; set; }

        public string IdentificadorAnalisis { get; set; } =
            string.Empty;

        public string Laboratorio { get; set; } =
            string.Empty;

        public string FechaAnalisis { get; set; } =
            string.Empty;

        public string FechaCreacionUtc { get; set; } =
            string.Empty;

        public string FechaActualizacionUtc { get; set; } =
            string.Empty;

        public int TerrenoId { get; set; }

        public int TipoCultivoId { get; set; }

        public int TipoAnalisisSueloId { get; set; }

        public decimal CantidadQuintalesOro { get; set; }

        public decimal TamanoFinca { get; set; }

        public decimal Ph { get; set; }

        public bool TieneBalance { get; set; }

        public bool TieneEnmienda { get; set; }

        public bool TieneMixta { get; set; }

        public string PayloadJson { get; set; } =
            string.Empty;

        public string VersionMotor { get; set; } =
            string.Empty;

        public string HashPaquete { get; set; } =
            string.Empty;

        [Indexed]
        public string Estado { get; set; } =
            AnalisisOfflineEstados.Pendiente;

        public int Intentos { get; set; }

        public string UltimoError { get; set; } =
            string.Empty;

        public string RespuestaServidorJson { get; set; } =
            string.Empty;
    }

    /// <summary>
    /// Base local exclusiva para análisis calculados fuera de línea.
    ///
    /// Se separa de contenido_local.db3 para que una limpieza de imágenes o
    /// catálogos nunca elimine operaciones pendientes.
    /// </summary>
    public sealed class AnalisisOfflineDatabaseService
    {
        public const int IdLocalBase = 1_700_000_000;

        private static readonly Lazy<
            AnalisisOfflineDatabaseService> lazy =
                new(() =>
                    new AnalisisOfflineDatabaseService());

        private readonly SemaphoreSlim initLock =
            new(1, 1);

        private readonly JsonSerializerOptions jsonOptions =
            new(JsonSerializerDefaults.Web)
            {
                PropertyNameCaseInsensitive = true
            };

        private SQLiteAsyncConnection? database;

        public static AnalisisOfflineDatabaseService Instance =>
            lazy.Value;

        public event EventHandler? DatosCambiados;

        public string DatabasePath =>
            Path.Combine(
                FileSystem.AppDataDirectory,
                "analisis_offline.db3");

        private AnalisisOfflineDatabaseService()
        {
        }

        public static bool EsIdLocal(
            int id) =>
            id >= IdLocalBase;

        public static int ObtenerIdInterno(
            int idLocal) =>
            idLocal - IdLocalBase;

        public static int CrearIdPublico(
            int idInterno) =>
            IdLocalBase + idInterno;

        public async Task<AnalisisOfflineLocalEntity>
            GuardarAsync(
                string payloadJson,
                string tipoOperacion,
                int? analisisSueloCalculoIdServidor,
                MotorCalculoPaquete paquete,
                int? idLocalExistente = null,
                CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(payloadJson))
            {
                throw new InvalidOperationException(
                    "No se recibió el contenido del análisis que se debe guardar.");
            }

            await InicializarAsync();

            AnalisisOfflineLocalEntity entity;

            if (idLocalExistente.HasValue &&
                EsIdLocal(idLocalExistente.Value))
            {
                int interno =
                    ObtenerIdInterno(
                        idLocalExistente.Value);

                entity =
                    await database!
                        .Table<AnalisisOfflineLocalEntity>()
                        .FirstOrDefaultAsync(item =>
                            item.Id == interno)
                    ?? throw new InvalidOperationException(
                        "No se encontró el análisis local que se debe actualizar.");
            }
            else
            {
                entity =
                    new AnalisisOfflineLocalEntity
                    {
                        OperacionLocalId =
                            Guid.NewGuid().ToString("D"),
                        UsuarioId =
                            ObtenerUsuarioId(),
                        FechaCreacionUtc =
                            DateTime.UtcNow.ToString("O")
                    };
            }

            bool yaFueSincronizado =
                string.Equals(
                    entity.Estado,
                    AnalisisOfflineEstados.Sincronizado,
                    StringComparison.OrdinalIgnoreCase);

            if (yaFueSincronizado)
            {
                /*
                 * Una edición posterior es una nueva operación idempotente.
                 * No puede reutilizar el UUID de la creación ya completada.
                 */
                entity.OperacionLocalId =
                    Guid.NewGuid().ToString("D");
            }

            CompletarResumen(
                entity,
                payloadJson);

            /*
             * Editar un registro que todavía nació localmente no lo convierte
             * en una edición del servidor. Conserva CREAR y reemplaza el mismo
             * payload para que la cola produzca un solo análisis.
             */
            if (!yaFueSincronizado &&
                entity.Id > 0 &&
                string.Equals(
                    entity.TipoOperacion,
                    "CREAR",
                    StringComparison.OrdinalIgnoreCase) &&
                entity.AnalisisSueloCalculoIdServidor == null)
            {
                entity.TipoOperacion = "CREAR";
            }
            else
            {
                entity.TipoOperacion =
                    NormalizarTipo(tipoOperacion);
            }

            entity.AnalisisSueloCalculoIdServidor =
                analisisSueloCalculoIdServidor ??
                entity.AnalisisSueloCalculoIdServidor;

            entity.PayloadJson =
                payloadJson;

            entity.VersionMotor =
                paquete.VersionPaquete;

            entity.HashPaquete =
                paquete.HashSha256;

            entity.FechaActualizacionUtc =
                DateTime.UtcNow.ToString("O");

            entity.Estado =
                AnalisisOfflineEstados.Pendiente;

            entity.Intentos = 0;
            entity.UltimoError = string.Empty;

            if (entity.Id == 0)
                await database!.InsertAsync(entity);
            else
                await database!.UpdateAsync(entity);

            DatosCambiados?.Invoke(
                this,
                EventArgs.Empty);

            return entity;
        }

        public async Task<AnalisisOfflineLocalEntity?>
            ObtenerPorIdPublicoAsync(
                int idPublico)
        {
            if (!EsIdLocal(idPublico))
                return null;

            await InicializarAsync();

            int id =
                ObtenerIdInterno(idPublico);

            return await database!
                .Table<AnalisisOfflineLocalEntity>()
                .FirstOrDefaultAsync(item =>
                    item.Id == id);
        }

        public async Task<List<
            AnalisisOfflineLocalEntity>>
            ListarPendientesAsync()
        {
            await InicializarAsync();

            string usuarioId =
                ObtenerUsuarioId();

            List<AnalisisOfflineLocalEntity> items =
                await database!
                    .Table<AnalisisOfflineLocalEntity>()
                    .Where(item =>
                        item.UsuarioId == usuarioId &&
                        item.Estado !=
                            AnalisisOfflineEstados.Sincronizado)
                    .ToListAsync();

            return items
                .OrderByDescending(item =>
                    ParseFecha(
                        item.FechaActualizacionUtc))
                .ToList();
        }

        public async Task<List<AnalisisGuardadoResumen>>
            ListarResumenPendienteAsync()
        {
            List<AnalisisOfflineLocalEntity> items =
                await ListarPendientesAsync();

            return CrearResumenes(items);
        }

        public async Task<List<AnalisisGuardadoResumen>>
            ListarResumenLocalAsync()
        {
            await InicializarAsync();

            string usuarioId =
                ObtenerUsuarioId();

            List<AnalisisOfflineLocalEntity> items =
                await database!
                    .Table<AnalisisOfflineLocalEntity>()
                    .Where(item =>
                        item.UsuarioId ==
                            usuarioId)
                    .ToListAsync();

            return CrearResumenes(
                items
                    .OrderByDescending(item =>
                        ParseFecha(
                            item.FechaActualizacionUtc))
                    .ToList());
        }

        private static List<AnalisisGuardadoResumen>
            CrearResumenes(
                IEnumerable<
                    AnalisisOfflineLocalEntity> items)
        {
            string usuario =
                Preferences.Get(
                    SessionKeys.KeyNombreCompletoUsuario,
                    "Usuario local");

            return items
                .Select(item => new AnalisisGuardadoResumen
                {
                    AnalisisSueloCalculoId =
                        CrearIdPublico(item.Id),
                    AnalisisSueloId =
                        CrearIdPublico(item.Id),
                    IdentificadorAnalisisSuelo =
                        AgregarEstado(
                            item.IdentificadorAnalisis,
                            item.Estado),
                    LaboratorioAnalasisSuelo =
                        item.Laboratorio,
                    FechaAnalisisSuelo =
                        item.FechaAnalisis,
                    FechaCreacionAnalisisSuelo =
                        item.FechaCreacionUtc,
                    FechaCalculo =
                        item.FechaActualizacionUtc,
                    TerrenoId =
                        item.TerrenoId,
                    CodigoTerreno =
                        $"LOCAL-{item.TerrenoId}",
                    NombreCliente =
                        item.Estado ==
                            AnalisisOfflineEstados
                                .Sincronizado
                            ? "Sincronizado con el servidor"
                            : "Pendiente de sincronización",
                    NombreTerreno =
                        $"Terreno #{item.TerrenoId}",
                    TipoCultivoId =
                        item.TipoCultivoId,
                    TipoAnalisisSueloId =
                        item.TipoAnalisisSueloId,
                    CantidadQuintalesOro =
                        item.CantidadQuintalesOro,
                    TamanoFinca =
                        item.TamanoFinca,
                    PhAnalisisSuelo =
                        item.Ph,
                    UsuarioId =
                        int.TryParse(
                            item.UsuarioId,
                            out int usuarioId)
                                ? usuarioId
                                : null,
                    NombreUsuario =
                        usuario,
                    TieneFormulaNutricional =
                        item.TieneBalance,
                    TieneEnmiendaCalcarea =
                        item.TieneEnmienda,
                    TieneFertilizacionMixta =
                        item.TieneMixta
                })
                .ToList();
        }

        public async Task<bool> EliminarLocalAsync(
            int idPublico)
        {
            if (!EsIdLocal(idPublico))
                return false;

            await InicializarAsync();

            int id =
                ObtenerIdInterno(idPublico);

            AnalisisOfflineLocalEntity? entity =
                await database!
                    .Table<AnalisisOfflineLocalEntity>()
                    .FirstOrDefaultAsync(item =>
                        item.Id == id &&
                        item.UsuarioId ==
                            ObtenerUsuarioId());

            if (entity == null)
                return false;

            await database.DeleteAsync(entity);

            DatosCambiados?.Invoke(
                this,
                EventArgs.Empty);

            return true;
        }

        public async Task MarcarSincronizandoAsync(
            AnalisisOfflineLocalEntity entity)
        {
            await InicializarAsync();

            entity.Estado =
                AnalisisOfflineEstados.Sincronizando;

            entity.Intentos++;

            entity.FechaActualizacionUtc =
                DateTime.UtcNow.ToString("O");

            await database!.UpdateAsync(entity);

            DatosCambiados?.Invoke(
                this,
                EventArgs.Empty);
        }

        public async Task MarcarSincronizadoAsync(
            AnalisisOfflineLocalEntity entity,
            int analisisSueloId,
            int analisisSueloCalculoId,
            string respuestaServidorJson)
        {
            await InicializarAsync();

            entity.Estado =
                AnalisisOfflineEstados.Sincronizado;

            entity.AnalisisSueloIdServidor =
                analisisSueloId;

            entity.AnalisisSueloCalculoIdServidor =
                analisisSueloCalculoId;

            entity.RespuestaServidorJson =
                respuestaServidorJson;

            entity.UltimoError =
                string.Empty;

            entity.FechaActualizacionUtc =
                DateTime.UtcNow.ToString("O");

            await database!.UpdateAsync(entity);

            DatosCambiados?.Invoke(
                this,
                EventArgs.Empty);
        }

        public async Task MarcarErrorAsync(
            AnalisisOfflineLocalEntity entity,
            string error,
            bool requiereRevision)
        {
            await InicializarAsync();

            entity.Estado =
                requiereRevision
                    ? AnalisisOfflineEstados
                        .RequiereRevision
                    : AnalisisOfflineEstados.Error;

            entity.UltimoError =
                error?.Trim() ??
                string.Empty;

            entity.FechaActualizacionUtc =
                DateTime.UtcNow.ToString("O");

            await database!.UpdateAsync(entity);

            DatosCambiados?.Invoke(
                this,
                EventArgs.Empty);
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

        private async Task InicializarAsync()
        {
            if (database != null)
                return;

            await initLock.WaitAsync();

            try
            {
                if (database != null)
                    return;

                database =
                    new SQLiteAsyncConnection(
                        DatabasePath,
                        SQLiteOpenFlags.ReadWrite |
                        SQLiteOpenFlags.Create |
                        SQLiteOpenFlags.SharedCache);

                await database.CreateTableAsync<
                    AnalisisOfflineLocalEntity>();
            }
            finally
            {
                initLock.Release();
            }
        }

        private static string ObtenerUsuarioId()
        {
            string value =
                Preferences.Get(
                    SessionKeys.KeyUserId,
                    "0");

            return string.IsNullOrWhiteSpace(value)
                ? "0"
                : value.Trim();
        }

        private static string NormalizarTipo(
            string? value) =>
            string.Equals(
                value,
                "EDITAR",
                StringComparison.OrdinalIgnoreCase)
                ? "EDITAR"
                : "CREAR";

        private static DateTime ParseFecha(
            string? value) =>
            DateTime.TryParse(
                value,
                out DateTime result)
                    ? result
                    : DateTime.MinValue;

        private static string AgregarEstado(
            string identificador,
            string estado)
        {
            string texto =
                string.IsNullOrWhiteSpace(
                    identificador)
                        ? "Análisis local"
                        : identificador.Trim();

            return estado switch
            {
                AnalisisOfflineEstados.RequiereRevision =>
                    $"{texto} · REVISAR",
                AnalisisOfflineEstados.Error =>
                    $"{texto} · ERROR DE SINCRONIZACIÓN",
                AnalisisOfflineEstados.Sincronizando =>
                    $"{texto} · SINCRONIZANDO",
                AnalisisOfflineEstados.Sincronizado =>
                    $"{texto} · SINCRONIZADO",
                _ =>
                    $"{texto} · PENDIENTE"
            };
        }

        private void CompletarResumen(
            AnalisisOfflineLocalEntity entity,
            string payloadJson)
        {
            using JsonDocument document =
                JsonDocument.Parse(payloadJson);

            JsonElement root =
                document.RootElement;

            JsonElement datos =
                GetProperty(
                    root,
                    "datosAnalisis");

            JsonElement requerimiento =
                GetProperty(
                    root,
                    "requerimientoAnual");

            entity.IdentificadorAnalisis =
                GetString(
                    datos,
                    "identificadorAnalisisSuelo");

            entity.Laboratorio =
                GetString(
                    datos,
                    "laboratorioAnalasisSuelo");

            entity.FechaAnalisis =
                GetString(
                    datos,
                    "fechaAnalisisSuelo");

            entity.TerrenoId =
                GetInt(
                    datos,
                    "terrenoId");

            entity.TipoCultivoId =
                GetInt(
                    datos,
                    "tipoCultivoId");

            entity.TipoAnalisisSueloId =
                GetInt(
                    datos,
                    "tipoAnalisisSueloId");

            entity.CantidadQuintalesOro =
                GetDecimal(
                    requerimiento,
                    "cantidadQuintalesOro");

            entity.TamanoFinca =
                GetDecimal(
                    requerimiento,
                    "tamanoFinca");

            entity.Ph =
                GetDecimal(
                    requerimiento,
                    "ph");

            entity.TieneBalance =
                GetProperty(
                    root,
                    "balanceNutricional")
                    .ValueKind ==
                    JsonValueKind.Object;

            entity.TieneEnmienda =
                GetProperty(
                    root,
                    "enmiendaCalcarea")
                    .ValueKind ==
                    JsonValueKind.Object;

            entity.TieneMixta =
                GetProperty(
                    root,
                    "fertilizacionMixta")
                    .ValueKind ==
                    JsonValueKind.Object;
        }

        private static JsonElement GetProperty(
            JsonElement element,
            string name)
        {
            if (element.ValueKind !=
                JsonValueKind.Object)
            {
                return default;
            }

            foreach (JsonProperty property
                     in element.EnumerateObject())
            {
                if (string.Equals(
                        property.Name,
                        name,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return property.Value;
                }
            }

            return default;
        }

        private static string GetString(
            JsonElement element,
            string name)
        {
            JsonElement value =
                GetProperty(element, name);

            return value.ValueKind ==
                    JsonValueKind.String
                ? value.GetString() ??
                  string.Empty
                : string.Empty;
        }

        private static int GetInt(
            JsonElement element,
            string name)
        {
            JsonElement value =
                GetProperty(element, name);

            return value.ValueKind ==
                    JsonValueKind.Number &&
                   value.TryGetInt32(
                       out int result)
                ? result
                : 0;
        }

        private static decimal GetDecimal(
            JsonElement element,
            string name)
        {
            JsonElement value =
                GetProperty(element, name);

            return value.ValueKind ==
                    JsonValueKind.Number &&
                   value.TryGetDecimal(
                       out decimal result)
                ? result
                : 0;
        }
    }
}
