using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace CONATRADEC.Models
{
    public class FuenteNutrienteResponse
    {
        [JsonPropertyName("fuenteNutrientesId")]
        public int? FuenteNutrientesId { get; set; }

        [JsonPropertyName("nombreNutriente")]
        public string? NombreNutriente { get; set; }

        [JsonPropertyName("descripcionNutriente")]
        public string? DescripcionNutriente { get; set; }

        [JsonPropertyName("precioNutriente")]
        public decimal? PrecioNutriente { get; set; }

        [JsonPropertyName("activo")]
        public bool? Activo { get; set; }

        [JsonPropertyName("elementosQuimicos")]
        public List<FuenteNutrienteElementoQuimicoResponse> ElementosQuimicos { get; set; } = new();

        public string NombreMostrar
        {
            get
            {
                string nombre = NombreNutriente ?? "Fuente sin nombre";

                if (ElementosQuimicos == null || ElementosQuimicos.Count == 0)
                    return nombre;

                string aportes = string.Join(
                    ", ",
                    ElementosQuimicos
                        .Where(x => !string.IsNullOrWhiteSpace(x.SimboloElementoQuimico))
                        .Select(x => $"{x.SimboloElementoQuimico?.Trim()} {x.CantidadAporte:N0}%")
                );

                if (string.IsNullOrWhiteSpace(aportes))
                    return nombre;

                return $"{nombre} ({aportes})";
            }
        }

        public bool TieneAportes =>
            ElementosQuimicos != null && ElementosQuimicos.Count > 0;
    }

    public class FuenteNutrienteElementoQuimicoResponse
    {
        [JsonPropertyName("fuenteNutrienteElementoQuimicoId")]
        public int? FuenteNutrienteElementoQuimicoId { get; set; }

        [JsonPropertyName("elementoQuimicosId")]
        public int? ElementoQuimicosId { get; set; }

        [JsonPropertyName("nombreElementoQuimico")]
        public string? NombreElementoQuimico { get; set; }

        [JsonPropertyName("simboloElementoQuimico")]
        public string? SimboloElementoQuimico { get; set; }

        [JsonPropertyName("cantidadAporte")]
        public decimal? CantidadAporte { get; set; }

        public string SimboloNormalizado =>
            (SimboloElementoQuimico ?? string.Empty)
                .Trim()
                .ToUpper();
    }
}