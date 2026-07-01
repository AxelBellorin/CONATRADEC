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

        [JsonIgnore]
        public bool HabilitadaEnmiendaCalcarea { get; set; }

        [JsonIgnore]
        public bool HabilitadaFertilizacionMixta { get; set; }

        [JsonIgnore]
        public decimal? PrntEnmiendaCalcarea { get; set; }

        [JsonIgnore]
        public string? DescripcionParametroEnmiendaCalcarea { get; set; }

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

            HabilitadaEnmiendaCalcarea = response.HabilitadaEnmiendaCalcarea == true;
            HabilitadaFertilizacionMixta = response.HabilitadaFertilizacionMixta == true;

            PrntEnmiendaCalcarea = response.Prnt;
            DescripcionParametroEnmiendaCalcarea = response.DescripcionParametro;

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

    public class HabilitarEnmiendaCalcareaRequest
    {
        [JsonPropertyName("prnt")]
        public decimal Prnt { get; set; }

        [JsonPropertyName("descripcionParametro")]
        public string DescripcionParametro { get; set; } = string.Empty;
    }

    public class FuenteNutrienteCategoriaOption
    {
        public const string CodigoTodas = "TODAS";
        public const string CodigoBalanceNutricional = "BALANCE_NUTRICIONAL";
        public const string CodigoEnmiendaCalcarea = "ENMIENDA_CALCAREA";
        public const string CodigoFertilizacionMixta = "FERTILIZACION_MIXTA";

        public string Codigo { get; set; } = string.Empty;

        public string Nombre { get; set; } = string.Empty;
    }
}