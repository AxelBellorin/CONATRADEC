using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CONATRADEC.Models
{
    public sealed class GuardarTodoRequest
    {
        [JsonPropertyName("datosAnalisis")]
        public GuardarTodoDatosAnalisisRequest DatosAnalisis { get; set; } = new();

        [JsonPropertyName("requerimientoAnual")]
        public GuardarTodoRequerimientoAnualRequest RequerimientoAnual { get; set; } = new();

        [JsonPropertyName("balanceNutricional")]
        public GuardarTodoBalanceNutricionalRequest? BalanceNutricional { get; set; }

        [JsonPropertyName("enmiendaCalcarea")]
        public GuardarTodoEnmiendaCalcareaRequest? EnmiendaCalcarea { get; set; }

        [JsonPropertyName("fertilizacionMixta")]
        public GuardarTodoFertilizacionMixtaRequest? FertilizacionMixta { get; set; }
    }

    public sealed class GuardarTodoDatosAnalisisRequest
    {
        [JsonPropertyName("terrenoId")]
        public int TerrenoId { get; set; }

        [JsonPropertyName("tipoCultivoId")]
        public int TipoCultivoId { get; set; }

        [JsonPropertyName("tipoAnalisisSueloId")]
        public int TipoAnalisisSueloId { get; set; }

        [JsonPropertyName("usuarioId")]
        public int? UsuarioId { get; set; }

        [JsonPropertyName("cantidadQuintalesOro")]
        public decimal CantidadQuintalesOro { get; set; }

        [JsonPropertyName("tamanoFinca")]
        public decimal TamanoFinca { get; set; }

        [JsonPropertyName("ph")]
        public decimal Ph { get; set; }

        [JsonPropertyName("materiaOrganica")]
        public decimal MateriaOrganica { get; set; }

        [JsonPropertyName("unidadMedidaMateriaOrganicaId")]
        public int UnidadMedidaMateriaOrganicaId { get; set; }

        [JsonPropertyName("acidezTotal")]
        public decimal? AcidezTotal { get; set; }

        [JsonPropertyName("elementosQuimicos")]
        public List<GuardarTodoElementoAnalisisRequest> ElementosQuimicos { get; set; } = new();

        [JsonPropertyName("fechaAnalisisSuelo")]
        public string FechaAnalisisSuelo { get; set; } = string.Empty;

        [JsonPropertyName("laboratorioAnalasisSuelo")]
        public string LaboratorioAnalasisSuelo { get; set; } = string.Empty;

        [JsonPropertyName("identificadorAnalisisSuelo")]
        public string IdentificadorAnalisisSuelo { get; set; } = string.Empty;
    }

    public sealed class GuardarTodoElementoAnalisisRequest
    {
        [JsonPropertyName("elementoQuimicosId")]
        public int ElementoQuimicosId { get; set; }

        [JsonPropertyName("unidadMedidaId")]
        public int UnidadMedidaId { get; set; }

        [JsonPropertyName("cantidadElemento")]
        public decimal CantidadElemento { get; set; }
    }

    public sealed class GuardarTodoRequerimientoAnualRequest
    {
        [JsonPropertyName("terrenoId")]
        public int TerrenoId { get; set; }

        [JsonPropertyName("tipoCultivoId")]
        public int TipoCultivoId { get; set; }

        [JsonPropertyName("tipoCultivo")]
        public string TipoCultivo { get; set; } = string.Empty;

        [JsonPropertyName("tipoAnalisisSueloId")]
        public int TipoAnalisisSueloId { get; set; }

        [JsonPropertyName("tipoAnalisisSuelo")]
        public string TipoAnalisisSuelo { get; set; } = string.Empty;

        [JsonPropertyName("cantidadQuintalesOro")]
        public decimal CantidadQuintalesOro { get; set; }

        [JsonPropertyName("tamanoFinca")]
        public decimal TamanoFinca { get; set; }

        [JsonPropertyName("ph")]
        public decimal Ph { get; set; }

        [JsonPropertyName("acidezTotal")]
        public decimal? AcidezTotal { get; set; }

        [JsonPropertyName("materiaOrganica")]
        public decimal MateriaOrganica { get; set; }

        [JsonPropertyName("unidadMedidaMateriaOrganicaId")]
        public int UnidadMedidaMateriaOrganicaId { get; set; }

        [JsonPropertyName("elementos")]
        public List<GuardarTodoRequerimientoElementoRequest> Elementos { get; set; } = new();

        [JsonPropertyName("recomendacionGeneral")]
        public string RecomendacionGeneral { get; set; } = string.Empty;

        [JsonPropertyName("observaciones")]
        public List<string> Observaciones { get; set; } = new();
    }

    public sealed class GuardarTodoRequerimientoElementoRequest
    {
        [JsonPropertyName("elementoQuimicosId")]
        public int ElementoQuimicosId { get; set; }

        [JsonPropertyName("simboloElementoQuimico")]
        public string SimboloElementoQuimico { get; set; } = string.Empty;

        [JsonPropertyName("nombreElementoQuimico")]
        public string NombreElementoQuimico { get; set; } = string.Empty;

        [JsonPropertyName("cantidadIngresada")]
        public decimal CantidadIngresada { get; set; }

        [JsonPropertyName("cantidadConvertidaLbMz")]
        public decimal? CantidadConvertidaLbMz { get; set; }

        [JsonPropertyName("extraccionPorQQOro")]
        public decimal? ExtraccionPorQQOro { get; set; }

        [JsonPropertyName("extraccionPorProduccion")]
        public decimal? ExtraccionPorProduccion { get; set; }

        [JsonPropertyName("rangoMinimo")]
        public decimal? RangoMinimo { get; set; }

        [JsonPropertyName("rangoMaximo")]
        public decimal? RangoMaximo { get; set; }

        [JsonPropertyName("rangoMinimoLbMz")]
        public decimal? RangoMinimoLbMz { get; set; }

        [JsonPropertyName("rangoMaximoLbMz")]
        public decimal? RangoMaximoLbMz { get; set; }

        [JsonPropertyName("requerimientoCalculado")]
        public decimal? RequerimientoCalculado { get; set; }

        [JsonPropertyName("unidadBase")]
        public string UnidadBase { get; set; } = string.Empty;

        [JsonPropertyName("unidadMedidaResultadoId")]
        public int? UnidadMedidaResultadoId { get; set; }

        [JsonPropertyName("unidadResultado")]
        public string UnidadResultado { get; set; } = string.Empty;

        [JsonPropertyName("clasificacion")]
        public string Clasificacion { get; set; } = string.Empty;

        [JsonPropertyName("observacion")]
        public string Observacion { get; set; } = string.Empty;
    }

    public sealed class GuardarTodoBalanceNutricionalRequest
    {
        [JsonPropertyName("terrenoId")]
        public int TerrenoId { get; set; }

        [JsonPropertyName("resultado")]
        public GuardarTodoBalanceResultadoRequest Resultado { get; set; } = new();

        [JsonPropertyName("items")]
        public List<GuardarTodoBalanceItemRequest> Items { get; set; } = new();
    }

    public sealed class GuardarTodoBalanceItemRequest
    {
        [JsonPropertyName("fuenteNutrientesId")]
        public int FuenteNutrientesId { get; set; }

        [JsonPropertyName("elementoQuimicosId")]
        public int ElementoQuimicosId { get; set; }

        [JsonPropertyName("libras")]
        public decimal Libras { get; set; }
    }

    public sealed class GuardarTodoBalanceResultadoRequest
    {
        [JsonPropertyName("nombreFormula")]
        public string NombreFormula { get; set; } = string.Empty;

        [JsonPropertyName("totalLibras")]
        public decimal TotalLibras { get; set; }

        [JsonPropertyName("mezclaTotalQq")]
        public decimal MezclaTotalQq { get; set; }

        [JsonPropertyName("totalPlantas")]
        public int TotalPlantas { get; set; }

        [JsonPropertyName("totalAplicaciones")]
        public int TotalAplicaciones { get; set; }

        [JsonPropertyName("totalOnzas")]
        public decimal TotalOnzas { get; set; }

        [JsonPropertyName("precioTotalFormula")]
        public decimal PrecioTotalFormula { get; set; }

        [JsonPropertyName("precioPorAplicacion")]
        public decimal PrecioPorAplicacion { get; set; }

        [JsonPropertyName("dosisPlantaAnualOz")]
        public decimal DosisPlantaAnualOz { get; set; }

        [JsonPropertyName("dosisPlantaPorAplicacionOz")]
        public decimal DosisPlantaPorAplicacionOz { get; set; }

        [JsonPropertyName("formulaComercial")]
        public Dictionary<string, decimal> FormulaComercial { get; set; } = new();

        [JsonPropertyName("detalle")]
        public List<GuardarTodoBalanceDetalleRequest> Detalle { get; set; } = new();
    }

    public sealed class GuardarTodoBalanceDetalleRequest
    {
        [JsonPropertyName("fuente")]
        public string Fuente { get; set; } = string.Empty;

        [JsonPropertyName("elemento")]
        public string Elemento { get; set; } = string.Empty;

        [JsonPropertyName("lb")]
        public decimal Lb { get; set; }

        [JsonPropertyName("qq")]
        public decimal Qq { get; set; }

        [JsonPropertyName("requerimientoLibras")]
        public decimal RequerimientoLibras { get; set; }

        [JsonPropertyName("librasPorAplicacion")]
        public decimal LibrasPorAplicacion { get; set; }

        [JsonPropertyName("onzasAnuales")]
        public decimal OnzasAnuales { get; set; }

        [JsonPropertyName("onzasPorAplicacion")]
        public decimal OnzasPorAplicacion { get; set; }

        [JsonPropertyName("precioPorQuintal")]
        public decimal PrecioPorQuintal { get; set; }

        [JsonPropertyName("subtotalFuente")]
        public decimal SubtotalFuente { get; set; }

        [JsonPropertyName("aportes")]
        public Dictionary<string, decimal> Aportes { get; set; } = new();
    }

    public sealed class GuardarTodoEnmiendaCalcareaRequest
    {
        [JsonPropertyName("fuenteNutrientesId")]
        public int FuenteNutrientesId { get; set; }

        [JsonPropertyName("resultado")]
        public GuardarTodoEnmiendaResultadoRequest Resultado { get; set; } = new();
    }

    public sealed class GuardarTodoEnmiendaResultadoRequest
    {
        [JsonPropertyName("enmiendaCalcareaId")]
        public int EnmiendaCalcareaId { get; set; }

        [JsonPropertyName("nombreAnalisis")]
        public string NombreAnalisis { get; set; } = string.Empty;

        [JsonPropertyName("fuenteNutriente")]
        public string FuenteNutriente { get; set; } = string.Empty;

        [JsonPropertyName("ph")]
        public decimal Ph { get; set; }

        [JsonPropertyName("ca")]
        public decimal Ca { get; set; }

        [JsonPropertyName("mg")]
        public decimal Mg { get; set; }

        [JsonPropertyName("k")]
        public decimal K { get; set; }

        [JsonPropertyName("acidezTotal")]
        public decimal AcidezTotal { get; set; }

        [JsonPropertyName("saturacionDeseada")]
        public decimal SaturacionDeseada { get; set; }

        [JsonPropertyName("prnt")]
        public decimal Prnt { get; set; }

        [JsonPropertyName("sumaBases")]
        public decimal SumaBases { get; set; }

        [JsonPropertyName("cice")]
        public decimal Cice { get; set; }

        [JsonPropertyName("saturacionActual")]
        public decimal SaturacionActual { get; set; }

        [JsonPropertyName("necesidadEncaladoTonHa")]
        public decimal NecesidadEncaladoTonHa { get; set; }

        [JsonPropertyName("necesidadEncaladoKgHa")]
        public decimal NecesidadEncaladoKgHa { get; set; }

        [JsonPropertyName("necesidadEncaladoLbHa")]
        public decimal NecesidadEncaladoLbHa { get; set; }

        [JsonPropertyName("terrenoId")]
        public int? TerrenoId { get; set; }

        [JsonPropertyName("totalPlantas")]
        public int TotalPlantas { get; set; }

        [JsonPropertyName("totalAplicaciones")]
        public int TotalAplicaciones { get; set; }

        [JsonPropertyName("necesidadEncaladoLbMz")]
        public decimal NecesidadEncaladoLbMz { get; set; }

        [JsonPropertyName("necesidadEncaladoOzMz")]
        public decimal NecesidadEncaladoOzMz { get; set; }

        [JsonPropertyName("dosisPlantaAnualOz")]
        public decimal DosisPlantaAnualOz { get; set; }

        [JsonPropertyName("dosisPlantaPorAplicacionOz")]
        public decimal DosisPlantaPorAplicacionOz { get; set; }
    }

    public sealed class GuardarTodoFertilizacionMixtaRequest
    {
        [JsonPropertyName("observacion")]
        public string Observacion { get; set; } = string.Empty;

        [JsonPropertyName("fuentes")]
        public List<GuardarTodoFertilizacionMixtaFuenteRequest> Fuentes { get; set; } = new();

        [JsonPropertyName("detalles")]
        public List<GuardarTodoFertilizacionMixtaDetalleRequest> Detalles { get; set; } = new();
    }

    public sealed class GuardarTodoFertilizacionMixtaFuenteRequest
    {
        [JsonPropertyName("fuenteNutrientesId")]
        public int FuenteNutrientesId { get; set; }

        [JsonPropertyName("nombreFuente")]
        public string NombreFuente { get; set; } = string.Empty;

        [JsonPropertyName("cantidadQq")]
        public decimal CantidadQq { get; set; }
    }

    public sealed class GuardarTodoFertilizacionMixtaDetalleRequest
    {
        [JsonPropertyName("elementoQuimicosId")]
        public int ElementoQuimicosId { get; set; }

        [JsonPropertyName("elemento")]
        public string Elemento { get; set; } = string.Empty;

        [JsonPropertyName("exportable")]
        public decimal Exportable { get; set; }

        [JsonPropertyName("aporteOrganico")]
        public decimal AporteOrganico { get; set; }

        [JsonPropertyName("diferencia")]
        public decimal Diferencia { get; set; }

        [JsonPropertyName("deficit")]
        public decimal Deficit { get; set; }

        [JsonPropertyName("sobrante")]
        public decimal Sobrante { get; set; }

        [JsonPropertyName("fuentes")]
        public List<GuardarTodoFertilizacionMixtaFuenteDetalleRequest> Fuentes { get; set; } = new();
    }

    public sealed class GuardarTodoFertilizacionMixtaFuenteDetalleRequest
    {
        [JsonPropertyName("fuenteNutrientesId")]
        public int FuenteNutrientesId { get; set; }

        [JsonPropertyName("nombreFuente")]
        public string NombreFuente { get; set; } = string.Empty;

        [JsonPropertyName("cantidadQq")]
        public decimal CantidadQq { get; set; }

        [JsonPropertyName("aportePorUnidad")]
        public decimal AportePorUnidad { get; set; }

        [JsonPropertyName("aporteTotal")]
        public decimal AporteTotal { get; set; }
    }
}
