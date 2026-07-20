namespace CONATRADEC.Models
{
    public class ExtraccionNutrienteResponse
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
    }
}
