namespace CONATRADEC.Models
{
    public static class CatalogoOfflineEstados
    {
        public const string SinDatos = "SIN_DATOS";
        public const string Descargando = "DESCARGANDO";
        public const string Completo = "COMPLETO";
        public const string ActualizacionDisponible = "ACTUALIZACION_DISPONIBLE";
        public const string Error = "ERROR";
    }

    public sealed class EstadoPaqueteOffline
    {
        public string Estado { get; init; } = CatalogoOfflineEstados.SinDatos;
        public string Mensaje { get; init; } =
            "No hay datos descargados para trabajar sin conexión.";
        public int ProgresoPorcentaje { get; init; }
        public int PasoActual { get; init; }
        public int TotalPasos { get; init; }
        public int TotalRegistros { get; init; }
        public long TamanoBytes { get; init; }
        public DateTime? UltimaDescargaCompletaUtc { get; init; }
        public bool TienePaqueteCompleto { get; init; }
        public bool EstaDescargando =>
            Estado == CatalogoOfflineEstados.Descargando;
        public bool HayActualizacion =>
            Estado == CatalogoOfflineEstados.ActualizacionDisponible;
    }

    public sealed class EstadoPaqueteOfflineEventArgs : EventArgs
    {
        public EstadoPaqueteOffline Estado { get; }

        public EstadoPaqueteOfflineEventArgs(EstadoPaqueteOffline estado)
        {
            Estado = estado;
        }
    }

    public sealed class ResultadoDescargaOffline
    {
        public bool Success { get; init; }
        public string Message { get; init; } = string.Empty;
        public int TotalRegistros { get; init; }
        public long TamanoBytes { get; init; }

        public static ResultadoDescargaOffline Ok(
            string message,
            int totalRegistros,
            long tamanoBytes) =>
            new()
            {
                Success = true,
                Message = message,
                TotalRegistros = totalRegistros,
                TamanoBytes = tamanoBytes
            };

        public static ResultadoDescargaOffline Fail(string message) =>
            new()
            {
                Success = false,
                Message = message
            };
    }
}
