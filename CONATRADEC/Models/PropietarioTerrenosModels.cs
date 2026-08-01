namespace CONATRADEC.Models
{
    /// <summary>
    /// Respuesta del detalle administrativo de un propietario.
    /// </summary>
    public sealed class PropietarioDetalleResponse
    {
        public PropietarioResponse? Propietario { get; set; }

        public List<PropietarioTerrenoResumenResponse>
            Terrenos { get; set; } = [];
    }

    /// <summary>
    /// Terreno vinculado actualmente a un propietario.
    /// </summary>
    public sealed class PropietarioTerrenoResumenResponse
    {
        public int TerrenoId { get; set; }

        public string CodigoTerreno { get; set; } =
            string.Empty;

        public string DireccionTerreno { get; set; } =
            string.Empty;

        public decimal ExtensionManzanas { get; set; }

        public decimal QuintalesOro { get; set; }

        public bool Activo { get; set; } = true;

        public DateTime? FechaAsignacionUtc { get; set; }

        public string TextoCodigo =>
            string.IsNullOrWhiteSpace(CodigoTerreno)
                ? $"Terreno #{TerrenoId}"
                : CodigoTerreno.Trim();

        public string TextoDireccion =>
            string.IsNullOrWhiteSpace(DireccionTerreno)
                ? "Sin dirección registrada"
                : DireccionTerreno.Trim();

        public string TextoExtension =>
            $"{ExtensionManzanas:N2} manzanas";

        public string TextoProduccion =>
            $"{QuintalesOro:N2} qq oro";

        public string TextoEstado =>
            Activo ? "Activo" : "Inactivo";
    }

    public sealed class VincularTerrenoPropietarioRequest
    {
        public int TerrenoId { get; set; }
    }
}
