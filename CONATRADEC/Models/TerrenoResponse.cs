namespace CONATRADEC.Models
{
    public sealed class TerrenoUbicacionResponse
    {
        public int? PaisId { get; set; }
        public string? NombrePais { get; set; }
        public int? DepartamentoId { get; set; }
        public string? NombreDepartamento { get; set; }
        public int? MunicipioId { get; set; }
        public string? NombreMunicipio { get; set; }
    }

    public sealed class TerrenoPropietarioResponse
    {
        public int PropietarioId { get; set; }

        public string Identificacion { get; set; } =
            string.Empty;

        public string NombreCompleto { get; set; } =
            string.Empty;

        public string? Telefono { get; set; }

        public string? Correo { get; set; }

        public string? Direccion { get; set; }

        public string TextoNombre =>
            string.IsNullOrWhiteSpace(NombreCompleto)
                ? "Sin propietario"
                : NombreCompleto.Trim();

        public string TextoIdentificacion =>
            string.IsNullOrWhiteSpace(Identificacion)
                ? "Sin identificación"
                : Identificacion.Trim();

        public string TextoTelefono =>
            string.IsNullOrWhiteSpace(Telefono)
                ? "Sin teléfono"
                : Telefono.Trim();

        public string TextoCorreo =>
            string.IsNullOrWhiteSpace(Correo)
                ? "Sin correo"
                : Correo.Trim();
    }

    public class TerrenoResponse
    {
        public int? TerrenoId { get; set; }

        public string? CodigoTerreno { get; set; }

        public int? PropietarioId { get; set; }

        public TerrenoPropietarioResponse? Propietario
        {
            get;
            set;
        }

        public string? DireccionTerreno { get; set; }

        public decimal? ExtensionManzanaTerreno { get; set; }

        public DateOnly? FechaIngresoTerreno { get; set; }

        public int? MunicipioId { get; set; }

        public decimal? CantidadQuintalesOro { get; set; }

        public int? CantidadPlantasTerreno { get; set; }

        public double? Latitud { get; set; }

        public double? Longitud { get; set; }

        public bool? Activo { get; set; }

        public TerrenoUbicacionResponse? Ubicacion { get; set; }

        public string NombreCliente =>
            Propietario?.TextoNombre ??
            "Sin propietario";

        public string NombreTerreno
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(
                        DireccionTerreno))
                {
                    return DireccionTerreno;
                }

                if (!string.IsNullOrWhiteSpace(
                        CodigoTerreno))
                {
                    return CodigoTerreno;
                }

                return "Terreno sin nombre";
            }
        }

        public decimal? TamanoFinca =>
            ExtensionManzanaTerreno;

        public string TextoUbicacion =>
            Ubicacion is null
                ? string.Empty
                : $"{Ubicacion.NombreMunicipio}, " +
                  $"{Ubicacion.NombreDepartamento}, " +
                  $"{Ubicacion.NombrePais}";
    }
}
