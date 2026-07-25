using System.Collections.ObjectModel;

namespace CONATRADEC.Models
{
    public sealed class FuenteNutrienteTablaDinamicaRow
    {
        public int? FuenteNutrientesId { get; set; }

        public string Fuente { get; set; } =
            string.Empty;

        public ObservableCollection<
            FuenteNutrienteTablaDinamicaCell>
            Celdas { get; set; } =
                new();
    }

    public sealed class FuenteNutrienteTablaDinamicaCell
    {
        public string SimboloElemento { get; set; } =
            string.Empty;

        public decimal Valor { get; set; }

        /*
         * Estandarización visual del proyecto:
         * los aportes siempre se presentan con dos decimales.
         */
        public string Texto =>
            Valor.ToString("N2");
    }
}
