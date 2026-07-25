using System.Text.Json.Serialization;

namespace CONATRADEC.Models
{
    public sealed class ElementoQuimicoRequest
    {
        public int? ElementoQuimicosId { get; set; }

        public string SimboloElementoQuimico { get; set; } =
            string.Empty;

        public string NombreElementoQuimico { get; set; } =
            string.Empty;

        [JsonPropertyName("pesoEquivalenteElementoQuimico")]
        public decimal? PesoEquivalenteElementoQuimico { get; set; }

        public ElementoQuimicoRequest()
        {
        }

        public ElementoQuimicoRequest(
            ElementoQuimicoResponse elemento)
        {
            ArgumentNullException.ThrowIfNull(elemento);

            ElementoQuimicosId =
                elemento.ElementoQuimicosId;

            SimboloElementoQuimico =
                elemento.SimboloElementoQuimico ??
                string.Empty;

            NombreElementoQuimico =
                elemento.NombreElementoQuimico ??
                string.Empty;

            PesoEquivalenteElementoQuimico =
                elemento.PesoEquivalenteElementoQuimico;
        }
    }
}
