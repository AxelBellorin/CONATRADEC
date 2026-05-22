using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CONATRADEC.Models
{
    public class AnalisisSueloCalculoResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }

        [JsonPropertyName("data")]
        public AnalisisSueloCalculoDataResponse? Data { get; set; }
    }

    public class AnalisisSueloCalculoDataResponse
    {
        [JsonPropertyName("analisisSueloId")]
        public int? AnalisisSueloId { get; set; }

        [JsonPropertyName("analisisSueloCalculoId")]
        public int? AnalisisSueloCalculoId { get; set; }

        [JsonPropertyName("identificadorAnalisisSuelo")]
        public string? IdentificadorAnalisisSuelo { get; set; }

        [JsonPropertyName("resultado")]
        public AnalisisSueloResultadoResponse? Resultado { get; set; }
    }

    public class AnalisisSueloResultadoResponse
    {
        [JsonPropertyName("terrenoId")]
        public int? TerrenoId { get; set; }

        [JsonPropertyName("tipoCultivoId")]
        public int? TipoCultivoId { get; set; }

        [JsonPropertyName("tipoCultivo")]
        public string? TipoCultivo { get; set; }

        [JsonPropertyName("tipoAnalisisSueloId")]
        public int? TipoAnalisisSueloId { get; set; }

        [JsonPropertyName("tipoAnalisisSuelo")]
        public string? TipoAnalisisSuelo { get; set; }

        [JsonPropertyName("cantidadQuintalesOro")]
        public decimal? CantidadQuintalesOro { get; set; }

        [JsonPropertyName("tamanoFinca")]
        public decimal? TamanoFinca { get; set; }

        [JsonPropertyName("ph")]
        public decimal? Ph { get; set; }

        [JsonPropertyName("materiaOrganica")]
        public decimal? MateriaOrganica { get; set; }

        [JsonPropertyName("acidezTotal")]
        public decimal? AcidezTotal { get; set; }

        [JsonPropertyName("elementos")]
        public List<ElementoResultadoAnalisisResponse> Elementos { get; set; } = new();

        [JsonPropertyName("fuentesFertilizantes")]
        public List<object> FuentesFertilizantes { get; set; } = new();

        [JsonPropertyName("enmiendaCalcarea")]
        public object? EnmiendaCalcarea { get; set; }

        [JsonPropertyName("fuentesOrganicas")]
        public List<object> FuentesOrganicas { get; set; } = new();

        [JsonPropertyName("recomendacionGeneral")]
        public string? RecomendacionGeneral { get; set; }

        [JsonPropertyName("observaciones")]
        public List<string> Observaciones { get; set; } = new();
    }

    public class ElementoResultadoAnalisisResponse
    {
        [JsonPropertyName("elementoQuimicosId")]
        public int? ElementoQuimicosId { get; set; }

        [JsonPropertyName("simboloElementoQuimico")]
        public string? SimboloElementoQuimico { get; set; }

        [JsonPropertyName("nombreElementoQuimico")]
        public string? NombreElementoQuimico { get; set; }

        [JsonPropertyName("cantidadIngresada")]
        public decimal? CantidadIngresada { get; set; }

        [JsonPropertyName("cantidadConvertidaLbMz")]
        public decimal? CantidadConvertidaLbMz { get; set; }

        [JsonPropertyName("extraccionPorQQOro")]
        public decimal? ExtraccionPorQQOro { get; set; }

        [JsonPropertyName("extraccionPorProduccion")]
        public decimal? ExtraccionPorProduccion { get; set; }

        [JsonPropertyName("rangoMinimo")]
        public decimal? RangoMinimo { get; set; }

        [JsonPropertyName("rangoMaximo")]
        public decimal? RangoMaximo { get; set; }

        [JsonPropertyName("rangoMinimoLbMz")]
        public decimal? RangoMinimoLbMz { get; set; }

        [JsonPropertyName("rangoMaximoLbMz")]
        public decimal? RangoMaximoLbMz { get; set; }

        [JsonPropertyName("requerimientoCalculado")]
        public decimal? RequerimientoCalculado { get; set; }

        [JsonPropertyName("unidadBase")]
        public string? UnidadBase { get; set; }

        [JsonPropertyName("unidadMedidaResultadoId")]
        public int? UnidadMedidaResultadoId { get; set; }

        [JsonPropertyName("unidadResultado")]
        public string? UnidadResultado { get; set; }

        [JsonPropertyName("clasificacion")]
        public string? Clasificacion { get; set; }

        [JsonPropertyName("observacion")]
        public string? Observacion { get; set; }
    }
}