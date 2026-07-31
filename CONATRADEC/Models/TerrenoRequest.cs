using System.Text.Json.Serialization;

namespace CONATRADEC.Models
{
    public class TerrenoRequest
    {
        public int? TerrenoId { get; set; }

        public string? CodigoTerreno { get; set; }

        public int? PropietarioId { get; set; }

        [JsonIgnore]
        public TerrenoPropietarioResponse? Propietario { get; set; }

        public string? DireccionTerreno { get; set; }

        public decimal? ExtensionManzanaTerreno { get; set; }

        public DateOnly? FechaIngresoTerreno { get; set; }

        public int? MunicipioId { get; set; }

        public decimal? CantidadQuintalesOro { get; set; }

        public int? CantidadPlantasTerreno { get; set; }

        public double? Latitud { get; set; }

        public double? Longitud { get; set; }

        public string TextoCantidadPlantas =>
            CantidadPlantasTerreno is null or <= 0
                ? "Plantas no registradas"
                : $"{CantidadPlantasTerreno:N0} plantas";

        public TerrenoRequest()
        {
        }

        public TerrenoRequest(TerrenoResponse terreno)
        {
            ArgumentNullException.ThrowIfNull(terreno);

            TerrenoId = terreno.TerrenoId;
            CodigoTerreno = terreno.CodigoTerreno;
            PropietarioId =
                terreno.Propietario?.PropietarioId ??
                terreno.PropietarioId;
            Propietario = terreno.Propietario;
            DireccionTerreno = terreno.DireccionTerreno;
            ExtensionManzanaTerreno =
                terreno.ExtensionManzanaTerreno;
            FechaIngresoTerreno =
                terreno.FechaIngresoTerreno;
            MunicipioId = terreno.MunicipioId;
            CantidadQuintalesOro =
                terreno.CantidadQuintalesOro;
            CantidadPlantasTerreno =
                terreno.CantidadPlantasTerreno;
            Latitud = terreno.Latitud;
            Longitud = terreno.Longitud;
        }
    }
}
