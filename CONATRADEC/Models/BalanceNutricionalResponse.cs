using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace CONATRADEC.Models
{
    public class BalanceNutricionalResponse
    {
        private string? nombreFormula;

        private Dictionary<string, decimal> formulaComercial =
            new(StringComparer.OrdinalIgnoreCase);

        [JsonIgnore]
        public bool Success { get; set; } = true;

        [JsonIgnore]
        public string? Message { get; set; }

        [JsonPropertyName("formulaNutricionalId")]
        public int? FormulaNutricionalId { get; set; }

        [JsonIgnore]
        public int? BalanceNutricionalId
        {
            get => FormulaNutricionalId;
            set => FormulaNutricionalId = value;
        }

        [JsonPropertyName("nombreFormula")]
        public string? NombreFormula
        {
            get => nombreFormula;
            set => nombreFormula = TextoBalanceHelper.Limpiar(value);
        }

        [JsonPropertyName("totalLibras")]
        public decimal? TotalLibras { get; set; }

        [JsonIgnore]
        public decimal? TotalMezclaLb
        {
            get => TotalLibras;
            set => TotalLibras = value;
        }

        [JsonPropertyName("mezclaTotalQq")]
        public decimal? MezclaTotalQq { get; set; }

        [JsonIgnore]
        public decimal? TotalMezclaQq
        {
            get => MezclaTotalQq;
            set => MezclaTotalQq = value;
        }

        [JsonPropertyName("totalOnzas")]
        public decimal? TotalOnzas { get; set; }

        [JsonIgnore]
        public decimal? TotalMezclaOz
        {
            get => TotalOnzas;
            set => TotalOnzas = value;
        }

        [JsonPropertyName("totalPlantas")]
        public int? TotalPlantas { get; set; }

        [JsonPropertyName("totalAplicaciones")]
        public int? TotalAplicaciones { get; set; }

        [JsonPropertyName("precioTotalFormula")]
        public decimal? PrecioTotalFormula { get; set; }

        [JsonPropertyName("precioPorAplicacion")]
        public decimal? PrecioPorAplicacion { get; set; }

        [JsonPropertyName("dosisPlantaAnualOz")]
        public decimal? DosisPlantaAnualOz { get; set; }

        [JsonPropertyName("dosisPlantaPorAplicacionOz")]
        public decimal? DosisPlantaPorAplicacionOz { get; set; }

        [JsonPropertyName("formulaComercial")]
        public Dictionary<string, decimal> FormulaComercial
        {
            get => formulaComercial;
            set => formulaComercial =
                TextoBalanceHelper.NormalizarDiccionario(value);
        }

        [JsonPropertyName("detalle")]
        public List<BalanceNutricionalDetalleResponse> Detalle { get; set; } = new();
    }

    public class BalanceNutricionalDetalleResponse
    {
        private string? fuente;
        private string? elemento;

        private Dictionary<string, decimal> aportes =
            new(StringComparer.OrdinalIgnoreCase);

        [JsonPropertyName("fuente")]
        public string? Fuente
        {
            get => fuente;
            set => fuente = TextoBalanceHelper.Limpiar(value);
        }

        [JsonPropertyName("elemento")]
        public string? Elemento
        {
            get => elemento;
            set => elemento = TextoBalanceHelper.Limpiar(value);
        }

        [JsonPropertyName("lb")]
        public decimal? Lb { get; set; }

        [JsonIgnore]
        public decimal? LibrasAnuales
        {
            get => Lb;
            set => Lb = value;
        }

        [JsonPropertyName("qq")]
        public decimal? Qq { get; set; }

        [JsonIgnore]
        public decimal? QuintalesAnuales
        {
            get => Qq;
            set => Qq = value;
        }

        [JsonPropertyName("requerimientoLibras")]
        public decimal? RequerimientoLibras { get; set; }

        [JsonPropertyName("librasPorAplicacion")]
        public decimal? LibrasPorAplicacion { get; set; }

        [JsonPropertyName("onzasAnuales")]
        public decimal? OnzasAnuales { get; set; }

        [JsonPropertyName("onzasPorAplicacion")]
        public decimal? OnzasPorAplicacion { get; set; }

        [JsonPropertyName("precioPorQuintal")]
        public decimal? PrecioPorQuintal { get; set; }

        [JsonPropertyName("subtotalFuente")]
        public decimal? SubtotalFuente { get; set; }

        [JsonPropertyName("aportes")]
        public Dictionary<string, decimal> Aportes
        {
            get => aportes;
            set => aportes =
                TextoBalanceHelper.NormalizarDiccionario(value);
        }
    }

    internal static class TextoBalanceHelper
    {
        public static string? Limpiar(string? valor)
        {
            if (string.IsNullOrWhiteSpace(valor))
                return null;

            return valor.Trim();
        }

        public static Dictionary<string, decimal> NormalizarDiccionario(
            Dictionary<string, decimal>? origen)
        {
            Dictionary<string, decimal> resultado =
                new(StringComparer.OrdinalIgnoreCase);

            if (origen == null)
                return resultado;

            foreach (KeyValuePair<string, decimal> item in origen)
            {
                string clave = (item.Key ?? string.Empty).Trim();

                if (string.IsNullOrWhiteSpace(clave))
                    continue;

                if (resultado.ContainsKey(clave))
                    resultado[clave] += item.Value;
                else
                    resultado[clave] = item.Value;
            }

            return resultado;
        }
    }
}
