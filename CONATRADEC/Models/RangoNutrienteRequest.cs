using System.Text.Json.Serialization;

namespace CONATRADEC.Models
{
    public class RangoNutrienteRequest
    {
        [JsonIgnore]
        public int ParametroRangoNutrienteCultivoId { get; set; }

        public int TipoCultivoId { get; set; }
        public int ElementoQuimicosId { get; set; }
        public decimal ValorMinimo { get; set; }
        public decimal ValorMaximo { get; set; }
        public string UnidadBase { get; set; } = string.Empty;
        public string DescripcionParametro { get; set; } = string.Empty;

        public RangoNutrienteRequest()
        {
        }

        public RangoNutrienteRequest(RangoNutrienteResponse response)
        {
            ArgumentNullException.ThrowIfNull(response);

            ParametroRangoNutrienteCultivoId = response.ParametroRangoNutrienteCultivoId;
            TipoCultivoId = response.TipoCultivoId;
            ElementoQuimicosId = response.ElementoQuimicosId;
            ValorMinimo = response.ValorMinimo;
            ValorMaximo = response.ValorMaximo;
            UnidadBase = response.UnidadBase ?? string.Empty;
            DescripcionParametro = response.DescripcionParametro ?? string.Empty;
        }
    }
}
