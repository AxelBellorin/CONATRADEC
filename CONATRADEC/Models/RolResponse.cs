namespace CONATRADEC.Models
{
    public sealed class RolResponse
    {
        public int? RolId { get; set; }
        public string? NombreRol { get; set; }
        public string? DescripcionRol { get; set; }
        public int CantidadUsuarios { get; set; }
        public int CantidadInterfaces { get; set; }

        public string NombreMostrar =>
            NombreRol?.Trim() ?? string.Empty;

        public string DescripcionMostrar =>
            string.IsNullOrWhiteSpace(DescripcionRol)
                ? "Sin descripción registrada."
                : DescripcionRol.Trim();

        public string UsuariosTexto =>
            CantidadUsuarios == 1
                ? "1 usuario activo"
                : $"{CantidadUsuarios} usuarios activos";

        public string InterfacesTexto =>
            CantidadInterfaces == 1
                ? "1 interfaz con permisos"
                : $"{CantidadInterfaces} interfaces con permisos";

        public bool EsAdministrador =>
            string.Equals(
                NombreMostrar,
                "ADMINISTRADOR",
                StringComparison.OrdinalIgnoreCase);

        public bool EsEditable =>
            !EsAdministrador;
    }
}
