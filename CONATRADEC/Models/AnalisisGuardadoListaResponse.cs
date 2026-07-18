using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CONATRADEC.Models
{
    public sealed class AnalisisGuardadoListaResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("total")]
        public int Total { get; set; }

        [JsonPropertyName("data")]
        public List<AnalisisGuardadoResumen> Data { get; set; } = new();
    }

    public sealed class AnalisisGuardadoResumen
    {
        [JsonPropertyName("analisisSueloCalculoId")]
        public int AnalisisSueloCalculoId { get; set; }

        [JsonPropertyName("analisisSueloId")]
        public int AnalisisSueloId { get; set; }

        [JsonPropertyName("identificadorAnalisisSuelo")]
        public string IdentificadorAnalisisSuelo { get; set; } = string.Empty;

        [JsonPropertyName("laboratorioAnalasisSuelo")]
        public string LaboratorioAnalasisSuelo { get; set; } = string.Empty;

        [JsonPropertyName("fechaAnalisisSuelo")]
        public string? FechaAnalisisSuelo { get; set; }

        [JsonPropertyName("fechaCalculo")]
        public string? FechaCalculo { get; set; }

        [JsonPropertyName("terrenoId")]
        public int TerrenoId { get; set; }

        [JsonPropertyName("codigoTerreno")]
        public string CodigoTerreno { get; set; } = string.Empty;

        [JsonPropertyName("nombreCliente")]
        public string NombreCliente { get; set; } = string.Empty;

        [JsonPropertyName("nombreTerreno")]
        public string NombreTerreno { get; set; } = string.Empty;

        [JsonPropertyName("tipoCultivoId")]
        public int TipoCultivoId { get; set; }

        [JsonPropertyName("tipoAnalisisSueloId")]
        public int TipoAnalisisSueloId { get; set; }

        [JsonPropertyName("cantidadQuintalesOro")]
        public decimal CantidadQuintalesOro { get; set; }

        [JsonPropertyName("tamanoFinca")]
        public decimal TamanoFinca { get; set; }

        [JsonPropertyName("phAnalisisSuelo")]
        public decimal PhAnalisisSuelo { get; set; }

        [JsonPropertyName("usuarioId")]
        public int? UsuarioId { get; set; }

        [JsonPropertyName("tieneFormulaNutricional")]
        public bool TieneFormulaNutricional { get; set; }

        [JsonPropertyName("tieneEnmiendaCalcarea")]
        public bool TieneEnmiendaCalcarea { get; set; }

        [JsonPropertyName("tieneFertilizacionMixta")]
        public bool TieneFertilizacionMixta { get; set; }

        [JsonIgnore]
        public string IdentificadorMostrar =>
            string.IsNullOrWhiteSpace(IdentificadorAnalisisSuelo)
                ? $"Análisis #{AnalisisSueloId}"
                : IdentificadorAnalisisSuelo.Trim();

        [JsonIgnore]
        public string LaboratorioMostrar =>
            string.IsNullOrWhiteSpace(LaboratorioAnalasisSuelo)
                ? "Laboratorio no especificado"
                : LaboratorioAnalasisSuelo.Trim();

        [JsonIgnore]
        public string ClienteMostrar =>
            string.IsNullOrWhiteSpace(NombreCliente)
                ? "Cliente no especificado"
                : NombreCliente.Trim();

        [JsonIgnore]
        public string TerrenoMostrar
        {
            get
            {
                string codigo = string.IsNullOrWhiteSpace(CodigoTerreno)
                    ? string.Empty
                    : CodigoTerreno.Trim();

                string nombre = string.IsNullOrWhiteSpace(NombreTerreno)
                    ? string.Empty
                    : NombreTerreno.Trim();

                if (!string.IsNullOrWhiteSpace(codigo) &&
                    !string.IsNullOrWhiteSpace(nombre))
                {
                    return $"{codigo} · {nombre}";
                }

                if (!string.IsNullOrWhiteSpace(codigo))
                    return codigo;

                if (!string.IsNullOrWhiteSpace(nombre))
                    return nombre;

                return $"Terreno #{TerrenoId}";
            }
        }

        [JsonIgnore]
        public DateTime? FechaAnalisisValor => ConvertirFecha(FechaAnalisisSuelo);

        [JsonIgnore]
        public DateTime? FechaCalculoValor => ConvertirFecha(FechaCalculo);

        [JsonIgnore]
        public string FechaAnalisisTexto =>
            FechaAnalisisValor?.ToString("dd/MM/yyyy") ?? "No disponible";

        [JsonIgnore]
        public string FechaCalculoTexto =>
            FechaCalculoValor?.ToString("dd/MM/yyyy HH:mm") ?? "No disponible";

        [JsonIgnore]
        public string ProduccionTexto => $"{CantidadQuintalesOro:N2} qq oro";

        [JsonIgnore]
        public string TamanoFincaTexto => $"{TamanoFinca:N2} mz";

        [JsonIgnore]
        public string PhTexto => PhAnalisisSuelo.ToString("N2");

        [JsonIgnore]
        public string TextoBusqueda => string.Join(" ", new[]
        {
            IdentificadorAnalisisSuelo,
            LaboratorioAnalasisSuelo,
            NombreCliente,
            CodigoTerreno,
            NombreTerreno
        }).ToUpperInvariant();

        private static DateTime? ConvertirFecha(string? valor)
        {
            if (string.IsNullOrWhiteSpace(valor))
                return null;

            return DateTime.TryParse(valor, out DateTime fecha)
                ? fecha
                : null;
        }
    }
}
