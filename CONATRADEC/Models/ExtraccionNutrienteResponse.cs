using System.Globalization;

namespace CONATRADEC.Models
{
    public sealed class ExtraccionNutrienteResponse
    {
        public int ParametroExtraccionNutrienteCafeId { get; set; }
        public int ElementoQuimicosId { get; set; }
        public string? NombreElementoQuimico { get; set; }
        public string? SimboloElementoQuimico { get; set; }
        public decimal CantidadExtraidaPorQQOro { get; set; }
        public string? DescripcionParametro { get; set; }
        public bool Activo { get; set; }

        public string ElementoTexto =>
            string.IsNullOrWhiteSpace(SimboloElementoQuimico)
                ? NombreElementoQuimico ?? string.Empty
                : $"{SimboloElementoQuimico} - {NombreElementoQuimico}";

        /*
         * Se conserva la precisión agronómica existente.
         * El valor participa directamente en el requerimiento anual.
         */
        public string CantidadMostrar =>
            CantidadExtraidaPorQQOro.ToString(
                "0.####",
                CultureInfo.InvariantCulture);

        public string ResumenCantidad =>
            $"{CantidadMostrar} lb por QQ oro";

        public string DescripcionMostrar =>
            string.IsNullOrWhiteSpace(DescripcionParametro)
                ? "Sin descripción registrada."
                : DescripcionParametro.Trim();
    }
}
