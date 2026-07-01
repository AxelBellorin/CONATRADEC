using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CONATRADEC.Models
{
    // ===============================================================
    // Request usado por:
    // POST api/analisis-suelo/calcular
    // ===============================================================
    public class AnalisisSueloCalcularRequest
    {
        [JsonPropertyName("terrenoId")]
        public int? TerrenoId { get; set; }

        [JsonPropertyName("tipoCultivoId")]
        public int? TipoCultivoId { get; set; }

        [JsonPropertyName("tipoAnalisisSueloId")]
        public int? TipoAnalisisSueloId { get; set; }

        [JsonPropertyName("usuarioId")]
        public int? UsuarioId { get; set; }

        [JsonPropertyName("cantidadQuintalesOro")]
        public decimal? CantidadQuintalesOro { get; set; }

        [JsonPropertyName("tamanoFinca")]
        public decimal? TamanoFinca { get; set; }

        [JsonPropertyName("ph")]
        public decimal? Ph { get; set; }

        [JsonPropertyName("materiaOrganica")]
        public decimal? MateriaOrganica { get; set; }

        [JsonPropertyName("unidadMedidaMateriaOrganicaId")]
        public int? UnidadMedidaMateriaOrganicaId { get; set; }

        [JsonPropertyName("acidezTotal")]
        public decimal? AcidezTotal { get; set; }

        // ===============================================================
        // Datos CICE usados principalmente para Enmienda Calcárea.
        // Son opcionales. Si el usuario no los escribe, se envían como 0.
        // ===============================================================
        [JsonPropertyName("calcioCice")]
        public decimal? CalcioCice { get; set; }

        [JsonPropertyName("magnesioCice")]
        public decimal? MagnesioCice { get; set; }

        [JsonPropertyName("potasioCice")]
        public decimal? PotasioCice { get; set; }

        [JsonPropertyName("elementosQuimicos")]
        public List<ElementoQuimicoAnalisisRequest> ElementosQuimicos { get; set; } = new();

        [JsonPropertyName("fuentesOrganicas")]
        public List<FuenteOrganicaAnalisisRequest> FuentesOrganicas { get; set; } = new();
    }

    // ===============================================================
    // Request usado por:
    // POST api/analisis-suelo/guardar-calculo
    // Hereda todos los campos del cálculo y agrega datos del laboratorio.
    // ===============================================================
    public class AnalisisSueloGuardarCalculoRequest : AnalisisSueloCalcularRequest
    {
        [JsonPropertyName("fechaAnalisisSuelo")]
        public string? FechaAnalisisSuelo { get; set; }

        // OJO: la API espera "Analasis", según tu JSON.
        [JsonPropertyName("laboratorioAnalasisSuelo")]
        public string? LaboratorioAnalasisSuelo { get; set; }

        [JsonPropertyName("identificadorAnalisisSuelo")]
        public string? IdentificadorAnalisisSuelo { get; set; }
    }

    public class ElementoQuimicoAnalisisRequest
    {
        [JsonPropertyName("elementoQuimicosId")]
        public int? ElementoQuimicosId { get; set; }

        [JsonPropertyName("unidadMedidaId")]
        public int? UnidadMedidaId { get; set; }

        [JsonPropertyName("cantidadElemento")]
        public decimal? CantidadElemento { get; set; }
    }

    public class FuenteOrganicaAnalisisRequest
    {
        [JsonPropertyName("fuenteNutrientesId")]
        public int? FuenteNutrientesId { get; set; }

        [JsonPropertyName("cantidadAplicada")]
        public decimal? CantidadAplicada { get; set; }
    }
}