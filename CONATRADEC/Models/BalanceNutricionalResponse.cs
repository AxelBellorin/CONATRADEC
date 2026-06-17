using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CONATRADEC.Models
{
    public class BalanceNutricionalResponse
    {
        [JsonIgnore]
        public bool Success { get; set; } = true;

        [JsonIgnore]
        public string? Message { get; set; }

        [JsonPropertyName("balanceNutricionalId")]
        public int? BalanceNutricionalId { get; set; }

        [JsonPropertyName("nombreFormula")]
        public string? NombreFormula { get; set; }

        [JsonPropertyName("totalMezclaLb")]
        public decimal? TotalMezclaLb { get; set; }

        [JsonPropertyName("totalMezclaOz")]
        public decimal? TotalMezclaOz { get; set; }

        [JsonPropertyName("librasPorDosAplicaciones")]
        public decimal? LibrasPorDosAplicaciones { get; set; }

        [JsonPropertyName("librasPorTresAplicaciones")]
        public decimal? LibrasPorTresAplicaciones { get; set; }

        [JsonPropertyName("totalPlantas")]
        public int? TotalPlantas { get; set; }

        [JsonPropertyName("dosisPlantaAnualOz")]
        public decimal? DosisPlantaAnualOz { get; set; }

        [JsonPropertyName("totalMezclaQq")]
        public decimal? TotalMezclaQq { get; set; }

        [JsonPropertyName("precioTotalFormula")]
        public decimal? PrecioTotalFormula { get; set; }

        [JsonPropertyName("precioPorAplicacion")]
        public decimal? PrecioPorAplicacion { get; set; }

        [JsonPropertyName("dosAplicaciones")]
        public BalanceNutricionalAplicacionResponse? DosAplicaciones { get; set; }

        [JsonPropertyName("tresAplicaciones")]
        public BalanceNutricionalAplicacionResponse? TresAplicaciones { get; set; }

        [JsonPropertyName("detalle")]
        public List<BalanceNutricionalDetalleResponse> Detalle { get; set; } = new();
    }

    public class BalanceNutricionalAplicacionResponse
    {
        [JsonPropertyName("dosisPlantaOz")]
        public decimal? DosisPlantaOz { get; set; }
    }

    public class BalanceNutricionalDetalleResponse
    {
        [JsonPropertyName("fuente")]
        public string? Fuente { get; set; }

        [JsonPropertyName("elemento")]
        public string? Elemento { get; set; }

        [JsonPropertyName("requerimientoLibras")]
        public decimal? RequerimientoLibras { get; set; }

        [JsonPropertyName("librasAnuales")]
        public decimal? LibrasAnuales { get; set; }

        [JsonPropertyName("onzasAnuales")]
        public decimal? OnzasAnuales { get; set; }

        [JsonPropertyName("dosAplicaciones")]
        public decimal? DosAplicaciones { get; set; }

        [JsonPropertyName("tresAplicaciones")]
        public decimal? TresAplicaciones { get; set; }

        [JsonPropertyName("quintalesAnuales")]
        public decimal? QuintalesAnuales { get; set; }

        [JsonPropertyName("precioPorQuintal")]
        public decimal? PrecioPorQuintal { get; set; }

        [JsonPropertyName("subtotalFuente")]
        public decimal? SubtotalFuente { get; set; }
    }
}