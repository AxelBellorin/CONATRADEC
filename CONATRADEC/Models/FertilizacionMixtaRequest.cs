using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CONATRADEC.Models
{
    // ===============================================================
    // Request usado por:
    // POST api/fertilizacion-mixta/calcular
    // ===============================================================
    public class FertilizacionMixtaCalcularRequest
    {
        [JsonPropertyName("observacion")]
        public string? Observacion { get; set; }

        [JsonPropertyName("elementos")]
        public List<ElementoFertilizacionMixtaRequest> Elementos { get; set; } = new();

        [JsonPropertyName("fuentes")]
        public List<FuenteFertilizacionMixtaRequest> Fuentes { get; set; } = new();
    }

    public class ElementoFertilizacionMixtaRequest
    {
        [JsonPropertyName("elementoQuimicosId")]
        public int? ElementoQuimicosId { get; set; }

        [JsonPropertyName("exportable")]
        public decimal? Exportable { get; set; }
    }

    public class FuenteFertilizacionMixtaRequest
    {
        [JsonPropertyName("fuenteNutrientesId")]
        public int? FuenteNutrientesId { get; set; }

        [JsonPropertyName("cantidadQq")]
        public decimal? CantidadQq { get; set; }
    }
}