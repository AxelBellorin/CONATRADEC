using System.Text.Json.Serialization;

namespace CONATRADEC.Models
{
    public class FuenteNutrienteAporteTablaResponse
    {
        [JsonPropertyName("fuenteNutrientesId")]
        public int? FuenteNutrientesId { get; set; }

        [JsonPropertyName("fuente")]
        public string? Fuente { get; set; }

        [JsonPropertyName("n")]
        public decimal N { get; set; }

        [JsonPropertyName("p")]
        public decimal P { get; set; }

        [JsonPropertyName("k")]
        public decimal K { get; set; }

        [JsonPropertyName("ca")]
        public decimal Ca { get; set; }

        [JsonPropertyName("mg")]
        public decimal Mg { get; set; }

        [JsonPropertyName("zn")]
        public decimal Zn { get; set; }

        [JsonPropertyName("s")]
        public decimal S { get; set; }

        [JsonPropertyName("b")]
        public decimal B { get; set; }
    }
}