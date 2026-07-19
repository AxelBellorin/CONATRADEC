using CONATRADEC.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace CONATRADEC.Services
{
    public static class AnalisisReporteMapper
    {
        public static AnalisisReporte DesdeSolicitudGuardada(
            GuardarTodoRequest solicitud,
            AnalisisGuardadoResumen? resumen = null)
        {
            ArgumentNullException.ThrowIfNull(solicitud);

            GuardarTodoDatosAnalisisRequest datos = solicitud.DatosAnalisis;
            GuardarTodoRequerimientoAnualRequest requerimiento = solicitud.RequerimientoAnual;

            Dictionary<int, GuardarTodoRequerimientoElementoRequest> elementosPorId =
                requerimiento.Elementos
                    .GroupBy(x => x.ElementoQuimicosId)
                    .ToDictionary(x => x.Key, x => x.First());

            AnalisisReporte reporte = new()
            {
                Identificador = ValorO(
                    datos.IdentificadorAnalisisSuelo,
                    "Análisis de suelo"),
                FechaAnalisis = FormatearFecha(datos.FechaAnalisisSuelo),
                Laboratorio = ValorO(
                    datos.LaboratorioAnalasisSuelo,
                    "No especificado"),
                Cliente = resumen?.ClienteMostrar ?? "No disponible",
                Terreno = resumen?.TerrenoMostrar ?? $"Terreno #{datos.TerrenoId}",
                TipoCultivo = ValorO(
                    requerimiento.TipoCultivo,
                    $"Cultivo #{requerimiento.TipoCultivoId}"),
                TipoAnalisis = ValorO(
                    requerimiento.TipoAnalisisSuelo,
                    $"Tipo #{requerimiento.TipoAnalisisSueloId}"),
                ProduccionQqOro = requerimiento.CantidadQuintalesOro,
                TamanoFincaMz = requerimiento.TamanoFinca,
                Ph = requerimiento.Ph,
                MateriaOrganica = requerimiento.MateriaOrganica,
                AcidezTotal = requerimiento.AcidezTotal,
                RecomendacionGeneral = requerimiento.RecomendacionGeneral ?? string.Empty,
                Observaciones = requerimiento.Observaciones?
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Select(x => x.Trim())
                    .ToList() ?? new()
            };

            reporte.ValoresLaboratorio = datos.ElementosQuimicos
                .Select(x =>
                {
                    elementosPorId.TryGetValue(
                        x.ElementoQuimicosId,
                        out GuardarTodoRequerimientoElementoRequest? elemento);

                    return new AnalisisReporteValorLaboratorio
                    {
                        Elemento = FormatearElemento(
                            elemento?.NombreElementoQuimico,
                            elemento?.SimboloElementoQuimico,
                            x.ElementoQuimicosId),
                        Cantidad = x.CantidadElemento,
                        Unidad = ValorO(
                            elemento?.UnidadBase,
                            $"Unidad #{x.UnidadMedidaId}")
                    };
                })
                .ToList();

            reporte.Requerimientos = requerimiento.Elementos
                .Select(x => new AnalisisReporteRequerimiento
                {
                    Elemento = FormatearElemento(
                        x.NombreElementoQuimico,
                        x.SimboloElementoQuimico,
                        x.ElementoQuimicosId),
                    CantidadIngresada = x.CantidadIngresada,
                    CantidadConvertidaLbMz = x.CantidadConvertidaLbMz,
                    RequerimientoLbMz = x.RequerimientoCalculado,
                    UnidadResultado = ValorO(x.UnidadResultado, "lb/mz"),
                    Clasificacion = x.Clasificacion ?? string.Empty,
                    Observacion = x.Observacion ?? string.Empty
                })
                .ToList();

            reporte.Balance = MapearBalance(solicitud.BalanceNutricional);
            reporte.Enmienda = MapearEnmienda(solicitud.EnmiendaCalcarea);
            reporte.FertilizacionMixta = MapearMixta(solicitud.FertilizacionMixta);

            return reporte;
        }

        public static AnalisisReporte DesdeDetalle(
            AnalisisGuardadoDetalleData detalle,
            AnalisisGuardadoResumen? resumen = null)
        {
            ArgumentNullException.ThrowIfNull(detalle);

            AnalisisGuardadoDatosAnalisis datos = detalle.DatosAnalisis;
            AnalisisGuardadoRequerimientoAnual requerimiento = detalle.RequerimientoAnual;

            AnalisisReporte reporte = new()
            {
                Identificador = ValorO(
                    datos.IdentificadorAnalisisSuelo,
                    $"Análisis #{datos.AnalisisSueloId}"),
                FechaAnalisis = datos.FechaAnalisisTexto,
                Laboratorio = ValorO(
                    datos.LaboratorioAnalasisSuelo,
                    "No especificado"),
                Cliente = resumen?.ClienteMostrar ?? "No disponible",
                Terreno = resumen?.TerrenoMostrar ?? $"Terreno #{requerimiento.TerrenoId}",
                TipoCultivo = $"Cultivo #{requerimiento.TipoCultivoId}",
                TipoAnalisis = $"Tipo #{requerimiento.TipoAnalisisSueloId}",
                ProduccionQqOro = requerimiento.CantidadQuintalesOro,
                TamanoFincaMz = requerimiento.TamanoFinca,
                Ph = requerimiento.Ph,
                MateriaOrganica = requerimiento.MateriaOrganica,
                AcidezTotal = requerimiento.AcidezTotal,
                RecomendacionGeneral = requerimiento.RecomendacionGeneral ?? string.Empty,
                Observaciones = requerimiento.Observaciones?
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Select(x => x.Trim())
                    .ToList() ?? new()
            };

            reporte.ValoresLaboratorio = datos.ElementosQuimicos
                .Select(x => new AnalisisReporteValorLaboratorio
                {
                    Elemento = ValorO(
                        x.ElementoMostrar,
                        $"Elemento #{x.ElementoQuimicosId}"),
                    Cantidad = x.CantidadElemento,
                    Unidad = x.UnidadMostrar
                })
                .ToList();

            reporte.Requerimientos = requerimiento.Elementos
                .Select(x => new AnalisisReporteRequerimiento
                {
                    Elemento = ValorO(
                        x.ElementoMostrar,
                        $"Elemento #{x.ElementoQuimicosId}"),
                    CantidadIngresada = x.CantidadIngresada,
                    CantidadConvertidaLbMz = x.CantidadConvertidaLbMz,
                    RequerimientoLbMz = x.RequerimientoCalculado,
                    UnidadResultado = x.UnidadMostrar,
                    Clasificacion = x.Clasificacion ?? string.Empty,
                    Observacion = x.Observacion ?? string.Empty
                })
                .ToList();

            reporte.Balance = MapearBalance(detalle.BalanceNutricional);
            reporte.Enmienda = MapearEnmienda(detalle.EnmiendaCalcarea);
            reporte.FertilizacionMixta = MapearMixta(detalle.FertilizacionMixta);

            return reporte;
        }

        private static AnalisisReporteBalance? MapearBalance(
            GuardarTodoBalanceNutricionalRequest? origen)
        {
            if (origen == null)
                return null;

            GuardarTodoBalanceResultadoRequest resultado = origen.Resultado;

            List<AnalisisReporteBalanceDetalle> detalles = resultado.Detalle
                .Select(x =>
                {
                    decimal quintalesComprar = Math.Ceiling(x.Qq);

                    return new AnalisisReporteBalanceDetalle
                    {
                        Fuente = x.Fuente ?? string.Empty,
                        Elemento = x.Elemento ?? string.Empty,
                        Libras = x.Lb,
                        QuintalesExactos = x.Qq,
                        QuintalesComprar = quintalesComprar,
                        PrecioPorQuintal = x.PrecioPorQuintal,
                        SubtotalExacto = x.SubtotalFuente,
                        CostoCompra = quintalesComprar * x.PrecioPorQuintal,
                        OnzasAnuales = x.OnzasAnuales,
                        OnzasPorAplicacion = x.OnzasPorAplicacion
                    };
                })
                .ToList();

            return new AnalisisReporteBalance
            {
                NombreFormula = resultado.NombreFormula ?? string.Empty,
                TotalLibras = resultado.TotalLibras,
                MezclaTotalQq = resultado.MezclaTotalQq,
                TotalPlantas = resultado.TotalPlantas,
                TotalAplicaciones = resultado.TotalAplicaciones,
                DosisPlantaAnualOz = resultado.DosisPlantaAnualOz,
                DosisPlantaPorAplicacionOz = resultado.DosisPlantaPorAplicacionOz,
                PrecioExactoReferencia = detalles.Sum(x => x.SubtotalExacto),
                CostoRealCompra = resultado.PrecioTotalFormula,
                PrecioPorAplicacion = resultado.PrecioPorAplicacion,
                FormulaComercial = resultado.FormulaComercial != null
                    ? new Dictionary<string, decimal>(resultado.FormulaComercial)
                    : new(),
                Detalles = detalles
            };
        }

        private static AnalisisReporteBalance? MapearBalance(
            AnalisisGuardadoBalanceNutricional? origen)
        {
            if (origen == null)
                return null;

            AnalisisGuardadoFormula formula = origen.Formula;

            List<AnalisisReporteBalanceDetalle> detalles = origen.Detalles
                .Select(x =>
                {
                    decimal quintalesComprar = Math.Ceiling(x.Qq);

                    return new AnalisisReporteBalanceDetalle
                    {
                        Fuente = x.FuenteMostrar,
                        Elemento = x.ElementoMostrar,
                        Libras = x.Libras,
                        QuintalesExactos = x.Qq,
                        QuintalesComprar = quintalesComprar,
                        PrecioPorQuintal = x.PrecioPorQuintal,
                        SubtotalExacto = x.SubtotalFuente,
                        CostoCompra = quintalesComprar * x.PrecioPorQuintal,
                        OnzasAnuales = x.OnzasAnuales,
                        OnzasPorAplicacion = x.OnzasPorAplicacion
                    };
                })
                .ToList();

            return new AnalisisReporteBalance
            {
                NombreFormula = formula.NombreFormula,
                TotalLibras = formula.TotalLibras,
                MezclaTotalQq = formula.MezclaTotalQq,
                TotalPlantas = formula.TotalPlantas,
                TotalAplicaciones = formula.TotalAplicaciones,
                DosisPlantaAnualOz = formula.DosisPlantaAnualOz,
                DosisPlantaPorAplicacionOz = formula.DosisPlantaPorAplicacionOz,
                PrecioExactoReferencia = detalles.Sum(x => x.SubtotalExacto),
                CostoRealCompra = formula.PrecioTotalFormula,
                PrecioPorAplicacion = formula.PrecioPorAplicacion,
                Detalles = detalles
            };
        }

        private static AnalisisReporteEnmienda? MapearEnmienda(
            GuardarTodoEnmiendaCalcareaRequest? origen)
        {
            if (origen == null)
                return null;

            GuardarTodoEnmiendaResultadoRequest resultado = origen.Resultado;

            return new AnalisisReporteEnmienda
            {
                NombreAnalisis = resultado.NombreAnalisis ?? string.Empty,
                Fuente = resultado.FuenteNutriente ?? string.Empty,
                TotalPlantas = resultado.TotalPlantas,
                TotalAplicaciones = resultado.TotalAplicaciones,
                Ph = resultado.Ph,
                Calcio = resultado.Ca,
                Magnesio = resultado.Mg,
                Potasio = resultado.K,
                AcidezTotal = resultado.AcidezTotal,
                Cice = resultado.Cice,
                SaturacionActual = resultado.SaturacionActual,
                SaturacionDeseada = resultado.SaturacionDeseada,
                Prnt = resultado.Prnt,
                NecesidadEncaladoTonHa = resultado.NecesidadEncaladoTonHa,
                NecesidadEncaladoKgHa = resultado.NecesidadEncaladoKgHa,
                NecesidadEncaladoLbHa = resultado.NecesidadEncaladoLbHa,
                NecesidadEncaladoLbMz = resultado.NecesidadEncaladoLbMz,
                DosisPlantaAnualOz = resultado.DosisPlantaAnualOz,
                DosisPlantaPorAplicacionOz = resultado.DosisPlantaPorAplicacionOz
            };
        }

        private static AnalisisReporteEnmienda? MapearEnmienda(
            AnalisisGuardadoEnmiendaCalcarea? origen)
        {
            if (origen == null)
                return null;

            return new AnalisisReporteEnmienda
            {
                NombreAnalisis = origen.NombreAnalisis,
                Fuente = origen.FuenteMostrar,
                TotalPlantas = origen.TotalPlantas,
                TotalAplicaciones = origen.TotalAplicaciones,
                Ph = origen.Ph,
                Calcio = origen.Ca,
                Magnesio = origen.Mg,
                Potasio = origen.K,
                AcidezTotal = origen.AcidezTotal,
                Cice = origen.Cice,
                SaturacionActual = origen.SaturacionActual,
                SaturacionDeseada = origen.SaturacionDeseada,
                Prnt = origen.Prnt,
                NecesidadEncaladoTonHa = origen.NecesidadEncaladoTonHa,
                NecesidadEncaladoKgHa = origen.NecesidadEncaladoKgHa,
                NecesidadEncaladoLbHa = origen.NecesidadEncaladoLbHa,
                NecesidadEncaladoLbMz = origen.NecesidadEncaladoLbMz,
                DosisPlantaAnualOz = origen.DosisPlantaAnualOz,
                DosisPlantaPorAplicacionOz = origen.DosisPlantaPorAplicacionOz
            };
        }

        private static AnalisisReporteFertilizacionMixta? MapearMixta(
            GuardarTodoFertilizacionMixtaRequest? origen)
        {
            if (origen == null)
                return null;

            return new AnalisisReporteFertilizacionMixta
            {
                Observacion = origen.Observacion ?? string.Empty,
                Fuentes = origen.Fuentes
                    .Select(x => new AnalisisReporteMixtaFuente
                    {
                        Fuente = ValorO(
                            x.NombreFuente,
                            $"Fuente #{x.FuenteNutrientesId}"),
                        CantidadQq = x.CantidadQq
                    })
                    .ToList(),
                Detalles = origen.Detalles
                    .Select(x => new AnalisisReporteMixtaDetalle
                    {
                        Elemento = ValorO(
                            x.Elemento,
                            $"Elemento #{x.ElementoQuimicosId}"),
                        RequerimientoOriginal = x.Exportable,
                        AporteOrganico = x.AporteOrganico,
                        Diferencia = x.Diferencia,
                        Deficit = x.Deficit,
                        Sobrante = x.Sobrante
                    })
                    .ToList()
            };
        }

        private static AnalisisReporteFertilizacionMixta? MapearMixta(
            AnalisisGuardadoFertilizacionMixta? origen)
        {
            if (origen == null)
                return null;

            return new AnalisisReporteFertilizacionMixta
            {
                Observacion = origen.Mixta.Observacion,
                Fuentes = origen.Fuentes
                    .Select(x => new AnalisisReporteMixtaFuente
                    {
                        Fuente = x.FuenteMostrar,
                        CantidadQq = x.CantidadQq
                    })
                    .ToList(),
                Detalles = origen.Detalles
                    .Select(x => new AnalisisReporteMixtaDetalle
                    {
                        Elemento = ValorO(
                            x.NombreElemento,
                            $"Elemento #{x.ElementoQuimicosId}"),
                        RequerimientoOriginal = x.RequerimientoOriginal,
                        AporteOrganico = x.AporteOrganico,
                        Diferencia = x.Diferencia,
                        Deficit = x.Deficit,
                        Sobrante = x.Sobrante
                    })
                    .ToList()
            };
        }

        private static string FormatearElemento(
            string? nombre,
            string? simbolo,
            int id)
        {
            string nombreLimpio = nombre?.Trim() ?? string.Empty;
            string simboloLimpio = simbolo?.Trim() ?? string.Empty;

            if (!string.IsNullOrWhiteSpace(nombreLimpio) &&
                !string.IsNullOrWhiteSpace(simboloLimpio))
            {
                return $"{nombreLimpio} ({simboloLimpio})";
            }

            return !string.IsNullOrWhiteSpace(nombreLimpio)
                ? nombreLimpio
                : !string.IsNullOrWhiteSpace(simboloLimpio)
                    ? simboloLimpio
                    : $"Elemento #{id}";
        }

        private static string FormatearFecha(string? valor)
        {
            if (DateTime.TryParse(
                    valor,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out DateTime fecha) ||
                DateTime.TryParse(valor, out fecha))
            {
                return fecha.ToString("dd/MM/yyyy");
            }

            return ValorO(valor, "No disponible");
        }

        private static string ValorO(string? valor, string alternativo) =>
            string.IsNullOrWhiteSpace(valor)
                ? alternativo
                : valor.Trim();
    }
}
