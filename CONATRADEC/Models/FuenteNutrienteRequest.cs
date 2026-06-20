using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace CONATRADEC.Models
{
    public class FuenteNutrienteRequest
    {
        [JsonIgnore]
        public int? FuenteNutrientesId { get; set; }

        [JsonPropertyName("nombreNutriente")]
        public string NombreNutriente { get; set; } = string.Empty;

        [JsonPropertyName("descripcionNutriente")]
        public string DescripcionNutriente { get; set; } = string.Empty;

        [JsonPropertyName("precioNutriente")]
        public decimal PrecioNutriente { get; set; }

        [JsonPropertyName("elementosQuimicos")]
        public List<FuenteNutrienteElementoQuimicoRequest> ElementosQuimicos { get; set; } = new();

        public FuenteNutrienteRequest()
        {
        }

        public FuenteNutrienteRequest(FuenteNutrienteResponse response)
        {
            if (response == null)
                return;

            FuenteNutrientesId = response.FuenteNutrientesId;
            NombreNutriente = response.NombreNutriente ?? string.Empty;
            DescripcionNutriente = response.DescripcionNutriente ?? string.Empty;
            PrecioNutriente = response.PrecioNutriente ?? 0;

            ElementosQuimicos = response.ElementosQuimicos?
                .Where(x => x.ElementoQuimicosId.HasValue)
                .Select(x => new FuenteNutrienteElementoQuimicoRequest
                {
                    ElementoQuimicosId = x.ElementoQuimicosId ?? 0,
                    CantidadAporte = x.CantidadAporte ?? 0
                })
                .ToList() ?? new List<FuenteNutrienteElementoQuimicoRequest>();
        }
    }

    public class FuenteNutrienteElementoQuimicoRequest
    {
        [JsonPropertyName("elementoQuimicosId")]
        public int ElementoQuimicosId { get; set; }

        [JsonPropertyName("cantidadAporte")]
        public decimal CantidadAporte { get; set; }
    }
}