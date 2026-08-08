namespace CONATRADEC.Models
{
    /// <summary>
    /// Estado de la reserva temporal de una inspección para una sesión de
    /// analizador o aprobador.
    /// </summary>
    public sealed class InspeccionRevisionBloqueo
    {
        public bool Adquirido { get; set; }
        public int InspeccionId { get; set; }
        public string Modo { get; set; } = string.Empty;
        public int UsuarioId { get; set; }
        public string UsuarioNombre { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public DateTime? FechaAdquisicionUtc { get; set; }
        public DateTime? UltimoHeartbeatUtc { get; set; }
        public DateTime? ExpiraUtc { get; set; }
        public int VigenciaSegundos { get; set; }
    }
}
