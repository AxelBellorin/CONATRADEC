using System.Globalization;

namespace CONATRADEC.Models
{
    public sealed class RangoNutrienteResponse
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

        public string ElementoTexto
        {
            get
            {
                string simbolo =
                    (SimboloElementoQuimico ?? string.Empty).Trim();

                string nombre =
                    (NombreElementoQuimico ?? string.Empty).Trim();

                if (string.IsNullOrWhiteSpace(simbolo))
                    return nombre;

                if (string.IsNullOrWhiteSpace(nombre))
                    return simbolo;

                return $"{simbolo} - {nombre}";
            }
        }

        public string RangoTexto =>
            $"{ValorMinimo.ToString("N2", CultureInfo.CurrentCulture)} - " +
            $"{ValorMaximo.ToString("N2", CultureInfo.CurrentCulture)} " +
            $"{(UnidadBase ?? string.Empty).Trim()}";

        public string DescripcionMostrar =>
            string.IsNullOrWhiteSpace(DescripcionParametro)
                ? "Sin descripción registrada."
                : DescripcionParametro.Trim();
    }
}
