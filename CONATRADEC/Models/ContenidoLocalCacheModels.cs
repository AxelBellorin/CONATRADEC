using SQLite;

namespace CONATRADEC.Models
{
    [Table("ContenidoRespuestaCache")]
    public sealed class ContenidoRespuestaCacheEntity
    {
        [PrimaryKey]
        public string CacheKey { get; set; } = string.Empty;
        [Indexed]
        public string UsuarioId { get; set; } = string.Empty;
        [Indexed]
        public string Modulo { get; set; } = string.Empty;
        public string Ruta { get; set; } = string.Empty;
        [Indexed]
        public string Version { get; set; } = string.Empty;
        public int StatusCode { get; set; }
        public string ContentType { get; set; } = "application/json";
        public string Json { get; set; } = string.Empty;
        public DateTime GuardadoUtc { get; set; }
        public DateTime UltimoUsoUtc { get; set; }
    }

    [Table("ContenidoModuloEstado")]
    public sealed class ContenidoModuloEstadoEntity
    {
        [PrimaryKey]
        public string Clave { get; set; } = string.Empty;
        [Indexed]
        public string UsuarioId { get; set; } = string.Empty;
        [Indexed]
        public string Modulo { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string VersionServidor { get; set; } = string.Empty;
        public DateTime FechaServidorUtc { get; set; }
        public DateTime VerificadoUtc { get; set; }
        public DateTime? UltimaSincronizacionExitosaUtc { get; set; }
        public DateTime? UltimoUsoLocalUtc { get; set; }
        public string OrigenUltimaCarga { get; set; } = string.Empty;
        public string UltimoError { get; set; } = string.Empty;
    }

    [Table("ContenidoImagenCache")]
    public sealed class ContenidoImagenCacheEntity
    {
        [PrimaryKey]
        public string Clave { get; set; } = string.Empty;
        [Indexed]
        public string UsuarioId { get; set; } = string.Empty;
        [Indexed]
        public string Modulo { get; set; } = string.Empty;
        [Indexed]
        public string Version { get; set; } = string.Empty;
        public string UrlRemota { get; set; } = string.Empty;
        [Indexed]
        public string RutaLocal { get; set; } = string.Empty;
        public bool EsOriginal { get; set; }
        public long TamanoBytes { get; set; }
        public DateTime GuardadoUtc { get; set; }
        public DateTime UltimoUsoUtc { get; set; }
    }

    [Table("OperacionPendiente")]
    public sealed class OperacionPendienteEntity
    {
        [PrimaryKey, AutoIncrement]
        public long OperacionId { get; set; }
        [Indexed]
        public string UsuarioId { get; set; } = string.Empty;
        [Indexed]
        public string Modulo { get; set; } = string.Empty;
        public string TipoOperacion { get; set; } = string.Empty;
        public string EntidadLocalId { get; set; } = string.Empty;
        public int? EntidadServidorId { get; set; }
        public string JsonPayload { get; set; } = string.Empty;
        [Indexed]
        public string Estado { get; set; } = "PENDIENTE";
        public int Intentos { get; set; }
        public string UltimoError { get; set; } = string.Empty;
        public DateTime FechaCreacionUtc { get; set; }
        public DateTime? FechaUltimoIntentoUtc { get; set; }
        public DateTime? FechaSincronizacionUtc { get; set; }
    }

    [Table("CatalogoOfflineSeccion")]
    public sealed class CatalogoOfflineSeccionEntity
    {
        [PrimaryKey]
        public string Clave { get; set; } = string.Empty;
        [Indexed]
        public string UsuarioId { get; set; } = string.Empty;
        [Indexed]
        public string PaqueteId { get; set; } = string.Empty;
        [Indexed]
        public string Seccion { get; set; } = string.Empty;
        public string Json { get; set; } = string.Empty;
        public int TotalRegistros { get; set; }
        public DateTime GuardadoUtc { get; set; }
    }

    [Table("CatalogoOfflineEstado")]
    public sealed class CatalogoOfflineEstadoEntity
    {
        [PrimaryKey]
        public string Clave { get; set; } = string.Empty;
        [Indexed]
        public string UsuarioId { get; set; } = string.Empty;
        public string PaqueteActivoId { get; set; } = string.Empty;
        public string VersionServidor { get; set; } = string.Empty;
        [Indexed]
        public string Estado { get; set; } = "SIN_DATOS";
        public int ProgresoPorcentaje { get; set; }
        public int PasoActual { get; set; }
        public int TotalPasos { get; set; }
        public string Mensaje { get; set; } = string.Empty;
        public string UltimoError { get; set; } = string.Empty;
        public int TotalRegistros { get; set; }
        public long TamanoBytes { get; set; }
        public DateTime? UltimaDescargaCompletaUtc { get; set; }
        public DateTime? UltimaVerificacionUtc { get; set; }
    }

    public sealed class ResumenCacheLocal
    {
        public int TotalRespuestas { get; init; }
        public int TotalImagenes { get; init; }
        public long TamanoImagenesBytes { get; init; }
        public int OperacionesPendientes { get; init; }
    }
}
