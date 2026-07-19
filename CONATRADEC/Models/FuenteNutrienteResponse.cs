using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace CONATRADEC.Models
{
    public class FuenteNutrienteResponse
    {
        private string? nombreNutriente;
        private string? descripcionNutriente;
        private string? descripcionParametro;

        [JsonPropertyName("fuenteNutrientesId")]
        public int? FuenteNutrientesId { get; set; }

        [JsonPropertyName("nombreNutriente")]
        public string? NombreNutriente
        {
            get => nombreNutriente;
            set => nombreNutriente = LimpiarTexto(value);
        }

        [JsonPropertyName("descripcionNutriente")]
        public string? DescripcionNutriente
        {
            get => descripcionNutriente;
            set => descripcionNutriente = LimpiarTexto(value);
        }

        [JsonPropertyName("precioNutriente")]
        public decimal? PrecioNutriente { get; set; }

        [JsonPropertyName("activo")]
        public bool? Activo { get; set; }

        [JsonPropertyName("habilitadaEnmiendaCalcarea")]
        public bool? HabilitadaEnmiendaCalcarea { get; set; }

        [JsonPropertyName("habilitadaFertilizacionMixta")]
        public bool? HabilitadaFertilizacionMixta { get; set; }

        [JsonPropertyName("prnt")]
        public decimal? Prnt { get; set; }

        [JsonPropertyName("descripcionParametro")]
        public string? DescripcionParametro
        {
            get => descripcionParametro;
            set => descripcionParametro = LimpiarTexto(value);
        }

        [JsonPropertyName("elementosQuimicos")]
        public List<FuenteNutrienteElementoQuimicoResponse>
            ElementosQuimicos { get; set; } = new();

        public string NombreMostrar
        {
            get
            {
                string nombre =
                    NombreNutriente ?? "Fuente sin nombre";

                if (ElementosQuimicos == null ||
                    ElementosQuimicos.Count == 0)
                {
                    return nombre;
                }

                string aportes = string.Join(
                    ", ",
                    ElementosQuimicos
                        .Where(x =>
                            !string.IsNullOrWhiteSpace(
                                x.SimboloElementoQuimico))
                        .Select(x =>
                            $"{x.SimboloElementoQuimico} " +
                            $"{FormatearAporte(x.CantidadAporte)}%"));

                if (string.IsNullOrWhiteSpace(aportes))
                    return nombre;

                return $"{nombre} ({aportes})";
            }
        }

        public string PrecioMostrar =>
            PrecioNutriente.HasValue
                ? $"C$ {PrecioNutriente.Value:N2} por quintal"
                : "Sin precio por quintal";

        public string EstadoMostrar =>
            Activo == true ? "Activo" : "Inactivo";

        public bool IsActivo => Activo == true;

        public bool EsEnmiendaCalcarea =>
            HabilitadaEnmiendaCalcarea == true;

        public bool EsFertilizacionMixta =>
            HabilitadaFertilizacionMixta == true;

        public bool EsBalanceNutricional =>
            HabilitadaEnmiendaCalcarea != true &&
            HabilitadaFertilizacionMixta != true;

        public bool TieneAporteElementoQuimico =>
            ElementosQuimicos != null &&
            ElementosQuimicos.Any(x =>
                !string.IsNullOrWhiteSpace(
                    x.SimboloElementoQuimico) &&
                (x.CantidadAporte ?? 0) > 0);

        public string CategoriaFuenteCodigo
        {
            get
            {
                if (EsEnmiendaCalcarea)
                    return FuenteNutrienteCategoriaOption
                        .CodigoEnmiendaCalcarea;

                if (EsFertilizacionMixta)
                    return FuenteNutrienteCategoriaOption
                        .CodigoFertilizacionMixta;

                return FuenteNutrienteCategoriaOption
                    .CodigoBalanceNutricional;
            }
        }

        public string CategoriaFuenteMostrar
        {
            get
            {
                if (EsEnmiendaCalcarea)
                    return "Enmienda calcárea";

                if (EsFertilizacionMixta)
                    return "Fertilización mixta";

                return "Balance nutricional";
            }
        }

        public string PrntMostrar =>
            Prnt.HasValue
                ? $"{Prnt.Value:N2}"
                : "Pendiente de API";

        public string DescripcionParametroMostrar =>
            !string.IsNullOrWhiteSpace(
                DescripcionParametro)
                ? DescripcionParametro
                : "Pendiente de API";

        public bool TieneAportes =>
            ElementosQuimicos != null &&
            ElementosQuimicos.Count > 0;

        public string AportesMostrar
        {
            get
            {
                if (!TieneAportes)
                    return "Sin aportes registrados";

                return string.Join(
                    " · ",
                    ElementosQuimicos
                        .Where(x =>
                            !string.IsNullOrWhiteSpace(
                                x.SimboloElementoQuimico))
                        .Select(x =>
                            $"{x.SimboloElementoQuimico}: " +
                            $"{FormatearAporte(x.CantidadAporte)}%"));
            }
        }

        private static string FormatearAporte(
            decimal? valor)
        {
            if (!valor.HasValue)
                return "0";

            decimal numero = valor.Value;

            if (numero == decimal.Truncate(numero))
                return numero.ToString("N0");

            return numero.ToString("N2");
        }

        private static string? LimpiarTexto(string? valor)
        {
            if (string.IsNullOrWhiteSpace(valor))
                return null;

            return valor.Trim();
        }
    }

    public class FuenteNutrienteElementoQuimicoResponse
    {
        private string? nombreElementoQuimico;
        private string? simboloElementoQuimico;

        [JsonPropertyName("fuenteNutrienteElementoQuimicoId")]
        public int? FuenteNutrienteElementoQuimicoId { get; set; }

        [JsonPropertyName("elementoQuimicosId")]
        public int? ElementoQuimicosId { get; set; }

        [JsonPropertyName("nombreElementoQuimico")]
        public string? NombreElementoQuimico
        {
            get => nombreElementoQuimico;
            set => nombreElementoQuimico =
                LimpiarTexto(value);
        }

        [JsonPropertyName("simboloElementoQuimico")]
        public string? SimboloElementoQuimico
        {
            get => simboloElementoQuimico;
            set => simboloElementoQuimico =
                LimpiarTexto(value);
        }

        [JsonPropertyName("cantidadAporte")]
        public decimal? CantidadAporte { get; set; }

        public string SimboloNormalizado =>
            (SimboloElementoQuimico ?? string.Empty)
                .Trim()
                .ToUpperInvariant();

        private static string? LimpiarTexto(string? valor)
        {
            if (string.IsNullOrWhiteSpace(valor))
                return null;

            return valor.Trim();
        }
    }
}
