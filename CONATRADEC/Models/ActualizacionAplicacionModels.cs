using System.Text.Json.Serialization;

namespace CONATRADEC.Models
{
    public sealed class RespuestaComprobacionActualizacion
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("actualizacionDisponible")]
        public bool ActualizacionDisponible { get; set; }

        [JsonPropertyName("data")]
        public ActualizacionDisponible? Data { get; set; }
    }

    public sealed class ActualizacionDisponible
    {
        [JsonPropertyName("actualizacionAplicacionId")]
        public int ActualizacionAplicacionId { get; set; }

        [JsonPropertyName("plataforma")]
        public string Plataforma { get; set; } = string.Empty;

        [JsonPropertyName("canal")]
        public string Canal { get; set; } = string.Empty;

        [JsonPropertyName("versionNombre")]
        public string VersionNombre { get; set; } = string.Empty;

        [JsonPropertyName("versionCodigo")]
        public long VersionCodigo { get; set; }

        [JsonPropertyName("notasVersion")]
        public string NotasVersion { get; set; } = string.Empty;

        [JsonPropertyName("obligatoria")]
        public bool Obligatoria { get; set; }

        [JsonPropertyName("versionMinimaCodigo")]
        public long? VersionMinimaCodigo { get; set; }

        [JsonPropertyName("nombreArchivo")]
        public string NombreArchivo { get; set; } = string.Empty;

        [JsonPropertyName("tipoContenido")]
        public string TipoContenido { get; set; } =
            "application/octet-stream";

        [JsonPropertyName("tamanoBytes")]
        public long TamanoBytes { get; set; }

        [JsonPropertyName("hashSha256")]
        public string HashSha256 { get; set; } = string.Empty;

        [JsonPropertyName("urlDescarga")]
        public string UrlDescarga { get; set; } = string.Empty;

        [JsonPropertyName("fechaPublicacionUtc")]
        public DateTime? FechaPublicacionUtc { get; set; }

        [JsonIgnore]
        public string TamanoVisible =>
            FormatearTamano(TamanoBytes);

        private static string FormatearTamano(long bytes)
        {
            string[] unidades = { "B", "KB", "MB", "GB" };
            double valor = Math.Max(0, bytes);
            int indice = 0;

            while (valor >= 1024 &&
                   indice < unidades.Length - 1)
            {
                valor /= 1024;
                indice++;
            }

            return indice == 0
                ? $"{valor:0} {unidades[indice]}"
                : $"{valor:0.##} {unidades[indice]}";
        }
    }

    /// <summary>
    /// Estado visible de una descarga administrada por Android, Windows
    /// o por el cliente HTTP de respaldo.
    /// </summary>
    public sealed class ProgresoDescargaActualizacion
    {
        public long BytesDescargados { get; init; }

        public long TotalBytes { get; init; }

        public double BytesPorSegundo { get; init; }

        public TimeSpan? TiempoRestante { get; init; }

        public string Estado { get; init; } = "Preparando";

        public bool EnSegundoPlano { get; init; }

        public double Porcentaje =>
            TotalBytes <= 0
                ? 0
                : Math.Clamp(
                    BytesDescargados * 100d / TotalBytes,
                    0,
                    100);
    }

    public sealed record ResultadoInstalacionActualizacion(
        bool Iniciado,
        bool RequierePermiso,
        string Mensaje);
}
