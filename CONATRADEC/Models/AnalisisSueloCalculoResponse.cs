using System.Collections.Generic;

namespace CONATRADEC.Models
{
    public class AnalisisSueloCalculoResponse
    {
        public bool Success { get; set; }

        public string? Message { get; set; }

        public AnalisisSueloCalculoDataResponse? Data { get; set; }
    }

    public class AnalisisSueloCalculoDataResponse
    {
        public int? TerrenoId { get; set; }

        public int? TipoCultivoId { get; set; }

        public string? TipoCultivo { get; set; }

        public int? TipoAnalisisSueloId { get; set; }

        public string? TipoAnalisisSuelo { get; set; }

        public decimal? CantidadQuintalesOro { get; set; }

        public decimal? TamanoFinca { get; set; }

        public decimal? Ph { get; set; }

        public decimal? AcidezTotal { get; set; }

        public List<ElementoResultadoCalculoResponse> Elementos { get; set; } = new();

        public List<object> FuentesFertilizantes { get; set; } = new();

        public object? EnmiendaCalcarea { get; set; }

        public List<object> FuentesOrganicas { get; set; } = new();

        public string? RecomendacionGeneral { get; set; }

        public List<string> Observaciones { get; set; } = new();
    }

    public class ElementoResultadoCalculoResponse
    {
        private string? simboloElementoQuimico;
        private string? nombreElementoQuimico;

        public int? ElementoQuimicosId { get; set; }

        public string? SimboloElementoQuimico
        {
            get => simboloElementoQuimico;
            set => simboloElementoQuimico = LimpiarTexto(value);
        }

        public string? NombreElementoQuimico
        {
            get => nombreElementoQuimico;
            set => nombreElementoQuimico = LimpiarTexto(value);
        }

        public decimal? CantidadIngresada { get; set; }

        public decimal? CantidadConvertidaLbMz { get; set; }

        public decimal? ExtraccionPorQQOro { get; set; }

        public decimal? ExtraccionPorProduccion { get; set; }

        public decimal? RangoMinimo { get; set; }

        public decimal? RangoMaximo { get; set; }

        public decimal? RangoMinimoLbMz { get; set; }

        public decimal? RangoMaximoLbMz { get; set; }

        public decimal? RequerimientoCalculado { get; set; }

        public string? UnidadBase { get; set; }

        public int? UnidadMedidaResultadoId { get; set; }

        public string? UnidadResultado { get; set; }

        public string? Clasificacion { get; set; }

        public string? Observacion { get; set; }

        private static string? LimpiarTexto(string? valor)
        {
            if (string.IsNullOrWhiteSpace(valor))
                return null;

            return valor.Trim();
        }
    }
}
