using CONATRADEC.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace CONATRADEC.Services
{
    public sealed class AnalisisEdicionContexto
    {
        public int AnalisisSueloCalculoId { get; set; }

        public AnalisisGuardadoResumen? Resumen { get; set; }

        public AnalisisGuardadoDetalleData Detalle { get; set; } = new();

        public AnalisisSueloGuardarCalculoRequest RequestOriginal { get; set; } = new();

        public AnalisisSueloGuardarCalculoRequest RequestActual { get; set; } = new();

        public AnalisisSueloCalculoDataResponse ResultadoOriginal { get; set; } = new();

        public List<TerrenoResponse> Terrenos { get; set; } = new();

        public List<TipoCultivoResponse> TiposCultivo { get; set; } = new();

        public List<UnidadMedidaResponse> UnidadesMedida { get; set; } = new();

        public List<ElementoQuimicoResponse> ElementosCatalogo { get; set; } = new();

        public List<FuenteNutrienteResponse> FuentesCatalogo { get; set; } = new();

        public int CantidadPlantas { get; set; }

        public string ClaveRequerimientoOriginal { get; set; } = string.Empty;

        public string ClaveBalanceOriginal { get; set; } = string.Empty;

        public string ClaveEnmiendaOriginal { get; set; } = string.Empty;

        public bool TieneBalance => Detalle.BalanceNutricional != null;

        public bool TieneEnmienda => Detalle.EnmiendaCalcarea != null;

        public bool TieneMixta => Detalle.FertilizacionMixta != null;
    }

    public sealed class AnalisisEdicionService
    {
        private static readonly Lazy<AnalisisEdicionService> instancia =
            new(() => new AnalisisEdicionService());

        public static AnalisisEdicionService Instance => instancia.Value;

        private readonly GuardarTodoApiService guardarTodoApiService = new();
        private readonly TerrenoApiService terrenoApiService = new();
        private readonly AnalisisSueloApiService analisisSueloApiService = new();
        private readonly UnidadMedidaApiService unidadMedidaApiService = new();
        private readonly ElementoQuimicoApiService elementoApiService = new();
        private readonly FuenteNutrienteApiService fuenteApiService = new();
        private readonly BalanceNutricionalApiService balanceApiService = new();
        private readonly EnmiendaCalcareaApiService enmiendaApiService = new();
        private readonly FertilizacionMixtaApiService mixtaApiService = new();

        private AnalisisEdicionService()
        {
        }

        public AnalisisEdicionContexto? ContextoActual { get; private set; }

        public bool EsModoEdicion => ContextoActual != null;

        public bool RestauracionUiRealizada { get; set; }

        public async Task<(bool Success, string Message)> PrepararAsync(
            int analisisSueloCalculoId,
            AnalisisGuardadoResumen? resumen)
        {
            if (analisisSueloCalculoId <= 0)
                return (false, "El identificador del análisis no es válido.");

            try
            {
                Task<AnalisisGuardadoDetalleResponse> tareaDetalle =
                    guardarTodoApiService.ObtenerDetalleAsync(analisisSueloCalculoId);

                Task<ObservableCollection<TerrenoResponse>> tareaTerrenos =
                    terrenoApiService.GetTerrenosAsync();

                Task<ObservableCollection<TipoCultivoResponse>> tareaCultivos =
                    analisisSueloApiService.ListarTiposCultivoAsync();

                Task<ObservableCollection<UnidadMedidaResponse>> tareaUnidades =
                    unidadMedidaApiService.GetUnidadMedidaAsync();

                Task<ObservableCollection<ElementoQuimicoResponse>> tareaElementos =
                    elementoApiService.GetElementoQuimicoAsync();

                Task<ObservableCollection<FuenteNutrienteResponse>> tareaFuentes =
                    fuenteApiService.GetFuenteNutrienteAsync();

                await Task.WhenAll(
                    tareaDetalle,
                    tareaTerrenos,
                    tareaCultivos,
                    tareaUnidades,
                    tareaElementos,
                    tareaFuentes);

                AnalisisGuardadoDetalleResponse respuestaDetalle =
                    await tareaDetalle;

                if (!respuestaDetalle.Success || respuestaDetalle.Data == null)
                {
                    return (
                        false,
                        string.IsNullOrWhiteSpace(respuestaDetalle.Message)
                            ? "No fue posible cargar el análisis."
                            : respuestaDetalle.Message);
                }

                AnalisisGuardadoDetalleData detalle = respuestaDetalle.Data;
                ObservableCollection<TerrenoResponse> terrenosApi =
                    await tareaTerrenos;

                ObservableCollection<TipoCultivoResponse> cultivosApi =
                    await tareaCultivos;

                ObservableCollection<UnidadMedidaResponse> unidadesApi =
                    await tareaUnidades;

                ObservableCollection<ElementoQuimicoResponse> elementosApi =
                    await tareaElementos;

                ObservableCollection<FuenteNutrienteResponse> fuentesApi =
                    await tareaFuentes;

                AnalisisEdicionContexto contexto = await Task.Run(() =>
                {
                    List<TerrenoResponse> terrenos = terrenosApi.ToList();
                    List<TipoCultivoResponse> cultivos = cultivosApi.ToList();
                    List<UnidadMedidaResponse> unidades = unidadesApi.ToList();
                    List<ElementoQuimicoResponse> elementos = elementosApi.ToList();
                    List<FuenteNutrienteResponse> fuentes = fuentesApi.ToList();

                    AnalisisSueloGuardarCalculoRequest request =
                        ConstruirRequestGuardado(detalle);

                    AnalisisSueloCalculoDataResponse resultado =
                        ConstruirResultadoGuardado(
                            detalle,
                            cultivos,
                            elementos);

                    int plantas =
                        ObtenerCantidadPlantas(
                            detalle,
                            terrenos);

                    return new AnalisisEdicionContexto
                    {
                        AnalisisSueloCalculoId =
                            analisisSueloCalculoId,
                        Resumen = resumen,
                        Detalle = detalle,
                        RequestOriginal = request,
                        RequestActual = ClonarRequest(request),
                        ResultadoOriginal = resultado,
                        Terrenos = terrenos,
                        TiposCultivo = cultivos,
                        UnidadesMedida = unidades,
                        ElementosCatalogo = elementos,
                        FuentesCatalogo = fuentes,
                        CantidadPlantas = plantas,
                        ClaveRequerimientoOriginal =
                            ConstruirClaveRequerimiento(request),
                        ClaveBalanceOriginal =
                            ConstruirClaveBalance(
                                request,
                                plantas,
                                detalle.BalanceNutricional?.Formula.TotalAplicaciones ?? 0),
                        ClaveEnmiendaOriginal =
                            ConstruirClaveEnmienda(
                                request,
                                plantas,
                                detalle.EnmiendaCalcarea?.TotalAplicaciones ?? 0)
                    };
                });

                ContextoActual = contexto;

                RestauracionUiRealizada = false;
                return (true, string.Empty);
            }
            catch (Exception ex)
            {
                Limpiar();
                return (false, $"No fue posible preparar la edición: {ex.Message}");
            }
        }

        public void GuardarFormularioActual(
            AnalisisSueloGuardarCalculoRequest request,
            int cantidadPlantas)
        {
            if (ContextoActual == null)
                return;

            ContextoActual.RequestActual = ClonarRequest(request);
            ContextoActual.CantidadPlantas = cantidadPlantas;
        }

        public void Limpiar()
        {
            ContextoActual = null;
            RestauracionUiRealizada = false;
        }

        public bool CambioRequerimiento(
            AnalisisSueloGuardarCalculoRequest request)
        {
            if (ContextoActual == null)
                return false;

            return !string.Equals(
                ContextoActual.ClaveRequerimientoOriginal,
                ConstruirClaveRequerimiento(request),
                StringComparison.Ordinal);
        }

        public bool CambioBalance(
            AnalisisSueloGuardarCalculoRequest request,
            int plantas)
        {
            if (ContextoActual?.Detalle.BalanceNutricional == null)
                return false;

            int aplicaciones =
                ContextoActual.Detalle.BalanceNutricional.Formula.TotalAplicaciones;

            return !string.Equals(
                ContextoActual.ClaveBalanceOriginal,
                ConstruirClaveBalance(request, plantas, aplicaciones),
                StringComparison.Ordinal);
        }

        public bool CambioEnmienda(
            AnalisisSueloGuardarCalculoRequest request,
            int plantas)
        {
            if (ContextoActual?.Detalle.EnmiendaCalcarea == null)
                return false;

            int aplicaciones =
                ContextoActual.Detalle.EnmiendaCalcarea.TotalAplicaciones;

            return !string.Equals(
                ContextoActual.ClaveEnmiendaOriginal,
                ConstruirClaveEnmienda(request, plantas, aplicaciones),
                StringComparison.Ordinal);
        }

        public async Task RestaurarTemporalAsync(
            AnalisisSueloCalculoDataResponse resultado,
            AnalisisSueloGuardarCalculoRequest request,
            int plantas,
            bool requerimientoCambio)
        {
            AnalisisEdicionContexto contexto =
                ContextoActual ??
                throw new InvalidOperationException(
                    "No existe un análisis preparado para edición.");

            CalculoAnalisisTemporalService temporal =
                CalculoAnalisisTemporalService.Instance;

            await temporal.LimpiarTodoAsync();
            await temporal.IniciarNuevoCalculoAsync(resultado, request);

            if (contexto.TieneBalance)
            {
                await RestaurarBalanceAsync(
                    contexto,
                    resultado,
                    plantas,
                    requerimientoCambio || CambioBalance(request, plantas));
            }

            if (contexto.TieneEnmienda)
            {
                await RestaurarEnmiendaAsync(
                    contexto,
                    request,
                    plantas,
                    CambioEnmienda(request, plantas));
            }

            if (contexto.TieneMixta)
            {
                await RestaurarMixtaAsync(
                    contexto,
                    resultado,
                    requerimientoCambio);
            }

            RestauracionUiRealizada = false;
        }

        private async Task RestaurarBalanceAsync(
            AnalisisEdicionContexto contexto,
            AnalisisSueloCalculoDataResponse resultadoAnual,
            int plantas,
            bool recalcular)
        {
            AnalisisGuardadoBalanceNutricional guardado =
                contexto.Detalle.BalanceNutricional!;

            BalanceNutricionalRequest request =
                ConstruirRequestBalance(guardado, resultadoAnual, plantas);

            BalanceNutricionalResponse resultado;

            if (recalcular)
            {
                BalanceNutricionalResponse? recalculado =
                    await balanceApiService.CalcularAsync(request);

                if (recalculado?.Success != true)
                {
                    throw new InvalidOperationException(
                        recalculado?.Message ??
                        "No fue posible actualizar automáticamente el balance.");
                }

                resultado = recalculado;
            }
            else
            {
                resultado =
                    ConstruirResultadoBalanceGuardado(contexto, guardado);
            }

            await CalculoAnalisisTemporalService.Instance.GuardarCalculoAsync(
                TipoCalculoTemporal.BalanceFormula,
                request,
                resultado,
                recalcular
                    ? "Balance actualizado automáticamente con los cambios del análisis."
                    : "Balance guardado cargado para edición.");
        }

        private async Task RestaurarEnmiendaAsync(
            AnalisisEdicionContexto contexto,
            AnalisisSueloGuardarCalculoRequest requestAnalisis,
            int plantas,
            bool recalcular)
        {
            AnalisisGuardadoEnmiendaCalcarea guardado =
                contexto.Detalle.EnmiendaCalcarea!;

            EnmiendaCalcareaCalcularRequest request = new()
            {
                NombreAnalisis =
                    requestAnalisis.IdentificadorAnalisisSuelo ??
                    guardado.NombreAnalisis,
                FuenteNutrientesId = guardado.FuenteNutrientesId,
                Ph = requestAnalisis.Ph ?? guardado.Ph,
                Ca = requestAnalisis.CalcioCice ?? guardado.Ca,
                Mg = requestAnalisis.MagnesioCice ?? guardado.Mg,
                K = requestAnalisis.PotasioCice ?? guardado.K,
                AcidezTotal =
                    requestAnalisis.AcidezTotal ?? guardado.AcidezTotal,
                TerrenoId =
                    requestAnalisis.TerrenoId ?? guardado.TerrenoId ?? 0,
                TotalPlantas =
                    plantas > 0 ? plantas : guardado.TotalPlantas,
                TotalAplicaciones =
                    guardado.TotalAplicaciones > 0
                        ? guardado.TotalAplicaciones
                        : 3
            };

            EnmiendaCalcareaCalcularResponse resultado;

            if (recalcular)
            {
                EnmiendaCalcareaCalcularResponse? recalculado =
                    await enmiendaApiService
                        .CalcularEnmiendaCalcareaAsync(request);

                if (recalculado == null)
                {
                    throw new InvalidOperationException(
                        "No fue posible actualizar automáticamente la enmienda calcárea.");
                }

                resultado = recalculado;
            }
            else
            {
                resultado =
                    ConstruirResultadoEnmiendaGuardado(contexto, guardado);
            }

            await CalculoAnalisisTemporalService.Instance.GuardarCalculoAsync(
                TipoCalculoTemporal.EnmiendaCalcarea,
                request,
                resultado,
                recalcular
                    ? "Enmienda actualizada automáticamente con los cambios del análisis."
                    : "Enmienda guardada cargada para edición.");
        }

        private async Task RestaurarMixtaAsync(
            AnalisisEdicionContexto contexto,
            AnalisisSueloCalculoDataResponse resultadoAnual,
            bool requerimientoCambio)
        {
            AnalisisGuardadoFertilizacionMixta guardado =
                contexto.Detalle.FertilizacionMixta!;

            FertilizacionMixtaCalcularRequest request = new()
            {
                Observacion = guardado.Mixta.Observacion,
                Elementos = resultadoAnual.Elementos
                    .Where(x =>
                        x.ElementoQuimicosId.HasValue &&
                        x.ElementoQuimicosId.Value > 0)
                    .Select(x =>
                        new ElementoFertilizacionMixtaRequest
                        {
                            ElementoQuimicosId = x.ElementoQuimicosId,
                            Exportable = x.RequerimientoCalculado ?? 0
                        })
                    .ToList(),
                Fuentes = guardado.Fuentes
                    .Select(x =>
                        new FuenteFertilizacionMixtaRequest
                        {
                            FuenteNutrientesId = x.FuenteNutrientesId,
                            CantidadQq = x.CantidadQq
                        })
                    .ToList()
            };

            FertilizacionMixtaCalculoResponse? calculado =
                await mixtaApiService.CalcularAsync(request);

            FertilizacionMixtaCalculoResponse resultado;

            if (calculado?.Success == true)
            {
                resultado = calculado;
            }
            else if (requerimientoCambio)
            {
                throw new InvalidOperationException(
                    calculado?.Message ??
                    "No fue posible actualizar automáticamente la fertilización mixta.");
            }
            else
            {
                resultado =
                    ConstruirResultadoMixtaGuardado(contexto, guardado);
            }

            await CalculoAnalisisTemporalService.Instance.GuardarCalculoAsync(
                TipoCalculoTemporal.FertilizacionMixta,
                request,
                resultado,
                "Fertilización mixta guardada cargada para edición.");
        }

        private static AnalisisSueloGuardarCalculoRequest
            ConstruirRequestGuardado(
                AnalisisGuardadoDetalleData detalle)
        {
            AnalisisGuardadoRequerimientoAnual anual =
                detalle.RequerimientoAnual;

            AnalisisGuardadoEnmiendaCalcarea? enmienda =
                detalle.EnmiendaCalcarea;

            return new AnalisisSueloGuardarCalculoRequest
            {
                TerrenoId = anual.TerrenoId,
                TipoCultivoId = anual.TipoCultivoId,
                TipoAnalisisSueloId = anual.TipoAnalisisSueloId,
                UsuarioId = detalle.DatosAnalisis.UsuarioId,
                CantidadQuintalesOro = anual.CantidadQuintalesOro,
                TamanoFinca = anual.TamanoFinca,
                Ph = anual.Ph,
                MateriaOrganica = anual.MateriaOrganica ?? 0,
                UnidadMedidaMateriaOrganicaId =
                    anual.UnidadMedidaMateriaOrganicaId,
                AcidezTotal = anual.AcidezTotal ?? 0,
                CalcioCice = enmienda?.Ca ?? 0,
                MagnesioCice = enmienda?.Mg ?? 0,
                PotasioCice = enmienda?.K ?? 0,
                ElementosQuimicos =
                    detalle.DatosAnalisis.ElementosQuimicos
                        .Select(x =>
                            new ElementoQuimicoAnalisisRequest
                            {
                                ElementoQuimicosId = x.ElementoQuimicosId,
                                UnidadMedidaId = x.UnidadMedidaId,
                                CantidadElemento = x.CantidadElemento
                            })
                        .ToList(),
                FuentesOrganicas =
                    new List<FuenteOrganicaAnalisisRequest>(),
                FechaAnalisisSuelo =
                    detalle.DatosAnalisis.FechaAnalisisSuelo,
                LaboratorioAnalasisSuelo =
                    detalle.DatosAnalisis.LaboratorioAnalasisSuelo,
                IdentificadorAnalisisSuelo =
                    detalle.DatosAnalisis.IdentificadorAnalisisSuelo
            };
        }

        private static AnalisisSueloCalculoDataResponse
            ConstruirResultadoGuardado(
                AnalisisGuardadoDetalleData detalle,
                IEnumerable<TipoCultivoResponse> cultivos,
                IEnumerable<ElementoQuimicoResponse> elementosCatalogo)
        {
            AnalisisGuardadoRequerimientoAnual anual =
                detalle.RequerimientoAnual;

            Dictionary<int, ElementoQuimicoResponse> elementos =
                elementosCatalogo
                    .Where(x => x.ElementoQuimicosId.HasValue)
                    .GroupBy(x => x.ElementoQuimicosId!.Value)
                    .ToDictionary(x => x.Key, x => x.First());

            TipoCultivoResponse? cultivo =
                cultivos.FirstOrDefault(x =>
                    x.TipoCultivoId == anual.TipoCultivoId);

            AnalisisSueloCalculoDataResponse resultado = new()
            {
                TerrenoId = anual.TerrenoId,
                TipoCultivoId = anual.TipoCultivoId,
                TipoCultivo =
                    cultivo?.NombreMostrar ??
                    $"Cultivo #{anual.TipoCultivoId}",
                TipoAnalisisSueloId = anual.TipoAnalisisSueloId,
                TipoAnalisisSuelo = "Requerimiento anual",
                CantidadQuintalesOro = anual.CantidadQuintalesOro,
                TamanoFinca = anual.TamanoFinca,
                Ph = anual.Ph,
                AcidezTotal = anual.AcidezTotal ?? 0,
                RecomendacionGeneral = anual.RecomendacionGeneral,
                Observaciones =
                    anual.Observaciones?.ToList() ??
                    new List<string>()
            };

            foreach (AnalisisGuardadoRequerimientoElemento item
                     in anual.Elementos)
            {
                elementos.TryGetValue(
                    item.ElementoQuimicosId,
                    out ElementoQuimicoResponse? catalogo);

                resultado.Elementos.Add(
                    new ElementoResultadoCalculoResponse
                    {
                        ElementoQuimicosId = item.ElementoQuimicosId,
                        SimboloElementoQuimico =
                            catalogo?.SimboloElementoQuimico ?? string.Empty,
                        NombreElementoQuimico =
                            catalogo?.NombreElementoQuimico ??
                            $"Elemento #{item.ElementoQuimicosId}",
                        CantidadIngresada = item.CantidadIngresada,
                        CantidadConvertidaLbMz =
                            item.CantidadConvertidaLbMz,
                        RequerimientoCalculado =
                            item.RequerimientoCalculado,
                        UnidadMedidaResultadoId = item.UnidadMedidaId,
                        Clasificacion = item.Clasificacion,
                        Observacion = item.Observacion
                    });
            }

            return resultado;
        }

        private static int ObtenerCantidadPlantas(
            AnalisisGuardadoDetalleData detalle,
            IEnumerable<TerrenoResponse> terrenos)
        {
            if (detalle.EnmiendaCalcarea?.TotalPlantas > 0)
                return detalle.EnmiendaCalcarea.TotalPlantas;

            if (detalle.BalanceNutricional?.Formula.TotalPlantas > 0)
                return detalle.BalanceNutricional.Formula.TotalPlantas;

            TerrenoResponse? terreno =
                terrenos.FirstOrDefault(x =>
                    x.TerrenoId == detalle.RequerimientoAnual.TerrenoId);

            return terreno?.CantidadPlantasTerreno ?? 0;
        }

        private static BalanceNutricionalRequest ConstruirRequestBalance(
            AnalisisGuardadoBalanceNutricional guardado,
            AnalisisSueloCalculoDataResponse resultadoAnual,
            int plantas)
        {
            BalanceNutricionalRequest request = new()
            {
                NombreFormula = guardado.Formula.NombreFormula,
                TerrenoId =
                    guardado.Formula.TerrenoId ?? resultadoAnual.TerrenoId,
                TotalPlantas =
                    plantas > 0
                        ? plantas
                        : guardado.Formula.TotalPlantas,
                TotalAplicaciones =
                    guardado.Formula.TotalAplicaciones
            };

            Dictionary<int, decimal> requerimientos =
                resultadoAnual.Elementos
                    .Where(x => x.ElementoQuimicosId.HasValue)
                    .GroupBy(x => x.ElementoQuimicosId!.Value)
                    .ToDictionary(
                        x => x.Key,
                        x => x.First().RequerimientoCalculado ?? 0);

            foreach (AnalisisGuardadoFormulaDetalle detalle
                     in guardado.Detalles)
            {
                request.Items.Add(
                    new BalanceNutricionalItemRequest
                    {
                        FuenteNutrientesId = detalle.FuenteNutrientesId,
                        ElementoQuimicosId = detalle.ElementoQuimicosId,
                        RequerimientoLibras =
                            requerimientos.TryGetValue(
                                detalle.ElementoQuimicosId,
                                out decimal valor)
                                ? valor
                                : detalle.RequerimientoLibras
                    });
            }

            return request;
        }

        private static BalanceNutricionalResponse
            ConstruirResultadoBalanceGuardado(
                AnalisisEdicionContexto contexto,
                AnalisisGuardadoBalanceNutricional guardado)
        {
            Dictionary<int, ElementoQuimicoResponse> elementosPorId =
                contexto.ElementosCatalogo
                    .Where(x => x.ElementoQuimicosId.HasValue)
                    .GroupBy(x => x.ElementoQuimicosId!.Value)
                    .ToDictionary(x => x.Key, x => x.First());

            Dictionary<int, FuenteNutrienteResponse> fuentesPorId =
                contexto.FuentesCatalogo
                    .Where(x => x.FuenteNutrientesId.HasValue)
                    .GroupBy(x => x.FuenteNutrientesId!.Value)
                    .ToDictionary(x => x.Key, x => x.First());

            BalanceNutricionalResponse resultado = new()
            {
                Success = true,
                FormulaNutricionalId =
                    guardado.Formula.FormulaNutricionalId,
                NombreFormula = guardado.Formula.NombreFormula,
                TotalLibras = guardado.Formula.TotalLibras,
                MezclaTotalQq = guardado.Formula.MezclaTotalQq,
                TotalOnzas = guardado.Formula.TotalOnzas,
                TotalPlantas = guardado.Formula.TotalPlantas,
                TotalAplicaciones = guardado.Formula.TotalAplicaciones,
                PrecioTotalFormula = guardado.Formula.PrecioTotalFormula,
                PrecioPorAplicacion = guardado.Formula.PrecioPorAplicacion,
                DosisPlantaAnualOz = guardado.Formula.DosisPlantaAnualOz,
                DosisPlantaPorAplicacionOz =
                    guardado.Formula.DosisPlantaPorAplicacionOz
            };

            foreach (AnalisisGuardadoFormulaDetalle detalle
                     in guardado.Detalles)
            {
                fuentesPorId.TryGetValue(
                    detalle.FuenteNutrientesId,
                    out FuenteNutrienteResponse? fuente);

                elementosPorId.TryGetValue(
                    detalle.ElementoQuimicosId,
                    out ElementoQuimicoResponse? elemento);

                Dictionary<string, decimal> aportes =
                    guardado.Aportes
                        .Where(x =>
                            x.FormulaNutricionalDetalleId ==
                            detalle.FormulaNutricionalDetalleId)
                        .GroupBy(x =>
                            elementosPorId.TryGetValue(
                                x.ElementoQuimicosId,
                                out ElementoQuimicoResponse? elementoAporte)
                                ? elementoAporte.SimboloElementoQuimico ??
                                  $"E{x.ElementoQuimicosId}"
                                : $"E{x.ElementoQuimicosId}")
                        .ToDictionary(
                            x => x.Key,
                            x => x.Sum(y => y.Valor));

                foreach (KeyValuePair<string, decimal> aporte in aportes)
                {
                    if (resultado.FormulaComercial.ContainsKey(aporte.Key))
                        resultado.FormulaComercial[aporte.Key] += aporte.Value;
                    else
                        resultado.FormulaComercial[aporte.Key] = aporte.Value;
                }

                resultado.Detalle.Add(
                    new BalanceNutricionalDetalleResponse
                    {
                        Fuente =
                            fuente?.NombreNutriente ??
                            $"Fuente #{detalle.FuenteNutrientesId}",
                        Elemento =
                            elemento?.SimboloElementoQuimico ??
                            elemento?.NombreElementoQuimico ??
                            $"Elemento #{detalle.ElementoQuimicosId}",
                        Lb = detalle.Libras,
                        Qq = detalle.Qq,
                        RequerimientoLibras =
                            detalle.RequerimientoLibras,
                        LibrasPorAplicacion =
                            guardado.Formula.TotalAplicaciones > 0
                                ? detalle.Libras /
                                  guardado.Formula.TotalAplicaciones
                                : detalle.Libras,
                        OnzasAnuales = detalle.OnzasAnuales,
                        OnzasPorAplicacion = detalle.OnzasPorAplicacion,
                        PrecioPorQuintal = detalle.PrecioPorQuintal,
                        SubtotalFuente = detalle.SubtotalFuente,
                        Aportes = aportes
                    });
            }

            return resultado;
        }

        private static EnmiendaCalcareaCalcularResponse
            ConstruirResultadoEnmiendaGuardado(
                AnalisisEdicionContexto contexto,
                AnalisisGuardadoEnmiendaCalcarea guardado)
        {
            FuenteNutrienteResponse? fuente =
                contexto.FuentesCatalogo.FirstOrDefault(x =>
                    x.FuenteNutrientesId == guardado.FuenteNutrientesId);

            return new EnmiendaCalcareaCalcularResponse
            {
                EnmiendaCalcareaId = guardado.EnmiendaCalcareaId,
                NombreAnalisis = guardado.NombreAnalisis,
                FuenteNutriente =
                    fuente?.NombreNutriente ?? guardado.FuenteMostrar,
                Ph = guardado.Ph,
                Ca = guardado.Ca,
                Mg = guardado.Mg,
                K = guardado.K,
                AcidezTotal = guardado.AcidezTotal,
                SaturacionDeseada = guardado.SaturacionDeseada,
                Prnt = guardado.Prnt,
                SumaBases = guardado.SumaBases,
                Cice = guardado.Cice,
                SaturacionActual = guardado.SaturacionActual,
                NecesidadEncaladoTonHa =
                    guardado.NecesidadEncaladoTonHa,
                NecesidadEncaladoKgHa =
                    guardado.NecesidadEncaladoKgHa,
                NecesidadEncaladoLbHa =
                    guardado.NecesidadEncaladoLbHa,
                TerrenoId = guardado.TerrenoId,
                TotalPlantas = guardado.TotalPlantas,
                TotalAplicaciones = guardado.TotalAplicaciones,
                NecesidadEncaladoLbMz =
                    guardado.NecesidadEncaladoLbMz,
                NecesidadEncaladoOzMz =
                    guardado.NecesidadEncaladoOzMz,
                DosisPlantaAnualOz =
                    guardado.DosisPlantaAnualOz,
                DosisPlantaPorAplicacionOz =
                    guardado.DosisPlantaPorAplicacionOz
            };
        }

        private static FertilizacionMixtaCalculoResponse
            ConstruirResultadoMixtaGuardado(
                AnalisisEdicionContexto contexto,
                AnalisisGuardadoFertilizacionMixta guardado)
        {
            Dictionary<int, ElementoQuimicoResponse> elementos =
                contexto.ElementosCatalogo
                    .Where(x => x.ElementoQuimicosId.HasValue)
                    .GroupBy(x => x.ElementoQuimicosId!.Value)
                    .ToDictionary(x => x.Key, x => x.First());

            Dictionary<int, FuenteNutrienteResponse> fuentes =
                contexto.FuentesCatalogo
                    .Where(x => x.FuenteNutrientesId.HasValue)
                    .GroupBy(x => x.FuenteNutrientesId!.Value)
                    .ToDictionary(x => x.Key, x => x.First());

            FertilizacionMixtaCalculoResponse resultado = new()
            {
                Success = true,
                Observacion = guardado.Mixta.Observacion
            };

            foreach (AnalisisGuardadoMixtaFuente fuente in guardado.Fuentes)
            {
                fuentes.TryGetValue(
                    fuente.FuenteNutrientesId,
                    out FuenteNutrienteResponse? catalogo);

                resultado.Fuentes.Add(
                    new FuenteFertilizacionMixtaResultadoResponse
                    {
                        FuenteNutrientesId = fuente.FuenteNutrientesId,
                        NombreFuente =
                            catalogo?.NombreNutriente ??
                            fuente.FuenteMostrar,
                        CantidadQq = fuente.CantidadQq
                    });
            }

            foreach (AnalisisGuardadoMixtaDetalle detalle in guardado.Detalles)
            {
                elementos.TryGetValue(
                    detalle.ElementoQuimicosId,
                    out ElementoQuimicoResponse? catalogo);

                resultado.Detalles.Add(
                    new DetalleFertilizacionMixtaResultadoResponse
                    {
                        ElementoQuimicosId = detalle.ElementoQuimicosId,
                        Elemento =
                            catalogo?.SimboloElementoQuimico ??
                            catalogo?.NombreElementoQuimico ??
                            $"Elemento #{detalle.ElementoQuimicosId}",
                        Exportable = detalle.RequerimientoOriginal,
                        AporteOrganico = detalle.AporteOrganico,
                        Diferencia = detalle.Diferencia,
                        Deficit = detalle.Deficit,
                        Sobrante = detalle.Sobrante
                    });
            }

            return resultado;
        }

        private static AnalisisSueloGuardarCalculoRequest ClonarRequest(
            AnalisisSueloGuardarCalculoRequest origen)
        {
            return new AnalisisSueloGuardarCalculoRequest
            {
                TerrenoId = origen.TerrenoId,
                TipoCultivoId = origen.TipoCultivoId,
                TipoAnalisisSueloId = origen.TipoAnalisisSueloId,
                UsuarioId = origen.UsuarioId,
                CantidadQuintalesOro = origen.CantidadQuintalesOro,
                TamanoFinca = origen.TamanoFinca,
                Ph = origen.Ph,
                MateriaOrganica = origen.MateriaOrganica,
                UnidadMedidaMateriaOrganicaId =
                    origen.UnidadMedidaMateriaOrganicaId,
                AcidezTotal = origen.AcidezTotal,
                CalcioCice = origen.CalcioCice,
                MagnesioCice = origen.MagnesioCice,
                PotasioCice = origen.PotasioCice,
                FechaAnalisisSuelo = origen.FechaAnalisisSuelo,
                LaboratorioAnalasisSuelo =
                    origen.LaboratorioAnalasisSuelo,
                IdentificadorAnalisisSuelo =
                    origen.IdentificadorAnalisisSuelo,
                ElementosQuimicos =
                    origen.ElementosQuimicos
                        .Select(x =>
                            new ElementoQuimicoAnalisisRequest
                            {
                                ElementoQuimicosId =
                                    x.ElementoQuimicosId,
                                UnidadMedidaId = x.UnidadMedidaId,
                                CantidadElemento =
                                    x.CantidadElemento
                            })
                        .ToList(),
                FuentesOrganicas =
                    origen.FuentesOrganicas
                        .Select(x =>
                            new FuenteOrganicaAnalisisRequest
                            {
                                FuenteNutrientesId =
                                    x.FuenteNutrientesId,
                                CantidadAplicada =
                                    x.CantidadAplicada
                            })
                        .ToList()
            };
        }

        public static string ConstruirClaveRequerimiento(
            AnalisisSueloGuardarCalculoRequest request)
        {
            IEnumerable<string> elementos =
                (request.ElementosQuimicos ??
                 new List<ElementoQuimicoAnalisisRequest>())
                    .OrderBy(x => x.ElementoQuimicosId ?? 0)
                    .Select(x =>
                        $"{x.ElementoQuimicosId}:{x.UnidadMedidaId}:{F(x.CantidadElemento)}");

            return string.Join(
                "|",
                request.TerrenoId,
                request.TipoCultivoId,
                request.TipoAnalisisSueloId,
                F(request.CantidadQuintalesOro),
                F(request.TamanoFinca),
                F(request.Ph),
                F(request.MateriaOrganica),
                request.UnidadMedidaMateriaOrganicaId,
                F(request.AcidezTotal),
                string.Join(";", elementos));
        }

        private static string ConstruirClaveBalance(
            AnalisisSueloGuardarCalculoRequest request,
            int plantas,
            int aplicaciones)
        {
            return string.Join(
                "|",
                ConstruirClaveRequerimiento(request),
                plantas,
                aplicaciones);
        }

        private static string ConstruirClaveEnmienda(
            AnalisisSueloGuardarCalculoRequest request,
            int plantas,
            int aplicaciones)
        {
            return string.Join(
                "|",
                request.TerrenoId,
                F(request.Ph),
                F(request.AcidezTotal),
                F(request.CalcioCice),
                F(request.MagnesioCice),
                F(request.PotasioCice),
                plantas,
                aplicaciones);
        }

        private static string F(decimal? valor) =>
            (valor ?? 0).ToString(
                "0.####",
                CultureInfo.InvariantCulture);
    }
}
