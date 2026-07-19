using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CONATRADEC.Models
{
    public class FertilizacionMixtaCalculoResponse
    {
        private string? observacion;

        [JsonIgnore]
        public bool Success { get; set; } = true;

        [JsonIgnore]
        public string? Message { get; set; }

        [JsonPropertyName("observacion")]
        public string? Observacion
        {
            get => observacion;
            set => observacion = TextoMixtaHelper.Limpiar(value);
        }

        [JsonPropertyName("fuentes")]
        public List<FuenteFertilizacionMixtaResultadoResponse> Fuentes { get; set; } = new();

        [JsonPropertyName("detalles")]
        public List<DetalleFertilizacionMixtaResultadoResponse> Detalles { get; set; } = new();
    }

    public class FuenteFertilizacionMixtaResultadoResponse
    {
        private string? nombreFuente;

        [JsonPropertyName("fuenteNutrientesId")]
        public int? FuenteNutrientesId { get; set; }

        [JsonPropertyName("nombreFuente")]
        public string? NombreFuente
        {
            get => nombreFuente;
            set => nombreFuente = TextoMixtaHelper.Limpiar(value);
        }

        [JsonPropertyName("cantidadQq")]
        public decimal? CantidadQq { get; set; }
    }

    public class DetalleFertilizacionMixtaResultadoResponse
    {
        private string? elemento;

        [JsonPropertyName("elementoQuimicosId")]
        public int? ElementoQuimicosId { get; set; }

        [JsonPropertyName("elemento")]
        public string? Elemento
        {
            get => elemento;
            set => elemento = TextoMixtaHelper.Limpiar(value);
        }

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
        private string? nombreFuente;

        [JsonPropertyName("fuenteNutrientesId")]
        public int? FuenteNutrientesId { get; set; }

        [JsonPropertyName("nombreFuente")]
        public string? NombreFuente
        {
            get => nombreFuente;
            set => nombreFuente = TextoMixtaHelper.Limpiar(value);
        }

        [JsonPropertyName("cantidadQq")]
        public decimal? CantidadQq { get; set; }

        [JsonPropertyName("aportePorUnidad")]
        public decimal? AportePorUnidad { get; set; }

        [JsonPropertyName("aporteTotal")]
        public decimal? AporteTotal { get; set; }
    }

    public class FuenteNutrienteFertilizacionMixtaResponse
    {
        private string? nombreNutriente;
        private string? descripcionNutriente;

        [JsonPropertyName("fuenteNutrientesId")]
        public int? FuenteNutrientesId { get; set; }

        [JsonPropertyName("nombreNutriente")]
        public string? NombreNutriente
        {
            get => nombreNutriente;
            set => nombreNutriente = TextoMixtaHelper.Limpiar(value);
        }

        [JsonPropertyName("descripcionNutriente")]
        public string? DescripcionNutriente
        {
            get => descripcionNutriente;
            set => descripcionNutriente = TextoMixtaHelper.Limpiar(value);
        }

        [JsonPropertyName("precioNutriente")]
        public decimal? PrecioNutriente { get; set; }

        [JsonPropertyName("activo")]
        public bool? Activo { get; set; }

        [JsonPropertyName("elementosQuimicos")]
        public List<ElementoFuenteNutrienteFertilizacionMixtaResponse>
            ElementosQuimicos { get; set; } = new();
    }

    public class ElementoFuenteNutrienteFertilizacionMixtaResponse
    {
        private string? nombreElementoQuimico;
        private string? simboloElementoQuimico;

        [JsonPropertyName("fuenteNutrienteElementoQuimicoId")]
        public int? FuenteNutrienteElementoQuimicoId { get; set; }

        [JsonPropertyName("elementoQuimicosId")]
        public int? ElementoQuimicosId { get; set; }

        [JsonPropertyName("nombreElementoQuimico")]
        public string? NombreElementoQuimico
        {
            get => nombreElementoQuimico;
            set => nombreElementoQuimico = TextoMixtaHelper.Limpiar(value);
        }

        [JsonPropertyName("simboloElementoQuimico")]
        public string? SimboloElementoQuimico
        {
            get => simboloElementoQuimico;
            set => simboloElementoQuimico = TextoMixtaHelper.Limpiar(value);
        }

        [JsonPropertyName("cantidadAporte")]
        public decimal? CantidadAporte { get; set; }
    }

    internal static class TextoMixtaHelper
    {
        public static string? Limpiar(string? valor)
        {
            if (string.IsNullOrWhiteSpace(valor))
                return null;

            return valor.Trim();
        }
    }
}
