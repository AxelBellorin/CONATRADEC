using System.Globalization;

namespace CONATRADEC.Models
{
    public class ParametroEnmiendaCalcareaResponse
    {
        public int? ParametroEnmiendaCalcareaId { get; set; }

        public int? FuenteNutrientesId { get; set; }

        public string? NombreNutriente { get; set; }

        public decimal? PrecioNutriente { get; set; }

        public decimal? Prnt { get; set; }

        public string? DescripcionParametro { get; set; }

        public string NombreMostrar
        {
            get
            {
                string nombre = string.IsNullOrWhiteSpace(NombreNutriente)
                    ? "Enmienda calcárea"
                    : NombreNutriente;

                string prntTexto = Prnt.HasValue
                    ? $"PRNT {Prnt.Value.ToString("N0", CultureInfo.InvariantCulture)}%"
                    : "PRNT N/D";

                string precioTexto = PrecioNutriente.HasValue
                    ? $"C$ {PrecioNutriente.Value.ToString("N2", CultureInfo.InvariantCulture)}"
                    : "Precio N/D";

                return $"{nombre} - {prntTexto} - {precioTexto}";
            }
        }
    }
}