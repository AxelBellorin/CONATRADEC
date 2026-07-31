namespace CONATRADEC.Models
{
    public sealed class PropietarioResponse
    {
        public int PropietarioId { get; set; }

        public string Identificacion { get; set; } =
            string.Empty;

        public string NombreCompleto { get; set; } =
            string.Empty;

        public string? Telefono { get; set; }

        public string? Correo { get; set; }

        public string? Direccion { get; set; }

        public bool Activo { get; set; } = true;

        public DateTime? FechaRegistroUtc { get; set; }

        public int TotalTerrenos { get; set; }

        public int? UsuarioPortalId { get; set; }

        public string? UsuarioPortal { get; set; }

        public string TextoPrincipal =>
            string.IsNullOrWhiteSpace(NombreCompleto)
                ? "Propietario sin nombre"
                : NombreCompleto.Trim();

        public string TextoIdentificacion =>
            string.IsNullOrWhiteSpace(Identificacion)
                ? "Sin identificación"
                : Identificacion.Trim();

        public string TextoContacto
        {
            get
            {
                List<string> valores = [];

                if (!string.IsNullOrWhiteSpace(Telefono))
                    valores.Add(Telefono.Trim());

                if (!string.IsNullOrWhiteSpace(Correo))
                    valores.Add(Correo.Trim());

                return valores.Count > 0
                    ? string.Join(" · ", valores)
                    : "Sin contacto registrado";
            }
        }

        public string TextoTerrenos =>
            TotalTerrenos == 1
                ? "1 terreno"
                : $"{TotalTerrenos} terrenos";

        public string TextoEstado =>
            Activo ? "Activo" : "Inactivo";
    }

    public sealed class PropietarioGuardarRequest
    {
        public string Identificacion { get; set; } =
            string.Empty;

        public string NombreCompleto { get; set; } =
            string.Empty;

        public string? Telefono { get; set; }

        public string? Correo { get; set; }

        public string? Direccion { get; set; }

        public bool Activo { get; set; } = true;
    }
}
