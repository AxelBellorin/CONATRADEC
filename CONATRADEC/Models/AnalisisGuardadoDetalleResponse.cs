using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CONATRADEC.Models
{
    public sealed class AnalisisGuardadoDetalleResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("data")]
        public AnalisisGuardadoDetalleData? Data { get; set; }
    }

    public sealed class AnalisisGuardadoDetalleData
    {
        [JsonPropertyName("datosAnalisis")]
        public AnalisisGuardadoDatosAnalisis DatosAnalisis { get; set; } = new();

        [JsonPropertyName("requerimientoAnual")]
        public AnalisisGuardadoRequerimientoAnual RequerimientoAnual { get; set; } = new();

        [JsonPropertyName("balanceNutricional")]
        public AnalisisGuardadoBalanceNutricional? BalanceNutricional { get; set; }

        [JsonPropertyName("enmiendaCalcarea")]
        public AnalisisGuardadoEnmiendaCalcarea? EnmiendaCalcarea { get; set; }

        [JsonPropertyName("fertilizacionMixta")]
        public AnalisisGuardadoFertilizacionMixta? FertilizacionMixta { get; set; }
    }

    public sealed class AnalisisGuardadoDatosAnalisis
    {
        [JsonPropertyName("analisisSueloId")]
        public int AnalisisSueloId { get; set; }

        [JsonPropertyName("fechaAnalisisSuelo")]
        public string? FechaAnalisisSuelo { get; set; }

        [JsonPropertyName("fechaCreacionAnalisisSuelo")]
        public string? FechaCreacionAnalisisSuelo { get; set; }

        [JsonPropertyName("laboratorioAnalasisSuelo")]
        public string LaboratorioAnalasisSuelo { get; set; } = string.Empty;

        [JsonPropertyName("identificadorAnalisisSuelo")]
        public string IdentificadorAnalisisSuelo { get; set; } = string.Empty;

        [JsonPropertyName("usuarioId")]
        public int? UsuarioId { get; set; }

        [JsonPropertyName("elementosQuimicos")]
        public List<AnalisisGuardadoElementoOriginal> ElementosQuimicos { get; set; } = new();

        [JsonIgnore]
        public DateTime? FechaAnalisisValor => ConvertirFecha(FechaAnalisisSuelo);

        [JsonIgnore]
        public string FechaAnalisisTexto => FechaAnalisisValor?.ToString("dd/MM/yyyy") ?? "No disponible";

        private static DateTime? ConvertirFecha(string? valor) =>
            DateTime.TryParse(valor, out DateTime fecha) ? fecha : null;
    }

    public sealed class AnalisisGuardadoElementoOriginal
    {
        [JsonPropertyName("analisisSueloElementoQuimicoId")]
        public int AnalisisSueloElementoQuimicoId { get; set; }

        [JsonPropertyName("elementoQuimicosId")]
        public int ElementoQuimicosId { get; set; }

        [JsonPropertyName("unidadMedidaId")]
        public int UnidadMedidaId { get; set; }

        [JsonPropertyName("cantidadElemento")]
        public decimal CantidadElemento { get; set; }

        [JsonIgnore]
        public string NombreElemento { get; set; } = string.Empty;

        [JsonIgnore]
        public string SimboloElemento { get; set; } = string.Empty;

        [JsonIgnore]
        public string NombreUnidad { get; set; } = string.Empty;

        [JsonIgnore]
        public string ElementoMostrar =>
            !string.IsNullOrWhiteSpace(SimboloElemento)
                ? $"{NombreElemento} ({SimboloElemento})"
                : !string.IsNullOrWhiteSpace(NombreElemento)
                    ? NombreElemento
                    : $"Elemento #{ElementoQuimicosId}";

        [JsonIgnore]
        public string UnidadMostrar =>
            string.IsNullOrWhiteSpace(NombreUnidad)
                ? $"Unidad #{UnidadMedidaId}"
                : NombreUnidad;
    }

    public sealed class AnalisisGuardadoRequerimientoAnual
    {
        [JsonPropertyName("analisisSueloCalculoId")]
        public int AnalisisSueloCalculoId { get; set; }

        [JsonPropertyName("terrenoId")]
        public int TerrenoId { get; set; }

        [JsonPropertyName("tipoCultivoId")]
        public int TipoCultivoId { get; set; }

        [JsonPropertyName("tipoAnalisisSueloId")]
        public int TipoAnalisisSueloId { get; set; }

        [JsonPropertyName("cantidadQuintalesOro")]
        public decimal CantidadQuintalesOro { get; set; }

        [JsonPropertyName("tamanoFinca")]
        public decimal TamanoFinca { get; set; }

        [JsonPropertyName("ph")]
        public decimal Ph { get; set; }

        [JsonPropertyName("materiaOrganica")]
        public decimal? MateriaOrganica { get; set; }

        [JsonPropertyName("acidezTotal")]
        public decimal? AcidezTotal { get; set; }

        [JsonPropertyName("unidadMedidaMateriaOrganicaId")]
        public int? UnidadMedidaMateriaOrganicaId { get; set; }

        [JsonPropertyName("recomendacionGeneral")]
        public string RecomendacionGeneral { get; set; } = string.Empty;

        [JsonPropertyName("observaciones")]
        public List<string> Observaciones { get; set; } = new();

        [JsonPropertyName("elementos")]
        public List<AnalisisGuardadoRequerimientoElemento> Elementos { get; set; } = new();
    }

    public sealed class AnalisisGuardadoRequerimientoElemento
    {
        [JsonPropertyName("analisisSueloCalculoElementoQuimicoId")]
        public int AnalisisSueloCalculoElementoQuimicoId { get; set; }

        [JsonPropertyName("elementoQuimicosId")]
        public int ElementoQuimicosId { get; set; }

        [JsonPropertyName("unidadMedidaId")]
        public int? UnidadMedidaId { get; set; }

        [JsonPropertyName("cantidadIngresada")]
        public decimal CantidadIngresada { get; set; }

        [JsonPropertyName("cantidadConvertidaLbMz")]
        public decimal? CantidadConvertidaLbMz { get; set; }

        [JsonPropertyName("requerimientoCalculado")]
        public decimal? RequerimientoCalculado { get; set; }

        [JsonPropertyName("clasificacion")]
        public string? Clasificacion { get; set; }

        [JsonPropertyName("observacion")]
        public string? Observacion { get; set; }

        [JsonIgnore]
        public string NombreElemento { get; set; } = string.Empty;

        [JsonIgnore]
        public string SimboloElemento { get; set; } = string.Empty;

        [JsonIgnore]
        public string NombreUnidad { get; set; } = string.Empty;

        [JsonIgnore]
        public string ElementoMostrar =>
            !string.IsNullOrWhiteSpace(SimboloElemento)
                ? $"{NombreElemento} ({SimboloElemento})"
                : !string.IsNullOrWhiteSpace(NombreElemento)
                    ? NombreElemento
                    : $"Elemento #{ElementoQuimicosId}";

        [JsonIgnore]
        public string UnidadMostrar =>
            string.IsNullOrWhiteSpace(NombreUnidad)
                ? UnidadMedidaId.HasValue
                    ? $"Unidad #{UnidadMedidaId.Value}"
                    : "lb/mz"
                : NombreUnidad;
    }

    public sealed class AnalisisGuardadoBalanceNutricional
    {
        [JsonPropertyName("formula")]
        public AnalisisGuardadoFormula Formula { get; set; } = new();

        [JsonPropertyName("detalles")]
        public List<AnalisisGuardadoFormulaDetalle> Detalles { get; set; } = new();

        [JsonPropertyName("aportes")]
        public List<AnalisisGuardadoFormulaAporte> Aportes { get; set; } = new();
    }

    public sealed class AnalisisGuardadoFormula
    {
        [JsonPropertyName("formulaNutricionalId")]
        public int FormulaNutricionalId { get; set; }

        [JsonPropertyName("nombreFormula")]
        public string NombreFormula { get; set; } = string.Empty;

        [JsonPropertyName("fechaCreacion")]
        public string? FechaCreacion { get; set; }

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

        [JsonPropertyName("terrenoId")]
        public int? TerrenoId { get; set; }
    }

    public sealed class AnalisisGuardadoFormulaDetalle
    {
        [JsonPropertyName("formulaNutricionalDetalleId")]
        public int FormulaNutricionalDetalleId { get; set; }

        [JsonPropertyName("fuenteNutrientesId")]
        public int FuenteNutrientesId { get; set; }

        [JsonPropertyName("elementoQuimicosId")]
        public int ElementoQuimicosId { get; set; }

        [JsonPropertyName("libras")]
        public decimal Libras { get; set; }

        [JsonPropertyName("qq")]
        public decimal Qq { get; set; }

        [JsonPropertyName("requerimientoLibras")]
        public decimal RequerimientoLibras { get; set; }

        [JsonPropertyName("precioPorQuintal")]
        public decimal PrecioPorQuintal { get; set; }

        [JsonPropertyName("subtotalFuente")]
        public decimal SubtotalFuente { get; set; }

        [JsonPropertyName("onzasAnuales")]
        public decimal OnzasAnuales { get; set; }

        [JsonPropertyName("onzasPorAplicacion")]
        public decimal OnzasPorAplicacion { get; set; }

        [JsonIgnore]
        public string NombreFuente { get; set; } = string.Empty;

        [JsonIgnore]
        public string NombreElemento { get; set; } = string.Empty;

        [JsonIgnore]
        public string FuenteMostrar =>
            string.IsNullOrWhiteSpace(NombreFuente)
                ? $"Fuente #{FuenteNutrientesId}"
                : NombreFuente;

        [JsonIgnore]
        public string ElementoMostrar =>
            string.IsNullOrWhiteSpace(NombreElemento)
                ? $"Elemento #{ElementoQuimicosId}"
                : NombreElemento;
    }

    public sealed class AnalisisGuardadoFormulaAporte
    {
        [JsonPropertyName("formulaNutricionalAporteId")]
        public int FormulaNutricionalAporteId { get; set; }

        [JsonPropertyName("formulaNutricionalDetalleId")]
        public int FormulaNutricionalDetalleId { get; set; }

        [JsonPropertyName("elementoQuimicosId")]
        public int ElementoQuimicosId { get; set; }

        [JsonPropertyName("valor")]
        public decimal Valor { get; set; }
    }

    public sealed class AnalisisGuardadoEnmiendaCalcarea
    {
        [JsonPropertyName("enmiendaCalcareaId")]
        public int EnmiendaCalcareaId { get; set; }

        [JsonPropertyName("nombreAnalisis")]
        public string NombreAnalisis { get; set; } = string.Empty;

        [JsonPropertyName("fuenteNutrientesId")]
        public int FuenteNutrientesId { get; set; }

        [JsonPropertyName("terrenoId")]
        public int? TerrenoId { get; set; }

        [JsonPropertyName("totalPlantas")]
        public int TotalPlantas { get; set; }

        [JsonPropertyName("totalAplicaciones")]
        public int TotalAplicaciones { get; set; }

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

        [JsonPropertyName("necesidadEncaladoLbMz")]
        public decimal NecesidadEncaladoLbMz { get; set; }

        [JsonPropertyName("necesidadEncaladoOzMz")]
        public decimal NecesidadEncaladoOzMz { get; set; }

        [JsonPropertyName("dosisPlantaAnualOz")]
        public decimal DosisPlantaAnualOz { get; set; }

        [JsonPropertyName("dosisPlantaPorAplicacionOz")]
        public decimal DosisPlantaPorAplicacionOz { get; set; }

        [JsonPropertyName("fechaCreacion")]
        public string? FechaCreacion { get; set; }

        [JsonIgnore]
        public string NombreFuente { get; set; } = string.Empty;

        [JsonIgnore]
        public string FuenteMostrar =>
            string.IsNullOrWhiteSpace(NombreFuente)
                ? $"Fuente #{FuenteNutrientesId}"
                : NombreFuente;
    }

    public sealed class AnalisisGuardadoFertilizacionMixta
    {
        [JsonPropertyName("mixta")]
        public AnalisisGuardadoMixtaCabecera Mixta { get; set; } = new();

        [JsonPropertyName("fuentes")]
        public List<AnalisisGuardadoMixtaFuente> Fuentes { get; set; } = new();

        [JsonPropertyName("detalles")]
        public List<AnalisisGuardadoMixtaDetalle> Detalles { get; set; } = new();
    }

    public sealed class AnalisisGuardadoMixtaCabecera
    {
        [JsonPropertyName("fertilizacionMixtaId")]
        public int FertilizacionMixtaId { get; set; }

        [JsonPropertyName("fechaCalculo")]
        public string? FechaCalculo { get; set; }

        [JsonPropertyName("observacion")]
        public string Observacion { get; set; } = string.Empty;

        [JsonPropertyName("esComplementoBalance")]
        public bool EsComplementoBalance { get; set; }
    }

    public sealed class AnalisisGuardadoMixtaFuente
    {
        [JsonPropertyName("fertilizacionMixtaFuenteId")]
        public int FertilizacionMixtaFuenteId { get; set; }

        [JsonPropertyName("fuenteNutrientesId")]
        public int FuenteNutrientesId { get; set; }

        [JsonPropertyName("cantidadQq")]
        public decimal CantidadQq { get; set; }

        [JsonIgnore]
        public string NombreFuente { get; set; } = string.Empty;

        [JsonIgnore]
        public string FuenteMostrar =>
            string.IsNullOrWhiteSpace(NombreFuente)
                ? $"Fuente #{FuenteNutrientesId}"
                : NombreFuente;
    }

    public sealed class AnalisisGuardadoMixtaDetalle
    {
        [JsonPropertyName("fertilizacionMixtaDetalleId")]
        public int FertilizacionMixtaDetalleId { get; set; }

        [JsonPropertyName("elementoQuimicosId")]
        public int ElementoQuimicosId { get; set; }

        [JsonPropertyName("requerimientoOriginal")]
        public decimal RequerimientoOriginal { get; set; }

        [JsonPropertyName("aporteOrganico")]
        public decimal AporteOrganico { get; set; }

        [JsonPropertyName("diferencia")]
        public decimal Diferencia { get; set; }

        [JsonPropertyName("deficit")]
        public decimal Deficit { get; set; }

        [JsonPropertyName("sobrante")]
        public decimal Sobrante { get; set; }

        [JsonIgnore]
        public string NombreElemento { get; set; } = string.Empty;
    }

    public sealed class CatalogoElementoAnalisis
    {
        [JsonPropertyName("elementoQuimicosId")]
        public int? ElementoQuimicosId { get; set; }

        [JsonPropertyName("simboloElementoQuimico")]
        public string SimboloElementoQuimico { get; set; } = string.Empty;

        [JsonPropertyName("nombreElementoQuimico")]
        public string NombreElementoQuimico { get; set; } = string.Empty;
    }

    public sealed class CatalogoFuenteAnalisis
    {
        [JsonPropertyName("fuenteNutrientesId")]
        public int? FuenteNutrientesId { get; set; }

        [JsonPropertyName("nombreNutriente")]
        public string NombreNutriente { get; set; } = string.Empty;
    }
}
