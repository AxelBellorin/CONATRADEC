namespace CONATRADEC.Models
{
    public sealed class SincronizacionOfflineManifiestoModulo
    {
        public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public bool Habilitado { get; set; }
        public string Version { get; set; } = string.Empty;
        public long TotalRegistros { get; set; }
    }

    public sealed class SincronizacionOfflineManifiesto
    {
        public int EsquemaVersion { get; set; }
        public int UsuarioId { get; set; }
        public DateTime GeneradoUtc { get; set; }
        public string VersionGeneral { get; set; } = string.Empty;
        public List<SincronizacionOfflineManifiestoModulo> Modulos
        {
            get;
            set;
        } = new();
    }

    public sealed class SincronizacionOfflineModuloComparacion
    {
        public string Codigo { get; init; } = string.Empty;
        public string Nombre { get; init; } = string.Empty;
        public bool Habilitado { get; init; }
        public bool RequiereActualizar { get; init; }
        public string VersionServidor { get; init; } = string.Empty;
        public string VersionLocal { get; init; } = string.Empty;
        public long TotalRegistrosServidor { get; init; }
    }

    public sealed class ResultadoComprobacionOffline
    {
        public bool Success { get; init; }
        public bool RequiereDescargaInicial { get; init; }
        public bool HayActualizaciones { get; init; }
        public string Message { get; init; } = string.Empty;
        public DateTime? FechaComprobacionUtc { get; init; }
        public SincronizacionOfflineManifiesto? Manifiesto { get; init; }
        public List<SincronizacionOfflineModuloComparacion> Modulos
        {
            get;
            init;
        } = new();

        public IEnumerable<SincronizacionOfflineModuloComparacion>
            ModulosPendientes =>
                Modulos.Where(x =>
                    x.Habilitado &&
                    x.RequiereActualizar);

        public static ResultadoComprobacionOffline Fail(
            string message) =>
            new()
            {
                Success = false,
                Message = message,
                FechaComprobacionUtc = DateTime.UtcNow
            };
    }

    internal sealed class SincronizacionOfflineManifiestoLocal
    {
        public int EsquemaVersion { get; set; }
        public int UsuarioId { get; set; }
        public DateTime FechaDescargaUtc { get; set; }
        public string VersionGeneral { get; set; } = string.Empty;
        public Dictionary<string, string> Versiones { get; set; } =
            new(StringComparer.OrdinalIgnoreCase);
    }
}
