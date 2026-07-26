using CONATRADEC.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CONATRADEC.Services
{
    /// <summary>
    /// Conserva el requerimiento anual completo mientras las pantallas
    /// complementarias trabajan únicamente con los elementos seleccionados.
    ///
    /// De esta forma, excluir un elemento de Balance o Mixta nunca lo elimina
    /// del análisis original ni del requerimiento anual que se guarda.
    /// </summary>
    public static class SeleccionElementosComplementariosService
    {
        private static readonly object Sync = new();

        private static AnalisisSueloCalculoDataResponse?
            requerimientoCompleto;

        private static string identificadorAnalisis =
            string.Empty;

        public static void GuardarRequerimientoCompleto(
            AnalisisSueloCalculoDataResponse resultado,
            string? identificador)
        {
            ArgumentNullException.ThrowIfNull(resultado);

            lock (Sync)
            {
                requerimientoCompleto =
                    ClonarResultado(
                        resultado,
                        solamenteIncluidos: false);

                identificadorAnalisis =
                    identificador?.Trim() ??
                    string.Empty;
            }
        }

        public static AnalisisSueloCalculoDataResponse
            CrearResultadoParaCalculosComplementarios(
                AnalisisSueloCalculoDataResponse resultado)
        {
            ArgumentNullException.ThrowIfNull(resultado);

            return ClonarResultado(
                resultado,
                solamenteIncluidos: true);
        }

        public static AnalisisSueloCalculoDataResponse?
            ObtenerRequerimientoCompleto(
                string? identificador)
        {
            lock (Sync)
            {
                if (requerimientoCompleto == null)
                    return null;

                string solicitado =
                    identificador?.Trim() ??
                    string.Empty;

                if (!string.IsNullOrWhiteSpace(
                        identificadorAnalisis) &&
                    !string.IsNullOrWhiteSpace(
                        solicitado) &&
                    !string.Equals(
                        identificadorAnalisis,
                        solicitado,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return null;
                }

                return ClonarResultado(
                    requerimientoCompleto,
                    solamenteIncluidos: false);
            }
        }

        public static void Limpiar()
        {
            lock (Sync)
            {
                requerimientoCompleto = null;
                identificadorAnalisis = string.Empty;
            }
        }

        private static AnalisisSueloCalculoDataResponse
            ClonarResultado(
                AnalisisSueloCalculoDataResponse origen,
                bool solamenteIncluidos)
        {
            AnalisisSueloCalculoDataResponse destino =
                new()
                {
                    TerrenoId = origen.TerrenoId,
                    TipoCultivoId = origen.TipoCultivoId,
                    TipoCultivo = origen.TipoCultivo,
                    TipoAnalisisSueloId =
                        origen.TipoAnalisisSueloId,
                    TipoAnalisisSuelo =
                        origen.TipoAnalisisSuelo,
                    CantidadQuintalesOro =
                        origen.CantidadQuintalesOro,
                    TamanoFinca = origen.TamanoFinca,
                    Ph = origen.Ph,
                    AcidezTotal = origen.AcidezTotal,
                    RecomendacionGeneral =
                        origen.RecomendacionGeneral,
                    Observaciones =
                        origen.Observaciones?
                            .ToList() ??
                        new List<string>(),
                    FuentesFertilizantes =
                        origen.FuentesFertilizantes?
                            .ToList() ??
                        new List<object>(),
                    EnmiendaCalcarea =
                        origen.EnmiendaCalcarea,
                    FuentesOrganicas =
                        origen.FuentesOrganicas?
                            .ToList() ??
                        new List<object>()
                };

            IEnumerable<
                ElementoResultadoCalculoResponse>
                elementos =
                    origen.Elementos ??
                    new List<
                        ElementoResultadoCalculoResponse>();

            if (solamenteIncluidos)
            {
                elementos = elementos.Where(x =>
                    x.IncluirEnCalculosComplementarios);
            }

            destino.Elementos =
                elementos
                    .Select(ClonarElemento)
                    .ToList();

            return destino;
        }

        private static
            ElementoResultadoCalculoResponse
            ClonarElemento(
                ElementoResultadoCalculoResponse origen)
        {
            return new
                ElementoResultadoCalculoResponse
                {
                    ElementoQuimicosId =
                        origen.ElementoQuimicosId,
                    SimboloElementoQuimico =
                        origen.SimboloElementoQuimico,
                    NombreElementoQuimico =
                        origen.NombreElementoQuimico,
                    CantidadIngresada =
                        origen.CantidadIngresada,
                    CantidadConvertidaLbMz =
                        origen.CantidadConvertidaLbMz,
                    ExtraccionPorQQOro =
                        origen.ExtraccionPorQQOro,
                    ExtraccionPorProduccion =
                        origen.ExtraccionPorProduccion,
                    RangoMinimo = origen.RangoMinimo,
                    RangoMaximo = origen.RangoMaximo,
                    RangoMinimoLbMz =
                        origen.RangoMinimoLbMz,
                    RangoMaximoLbMz =
                        origen.RangoMaximoLbMz,
                    RequerimientoCalculado =
                        origen.RequerimientoCalculado,
                    UnidadBase = origen.UnidadBase,
                    UnidadMedidaResultadoId =
                        origen.UnidadMedidaResultadoId,
                    UnidadResultado =
                        string.IsNullOrWhiteSpace(
                            origen.UnidadResultado)
                            ? "lb/Mz"
                            : origen.UnidadResultado,
                    Clasificacion =
                        origen.Clasificacion,
                    Observacion = origen.Observacion,
                    IncluirEnCalculosComplementarios =
                        origen
                            .IncluirEnCalculosComplementarios
                };
        }
    }
}
