using System.Globalization;
using System.Text.Json.Serialization;

namespace CONATRADEC.Models
{
    public sealed class ElementoQuimicoResponse
    {
        public int? ElementoQuimicosId { get; set; }

        public string SimboloElementoQuimico { get; set; } =
            string.Empty;

        public string NombreElementoQuimico { get; set; } =
            string.Empty;

        [JsonPropertyName("pesoEquivalenteElementoQuimico")]
        public decimal? PesoEquivalenteElementoQuimico { get; set; }

        public bool Activo { get; set; }

        public string PesoEquivalenteFormateado =>
            PesoEquivalenteElementoQuimico.HasValue
                ? $"Peso equivalente: " +
                  PesoEquivalenteElementoQuimico.Value.ToString(
                      "N2",
                      CultureInfo.CurrentCulture)
                : "Peso equivalente: 0.00";
    }
}
