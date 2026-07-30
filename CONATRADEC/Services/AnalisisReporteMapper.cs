using CONATRADEC.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace CONATRADEC.Services
{
    public static class AnalisisReporteMapper
    {
        public static AnalisisReporte DesdeSolicitudGuardada(
            GuardarTodoRequest solicitud,
            AnalisisGuardadoResumen? resumen = null)
        {
            ArgumentNullException.ThrowIfNull(solicitud);

            CatalogoReporteLocal catalogo =
                CatalogoReporteLocal.Crear(
                    ObtenerPaqueteLocalSeguro());

            GuardarTodoDatosAnalisisRequest datos =
                solicitud.DatosAnalisis;

            GuardarTodoRequerimientoAnualRequest requerimiento =
                solicitud.RequerimientoAnual;

            Dictionary<int, GuardarTodoRequerimientoElementoRequest>
                elementosPorId = requerimiento.Elementos
                    .GroupBy(x => x.ElementoQuimicosId)
                    .ToDictionary(
                        x => x.Key,
                        x => x.First());

            AnalisisReporte reporte = new()
            {
                Identificador = ValorO(
                    datos.IdentificadorAnalisisSuelo,
                    "Análisis de suelo"),

                FechaAnalisis = FormatearFecha(
                    datos.FechaAnalisisSuelo),

                Laboratorio = ValorO(
                    datos.LaboratorioAnalasisSuelo,
                    "No especificado"),

                Cliente = resumen?.ClienteMostrar ??
                    "No disponible",

                Terreno = resumen?.TerrenoMostrar ??
                    $"Terreno #{datos.TerrenoId}",

                TipoCultivo = ValorO(
                    requerimiento.TipoCultivo,
                    catalogo.ObtenerTipoCultivo(
                        requerimiento.TipoCultivoId,
                        $"Cultivo #{requerimiento.TipoCultivoId}")),

                TipoAnalisis = ValorO(
                    requerimiento.TipoAnalisisSuelo,
                    catalogo.ObtenerTipoAnalisis(
                        requerimiento.TipoAnalisisSueloId,
                        $"Tipo #{requerimiento.TipoAnalisisSueloId}")),

                ProduccionQqOro =
                    requerimiento.CantidadQuintalesOro,

                TamanoFincaMz = requerimiento.TamanoFinca,
                Ph = requerimiento.Ph,
                MateriaOrganica = requerimiento.MateriaOrganica,

                UnidadMateriaOrganica =
                    catalogo.ObtenerUnidad(
                        requerimiento
                            .UnidadMedidaMateriaOrganicaId,
                        string.Empty),

                AcidezTotal = requerimiento.AcidezTotal,

                RecomendacionGeneral =
                    requerimiento.RecomendacionGeneral ??
                    string.Empty,

                Observaciones = requerimiento.Observaciones?
                    .Where(x =>
                        !string.IsNullOrWhiteSpace(x))
                    .Select(x => x.Trim())
                    .ToList() ?? new()
            };

            reporte.ValoresLaboratorio =
                datos.ElementosQuimicos
                    .Select(x =>
                    {
                        elementosPorId.TryGetValue(
                            x.ElementoQuimicosId,
                            out GuardarTodoRequerimientoElementoRequest?
                                elemento);

                        string nombreElemento =
                            catalogo.ObtenerElementoMostrar(
                                x.ElementoQuimicosId,
                                elemento?.NombreElementoQuimico,
                                elemento?.SimboloElementoQuimico);

                        string unidad =
                            catalogo.ObtenerUnidad(
                                x.UnidadMedidaId,
                                ValorO(
                                    elemento?.UnidadBase,
                                    $"Unidad #{x.UnidadMedidaId}"));

                        return new AnalisisReporteValorLaboratorio
                        {
                            Elemento = nombreElemento,
                            Cantidad = x.CantidadElemento,
                            Unidad = unidad
                        };
                    })
                    .OrderBy(
                        x => x.Elemento,
                        StringComparer.CurrentCultureIgnoreCase)
                    .ToList();

            reporte.Requerimientos =
                requerimiento.Elementos
                    .Select(x =>
                        new AnalisisReporteRequerimiento
                        {
                            Elemento =
                                catalogo.ObtenerElementoMostrar(
                                    x.ElementoQuimicosId,
                                    x.NombreElementoQuimico,
                                    x.SimboloElementoQuimico),

                            CantidadIngresada =
                                x.CantidadIngresada,

                            CantidadConvertidaLbMz =
                                x.CantidadConvertidaLbMz,

                            RequerimientoLbMz =
                                x.RequerimientoCalculado,

                            UnidadResultado = ValorO(
                                x.UnidadResultado,
                                "lb/mz"),

                            Clasificacion =
                                x.Clasificacion?.Trim() ??
                                string.Empty,

                            Observacion =
                                x.Observacion?.Trim() ??
                                string.Empty
                        })
                    .OrderBy(
                        x => x.Elemento,
                        StringComparer.CurrentCultureIgnoreCase)
                    .ToList();

            reporte.Balance = MapearBalance(
                solicitud.BalanceNutricional,
                catalogo);

            reporte.Enmienda = MapearEnmienda(
                solicitud.EnmiendaCalcarea,
                catalogo);

            reporte.FertilizacionMixta = MapearMixta(
                solicitud.FertilizacionMixta,
                reporte.Balance,
                catalogo);

            return reporte;
        }

        public static AnalisisReporte DesdeDetalle(
            AnalisisGuardadoDetalleData detalle,
            AnalisisGuardadoResumen? resumen = null)
        {
            ArgumentNullException.ThrowIfNull(detalle);

            CatalogoReporteLocal catalogo =
                CatalogoReporteLocal.Crear(
                    ObtenerPaqueteLocalSeguro());

            AnalisisGuardadoDatosAnalisis datos =
                detalle.DatosAnalisis;

            AnalisisGuardadoRequerimientoAnual requerimiento =
                detalle.RequerimientoAnual;

            AnalisisReporte reporte = new()
            {
                Identificador = ValorO(
                    datos.IdentificadorAnalisisSuelo,
                    $"Análisis #{datos.AnalisisSueloId}"),

                FechaAnalisis = datos.FechaAnalisisTexto,

                Laboratorio = ValorO(
                    datos.LaboratorioAnalasisSuelo,
                    "No especificado"),

                Cliente = resumen?.ClienteMostrar ??
                    "No disponible",

                Terreno = resumen?.TerrenoMostrar ??
                    $"Terreno #{requerimiento.TerrenoId}",

                TipoCultivo = catalogo.ObtenerTipoCultivo(
                    requerimiento.TipoCultivoId,
                    $"Cultivo #{requerimiento.TipoCultivoId}"),

                TipoAnalisis = catalogo.ObtenerTipoAnalisis(
                    requerimiento.TipoAnalisisSueloId,
                    $"Tipo #{requerimiento.TipoAnalisisSueloId}"),

                ProduccionQqOro =
                    requerimiento.CantidadQuintalesOro,

                TamanoFincaMz = requerimiento.TamanoFinca,
                Ph = requerimiento.Ph,
                MateriaOrganica = requerimiento.MateriaOrganica,

                UnidadMateriaOrganica =
                    catalogo.ObtenerUnidad(
                        requerimiento
                            .UnidadMedidaMateriaOrganicaId,
                        string.Empty),

                AcidezTotal = requerimiento.AcidezTotal,

                RecomendacionGeneral =
                    requerimiento.RecomendacionGeneral ??
                    string.Empty,

                Observaciones = requerimiento.Observaciones?
                    .Where(x =>
                        !string.IsNullOrWhiteSpace(x))
                    .Select(x => x.Trim())
                    .ToList() ?? new()
            };

            reporte.ValoresLaboratorio =
                datos.ElementosQuimicos
                    .Select(x =>
                        new AnalisisReporteValorLaboratorio
                        {
                            Elemento =
                                catalogo.ObtenerElementoMostrar(
                                    x.ElementoQuimicosId,
                                    x.NombreElemento,
                                    x.SimboloElemento),

                            Cantidad = x.CantidadElemento,

                            Unidad = catalogo.ObtenerUnidad(
                                x.UnidadMedidaId,
                                x.UnidadMostrar)
                        })
                    .OrderBy(
                        x => x.Elemento,
                        StringComparer.CurrentCultureIgnoreCase)
                    .ToList();

            reporte.Requerimientos =
                requerimiento.Elementos
                    .Select(x =>
                        new AnalisisReporteRequerimiento
                        {
                            Elemento =
                                catalogo.ObtenerElementoMostrar(
                                    x.ElementoQuimicosId,
                                    x.NombreElemento,
                                    x.SimboloElemento),

                            CantidadIngresada =
                                x.CantidadIngresada,

                            CantidadConvertidaLbMz =
                                x.CantidadConvertidaLbMz,

                            RequerimientoLbMz =
                                x.RequerimientoCalculado,

                            UnidadResultado =
                                catalogo.ObtenerUnidad(
                                    x.UnidadMedidaId,
                                    x.UnidadMostrar),

                            Clasificacion =
                                x.Clasificacion?.Trim() ??
                                string.Empty,

                            Observacion =
                                x.Observacion?.Trim() ??
                                string.Empty
                        })
                    .OrderBy(
                        x => x.Elemento,
                        StringComparer.CurrentCultureIgnoreCase)
                    .ToList();

            reporte.Balance = MapearBalance(
                detalle.BalanceNutricional,
                catalogo);

            reporte.Enmienda = MapearEnmienda(
                detalle.EnmiendaCalcarea,
                catalogo);

            reporte.FertilizacionMixta = MapearMixta(
                detalle.FertilizacionMixta,
                reporte.Balance,
                catalogo);

            return reporte;
        }

        private static AnalisisReporteBalance? MapearBalance(
            GuardarTodoBalanceNutricionalRequest? origen,
            CatalogoReporteLocal catalogo)
        {
            if (origen == null)
                return null;

            GuardarTodoBalanceResultadoRequest resultado =
                origen.Resultado;

            List<GuardarTodoBalanceItemRequest> items =
                origen.Items ?? new();

            List<GuardarTodoBalanceDetalleRequest> origenDetalles =
                resultado.Detalle ?? new();

            List<AnalisisReporteBalanceDetalle> detalles = new();

            for (int indice = 0;
                 indice < origenDetalles.Count;
                 indice++)
            {
                GuardarTodoBalanceDetalleRequest itemDetalle =
                    origenDetalles[indice];

                GuardarTodoBalanceItemRequest? itemSolicitud =
                    indice < items.Count
                        ? items[indice]
                        : null;

                int fuenteId =
                    itemSolicitud?.FuenteNutrientesId ?? 0;

                int elementoId =
                    itemSolicitud?.ElementoQuimicosId ?? 0;

                decimal quintalesComprar =
                    Math.Ceiling(itemDetalle.Qq);

                Dictionary<string, decimal> aportes =
                    NormalizarAportes(
                        itemDetalle.Aportes);

                detalles.Add(
                    new AnalisisReporteBalanceDetalle
                    {
                        FuenteNutrientesId = fuenteId,
                        ElementoQuimicosId = elementoId,

                        Fuente = ValorO(
                            itemDetalle.Fuente,
                            catalogo.ObtenerNombreFuente(
                                fuenteId,
                                $"Fuente #{fuenteId}")),

                        Elemento = ValorO(
                            itemDetalle.Elemento,
                            catalogo.ObtenerElementoMostrar(
                                elementoId)),

                        RequerimientoLibras =
                            itemDetalle.RequerimientoLibras,

                        Libras = itemDetalle.Lb,

                        LibrasPorAplicacion =
                            itemDetalle.LibrasPorAplicacion != 0
                                ? itemDetalle.LibrasPorAplicacion
                                : resultado.TotalAplicaciones > 0
                                    ? itemDetalle.Lb /
                                      resultado.TotalAplicaciones
                                    : 0,

                        QuintalesExactos = itemDetalle.Qq,
                        QuintalesComprar = quintalesComprar,

                        PrecioPorQuintal =
                            itemDetalle.PrecioPorQuintal,

                        SubtotalExacto =
                            itemDetalle.SubtotalFuente,

                        CostoCompra =
                            quintalesComprar *
                            itemDetalle.PrecioPorQuintal,

                        OnzasAnuales =
                            itemDetalle.OnzasAnuales,

                        OnzasPorAplicacion =
                            itemDetalle.OnzasPorAplicacion,

                        Aportes = aportes
                    });
            }

            detalles = detalles
                .OrderBy(
                    x => x.Fuente,
                    StringComparer.CurrentCultureIgnoreCase)
                .ThenBy(
                    x => x.Elemento,
                    StringComparer.CurrentCultureIgnoreCase)
                .ToList();

            decimal costoRealCompra =
                detalles.Sum(x => x.CostoCompra);

            Dictionary<string, decimal> formulaComercial =
                NormalizarAportes(
                    resultado.FormulaComercial);

            if (formulaComercial.Count == 0)
            {
                formulaComercial =
                    ConstruirFormulaComercial(
                        detalles,
                        resultado.MezclaTotalQq);
            }

            return new AnalisisReporteBalance
            {
                NombreFormula =
                    resultado.NombreFormula?.Trim() ??
                    string.Empty,

                TotalLibras = resultado.TotalLibras,
                MezclaTotalQq = resultado.MezclaTotalQq,

                TotalOnzas = resultado.TotalOnzas != 0
                    ? resultado.TotalOnzas
                    : detalles.Sum(x => x.OnzasAnuales),

                TotalPlantas = resultado.TotalPlantas,
                TotalAplicaciones = resultado.TotalAplicaciones,

                DosisPlantaAnualOz =
                    resultado.DosisPlantaAnualOz,

                DosisPlantaPorAplicacionOz =
                    resultado.DosisPlantaPorAplicacionOz,

                PrecioExactoReferencia =
                    detalles.Sum(x => x.SubtotalExacto),

                CostoRealCompra = costoRealCompra,

                PrecioPorAplicacion =
                    resultado.TotalAplicaciones > 0
                        ? costoRealCompra /
                          resultado.TotalAplicaciones
                        : 0,

                FormulaComercial = formulaComercial,
                Detalles = detalles
            };
        }

        private static AnalisisReporteBalance? MapearBalance(
            AnalisisGuardadoBalanceNutricional? origen,
            CatalogoReporteLocal catalogo)
        {
            if (origen == null)
                return null;

            AnalisisGuardadoFormula formula = origen.Formula;

            Dictionary<int, List<AnalisisGuardadoFormulaAporte>>
                aportesPorDetalle = origen.Aportes
                    .GroupBy(x =>
                        x.FormulaNutricionalDetalleId)
                    .ToDictionary(
                        x => x.Key,
                        x => x.ToList());

            List<AnalisisReporteBalanceDetalle> detalles =
                origen.Detalles
                    .Select(x =>
                    {
                        decimal quintalesComprar =
                            Math.Ceiling(x.Qq);

                        Dictionary<string, decimal> aportes =
                            new(
                                StringComparer.OrdinalIgnoreCase);

                        if (aportesPorDetalle.TryGetValue(
                                x.FormulaNutricionalDetalleId,
                                out List<AnalisisGuardadoFormulaAporte>?
                                    aportesGuardados))
                        {
                            aportes = aportesGuardados
                                .GroupBy(a =>
                                    catalogo.ObtenerSimboloElemento(
                                        a.ElementoQuimicosId,
                                        $"E{a.ElementoQuimicosId}"),
                                    StringComparer.OrdinalIgnoreCase)
                                .ToDictionary(
                                    grupo =>
                                        FormatearSimbolo(
                                            grupo.Key),
                                    grupo => Math.Round(
                                        grupo.Sum(a => a.Valor),
                                        4),
                                    StringComparer.OrdinalIgnoreCase);
                        }

                        return new AnalisisReporteBalanceDetalle
                        {
                            FormulaNutricionalDetalleId =
                                x.FormulaNutricionalDetalleId,

                            FuenteNutrientesId =
                                x.FuenteNutrientesId,

                            ElementoQuimicosId =
                                x.ElementoQuimicosId,

                            Fuente = catalogo.ObtenerNombreFuente(
                                x.FuenteNutrientesId,
                                x.FuenteMostrar),

                            Elemento =
                                catalogo.ObtenerElementoMostrar(
                                    x.ElementoQuimicosId,
                                    x.NombreElemento,
                                    null),

                            RequerimientoLibras =
                                x.RequerimientoLibras,

                            Libras = x.Libras,

                            LibrasPorAplicacion =
                                formula.TotalAplicaciones > 0
                                    ? x.Libras /
                                      formula.TotalAplicaciones
                                    : 0,

                            QuintalesExactos = x.Qq,
                            QuintalesComprar = quintalesComprar,
                            PrecioPorQuintal = x.PrecioPorQuintal,
                            SubtotalExacto = x.SubtotalFuente,

                            CostoCompra =
                                quintalesComprar *
                                x.PrecioPorQuintal,

                            OnzasAnuales = x.OnzasAnuales,
                            OnzasPorAplicacion =
                                x.OnzasPorAplicacion,

                            Aportes = aportes
                        };
                    })
                    .OrderBy(
                        x => x.Fuente,
                        StringComparer.CurrentCultureIgnoreCase)
                    .ThenBy(
                        x => x.Elemento,
                        StringComparer.CurrentCultureIgnoreCase)
                    .ToList();

            decimal costoRealCompra =
                detalles.Sum(x => x.CostoCompra);

            return new AnalisisReporteBalance
            {
                NombreFormula =
                    formula.NombreFormula?.Trim() ??
                    string.Empty,

                TotalLibras = formula.TotalLibras,
                MezclaTotalQq = formula.MezclaTotalQq,

                TotalOnzas = formula.TotalOnzas != 0
                    ? formula.TotalOnzas
                    : detalles.Sum(x => x.OnzasAnuales),

                TotalPlantas = formula.TotalPlantas,
                TotalAplicaciones = formula.TotalAplicaciones,

                DosisPlantaAnualOz =
                    formula.DosisPlantaAnualOz,

                DosisPlantaPorAplicacionOz =
                    formula.DosisPlantaPorAplicacionOz,

                PrecioExactoReferencia =
                    detalles.Sum(x => x.SubtotalExacto),

                CostoRealCompra = costoRealCompra,

                PrecioPorAplicacion =
                    formula.TotalAplicaciones > 0
                        ? costoRealCompra /
                          formula.TotalAplicaciones
                        : 0,

                FormulaComercial =
                    ConstruirFormulaComercial(
                        detalles,
                        formula.MezclaTotalQq),

                Detalles = detalles
            };
        }

        private static AnalisisReporteEnmienda? MapearEnmienda(
            GuardarTodoEnmiendaCalcareaRequest? origen,
            CatalogoReporteLocal catalogo)
        {
            if (origen == null)
                return null;

            GuardarTodoEnmiendaResultadoRequest resultado =
                origen.Resultado;

            return new AnalisisReporteEnmienda
            {
                NombreAnalisis =
                    resultado.NombreAnalisis?.Trim() ??
                    string.Empty,

                Fuente = ValorO(
                    resultado.FuenteNutriente,
                    catalogo.ObtenerNombreFuente(
                        origen.FuenteNutrientesId,
                        $"Fuente #{origen.FuenteNutrientesId}")),

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
            AnalisisGuardadoEnmiendaCalcarea? origen,
            CatalogoReporteLocal catalogo)
        {
            if (origen == null)
                return null;

            return new AnalisisReporteEnmienda
            {
                NombreAnalisis =
                    origen.NombreAnalisis?.Trim() ??
                    string.Empty,

                Fuente = catalogo.ObtenerNombreFuente(
                    origen.FuenteNutrientesId,
                    origen.FuenteMostrar),

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
            GuardarTodoFertilizacionMixtaRequest? origen,
            AnalisisReporteBalance? balance,
            CatalogoReporteLocal catalogo)
        {
            if (origen == null)
                return null;

            List<AnalisisReporteMixtaFuente> fuentes =
                origen.Fuentes
                    .Select(x =>
                    {
                        decimal precio =
                            catalogo.ObtenerPrecioFuente(
                                x.FuenteNutrientesId);

                        return new AnalisisReporteMixtaFuente
                        {
                            FuenteNutrientesId =
                                x.FuenteNutrientesId,

                            Fuente = ValorO(
                                x.NombreFuente,
                                catalogo.ObtenerNombreFuente(
                                    x.FuenteNutrientesId,
                                    $"Fuente #{x.FuenteNutrientesId}")),

                            CantidadQq = x.CantidadQq,
                            PrecioPorQq = precio,
                            Costo = x.CantidadQq * precio
                        };
                    })
                    .OrderBy(
                        x => x.Fuente,
                        StringComparer.CurrentCultureIgnoreCase)
                    .ToList();

            List<AnalisisReporteMixtaDetalle> detalles =
                origen.Detalles
                    .Select(x =>
                        new AnalisisReporteMixtaDetalle
                        {
                            ElementoQuimicosId =
                                x.ElementoQuimicosId,

                            Elemento = ValorO(
                                x.Elemento,
                                catalogo.ObtenerElementoMostrar(
                                    x.ElementoQuimicosId)),

                            RequerimientoOriginal = x.Exportable,
                            AporteOrganico = x.AporteOrganico,
                            Diferencia = x.Diferencia,
                            Deficit = x.Deficit,
                            Sobrante = x.Sobrante
                        })
                    .OrderBy(
                        x => x.Elemento,
                        StringComparer.CurrentCultureIgnoreCase)
                    .ToList();

            List<AnalisisReporteMixtaAporteFuente>
                aportesPorFuente = origen.Detalles
                    .SelectMany(detalle =>
                        detalle.Fuentes.Select(fuente =>
                            new AnalisisReporteMixtaAporteFuente
                            {
                                FuenteNutrientesId =
                                    fuente.FuenteNutrientesId,

                                ElementoQuimicosId =
                                    detalle.ElementoQuimicosId,

                                Fuente = ValorO(
                                    fuente.NombreFuente,
                                    catalogo.ObtenerNombreFuente(
                                        fuente.FuenteNutrientesId,
                                        $"Fuente #{fuente.FuenteNutrientesId}")),

                                Elemento =
                                    catalogo.ObtenerSimboloElemento(
                                        detalle.ElementoQuimicosId,
                                        $"E{detalle.ElementoQuimicosId}"),

                                CantidadQq = fuente.CantidadQq,
                                AportePorQq = fuente.AportePorUnidad,
                                AporteTotal = fuente.AporteTotal
                            }))
                    .ToList();

            /*
             * Los análisis creados con versiones anteriores podían no
             * incluir el detalle de aportes dentro de la solicitud. En
             * ese caso se reconstruye con la composición del paquete
             * descargado, igual que lo hace el reporte en línea.
             */
            if (aportesPorFuente.Count == 0)
            {
                Dictionary<int, AnalisisReporteMixtaFuente>
                    fuentePorId = fuentes
                        .GroupBy(x => x.FuenteNutrientesId)
                        .ToDictionary(
                            x => x.Key,
                            x => x.First());

                HashSet<int> elementosIds = detalles
                    .Select(x => x.ElementoQuimicosId)
                    .ToHashSet();

                aportesPorFuente = catalogo.AportesFuentes
                    .Where(x =>
                        x.Activo &&
                        fuentePorId.ContainsKey(
                            x.FuenteNutrientesId) &&
                        elementosIds.Contains(
                            x.ElementoQuimicosId))
                    .Select(x =>
                    {
                        AnalisisReporteMixtaFuente fuente =
                            fuentePorId[x.FuenteNutrientesId];

                        return new AnalisisReporteMixtaAporteFuente
                        {
                            FuenteNutrientesId =
                                x.FuenteNutrientesId,

                            ElementoQuimicosId =
                                x.ElementoQuimicosId,

                            Fuente = fuente.Fuente,

                            Elemento =
                                catalogo.ObtenerSimboloElemento(
                                    x.ElementoQuimicosId,
                                    $"E{x.ElementoQuimicosId}"),

                            CantidadQq = fuente.CantidadQq,
                            AportePorQq = x.CantidadAporte,

                            AporteTotal =
                                fuente.CantidadQq *
                                x.CantidadAporte
                        };
                    })
                    .ToList();
            }

            aportesPorFuente = aportesPorFuente
                .OrderBy(
                    x => x.Fuente,
                    StringComparer.CurrentCultureIgnoreCase)
                .ThenBy(
                    x => x.Elemento,
                    StringComparer.CurrentCultureIgnoreCase)
                .ToList();

            return ConstruirMixtaCompleta(
                origen.Observacion,
                origen.EsComplementoBalance,
                fuentes,
                detalles,
                aportesPorFuente,
                balance);
        }

        private static AnalisisReporteFertilizacionMixta? MapearMixta(
            AnalisisGuardadoFertilizacionMixta? origen,
            AnalisisReporteBalance? balance,
            CatalogoReporteLocal catalogo)
        {
            if (origen == null)
                return null;

            List<AnalisisReporteMixtaFuente> fuentes =
                origen.Fuentes
                    .Select(x =>
                    {
                        decimal precio =
                            catalogo.ObtenerPrecioFuente(
                                x.FuenteNutrientesId);

                        return new AnalisisReporteMixtaFuente
                        {
                            FuenteNutrientesId =
                                x.FuenteNutrientesId,

                            Fuente = catalogo.ObtenerNombreFuente(
                                x.FuenteNutrientesId,
                                x.FuenteMostrar),

                            CantidadQq = x.CantidadQq,
                            PrecioPorQq = precio,
                            Costo = x.CantidadQq * precio
                        };
                    })
                    .OrderBy(
                        x => x.Fuente,
                        StringComparer.CurrentCultureIgnoreCase)
                    .ToList();

            List<AnalisisReporteMixtaDetalle> detalles =
                origen.Detalles
                    .Select(x =>
                        new AnalisisReporteMixtaDetalle
                        {
                            ElementoQuimicosId =
                                x.ElementoQuimicosId,

                            Elemento =
                                catalogo.ObtenerElementoMostrar(
                                    x.ElementoQuimicosId,
                                    x.NombreElemento,
                                    null),

                            RequerimientoOriginal =
                                x.RequerimientoOriginal,

                            AporteOrganico = x.AporteOrganico,
                            Diferencia = x.Diferencia,
                            Deficit = x.Deficit,
                            Sobrante = x.Sobrante
                        })
                    .OrderBy(
                        x => x.Elemento,
                        StringComparer.CurrentCultureIgnoreCase)
                    .ToList();

            Dictionary<int, AnalisisReporteMixtaFuente>
                fuentePorId = fuentes
                    .GroupBy(x => x.FuenteNutrientesId)
                    .ToDictionary(
                        x => x.Key,
                        x => x.First());

            HashSet<int> elementosIds = detalles
                .Select(x => x.ElementoQuimicosId)
                .ToHashSet();

            List<AnalisisReporteMixtaAporteFuente>
                aportesPorFuente = catalogo.AportesFuentes
                    .Where(x =>
                        x.Activo &&
                        fuentePorId.ContainsKey(
                            x.FuenteNutrientesId) &&
                        elementosIds.Contains(
                            x.ElementoQuimicosId))
                    .Select(x =>
                    {
                        AnalisisReporteMixtaFuente fuente =
                            fuentePorId[x.FuenteNutrientesId];

                        return new AnalisisReporteMixtaAporteFuente
                        {
                            FuenteNutrientesId =
                                x.FuenteNutrientesId,

                            ElementoQuimicosId =
                                x.ElementoQuimicosId,

                            Fuente = fuente.Fuente,

                            Elemento =
                                catalogo.ObtenerSimboloElemento(
                                    x.ElementoQuimicosId,
                                    $"E{x.ElementoQuimicosId}"),

                            CantidadQq = fuente.CantidadQq,
                            AportePorQq = x.CantidadAporte,

                            AporteTotal =
                                fuente.CantidadQq *
                                x.CantidadAporte
                        };
                    })
                    .OrderBy(
                        x => x.Fuente,
                        StringComparer.CurrentCultureIgnoreCase)
                    .ThenBy(
                        x => x.Elemento,
                        StringComparer.CurrentCultureIgnoreCase)
                    .ToList();

            return ConstruirMixtaCompleta(
                origen.Mixta.Observacion,
                origen.Mixta.EsComplementoBalance,
                fuentes,
                detalles,
                aportesPorFuente,
                balance);
        }

        private static AnalisisReporteFertilizacionMixta
            ConstruirMixtaCompleta(
                string? observacion,
                bool esComplementoBalance,
                List<AnalisisReporteMixtaFuente> fuentes,
                List<AnalisisReporteMixtaDetalle> detalles,
                List<AnalisisReporteMixtaAporteFuente>
                    aportesPorFuente,
                AnalisisReporteBalance? balance)
        {
            AnalisisReporteBalanceAjustado? balanceAjustado =
                null;

            AnalisisReporteResumenEconomico? resumenEconomico =
                null;

            if (esComplementoBalance && balance != null)
            {
                balanceAjustado = ConstruirBalanceAjustado(
                    balance,
                    detalles);

                decimal costoMixta =
                    fuentes.Sum(x => x.Costo);

                decimal costoTotal =
                    costoMixta +
                    balanceAjustado.CostoRealCompra;

                resumenEconomico =
                    new AnalisisReporteResumenEconomico
                    {
                        CostoComercialOriginal =
                            balance.CostoRealCompra,

                        CostoFertilizacionMixta = costoMixta,

                        CostoComercialAjustado =
                            balanceAjustado.CostoRealCompra,

                        CostoTotalFinal = costoTotal,

                        DiferenciaEconomica =
                            balance.CostoRealCompra -
                            costoTotal,

                        EsAhorro =
                            balance.CostoRealCompra -
                            costoTotal >= 0
                    };
            }

            return new AnalisisReporteFertilizacionMixta
            {
                Observacion = observacion?.Trim() ??
                    string.Empty,

                EsComplementoBalance =
                    esComplementoBalance,

                Fuentes = fuentes,
                Detalles = detalles,
                AportesPorFuente = aportesPorFuente,
                BalanceAjustado = balanceAjustado,
                ResumenEconomico = resumenEconomico
            };
        }

        private static AnalisisReporteBalanceAjustado
            ConstruirBalanceAjustado(
                AnalisisReporteBalance balance,
                IReadOnlyCollection<
                    AnalisisReporteMixtaDetalle> detallesMixta)
        {
            List<AnalisisReporteCompraAjustada> detalles =
                balance.Detalles
                    .Select(original =>
                    {
                        AnalisisReporteMixtaDetalle? mixta =
                            detallesMixta.FirstOrDefault(x =>
                                x.ElementoQuimicosId ==
                                original.ElementoQuimicosId);

                        decimal aporteOrganico =
                            mixta?.AporteOrganico ?? 0;

                        decimal requerimientoAjustado =
                            Math.Max(
                                original.RequerimientoLibras -
                                aporteOrganico,
                                0);

                        decimal quintalesAjustados =
                            requerimientoAjustado / 100m;

                        decimal factor =
                            original.QuintalesExactos > 0
                                ? quintalesAjustados /
                                  original.QuintalesExactos
                                : 0;

                        decimal quintalesComprar =
                            Math.Ceiling(
                                quintalesAjustados);

                        Dictionary<string, decimal> aportes =
                            original.Aportes.ToDictionary(
                                x => x.Key,
                                x => Math.Round(
                                    x.Value * factor,
                                    4),
                                StringComparer.OrdinalIgnoreCase);

                        return new AnalisisReporteCompraAjustada
                        {
                            FuenteNutrientesId =
                                original.FuenteNutrientesId,

                            ElementoQuimicosId =
                                original.ElementoQuimicosId,

                            Fuente = original.Fuente,
                            Elemento = original.Elemento,

                            RequerimientoOriginalLb =
                                original.RequerimientoLibras,

                            AporteOrganicoLb = aporteOrganico,

                            RequerimientoAjustadoLb =
                                requerimientoAjustado,

                            QuintalesOriginales =
                                original.QuintalesExactos,

                            QuintalesAjustados =
                                quintalesAjustados,

                            ReduccionQuintales = Math.Max(
                                original.QuintalesExactos -
                                quintalesAjustados,
                                0),

                            PrecioPorQq =
                                original.PrecioPorQuintal,

                            QuintalesComprar =
                                quintalesComprar,

                            SubtotalExacto =
                                quintalesAjustados *
                                original.PrecioPorQuintal,

                            CostoCompra =
                                quintalesComprar *
                                original.PrecioPorQuintal,

                            Aportes = aportes
                        };
                    })
                    .OrderBy(
                        x => x.Fuente,
                        StringComparer.CurrentCultureIgnoreCase)
                    .ThenBy(
                        x => x.Elemento,
                        StringComparer.CurrentCultureIgnoreCase)
                    .ToList();

            decimal totalLibras =
                detalles.Sum(x =>
                    x.RequerimientoAjustadoLb);

            decimal mezclaTotalQq =
                totalLibras / 100m;

            decimal totalOnzas =
                totalLibras * 16m;

            decimal costoReal =
                detalles.Sum(x => x.CostoCompra);

            Dictionary<string, decimal> formulaComercial =
                detalles
                    .SelectMany(x => x.Aportes)
                    .GroupBy(
                        x => x.Key,
                        StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(
                        grupo => grupo.Key,
                        grupo => mezclaTotalQq > 0
                            ? Math.Round(
                                grupo.Sum(x => x.Value) /
                                mezclaTotalQq,
                                4)
                            : 0,
                        StringComparer.OrdinalIgnoreCase);

            return new AnalisisReporteBalanceAjustado
            {
                NombreFormula =
                    $"{balance.NombreFormula} - Ajustado",

                TotalLibras = totalLibras,
                MezclaTotalQq = mezclaTotalQq,
                TotalOnzas = totalOnzas,
                TotalPlantas = balance.TotalPlantas,
                TotalAplicaciones = balance.TotalAplicaciones,

                DosisPlantaAnualOz =
                    balance.TotalPlantas > 0
                        ? totalOnzas /
                          balance.TotalPlantas
                        : 0,

                DosisPlantaPorAplicacionOz =
                    balance.TotalPlantas > 0 &&
                    balance.TotalAplicaciones > 0
                        ? totalOnzas /
                          balance.TotalPlantas /
                          balance.TotalAplicaciones
                        : 0,

                PrecioExactoReferencia =
                    detalles.Sum(x => x.SubtotalExacto),

                CostoRealCompra = costoReal,

                PrecioPorAplicacion =
                    balance.TotalAplicaciones > 0
                        ? costoReal /
                          balance.TotalAplicaciones
                        : 0,

                FormulaComercial = formulaComercial,
                Detalles = detalles
            };
        }

        private static Dictionary<string, decimal>
            ConstruirFormulaComercial(
                IEnumerable<AnalisisReporteBalanceDetalle>
                    detalles,
                decimal mezclaTotalQq)
        {
            if (mezclaTotalQq <= 0)
            {
                return new Dictionary<string, decimal>(
                    StringComparer.OrdinalIgnoreCase);
            }

            return detalles
                .SelectMany(x => x.Aportes)
                .GroupBy(
                    x => FormatearSimbolo(x.Key),
                    StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    grupo => grupo.Key,
                    grupo => Math.Round(
                        grupo.Sum(x => x.Value) /
                        mezclaTotalQq,
                        4),
                    StringComparer.OrdinalIgnoreCase);
        }

        private static Dictionary<string, decimal>
            NormalizarAportes(
                IDictionary<string, decimal>? origen)
        {
            if (origen == null || origen.Count == 0)
            {
                return new Dictionary<string, decimal>(
                    StringComparer.OrdinalIgnoreCase);
            }

            return origen
                .GroupBy(
                    x => FormatearSimbolo(x.Key),
                    StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    x => x.Key,
                    x => Math.Round(
                        x.Sum(item => item.Value),
                        4),
                    StringComparer.OrdinalIgnoreCase);
        }

        private static string FormatearElemento(
            string? nombre,
            string? simbolo,
            int id)
        {
            string nombreLimpio =
                nombre?.Trim() ??
                string.Empty;

            string simboloLimpio =
                simbolo?.Trim() ??
                string.Empty;

            if (!string.IsNullOrWhiteSpace(
                    nombreLimpio) &&
                !string.IsNullOrWhiteSpace(
                    simboloLimpio))
            {
                return $"{nombreLimpio} ({simboloLimpio})";
            }

            return !string.IsNullOrWhiteSpace(
                    nombreLimpio)
                ? nombreLimpio
                : !string.IsNullOrWhiteSpace(
                        simboloLimpio)
                    ? simboloLimpio
                    : $"Elemento #{id}";
        }

        private static string FormatearSimbolo(
            string? simbolo)
        {
            string normalizado =
                (simbolo ?? string.Empty)
                    .Trim()
                    .ToUpperInvariant()
                    .Replace(" ", string.Empty);

            return normalizado switch
            {
                "CA" => "Ca",
                "MG" => "Mg",
                "ZN" => "Zn",
                _ => normalizado.Length > 0
                    ? normalizado
                    : "Nutriente"
            };
        }

        private static string FormatearFecha(
            string? valor)
        {
            if (DateTime.TryParse(
                    valor,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out DateTime fecha) ||
                DateTime.TryParse(
                    valor,
                    out fecha))
            {
                return fecha.ToString("dd/MM/yyyy");
            }

            return ValorO(
                valor,
                "No disponible");
        }

        private static string ValorO(
            string? valor,
            string alternativo) =>
            string.IsNullOrWhiteSpace(valor)
                ? alternativo
                : valor.Trim();

        private static MotorCalculoPaquete?
            ObtenerPaqueteLocalSeguro()
        {
            try
            {
                /*
                 * El mapper es síncrono porque también se utiliza desde
                 * generadores de PDF y Excel. El paquete se encuentra en
                 * almacenamiento local y normalmente ya está en memoria.
                 * Task.Run evita bloquear el contexto de interfaz cuando
                 * fuese necesaria una lectura del archivo.
                 */
                return Task.Run(() =>
                        MotorCalculoPaqueteService.Instance
                            .ObtenerPaqueteActivoAsync())
                    .GetAwaiter()
                    .GetResult();
            }
            catch
            {
                return null;
            }
        }

        private sealed class CatalogoReporteLocal
        {
            private readonly Dictionary<int, MotorElemento>
                elementos;

            private readonly Dictionary<int, MotorUnidad>
                unidades;

            private readonly Dictionary<int, MotorFuenteNutriente>
                fuentes;

            private readonly Dictionary<int, MotorTipoCultivo>
                cultivos;

            private readonly Dictionary<int, MotorTipoAnalisis>
                tiposAnalisis;

            public IReadOnlyList<MotorFuenteAporte>
                AportesFuentes { get; }

            private CatalogoReporteLocal(
                Dictionary<int, MotorElemento> elementos,
                Dictionary<int, MotorUnidad> unidades,
                Dictionary<int, MotorFuenteNutriente> fuentes,
                Dictionary<int, MotorTipoCultivo> cultivos,
                Dictionary<int, MotorTipoAnalisis>
                    tiposAnalisis,
                IReadOnlyList<MotorFuenteAporte>
                    aportesFuentes)
            {
                this.elementos = elementos;
                this.unidades = unidades;
                this.fuentes = fuentes;
                this.cultivos = cultivos;
                this.tiposAnalisis = tiposAnalisis;
                AportesFuentes = aportesFuentes;
            }

            public static CatalogoReporteLocal Crear(
                MotorCalculoPaquete? paquete)
            {
                MotorCalculoContenido? contenido =
                    paquete?.Contenido;

                return new CatalogoReporteLocal(
                    CrearDiccionario(
                        (contenido?.Elementos ??
                         new List<MotorElemento>())
                            .Where(x => x.Activo),
                        x => x.ElementoQuimicosId),

                    CrearDiccionario(
                        (contenido?.Unidades ??
                         new List<MotorUnidad>())
                            .Where(x => x.Activo),
                        x => x.UnidadMedidaId),

                    CrearDiccionario(
                        (contenido?.FuentesNutrientes ??
                         new List<MotorFuenteNutriente>())
                            .Where(x => x.Activo),
                        x => x.FuenteNutrientesId),

                    CrearDiccionario(
                        (contenido?.TiposCultivo ??
                         new List<MotorTipoCultivo>())
                            .Where(x => x.Activo),
                        x => x.TipoCultivoId),

                    CrearDiccionario(
                        (contenido?.TiposAnalisis ??
                         new List<MotorTipoAnalisis>())
                            .Where(x => x.Activo),
                        x => x.TipoAnalisisSueloId),

                    contenido?.AportesFuentes?
                        .ToList() ??
                    new List<MotorFuenteAporte>());
            }

            public string ObtenerElementoMostrar(
                int elementoId,
                string? nombreRespaldo = null,
                string? simboloRespaldo = null)
            {
                if (elementos.TryGetValue(
                        elementoId,
                        out MotorElemento? elemento))
                {
                    return FormatearElemento(
                        elemento.NombreElementoQuimico,
                        elemento.SimboloElementoQuimico,
                        elementoId);
                }

                return FormatearElemento(
                    nombreRespaldo,
                    simboloRespaldo,
                    elementoId);
            }

            public string ObtenerSimboloElemento(
                int elementoId,
                string respaldo)
            {
                if (elementos.TryGetValue(
                        elementoId,
                        out MotorElemento? elemento) &&
                    !string.IsNullOrWhiteSpace(
                        elemento.SimboloElementoQuimico))
                {
                    return FormatearSimbolo(
                        elemento.SimboloElementoQuimico);
                }

                return FormatearSimbolo(respaldo);
            }

            public string ObtenerUnidad(
                int? unidadId,
                string respaldo)
            {
                if (unidadId.HasValue &&
                    unidades.TryGetValue(
                        unidadId.Value,
                        out MotorUnidad? unidad) &&
                    !string.IsNullOrWhiteSpace(
                        unidad.NombreUnidadMedida))
                {
                    return unidad.NombreUnidadMedida.Trim();
                }

                return respaldo?.Trim() ??
                    string.Empty;
            }

            public string ObtenerNombreFuente(
                int fuenteId,
                string respaldo)
            {
                if (fuentes.TryGetValue(
                        fuenteId,
                        out MotorFuenteNutriente? fuente) &&
                    !string.IsNullOrWhiteSpace(
                        fuente.NombreNutriente))
                {
                    return fuente.NombreNutriente.Trim();
                }

                return respaldo?.Trim() ??
                    $"Fuente #{fuenteId}";
            }

            public decimal ObtenerPrecioFuente(
                int fuenteId) =>
                fuentes.TryGetValue(
                    fuenteId,
                    out MotorFuenteNutriente? fuente)
                        ? fuente.PrecioNutriente
                        : 0;

            public string ObtenerTipoCultivo(
                int cultivoId,
                string respaldo)
            {
                if (cultivos.TryGetValue(
                        cultivoId,
                        out MotorTipoCultivo? cultivo) &&
                    !string.IsNullOrWhiteSpace(
                        cultivo.NombreTipoCultivo))
                {
                    return cultivo.NombreTipoCultivo.Trim();
                }

                return respaldo;
            }

            public string ObtenerTipoAnalisis(
                int tipoId,
                string respaldo)
            {
                if (tiposAnalisis.TryGetValue(
                        tipoId,
                        out MotorTipoAnalisis? tipo) &&
                    !string.IsNullOrWhiteSpace(
                        tipo.NombreTipoAnalisisSuelo))
                {
                    return tipo.NombreTipoAnalisisSuelo.Trim();
                }

                return respaldo;
            }

            private static Dictionary<int, T>
                CrearDiccionario<T>(
                    IEnumerable<T>? items,
                    Func<T, int> obtenerId)
            {
                return (items ?? Enumerable.Empty<T>())
                    .GroupBy(obtenerId)
                    .ToDictionary(
                        x => x.Key,
                        x => x.First());
            }
        }
    }
}
