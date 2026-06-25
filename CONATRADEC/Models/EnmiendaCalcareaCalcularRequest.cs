namespace CONATRADEC.Models
{
    public class EnmiendaCalcareaCalcularRequest
    {
        public string NombreAnalisis { get; set; } = string.Empty;

        public int FuenteNutrientesId { get; set; }

        public decimal Ph { get; set; }

        public decimal Ca { get; set; }

        public decimal Mg { get; set; }

        public decimal K { get; set; }

        public decimal AcidezTotal { get; set; }

        public int TerrenoId { get; set; }

        public int TotalPlantas { get; set; }

        public int TotalAplicaciones { get; set; }
    }
}