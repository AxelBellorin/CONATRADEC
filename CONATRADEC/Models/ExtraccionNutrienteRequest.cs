using System.Text.Json.Serialization;

namespace CONATRADEC.Models
{
    public class ExtraccionNutrienteRequest
    {
        [JsonIgnore]
        public int ParametroExtraccionNutrienteCafeId { get; set; }

        public int ElementoQuimicosId { get; set; }
        public decimal CantidadExtraidaPorQQOro { get; set; }
        public string DescripcionParametro { get; set; } = string.Empty;

        public ExtraccionNutrienteRequest()
        {
        }

        public ExtraccionNutrienteRequest(ExtraccionNutrienteResponse response)
        {
            ArgumentNullException.ThrowIfNull(response);

            ParametroExtraccionNutrienteCafeId = response.ParametroExtraccionNutrienteCafeId;
            ElementoQuimicosId = response.ElementoQuimicosId;
            CantidadExtraidaPorQQOro = response.CantidadExtraidaPorQQOro;
            DescripcionParametro = response.DescripcionParametro ?? string.Empty;
        }
    }
}
