namespace CONATRADEC.Models
{
    public class RangoNutrienteResponse
    {
        public int ParametroRangoNutrienteCultivoId { get; set; }
        public int TipoCultivoId { get; set; }
        public string? NombreTipoCultivo { get; set; }
        public int ElementoQuimicosId { get; set; }
        public string? NombreElementoQuimico { get; set; }
        public string? SimboloElementoQuimico { get; set; }
        public decimal ValorMinimo { get; set; }
        public decimal ValorMaximo { get; set; }
        public string? UnidadBase { get; set; }
        public string? DescripcionParametro { get; set; }
        public bool Activo { get; set; }

        private string SimboloLimpio =>
            (SimboloElementoQuimico ?? string.Empty)
                .Trim();

        private string NombreElementoLimpio =>
            (NombreElementoQuimico ?? string.Empty)
                .Trim();

        private string UnidadBaseLimpia =>
            (UnidadBase ?? string.Empty)
                .Trim();

        public string ElementoTexto
        {
            get
            {
                if (string.IsNullOrWhiteSpace(
                        SimboloLimpio))
                {
                    return NombreElementoLimpio;
                }

                if (string.IsNullOrWhiteSpace(
                        NombreElementoLimpio))
                {
                    return SimboloLimpio;
                }

                return
                    $"{SimboloLimpio} - " +
                    $"{NombreElementoLimpio}";
            }
        }

        public string RangoTexto =>
            $"{ValorMinimo:0.##} - " +
            $"{ValorMaximo:0.##} " +
            $"{UnidadBaseLimpia}";
    }
}
