namespace CONATRADEC.Models
{
    public class ElementoQuimicoSelectorItem
    {
        public int ElementoQuimicosId { get; set; }
        public string NombreElementoQuimico { get; set; } = string.Empty;
        public string SimboloElementoQuimico { get; set; } = string.Empty;

        public string Texto =>
            string.IsNullOrWhiteSpace(SimboloElementoQuimico)
                ? NombreElementoQuimico
                : $"{SimboloElementoQuimico} - {NombreElementoQuimico}";

        public static ElementoQuimicoSelectorItem FromResponse(
            ElementoQuimicoResponse response)
        {
            return new ElementoQuimicoSelectorItem
            {
                ElementoQuimicosId = response.ElementoQuimicosId ?? 0,
                NombreElementoQuimico = response.NombreElementoQuimico ?? string.Empty,
                SimboloElementoQuimico = response.SimboloElementoQuimico ?? string.Empty
            };
        }
    }
}
