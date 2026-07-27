using SQLite;

namespace CONATRADEC.Models
{
    [Table("AnalisisHistorialLocal")]
    public sealed class AnalisisHistorialLocalEntity
    {
        [PrimaryKey]
        public string Clave { get; set; } = string.Empty;

        [Indexed]
        public string UsuarioId { get; set; } = string.Empty;

        [Indexed]
        public int AnalisisSueloCalculoId { get; set; }

        [Indexed]
        public int AnalisisSueloId { get; set; }

        [Indexed]
        public string PaqueteId { get; set; } = string.Empty;

        [Indexed]
        public bool Activo { get; set; }

        public string ResumenJson { get; set; } = string.Empty;
        public string DetalleJson { get; set; } = string.Empty;
        public string ReporteJson { get; set; } = string.Empty;
        public DateTime GuardadoUtc { get; set; }
        public DateTime UltimoUsoUtc { get; set; }
    }

    [Table("AnalisisHistorialEstado")]
    public sealed class AnalisisHistorialEstadoEntity
    {
        [PrimaryKey]
        public string UsuarioId { get; set; } = string.Empty;

        public string PaqueteActivoId { get; set; } = string.Empty;
        public int TotalAnalisis { get; set; }
        public int TotalDetalles { get; set; }
        public int TotalReportes { get; set; }
        public string UsuariosFiltroJson { get; set; } = string.Empty;
        public DateTime? UltimaDescargaCompletaUtc { get; set; }
        public long TamanoBytes { get; set; }
    }

    public sealed class AnalisisHistorialDescargaProgreso
    {
        public int Procesados { get; init; }
        public int Total { get; init; }
        public int Porcentaje => Total <= 0
            ? 0
            : Math.Clamp(
                (int)Math.Round(
                    Procesados * 100d / Total),
                0,
                100);
        public string Mensaje { get; init; } = string.Empty;
    }

    public sealed class AnalisisHistorialDescargaResultado
    {
        public bool Success { get; init; }
        public string Message { get; init; } = string.Empty;
        public int TotalAnalisis { get; init; }
        public int TotalDetalles { get; init; }
        public int TotalReportes { get; init; }
        public long TamanoBytes { get; init; }

        public static AnalisisHistorialDescargaResultado Ok(
            int totalAnalisis,
            int totalDetalles,
            int totalReportes,
            long tamanoBytes,
            string message) =>
            new()
            {
                Success = true,
                TotalAnalisis = totalAnalisis,
                TotalDetalles = totalDetalles,
                TotalReportes = totalReportes,
                TamanoBytes = tamanoBytes,
                Message = message
            };

        public static AnalisisHistorialDescargaResultado Fail(
            string message) =>
            new()
            {
                Success = false,
                Message = message
            };
    }
}
