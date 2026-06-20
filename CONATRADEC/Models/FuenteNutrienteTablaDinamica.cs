using System.Collections.ObjectModel;

namespace CONATRADEC.Models
{
    public class FuenteNutrienteTablaDinamicaRow
    {
        public int? FuenteNutrientesId { get; set; }

        public string Fuente { get; set; } = string.Empty;

        public ObservableCollection<FuenteNutrienteTablaDinamicaCell> Celdas { get; set; } = new();
    }

    public class FuenteNutrienteTablaDinamicaCell
    {
        public string SimboloElemento { get; set; } = string.Empty;

        public decimal Valor { get; set; }

        public string Texto => Valor > 0 ? $"{Valor:N0}" : "0";
    }
}