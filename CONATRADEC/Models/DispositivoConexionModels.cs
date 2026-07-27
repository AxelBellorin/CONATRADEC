namespace CONATRADEC.Models
{
    /// <summary>
    /// Datos enviados periódicamente por una instalación de la app.
    /// La ubicación es opcional, aproximada y solo se obtiene con permiso del
    /// usuario mientras la aplicación está en uso.
    /// </summary>
    public sealed class ReportarDispositivoConexionRequest
    {
        public string InstalacionId { get; set; } = string.Empty;
        public string SesionId { get; set; } = string.Empty;
        public int UsuarioId { get; set; }
        public string Plataforma { get; set; } = string.Empty;
        public string TipoDispositivo { get; set; } = string.Empty;
        public string Fabricante { get; set; } = string.Empty;
        public string Modelo { get; set; } = string.Empty;
        public string NombreDispositivo { get; set; } = string.Empty;
        public string SistemaOperativo { get; set; } = string.Empty;
        public string VersionSistema { get; set; } = string.Empty;
        public string VersionApp { get; set; } = string.Empty;
        public string BuildApp { get; set; } = string.Empty;
        public string Idioma { get; set; } = string.Empty;
        public string TipoConexion { get; set; } = string.Empty;
        public string PaginaActual { get; set; } = string.Empty;
        public double? Latitud { get; set; }
        public double? Longitud { get; set; }
        public double? PrecisionMetros { get; set; }
        public DateTime? FechaUbicacionUtc { get; set; }
        public string OrigenUbicacion { get; set; } = string.Empty;
        public string EstadoPermisoUbicacion { get; set; } = string.Empty;
        public bool? UbicacionSimulada { get; set; }
    }

    public sealed class DesconectarDispositivoConexionRequest
    {
        public string InstalacionId { get; set; } = string.Empty;
        public string SesionId { get; set; } = string.Empty;
        public string Motivo { get; set; } = string.Empty;
    }
}
