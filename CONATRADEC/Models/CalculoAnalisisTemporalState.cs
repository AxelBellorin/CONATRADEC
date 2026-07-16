using System;
using System.Text.Json.Serialization;

namespace CONATRADEC.Models
{
    // ===============================================================
    // Enum: TipoCalculoTemporal
    // Descripción:
    //   Identifica cada cálculo dentro del flujo único del análisis.
    // ===============================================================
    public enum TipoCalculoTemporal
    {
        RequerimientoAnual = 1,
        BalanceFormula = 2,
        FertilizacionMixta = 3,
        EnmiendaCalcarea = 4
    }

    // ===============================================================
    // Enum: EstadoCalculoTemporal
    // Descripción:
    //   Controla si un cálculo está actualizado o pendiente.
    // ===============================================================
    public enum EstadoCalculoTemporal
    {
        NoIniciado = 0,
        Calculado = 1,
        PendienteRecalculo = 2,
        Reiniciado = 3
    }

    // ===============================================================
    // Modelo: CalculoSeccionTemporalState
    // Descripción:
    //   Representa el estado temporal de una sección/cálculo.
    //
    // Nota:
    //   Se guardan RequestJson y ResultadoJson para no amarrarnos
    //   todavía a modelos finales de guardado.
    // ===============================================================
    public class CalculoSeccionTemporalState
    {
        public TipoCalculoTemporal TipoCalculo { get; set; }

        public EstadoCalculoTemporal Estado { get; set; } = EstadoCalculoTemporal.NoIniciado;

        public string? RequestJson { get; set; }

        public string? ResultadoJson { get; set; }

        public DateTime? FechaCalculo { get; set; }

        public DateTime? FechaUltimaModificacion { get; set; }

        public string? MensajeEstado { get; set; }

        [JsonIgnore]
        public bool TieneResultadoValido =>
            Estado == EstadoCalculoTemporal.Calculado &&
            !string.IsNullOrWhiteSpace(ResultadoJson);

        [JsonIgnore]
        public bool RequiereRecalculo =>
            Estado == EstadoCalculoTemporal.PendienteRecalculo;
    }

    // ===============================================================
    // Modelo: CalculoAnalisisTemporalState
    // Descripción:
    //   Representa TODO el estado temporal del flujo actual.
    //
    // Regla:
    //   Solo existe un cálculo temporal activo.
    //   Si inicia un nuevo análisis, reemplaza el anterior.
    // ===============================================================
    public class CalculoAnalisisTemporalState
    {
        public string? CalculoKey { get; set; }

        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        public DateTime FechaUltimaModificacion { get; set; } = DateTime.Now;

        // Resultado base del análisis de suelo.
        public AnalisisSueloCalculoDataResponse? ResultadoAnalisisSuelo { get; set; }

        // Request que posteriormente servirá para guardar el análisis.
        public AnalisisSueloGuardarCalculoRequest? RequestGuardarAnalisis { get; set; }

        // Estado del resultado base / requerimiento anual.
        public CalculoSeccionTemporalState RequerimientoAnual { get; set; } = new()
        {
            TipoCalculo = TipoCalculoTemporal.RequerimientoAnual
        };

        // Estado del balance de fórmula.
        public CalculoSeccionTemporalState BalanceFormula { get; set; } = new()
        {
            TipoCalculo = TipoCalculoTemporal.BalanceFormula
        };

        // Estado de fertilización mixta.
        public CalculoSeccionTemporalState FertilizacionMixta { get; set; } = new()
        {
            TipoCalculo = TipoCalculoTemporal.FertilizacionMixta
        };

        // Estado de enmienda calcárea.
        public CalculoSeccionTemporalState EnmiendaCalcarea { get; set; } = new()
        {
            TipoCalculo = TipoCalculoTemporal.EnmiendaCalcarea
        };

        [JsonIgnore]
        public bool TieneRequerimientoAnual =>
            RequerimientoAnual.TieneResultadoValido;

        [JsonIgnore]
        public bool TieneBalanceFormula =>
            BalanceFormula.TieneResultadoValido;

        [JsonIgnore]
        public bool TieneFertilizacionMixta =>
            FertilizacionMixta.TieneResultadoValido;

        [JsonIgnore]
        public bool TieneEnmiendaCalcarea =>
            EnmiendaCalcarea.TieneResultadoValido;
    }
}