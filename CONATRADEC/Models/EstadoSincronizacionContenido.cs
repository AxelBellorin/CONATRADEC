namespace CONATRADEC.Models
{
    public enum TipoEstadoSincronizacionContenido
    {
        SinDatos,
        Verificando,
        Servidor,
        Local,
        SinConexionLocal,
        Error
    }

    public sealed class EstadoSincronizacionContenido
    {
        public string Modulo { get; init; } = string.Empty;
        public TipoEstadoSincronizacionContenido Tipo { get; init; }
        public string Mensaje { get; init; } = string.Empty;
        public string Detalle { get; init; } = string.Empty;
        public string Version { get; init; } = string.Empty;
        public DateTime? UltimaSincronizacionUtc { get; init; }
        public DateTime ActualizadoUtc { get; init; } = DateTime.UtcNow;
    }

    public sealed class EstadoSincronizacionContenidoEventArgs : EventArgs
    {
        public EstadoSincronizacionContenidoEventArgs(
            EstadoSincronizacionContenido estado)
        {
            Estado = estado;
        }

        public EstadoSincronizacionContenido Estado { get; }
    }
}
