using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CONATRADEC.Models
{
    public sealed class AnalisisGuardadoUsuarioListaResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("data")]
        public List<AnalisisGuardadoUsuarioItem> Data { get; set; } = new();
    }

    public sealed class AnalisisGuardadoUsuarioItem
    {
        [JsonPropertyName("analisisSueloId")]
        public int AnalisisSueloId { get; set; }

        [JsonPropertyName("fechaAnalisisSuelo")]
        public string? FechaAnalisisSuelo { get; set; }

        [JsonPropertyName("laboratorioAnalasisSuelo")]
        public string LaboratorioAnalasisSuelo { get; set; } = string.Empty;

        [JsonPropertyName("identificadorAnalisisSuelo")]
        public string IdentificadorAnalisisSuelo { get; set; } = string.Empty;

        [JsonPropertyName("activo")]
        public bool Activo { get; set; }

        [JsonPropertyName("terreno")]
        public AnalisisGuardadoUsuarioTerreno? Terreno { get; set; }

        [JsonPropertyName("tipoCultivo")]
        public AnalisisGuardadoUsuarioTipoCultivo? TipoCultivo { get; set; }

        [JsonPropertyName("tipoAnalisisSuelo")]
        public AnalisisGuardadoUsuarioTipoAnalisis? TipoAnalisisSuelo { get; set; }

        [JsonPropertyName("calculo")]
        public AnalisisGuardadoUsuarioCalculo? Calculo { get; set; }

        [JsonPropertyName("totalElementosIngresados")]
        public int TotalElementosIngresados { get; set; }

        [JsonPropertyName("totalElementosCalculados")]
        public int TotalElementosCalculados { get; set; }
    }

    public sealed class AnalisisGuardadoUsuarioTerreno
    {
        [JsonPropertyName("terrenoId")]
        public int TerrenoId { get; set; }

        [JsonPropertyName("codigoTerreno")]
        public string CodigoTerreno { get; set; } = string.Empty;

        [JsonPropertyName("nombrePropietarioTerreno")]
        public string NombrePropietarioTerreno { get; set; } = string.Empty;

        [JsonPropertyName("extensionManzanaTerreno")]
        public decimal ExtensionManzanaTerreno { get; set; }

        [JsonPropertyName("cantidadQuintalesOro")]
        public decimal CantidadQuintalesOro { get; set; }
    }

    public sealed class AnalisisGuardadoUsuarioTipoCultivo
    {
        [JsonPropertyName("tipoCultivoId")]
        public int TipoCultivoId { get; set; }

        [JsonPropertyName("nombreTipoCultivo")]
        public string NombreTipoCultivo { get; set; } = string.Empty;
    }

    public sealed class AnalisisGuardadoUsuarioTipoAnalisis
    {
        [JsonPropertyName("tipoAnalisisSueloId")]
        public int TipoAnalisisSueloId { get; set; }

        [JsonPropertyName("nombreTipoAnalisisSuelo")]
        public string NombreTipoAnalisisSuelo { get; set; } = string.Empty;
    }

    public sealed class AnalisisGuardadoUsuarioCalculo
    {
        [JsonPropertyName("analisisSueloCalculoId")]
        public int AnalisisSueloCalculoId { get; set; }

        [JsonPropertyName("cantidadQuintalesOro")]
        public decimal CantidadQuintalesOro { get; set; }

        [JsonPropertyName("tamanoFinca")]
        public decimal TamanoFinca { get; set; }

        [JsonPropertyName("phAnalisisSuelo")]
        public decimal PhAnalisisSuelo { get; set; }

        [JsonPropertyName("acidezTotal")]
        public decimal? AcidezTotal { get; set; }

        [JsonPropertyName("recomendacionGeneral")]
        public string RecomendacionGeneral { get; set; } = string.Empty;

        [JsonPropertyName("fechaCalculo")]
        public string? FechaCalculo { get; set; }

        [JsonPropertyName("usuarioId")]
        public int? UsuarioId { get; set; }
    }

    public sealed class UsuarioFiltroAnalisis
    {
        public int? UsuarioId { get; set; }

        public string NombreCompleto { get; set; } = string.Empty;

        public string TextoMostrar =>
            UsuarioId.HasValue
                ? NombreCompleto
                : "Todos los usuarios";
    }
}
