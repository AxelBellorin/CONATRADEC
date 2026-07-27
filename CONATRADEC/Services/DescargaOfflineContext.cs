namespace CONATRADEC.Services
{
    /// <summary>
    /// Contexto de la operación manual Descargar todo.
    ///
    /// Todas las respuestas de una misma descarga reciben exactamente la misma
    /// versión transaccional. Así ninguna página, categoría o detalle recién
    /// descargado puede eliminar a otro por tener un JSON diferente.
    /// </summary>
    public static class DescargaOfflineContext
    {
        private static readonly AsyncLocal<EstadoContexto?> actual =
            new();

        public static bool Activa =>
            actual.Value?.Profundidad > 0;

        public static string VersionTransaccional =>
            actual.Value?.VersionTransaccional ??
            string.Empty;

        public static IDisposable Iniciar(
            string? versionTransaccional = null)
        {
            EstadoContexto? estado = actual.Value;

            if (estado == null)
            {
                estado = new EstadoContexto
                {
                    VersionTransaccional =
                        string.IsNullOrWhiteSpace(
                            versionTransaccional)
                            ? CrearVersion()
                            : versionTransaccional.Trim(),
                    Profundidad = 0
                };

                actual.Value = estado;
            }

            estado.Profundidad++;
            return new Scope(estado);
        }

        private static string CrearVersion() =>
            "descarga-" +
            DateTime.UtcNow.ToString("yyyyMMddHHmmssfff") +
            "-" +
            Guid.NewGuid().ToString("N");

        private sealed class EstadoContexto
        {
            public string VersionTransaccional { get; init; } =
                string.Empty;

            public int Profundidad { get; set; }
        }

        private sealed class Scope : IDisposable
        {
            private readonly EstadoContexto estado;
            private bool disposed;

            public Scope(EstadoContexto estado)
            {
                this.estado = estado;
            }

            public void Dispose()
            {
                if (disposed)
                    return;

                disposed = true;
                estado.Profundidad =
                    Math.Max(0, estado.Profundidad - 1);

                if (estado.Profundidad == 0 &&
                    ReferenceEquals(
                        actual.Value,
                        estado))
                {
                    actual.Value = null;
                }
            }
        }
    }
}
