using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace CONATRADEC.Models
{
    public class FuenteNutrienteResponse
    {
        [JsonPropertyName("fuenteNutrientesId")]
        public int? FuenteNutrientesId { get; set; }

        [JsonPropertyName("nombreNutriente")]
        public string? NombreNutriente { get; set; }

        [JsonPropertyName("descripcionNutriente")]
        public string? DescripcionNutriente { get; set; }

        [JsonPropertyName("precioNutriente")]
        public decimal? PrecioNutriente { get; set; }

        [JsonPropertyName("activo")]
        public bool? Activo { get; set; }

        [JsonPropertyName("habilitadaEnmiendaCalcarea")]
        public bool? HabilitadaEnmiendaCalcarea { get; set; }

        [JsonPropertyName("habilitadaFertilizacionMixta")]
        public bool? HabilitadaFertilizacionMixta { get; set; }

        // Cuando la API los devuelva, estos campos se llenarán automáticamente.
        [JsonPropertyName("prnt")]
        public decimal? Prnt { get; set; }

        [JsonPropertyName("descripcionParametro")]
        public string? DescripcionParametro { get; set; }

        [JsonPropertyName("elementosQuimicos")]
        public List<FuenteNutrienteElementoQuimicoResponse> ElementosQuimicos { get; set; } = new();

        public string NombreMostrar
        {
            get
            {
                string nombre = NombreNutriente ?? "Fuente sin nombre";

                if (ElementosQuimicos == null || ElementosQuimicos.Count == 0)
                    return nombre;

                string aportes = string.Join(
                    ", ",
                    ElementosQuimicos
                        .Where(x => !string.IsNullOrWhiteSpace(x.SimboloElementoQuimico))
                        .Select(x => $"{x.SimboloElementoQuimico?.Trim()} {FormatearAporte(x.CantidadAporte)}%")
                );

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

        public bool EsEnmiendaCalcarea => HabilitadaEnmiendaCalcarea == true;

        public bool EsFertilizacionMixta => HabilitadaFertilizacionMixta == true;

        public bool EsBalanceNutricional =>
            HabilitadaEnmiendaCalcarea != true &&
            HabilitadaFertilizacionMixta != true;

        public bool TieneAporteElementoQuimico =>
            ElementosQuimicos != null &&
            ElementosQuimicos.Any(x =>
                !string.IsNullOrWhiteSpace(x.SimboloElementoQuimico) &&
                (x.CantidadAporte ?? 0) > 0);

        public string CategoriaFuenteCodigo
        {
            get
            {
                if (EsEnmiendaCalcarea)
                    return FuenteNutrienteCategoriaOption.CodigoEnmiendaCalcarea;

                if (EsFertilizacionMixta)
                    return FuenteNutrienteCategoriaOption.CodigoFertilizacionMixta;

                return FuenteNutrienteCategoriaOption.CodigoBalanceNutricional;
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
            !string.IsNullOrWhiteSpace(DescripcionParametro)
                ? DescripcionParametro
                : "Pendiente de API";

        public bool TieneAportes =>
            ElementosQuimicos != null && ElementosQuimicos.Count > 0;

        public string AportesMostrar
        {
            get
            {
                if (!TieneAportes)
                    return "Sin aportes registrados";

                return string.Join(
                    " · ",
                    ElementosQuimicos
                        .Where(x => !string.IsNullOrWhiteSpace(x.SimboloElementoQuimico))
                        .Select(x => $"{x.SimboloElementoQuimico?.Trim()}: {FormatearAporte(x.CantidadAporte)}%")
                );
            }
        }

        private static string FormatearAporte(decimal? valor)
        {
            if (!valor.HasValue)
                return "0";

            decimal numero = valor.Value;

            if (numero == decimal.Truncate(numero))
                return numero.ToString("N0");

            return numero.ToString("N2");
        }
    }

    public class FuenteNutrienteElementoQuimicoResponse
    {
        [JsonPropertyName("fuenteNutrienteElementoQuimicoId")]
        public int? FuenteNutrienteElementoQuimicoId { get; set; }

        [JsonPropertyName("elementoQuimicosId")]
        public int? ElementoQuimicosId { get; set; }

        [JsonPropertyName("nombreElementoQuimico")]
        public string? NombreElementoQuimico { get; set; }

        [JsonPropertyName("simboloElementoQuimico")]
        public string? SimboloElementoQuimico { get; set; }

        [JsonPropertyName("cantidadAporte")]
        public decimal? CantidadAporte { get; set; }

        public string SimboloNormalizado =>
            (SimboloElementoQuimico ?? string.Empty)
                .Trim()
                .ToUpper();
    }
}