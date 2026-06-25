namespace CONATRADEC.Models
{
    public class EnmiendaCalcareaCalcularResponse
    {
        public int? EnmiendaCalcareaId { get; set; }

        public string? NombreAnalisis { get; set; }

        public string? FuenteNutriente { get; set; }

        public decimal? Ph { get; set; }

        public decimal? Ca { get; set; }

        public decimal? Mg { get; set; }

        public decimal? K { get; set; }

        public decimal? AcidezTotal { get; set; }

        public decimal? SaturacionDeseada { get; set; }

        public decimal? Prnt { get; set; }

        public decimal? SumaBases { get; set; }

        public decimal? Cice { get; set; }

        public decimal? SaturacionActual { get; set; }

        public decimal? NecesidadEncaladoTonHa { get; set; }

        public decimal? NecesidadEncaladoKgHa { get; set; }

        public decimal? NecesidadEncaladoLbHa { get; set; }

        public int? TerrenoId { get; set; }

        public int? TotalPlantas { get; set; }

        public int? TotalAplicaciones { get; set; }

        public decimal? NecesidadEncaladoLbMz { get; set; }

        public decimal? NecesidadEncaladoOzMz { get; set; }

        public decimal? DosisPlantaAnualOz { get; set; }

        public decimal? DosisPlantaPorAplicacionOz { get; set; }
    }
}