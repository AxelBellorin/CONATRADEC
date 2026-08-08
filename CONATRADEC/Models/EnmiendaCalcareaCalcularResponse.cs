using System.Text.Json.Serialization;

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

        /// <summary>
        /// Interpretación visible del resultado final de enmienda.
        /// La fórmula puede conservar equivalencias técnicas en ton/ha,
        /// kg/ha y otras unidades, pero la unidad final que se presenta al
        /// usuario en CONATRADEC es libra por manzana (lb/Mz).
        /// </summary>
        [JsonIgnore]
        public string InterpretacionResultadoLbMz
        {
            get
            {
                decimal necesidadLbMz =
                    NecesidadEncaladoLbMz ?? 0;

                decimal actual =
                    SaturacionActual ?? 0;

                decimal deseada =
                    SaturacionDeseada ?? 0;

                if (necesidadLbMz > 0)
                {
                    return
                        "Cálculo realizado: se requieren " +
                        $"{necesidadLbMz:N2} lb/Mz de enmienda calcárea.";
                }

                if (actual >= deseada)
                {
                    return
                        "Cálculo realizado: la saturación actual " +
                        $"({actual:N2}%) alcanza o supera la deseada " +
                        $"({deseada:N2}%). Por eso la necesidad final " +
                        "en lb/Mz y las dosis calculadas son 0.";
                }

                return
                    "El cálculo fue realizado y no determinó una dosis " +
                    "positiva expresada en lb/Mz con los parámetros configurados.";
            }
        }
    }
}
