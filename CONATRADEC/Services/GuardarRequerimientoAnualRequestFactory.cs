using CONATRADEC.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CONATRADEC.Services
{
    /// <summary>
    /// Construye la solicitud para guardar únicamente el análisis original
    /// y su requerimiento anual. Los módulos complementarios se envían null.
    /// </summary>
    public static class GuardarRequerimientoAnualRequestFactory
    {
        public static GuardarTodoRequest Crear(
            AnalisisSueloGuardarCalculoRequest datosOrigen,
            AnalisisSueloCalculoDataResponse requerimientoOrigen)
        {
            ArgumentNullException.ThrowIfNull(datosOrigen);
            ArgumentNullException.ThrowIfNull(requerimientoOrigen);

            if (datosOrigen.ElementosQuimicos == null ||
                datosOrigen.ElementosQuimicos.Count == 0)
            {
                throw new InvalidOperationException(
                    "El análisis no contiene elementos químicos originales.");
            }

            if (requerimientoOrigen.Elementos == null ||
                requerimientoOrigen.Elementos.Count == 0)
            {
                throw new InvalidOperationException(
                    "El requerimiento anual no contiene elementos calculados.");
            }

            GuardarTodoRequest request = new()
            {
                DatosAnalisis =
                    ConstruirDatosAnalisis(datosOrigen),

                RequerimientoAnual =
                    ConstruirRequerimientoAnual(
                        datosOrigen,
                        requerimientoOrigen),

                BalanceNutricional = null,
                EnmiendaCalcarea = null,
                FertilizacionMixta = null
            };

            return request;
        }

        private static GuardarTodoDatosAnalisisRequest
            ConstruirDatosAnalisis(
                AnalisisSueloGuardarCalculoRequest origen)
        {
            GuardarTodoDatosAnalisisRequest destino = new()
            {
                TerrenoId = origen.TerrenoId ?? 0,
                TipoCultivoId = origen.TipoCultivoId ?? 0,
                TipoAnalisisSueloId =
                    origen.TipoAnalisisSueloId ?? 0,
                UsuarioId = origen.UsuarioId,
                CantidadQuintalesOro =
                    origen.CantidadQuintalesOro ?? 0,
                TamanoFinca = origen.TamanoFinca ?? 0,
                Ph = origen.Ph ?? 0,
                MateriaOrganica =
                    origen.MateriaOrganica ?? 0,
                UnidadMedidaMateriaOrganicaId =
                    origen.UnidadMedidaMateriaOrganicaId ?? 0,
                AcidezTotal = origen.AcidezTotal,
                FechaAnalisisSuelo =
                    NormalizarFecha(
                        origen.FechaAnalisisSuelo),
                LaboratorioAnalasisSuelo =
                    origen.LaboratorioAnalasisSuelo?
                        .Trim() ??
                    string.Empty,
                IdentificadorAnalisisSuelo =
                    origen.IdentificadorAnalisisSuelo?
                        .Trim() ??
                    string.Empty
            };

            foreach (
                ElementoQuimicoAnalisisRequest elemento
                in origen.ElementosQuimicos)
            {
                if (elemento.ElementoQuimicosId is null or <= 0 ||
                    elemento.UnidadMedidaId is null or <= 0)
                {
                    throw new InvalidOperationException(
                        "Uno de los elementos originales no tiene identificadores válidos.");
                }

                destino.ElementosQuimicos.Add(
                    new GuardarTodoElementoAnalisisRequest
                    {
                        ElementoQuimicosId =
                            elemento.ElementoQuimicosId.Value,
                        UnidadMedidaId =
                            elemento.UnidadMedidaId.Value,
                        CantidadElemento =
                            elemento.CantidadElemento ?? 0
                    });
            }

            if (destino.TerrenoId <= 0 ||
                destino.TipoCultivoId <= 0 ||
                destino.TipoAnalisisSueloId <= 0)
            {
                throw new InvalidOperationException(
                    "El terreno, el cultivo o el tipo de análisis no son válidos.");
            }

            if (string.IsNullOrWhiteSpace(
                    destino.IdentificadorAnalisisSuelo))
            {
                throw new InvalidOperationException(
                    "El identificador del análisis es obligatorio.");
            }

            if (string.IsNullOrWhiteSpace(
                    destino.LaboratorioAnalasisSuelo))
            {
                throw new InvalidOperationException(
                    "El laboratorio del análisis es obligatorio.");
            }

            return destino;
        }

        private static GuardarTodoRequerimientoAnualRequest
            ConstruirRequerimientoAnual(
                AnalisisSueloGuardarCalculoRequest datos,
                AnalisisSueloCalculoDataResponse origen)
        {
            GuardarTodoRequerimientoAnualRequest destino =
                new()
                {
                    TerrenoId =
                        origen.TerrenoId ??
                        datos.TerrenoId ??
                        0,
                    TipoCultivoId =
                        origen.TipoCultivoId ??
                        datos.TipoCultivoId ??
                        0,
                    TipoCultivo =
                        origen.TipoCultivo?.Trim() ??
                        string.Empty,
                    TipoAnalisisSueloId =
                        origen.TipoAnalisisSueloId ??
                        datos.TipoAnalisisSueloId ??
                        0,
                    TipoAnalisisSuelo =
                        origen.TipoAnalisisSuelo?.Trim() ??
                        string.Empty,
                    CantidadQuintalesOro =
                        origen.CantidadQuintalesOro ??
                        datos.CantidadQuintalesOro ??
                        0,
                    TamanoFinca =
                        origen.TamanoFinca ??
                        datos.TamanoFinca ??
                        0,
                    Ph =
                        origen.Ph ??
                        datos.Ph ??
                        0,
                    AcidezTotal =
                        origen.AcidezTotal ??
                        datos.AcidezTotal,
                    MateriaOrganica =
                        datos.MateriaOrganica ?? 0,
                    UnidadMedidaMateriaOrganicaId =
                        datos.UnidadMedidaMateriaOrganicaId ??
                        0,
                    RecomendacionGeneral =
                        origen.RecomendacionGeneral?
                            .Trim() ??
                        string.Empty,
                    Observaciones =
                        origen.Observaciones?
                            .ToList() ??
                        new List<string>()
                };

            foreach (
                ElementoResultadoCalculoResponse elemento
                in origen.Elementos)
            {
                if (elemento.ElementoQuimicosId
                    is null or <= 0)
                {
                    throw new InvalidOperationException(
                        "Uno de los elementos calculados no tiene identificador válido.");
                }

                destino.Elementos.Add(
                    new GuardarTodoRequerimientoElementoRequest
                    {
                        ElementoQuimicosId =
                            elemento.ElementoQuimicosId.Value,
                        SimboloElementoQuimico =
                            elemento.SimboloElementoQuimico?
                                .Trim() ??
                            string.Empty,
                        NombreElementoQuimico =
                            elemento.NombreElementoQuimico?
                                .Trim() ??
                            string.Empty,
                        CantidadIngresada =
                            elemento.CantidadIngresada ?? 0,
                        CantidadConvertidaLbMz =
                            elemento.CantidadConvertidaLbMz,
                        ExtraccionPorQQOro =
                            elemento.ExtraccionPorQQOro,
                        ExtraccionPorProduccion =
                            elemento.ExtraccionPorProduccion,
                        RangoMinimo =
                            elemento.RangoMinimo,
                        RangoMaximo =
                            elemento.RangoMaximo,
                        RangoMinimoLbMz =
                            elemento.RangoMinimoLbMz,
                        RangoMaximoLbMz =
                            elemento.RangoMaximoLbMz,
                        RequerimientoCalculado =
                            elemento.RequerimientoCalculado,
                        UnidadBase =
                            elemento.UnidadBase?
                                .Trim() ??
                            string.Empty,
                        UnidadMedidaResultadoId =
                            elemento.UnidadMedidaResultadoId,
                        UnidadResultado =
                            string.IsNullOrWhiteSpace(
                                elemento.UnidadResultado)
                                ? "lb/Mz"
                                : elemento.UnidadResultado.Trim(),
                        Clasificacion =
                            elemento.Clasificacion?
                                .Trim() ??
                            string.Empty,
                        Observacion =
                            elemento.Observacion?
                                .Trim() ??
                            string.Empty,
                        IncluirCalculosComplementarios =
                            elemento
                                .IncluirEnCalculosComplementarios
                    });
            }

            return destino;
        }

        private static string NormalizarFecha(
            string? fecha)
        {
            if (DateTime.TryParse(
                    fecha,
                    out DateTime valor))
            {
                return valor.ToString("yyyy-MM-dd");
            }

            return fecha?.Trim() ?? string.Empty;
        }
    }
}
