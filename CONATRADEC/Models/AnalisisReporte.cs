using System;
using System.Collections.Generic;

namespace CONATRADEC.Models
{
    public sealed class AnalisisReporte
    {
        public string Identificador { get; set; } = string.Empty;

        public string FechaAnalisis { get; set; } = string.Empty;

        public string Laboratorio { get; set; } = string.Empty;

        public string Cliente { get; set; } = string.Empty;

        public string Terreno { get; set; } = string.Empty;

        public string TipoCultivo { get; set; } = string.Empty;

        public string TipoAnalisis { get; set; } = string.Empty;

        public decimal ProduccionQqOro { get; set; }

        public decimal TamanoFincaMz { get; set; }

        public decimal Ph { get; set; }

        public decimal? MateriaOrganica { get; set; }

        public decimal? AcidezTotal { get; set; }

        public string RecomendacionGeneral { get; set; } = string.Empty;

        public List<string> Observaciones { get; set; } = new();

        public List<AnalisisReporteValorLaboratorio> ValoresLaboratorio { get; set; } = new();

        public List<AnalisisReporteRequerimiento> Requerimientos { get; set; } = new();

        public AnalisisReporteBalance? Balance { get; set; }

        public AnalisisReporteEnmienda? Enmienda { get; set; }

        public AnalisisReporteFertilizacionMixta? FertilizacionMixta { get; set; }

        public string NombreArchivoBase
        {
            get
            {
                string identificador = string.IsNullOrWhiteSpace(Identificador)
                    ? $"Analisis_{DateTime.Now:yyyyMMdd_HHmmss}"
                    : Identificador.Trim();

                foreach (char caracter in System.IO.Path.GetInvalidFileNameChars())
                    identificador = identificador.Replace(caracter, '_');

                return $"Reporte_Analisis_{identificador}";
            }
        }
    }

    public sealed class AnalisisReporteValorLaboratorio
    {
        public string Elemento { get; set; } = string.Empty;

        public decimal Cantidad { get; set; }

        public string Unidad { get; set; } = string.Empty;
    }

    public sealed class AnalisisReporteRequerimiento
    {
        public string Elemento { get; set; } = string.Empty;

        public decimal CantidadIngresada { get; set; }

        public decimal? CantidadConvertidaLbMz { get; set; }

        public decimal? RequerimientoLbMz { get; set; }

        public string UnidadResultado { get; set; } = string.Empty;

        public string Clasificacion { get; set; } = string.Empty;

        public string Observacion { get; set; } = string.Empty;
    }

    public sealed class AnalisisReporteBalance
    {
        public string NombreFormula { get; set; } = string.Empty;

        public decimal TotalLibras { get; set; }

        public decimal MezclaTotalQq { get; set; }

        public int TotalPlantas { get; set; }

        public int TotalAplicaciones { get; set; }

        public decimal DosisPlantaAnualOz { get; set; }

        public decimal DosisPlantaPorAplicacionOz { get; set; }

        public decimal PrecioExactoReferencia { get; set; }

        public decimal CostoRealCompra { get; set; }

        public decimal PrecioPorAplicacion { get; set; }

        public Dictionary<string, decimal> FormulaComercial { get; set; } = new();

        public List<AnalisisReporteBalanceDetalle> Detalles { get; set; } = new();
    }

    public sealed class AnalisisReporteBalanceDetalle
    {
        public string Fuente { get; set; } = string.Empty;

        public string Elemento { get; set; } = string.Empty;

        public decimal Libras { get; set; }

        public decimal QuintalesExactos { get; set; }

        public decimal QuintalesComprar { get; set; }

        public decimal PrecioPorQuintal { get; set; }

        public decimal SubtotalExacto { get; set; }

        public decimal CostoCompra { get; set; }

        public decimal OnzasAnuales { get; set; }

        public decimal OnzasPorAplicacion { get; set; }
    }

    public sealed class AnalisisReporteEnmienda
    {
        public string NombreAnalisis { get; set; } = string.Empty;

        public string Fuente { get; set; } = string.Empty;

        public int TotalPlantas { get; set; }

        public int TotalAplicaciones { get; set; }

        public decimal Ph { get; set; }

        public decimal Calcio { get; set; }

        public decimal Magnesio { get; set; }

        public decimal Potasio { get; set; }

        public decimal AcidezTotal { get; set; }

        public decimal Cice { get; set; }

        public decimal SaturacionActual { get; set; }

        public decimal SaturacionDeseada { get; set; }

        public decimal Prnt { get; set; }

        public decimal NecesidadEncaladoTonHa { get; set; }

        public decimal NecesidadEncaladoKgHa { get; set; }

        public decimal NecesidadEncaladoLbHa { get; set; }

        public decimal NecesidadEncaladoLbMz { get; set; }

        public decimal DosisPlantaAnualOz { get; set; }

        public decimal DosisPlantaPorAplicacionOz { get; set; }
    }

    public sealed class AnalisisReporteFertilizacionMixta
    {
        public string Observacion { get; set; } = string.Empty;

        public List<AnalisisReporteMixtaFuente> Fuentes { get; set; } = new();

        public List<AnalisisReporteMixtaDetalle> Detalles { get; set; } = new();
    }

    public sealed class AnalisisReporteMixtaFuente
    {
        public string Fuente { get; set; } = string.Empty;

        public decimal CantidadQq { get; set; }
    }

    public sealed class AnalisisReporteMixtaDetalle
    {
        public string Elemento { get; set; } = string.Empty;

        public decimal RequerimientoOriginal { get; set; }

        public decimal AporteOrganico { get; set; }

        public decimal Diferencia { get; set; }

        public decimal Deficit { get; set; }

        public decimal Sobrante { get; set; }
    }
}
