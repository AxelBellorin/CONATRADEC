using System;
using System.Collections.Generic;

namespace CONATRADEC.Models
{
    /// <summary>
    /// Estado del balance original que se comparte con fertilización mixta
    /// cuando el usuario activa el complemento.
    /// </summary>
    public class BalanceFertilizacionMixtaContext
    {
        public string NombreFormula { get; set; } = string.Empty;

        public int? TerrenoId { get; set; }

        public int TotalPlantas { get; set; }

        public int TotalAplicaciones { get; set; }

        public decimal CostoCompraOriginal { get; set; }

        public BalanceNutricionalResponse? ResultadoOriginal { get; set; }

        public List<BalanceFertilizacionMixtaItem> Items { get; set; } = new();
    }

    public class BalanceFertilizacionMixtaItem
    {
        public int? FuenteNutrientesId { get; set; }

        public string NombreFuente { get; set; } = string.Empty;

        public int? ElementoQuimicosId { get; set; }

        public string SimboloElementoQuimico { get; set; } = string.Empty;

        public decimal RequerimientoOriginal { get; set; }
    }

    public class BalanceFertilizacionMixtaChangedEventArgs : EventArgs
    {
        public BalanceFertilizacionMixtaChangedEventArgs(
            bool activado,
            BalanceFertilizacionMixtaContext? contexto)
        {
            Activado = activado;
            Contexto = contexto;
        }

        public bool Activado { get; }

        public BalanceFertilizacionMixtaContext? Contexto { get; }
    }
}
