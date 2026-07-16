using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CONATRADEC.Models
{
    // ===============================================================
    // Response de:
    // POST api/fertilizacion-mixta/calcular
    // ===============================================================
    public class FertilizacionMixtaCalculoResponse
    {
        [JsonIgnore]
        public bool Success { get; set; } = true;

        [JsonIgnore]
        public string? Message { get; set; }

        [JsonPropertyName("observacion")]
        public string? Observacion { get; set; }

        [JsonPropertyName("fuentes")]
        public List<FuenteFertilizacionMixtaResultadoResponse> Fuentes { get; set; } = new();

        [JsonPropertyName("detalles")]
        public List<DetalleFertilizacionMixtaResultadoResponse> Detalles { get; set; } = new();
    }

    public class FuenteFertilizacionMixtaResultadoResponse
    {
        [JsonPropertyName("fuenteNutrientesId")]
        public int? FuenteNutrientesId { get; set; }

        [JsonPropertyName("nombreFuente")]
        public string? NombreFuente { get; set; }

        [JsonPropertyName("cantidadQq")]
        public decimal? CantidadQq { get; set; }
    }

    public class DetalleFertilizacionMixtaResultadoResponse
    {
        [JsonPropertyName("elementoQuimicosId")]
        public int? ElementoQuimicosId { get; set; }

        [JsonPropertyName("elemento")]
        public string? Elemento { get; set; }

        [JsonPropertyName("exportable")]
        public decimal? Exportable { get; set; }

        [JsonPropertyName("aporteOrganico")]
        public decimal? AporteOrganico { get; set; }

        [JsonPropertyName("diferencia")]
        public decimal? Diferencia { get; set; }

        [JsonPropertyName("deficit")]
        public decimal? Deficit { get; set; }

        [JsonPropertyName("sobrante")]
        public decimal? Sobrante { get; set; }

        [JsonPropertyName("fuentes")]
        public List<FuenteDetalleFertilizacionMixtaResponse> Fuentes { get; set; } = new();
    }

    public class FuenteDetalleFertilizacionMixtaResponse
    {
        [JsonPropertyName("fuenteNutrientesId")]
        public int? FuenteNutrientesId { get; set; }

        [JsonPropertyName("nombreFuente")]
        public string? NombreFuente { get; set; }

        [JsonPropertyName("cantidadQq")]
        public decimal? CantidadQq { get; set; }

        [JsonPropertyName("aportePorUnidad")]
        public decimal? AportePorUnidad { get; set; }

        [JsonPropertyName("aporteTotal")]
        public decimal? AporteTotal { get; set; }
    }

    // ===============================================================
    // Response de:
    // GET api/fuente-nutriente/listar-fertilizacion-mixta
    // ===============================================================
    public class FuenteNutrienteFertilizacionMixtaResponse
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
        public List<ElementoFuenteNutrienteFertilizacionMixtaResponse> ElementosQuimicos { get; set; } = new();
    }

    public class ElementoFuenteNutrienteFertilizacionMixtaResponse
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
    }
}