using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CONATRADEC.Models
{
    public class BalanceNutricionalRequest
    {
        [JsonPropertyName("nombreFormula")]
        public string? NombreFormula { get; set; }

        [JsonPropertyName("terrenoId")]
        public int? TerrenoId { get; set; }

        [JsonPropertyName("totalPlantas")]
        public int? TotalPlantas { get; set; }

        [JsonPropertyName("totalAplicaciones")]
        public int? TotalAplicaciones { get; set; }

        [JsonPropertyName("items")]
        public List<BalanceNutricionalItemRequest> Items { get; set; } = new();
    }

    public class BalanceNutricionalItemRequest
    {
        [JsonPropertyName("fuenteNutrientesId")]
        public int? FuenteNutrientesId { get; set; }

        [JsonPropertyName("elementoQuimicosId")]
        public int? ElementoQuimicosId { get; set; }

        [JsonPropertyName("requerimientoLibras")]
        public decimal? RequerimientoLibras { get; set; }
    }
}