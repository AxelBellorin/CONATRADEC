namespace CONATRADEC.Models
{
    /// <summary>
    /// Técnico disponible para filtrar bandejas de inspecciones. El filtro usa
    /// el identificador interno y presenta el nombre completo junto al usuario.
    /// </summary>
    public sealed class TecnicoInspeccionFiltroItem
    {
        public int UsuarioTecnicoId { get; set; }
        public string NombreCompleto { get; set; } = string.Empty;
        public string NombreUsuario { get; set; } = string.Empty;

        public bool EsTodos => UsuarioTecnicoId <= 0;

        public string TextoMostrar
        {
            get
            {
                if (EsTodos)
                    return "Todos los técnicos";

                string nombre = NombreCompleto?.Trim() ?? string.Empty;
                string usuario = NombreUsuario?.Trim() ?? string.Empty;

                if (string.IsNullOrWhiteSpace(nombre))
                    nombre = string.IsNullOrWhiteSpace(usuario)
                        ? $"Técnico #{UsuarioTecnicoId}"
                        : usuario;

                return string.IsNullOrWhiteSpace(usuario) ||
                       string.Equals(
                           nombre,
                           usuario,
                           StringComparison.OrdinalIgnoreCase)
                    ? nombre
                    : $"{nombre} — {usuario}";
            }
        }

        /// <summary>
        /// Picker de WinUI presenta de forma más consistente el elemento
        /// seleccionado cuando puede obtener directamente su texto visible.
        /// </summary>
        public override string ToString() => TextoMostrar;

        public static TecnicoInspeccionFiltroItem Todos() =>
            new()
            {
                UsuarioTecnicoId = 0,
                NombreCompleto = "Todos los técnicos"
            };
    }

    public sealed class TecnicoInspeccionAsignacionItem
    {
        public int InspeccionId { get; set; }
        public int UsuarioTecnicoId { get; set; }
        public string NombreCompleto { get; set; } = string.Empty;
        public string NombreUsuario { get; set; } = string.Empty;
    }

    public sealed class TecnicoInspeccionFiltroRespuesta
    {
        public List<TecnicoInspeccionFiltroItem> Tecnicos { get; set; } = [];
        public List<TecnicoInspeccionAsignacionItem> Asignaciones { get; set; } = [];
    }

    /// <summary>
    /// Conserva la relación inspección-técnico obtenida en una sola consulta.
    /// Los convertidores de las tarjetas pueden mostrar el responsable sin
    /// ejecutar consultas adicionales por cada registro.
    /// </summary>
    public static class TecnicoInspeccionCacheService
    {
        private static readonly object sync = new();
        private static readonly Dictionary<int, TecnicoInspeccionAsignacionItem>
            asignaciones = [];

        public static void Establecer(
            IEnumerable<TecnicoInspeccionAsignacionItem>? items)
        {
            if (items == null)
                return;

            lock (sync)
            {
                foreach (TecnicoInspeccionAsignacionItem item in items)
                {
                    if (item.InspeccionId <= 0)
                        continue;

                    asignaciones[item.InspeccionId] = item;
                }
            }
        }

        public static void Establecer(
            int inspeccionId,
            TecnicoInspeccionFiltroItem? tecnico)
        {
            if (inspeccionId <= 0 || tecnico == null)
                return;

            Establecer(
            [
                new TecnicoInspeccionAsignacionItem
                {
                    InspeccionId = inspeccionId,
                    UsuarioTecnicoId = tecnico.UsuarioTecnicoId,
                    NombreCompleto = tecnico.NombreCompleto,
                    NombreUsuario = tecnico.NombreUsuario
                }
            ]);
        }

        public static string ObtenerTexto(int inspeccionId)
        {
            lock (sync)
            {
                return asignaciones.TryGetValue(
                        inspeccionId,
                        out TecnicoInspeccionAsignacionItem? item)
                    ? CrearTexto(item)
                    : "Usuario no disponible";
            }
        }

        private static string CrearTexto(
            TecnicoInspeccionAsignacionItem item)
        {
            string nombre = item.NombreCompleto?.Trim() ?? string.Empty;
            string usuario = item.NombreUsuario?.Trim() ?? string.Empty;

            if (!string.IsNullOrWhiteSpace(nombre))
                return nombre;

            if (!string.IsNullOrWhiteSpace(usuario))
                return usuario;

            return item.UsuarioTecnicoId > 0
                ? $"Usuario #{item.UsuarioTecnicoId}"
                : "Usuario no disponible";
        }
    }
}
