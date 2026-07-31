using CONATRADEC.Models;
using CONATRADEC.ViewModels;
using Microsoft.Maui.ApplicationModel;
using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace CONATRADEC.Services
{
    public sealed class RestaurarCalculosEdicionUiService
    {
        private static readonly Lazy<
            RestaurarCalculosEdicionUiService> instancia =
                new(() =>
                    new RestaurarCalculosEdicionUiService());

        private readonly SemaphoreSlim restauracionLock =
            new(1, 1);

        public static RestaurarCalculosEdicionUiService Instance =>
            instancia.Value;

        private RestaurarCalculosEdicionUiService()
        {
        }

        public async Task RestaurarAsync(
            MultiCalculoViewModel viewModel)
        {
            if (!AnalisisEdicionService.Instance.EsModoEdicion ||
                AnalisisEdicionService.Instance.RestauracionUiRealizada)
            {
                return;
            }

            await restauracionLock.WaitAsync();

            try
            {
                if (!AnalisisEdicionService.Instance.EsModoEdicion ||
                    AnalisisEdicionService.Instance.RestauracionUiRealizada)
                {
                    return;
                }

                bool parametrosListos =
                    await EsperarParametrosAsync(viewModel);

                if (!parametrosListos)
                    return;

                AnalisisEdicionContexto? contexto =
                    AnalisisEdicionService.Instance.ContextoActual;

                if (contexto == null)
                    return;

                /*
                 * Si el temporal se perdió, quedó incompleto o fue
                 * reemplazado durante la navegación, se reconstruye
                 * desde el detalle persistido.
                 */
                await AsegurarTemporalesGuardadosAsync(contexto);

                bool debeRestaurarBalance =
                    viewModel.MostrarBalanceFormula &&
                    contexto.TieneBalance;

                bool debeRestaurarEnmienda =
                    viewModel.MostrarEnmiendaCalcarea &&
                    contexto.TieneEnmienda;

                bool debeRestaurarMixta =
                    viewModel.MostrarFertilizacionMixta &&
                    contexto.TieneMixta;

                /*
                 * Los tres checkbox de la pantalla de resultado representan
                 * la selección final que se conservará al actualizar.
                 *
                 * Si Balance o Mixta fue desmarcado, el complemento no puede
                 * permanecer activo por un valor visual de una navegación
                 * anterior. Se apaga antes de restaurar cualquier pestaña.
                 */
                if ((!viewModel.MostrarBalanceFormula ||
                     !viewModel.MostrarFertilizacionMixta) &&
                    viewModel.BalanceFormula
                        .ComplementarConFertilizacionMixta)
                {
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        viewModel.BalanceFormula
                            .ComplementarConFertilizacionMixta = false;
                    });
                }

                Task<bool> tareaBalance =
                    debeRestaurarBalance
                        ? EsperarYRestaurarBalanceAsync(
                            viewModel.BalanceFormula,
                            contexto)
                        : Task.FromResult(true);

                Task<bool> tareaEnmienda =
                    debeRestaurarEnmienda
                        ? EsperarYRestaurarEnmiendaAsync(
                            viewModel.EnmiendaCalcarea,
                            contexto)
                        : Task.FromResult(true);

                /*
                 * La fertilización mixta inicia su propia carga asíncrona.
                 * Se espera que termine antes de activar nuevamente el
                 * checkbox del complemento; así la primera vinculación no
                 * se confunde con un cambio realizado por el usuario.
                 */
                Task<bool> tareaMixtaLista =
                    debeRestaurarMixta
                        ? EsperarMixtaListaAsync(
                            viewModel.FertilizacionMixta)
                        : Task.FromResult(true);

                bool balanceRestaurado =
                    await tareaBalance;

                bool mixtaLista =
                    await tareaMixtaLista;

                bool complementoGuardado =
                    contexto.Detalle.BalanceNutricional?
                        .Formula
                        .EsComplementoFertilizacionMixta == true ||
                    contexto.Detalle.FertilizacionMixta?
                        .Mixta.EsComplementoBalance == true;

                bool restaurarComplemento =
                    complementoGuardado &&
                    debeRestaurarBalance &&
                    debeRestaurarMixta &&
                    balanceRestaurado;

                bool recalcularMixtaComoIndependiente =
                    complementoGuardado &&
                    debeRestaurarMixta &&
                    !viewModel.MostrarBalanceFormula;

                /*
                 * El resultado temporal se conserva aparte. Se retira
                 * momentáneamente de la pantalla antes de marcar el checkbox
                 * para impedir que ConfigurarComplementoBalanceAsync lo trate
                 * como un resultado modificado y solicite recalcular.
                 */
                if (restaurarComplemento &&
                    debeRestaurarMixta &&
                    mixtaLista)
                {
                    await PrepararMixtaAntesDeVincularAsync(
                        viewModel.FertilizacionMixta);
                }

                if (restaurarComplemento)
                {
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        viewModel.BalanceFormula
                            .ComplementarConFertilizacionMixta = true;
                    });
                }

                bool mixtaRestaurada;

                if (!debeRestaurarMixta)
                {
                    mixtaRestaurada = true;
                }
                else if (recalcularMixtaComoIndependiente)
                {
                    /*
                     * La Mixta guardada dependía del Balance, pero el usuario
                     * decidió conservar solo Mixta. Se recuperan sus fuentes
                     * y cantidades, se quita el resultado anterior y queda
                     * obligatoriamente pendiente de recalcular como cálculo
                     * independiente.
                     */
                    mixtaRestaurada =
                        await EsperarYPrepararMixtaIndependienteAsync(
                            viewModel.FertilizacionMixta);
                }
                else
                {
                    mixtaRestaurada =
                        await EsperarYRestaurarMixtaAsync(
                            viewModel.FertilizacionMixta,
                            restaurarComplemento);
                }

                bool enmiendaRestaurada =
                    await tareaEnmienda;

                if (balanceRestaurado &&
                    enmiendaRestaurada &&
                    mixtaRestaurada)
                {
                    AnalisisEdicionService
                        .Instance
                        .RestauracionUiRealizada = true;
                }
            }
            catch (Exception ex)
            {
                viewModel.Mensaje =
                    "No fue posible restaurar completamente los " +
                    $"cálculos guardados: {ex.Message}";
            }
            finally
            {
                restauracionLock.Release();
            }
        }

        private static async Task<bool> EsperarParametrosAsync(
            MultiCalculoViewModel viewModel)
        {
            for (int intento = 0;
                 intento < 300;
                 intento++)
            {
                if (!AnalisisEdicionService.Instance.EsModoEdicion)
                    return false;

                /*
                 * En edición es válido desmarcar los tres cálculos. Por eso
                 * no se exige que exista una pestaña visible para considerar
                 * que MultiCálculo ya recibió sus parámetros.
                 */
                bool recibioParametros =
                    viewModel.EsModoEdicion &&
                    viewModel.ResultadoCalculo != null &&
                    viewModel.RequestGuardarAnalisis != null;

                if (recibioParametros)
                    return true;

                await Task.Delay(100);
            }

            return false;
        }

        private static async Task
            AsegurarTemporalesGuardadosAsync(
                AnalisisEdicionContexto contexto)
        {
            bool balanceFaltante =
                contexto.TieneBalance &&
                !TieneBalanceTemporal();

            bool enmiendaFaltante =
                contexto.TieneEnmienda &&
                !TieneEnmiendaTemporal();

            bool mixtaFaltante =
                contexto.TieneMixta &&
                !TieneMixtaTemporal();

            if (!balanceFaltante &&
                !enmiendaFaltante &&
                !mixtaFaltante)
            {
                return;
            }

            await AnalisisEdicionService
                .Instance
                .RestaurarTemporalAsync(
                    contexto.ResultadoOriginal,
                    contexto.RequestActual,
                    contexto.CantidadPlantas,
                    false);
        }

        private static async Task<bool>
            EsperarYRestaurarBalanceAsync(
                BalanceFormulaViewModel viewModel,
                AnalisisEdicionContexto contexto)
        {
            BalanceNutricionalRequest? request =
                CalculoAnalisisTemporalService.Instance
                    .ObtenerRequest<BalanceNutricionalRequest>(
                        TipoCalculoTemporal.BalanceFormula);

            BalanceNutricionalResponse? resultadoTemporal =
                CalculoAnalisisTemporalService.Instance
                    .ObtenerResultado<BalanceNutricionalResponse>(
                        TipoCalculoTemporal.BalanceFormula);

            if (request?.Items == null ||
                request.Items.Count == 0 ||
                resultadoTemporal == null)
            {
                return false;
            }

            for (int intento = 0;
                 intento < 300;
                 intento++)
            {
                if (!AnalisisEdicionService.Instance.EsModoEdicion)
                    return false;

                bool interfazLista =
                    !viewModel.IsBusy &&
                    request.Items.All(item =>
                        viewModel.ElementosBalance.Any(elemento =>
                            elemento.ElementoQuimicosId ==
                                item.ElementoQuimicosId));

                if (interfazLista)
                {
                    bool restaurado = false;

                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        restaurado =
                            RestaurarBalance(
                                viewModel,
                                contexto);
                    });

                    if (restaurado)
                        return true;
                }

                await Task.Delay(100);
            }

            return false;
        }

        private static async Task<bool>
            EsperarYRestaurarEnmiendaAsync(
                EnmiendaCalcareaTabViewModel viewModel,
                AnalisisEdicionContexto contexto)
        {
            if (!TieneEnmiendaTemporal())
                return false;

            for (int intento = 0;
                 intento < 300;
                 intento++)
            {
                if (!AnalisisEdicionService.Instance.EsModoEdicion)
                    return false;

                if (viewModel.CargaEnmiendasFinalizada)
                {
                    bool restaurado = false;

                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        restaurado =
                            RestaurarEnmienda(
                                viewModel,
                                contexto);
                    });

                    return restaurado;
                }

                await Task.Delay(100);
            }

            return false;
        }

        private static async Task<bool>
            EsperarMixtaListaAsync(
                FertilizacionMixtaTabViewModel viewModel)
        {
            FieldInfo? suspendiendo =
                ObtenerCampoMixta(
                    "suspendiendoCambiosTemporales");

            if (suspendiendo == null)
                return false;

            FertilizacionMixtaCalcularRequest? request =
                CalculoAnalisisTemporalService.Instance
                    .ObtenerRequest<
                        FertilizacionMixtaCalcularRequest>(
                            TipoCalculoTemporal.FertilizacionMixta);

            FertilizacionMixtaCalculoResponse? resultado =
                CalculoAnalisisTemporalService.Instance
                    .ObtenerResultado<
                        FertilizacionMixtaCalculoResponse>(
                            TipoCalculoTemporal.FertilizacionMixta);

            if (request == null ||
                resultado == null)
            {
                return false;
            }

            for (int intento = 0;
                 intento < 300;
                 intento++)
            {
                if (!AnalisisEdicionService.Instance.EsModoEdicion)
                    return false;

                bool estaSuspendiendo =
                    suspendiendo.GetValue(viewModel) is true;

                bool fuentesDisponibles =
                    request.Fuentes == null ||
                    request.Fuentes.Count == 0 ||
                    request.Fuentes.All(item =>
                        viewModel.FuentesDisponibles.Any(fuente =>
                            fuente.FuenteNutrientesId ==
                                item.FuenteNutrientesId));

                if (!viewModel.IsBusy &&
                    !estaSuspendiendo &&
                    fuentesDisponibles)
                {
                    return true;
                }

                await Task.Delay(100);
            }

            return false;
        }

        private static async Task
            PrepararMixtaAntesDeVincularAsync(
                FertilizacionMixtaTabViewModel viewModel)
        {
            FieldInfo? suspendiendo =
                ObtenerCampoMixta(
                    "suspendiendoCambiosTemporales");

            FieldInfo? recalculoPendiente =
                ObtenerCampoMixta(
                    "recalcularComplementoPendiente");

            MethodInfo? limpiarPresentacion =
                ObtenerMetodoMixta(
                    "LimpiarResultadosPresentacion");

            if (suspendiendo == null ||
                recalculoPendiente == null ||
                limpiarPresentacion == null)
            {
                return;
            }

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                suspendiendo.SetValue(
                    viewModel,
                    true);

                try
                {
                    recalculoPendiente.SetValue(
                        viewModel,
                        false);

                    viewModel.ResultadoFertilizacionMixta =
                        null;

                    limpiarPresentacion.Invoke(
                        viewModel,
                        null);
                }
                finally
                {
                    suspendiendo.SetValue(
                        viewModel,
                        false);
                }
            });
        }

        private static async Task<bool>
            EsperarYPrepararMixtaIndependienteAsync(
                FertilizacionMixtaTabViewModel viewModel)
        {
            FertilizacionMixtaCalcularRequest? request =
                CalculoAnalisisTemporalService.Instance
                    .ObtenerRequest<
                        FertilizacionMixtaCalcularRequest>(
                            TipoCalculoTemporal.FertilizacionMixta);

            if (request == null)
                return false;

            FieldInfo? suspendiendo =
                ObtenerCampoMixta(
                    "suspendiendoCambiosTemporales");

            FieldInfo? recalculoPendiente =
                ObtenerCampoMixta(
                    "recalcularComplementoPendiente");

            MethodInfo? limpiarPresentacion =
                ObtenerMetodoMixta(
                    "LimpiarResultadosPresentacion");

            MethodInfo? restaurarRequerimientos =
                ObtenerMetodoMixta(
                    "RestaurarRequerimientosDesdeAnalisis");

            if (suspendiendo == null ||
                recalculoPendiente == null ||
                limpiarPresentacion == null ||
                restaurarRequerimientos == null)
            {
                return false;
            }

            for (int intento = 0;
                 intento < 300;
                 intento++)
            {
                if (!AnalisisEdicionService.Instance.EsModoEdicion)
                    return false;

                bool estaSuspendiendo =
                    suspendiendo.GetValue(viewModel) is true;

                bool fuentesListas =
                    request.Fuentes == null ||
                    request.Fuentes.Count == 0 ||
                    request.Fuentes.All(item =>
                        viewModel.FuentesDisponibles.Any(fuente =>
                            fuente.FuenteNutrientesId ==
                                item.FuenteNutrientesId));

                if (!viewModel.IsBusy &&
                    !estaSuspendiendo &&
                    fuentesListas)
                {
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        suspendiendo.SetValue(
                            viewModel,
                            true);

                        try
                        {
                            foreach (
                                FuenteFertilizacionMixtaItemViewModel fuente
                                in viewModel.FuentesDisponibles)
                            {
                                fuente.EstaSeleccionada = false;
                                fuente.CantidadQq = string.Empty;
                                fuente.ErrorCantidad = string.Empty;
                            }

                            foreach (
                                FuenteFertilizacionMixtaRequest fuenteRequest
                                in request.Fuentes)
                            {
                                if (fuenteRequest.FuenteNutrientesId
                                    is null or <= 0)
                                {
                                    continue;
                                }

                                FuenteFertilizacionMixtaItemViewModel? fuente =
                                    viewModel.FuentesDisponibles
                                        .FirstOrDefault(x =>
                                            x.FuenteNutrientesId ==
                                                fuenteRequest
                                                    .FuenteNutrientesId);

                                if (fuente == null)
                                    continue;

                                fuente.EstaSeleccionada = true;
                                fuente.CantidadQq =
                                    (fuenteRequest.CantidadQq ?? 0)
                                        .ToString(
                                            "0.00",
                                            CultureInfo.InvariantCulture);
                            }

                            recalculoPendiente.SetValue(
                                viewModel,
                                false);

                            viewModel.ResultadoFertilizacionMixta =
                                null;

                            limpiarPresentacion.Invoke(
                                viewModel,
                                null);

                            restaurarRequerimientos.Invoke(
                                viewModel,
                                null);

                            viewModel.Mensaje =
                                "El balance fue desmarcado. Se conservaron " +
                                "las fuentes de fertilización mixta, pero " +
                                "debe calcularla nuevamente como " +
                                "fertilización independiente.";
                        }
                        finally
                        {
                            suspendiendo.SetValue(
                                viewModel,
                                false);
                        }
                    });

                    await CalculoAnalisisTemporalService.Instance
                        .MarcarPendienteRecalculoAsync(
                            TipoCalculoTemporal.FertilizacionMixta,
                            "La fertilización mixta estaba vinculada al " +
                            "balance que fue desmarcado. Debe recalcularse " +
                            "como cálculo independiente.",
                            true);

                    return true;
                }

                await Task.Delay(100);
            }

            return false;
        }

        private static async Task<bool>
            EsperarYRestaurarMixtaAsync(
                FertilizacionMixtaTabViewModel viewModel,
                bool esComplemento)
        {
            FertilizacionMixtaCalcularRequest? request =
                CalculoAnalisisTemporalService.Instance
                    .ObtenerRequest<
                        FertilizacionMixtaCalcularRequest>(
                            TipoCalculoTemporal.FertilizacionMixta);

            FertilizacionMixtaCalculoResponse? resultado =
                CalculoAnalisisTemporalService.Instance
                    .ObtenerResultado<
                        FertilizacionMixtaCalculoResponse>(
                            TipoCalculoTemporal.FertilizacionMixta);

            if (request == null ||
                resultado == null)
            {
                return false;
            }

            FieldInfo? suspendiendo =
                ObtenerCampoMixta(
                    "suspendiendoCambiosTemporales");

            if (suspendiendo == null)
                return false;

            for (int intento = 0;
                 intento < 300;
                 intento++)
            {
                if (!AnalisisEdicionService.Instance.EsModoEdicion)
                    return false;

                bool estaSuspendiendo =
                    suspendiendo.GetValue(viewModel) is true;

                bool contextoListo =
                    !esComplemento ||
                    (
                        viewModel.EsComplementoBalance &&
                        viewModel.TieneContextoBalance
                    );

                bool fuentesListas =
                    request.Fuentes == null ||
                    request.Fuentes.Count == 0 ||
                    request.Fuentes.All(item =>
                        viewModel.FuentesDisponibles.Any(fuente =>
                            fuente.FuenteNutrientesId ==
                                item.FuenteNutrientesId));

                if (!viewModel.IsBusy &&
                    !estaSuspendiendo &&
                    contextoListo &&
                    fuentesListas)
                {
                    return await RestaurarMixtaAsync(
                        viewModel,
                        request,
                        resultado,
                        esComplemento);
                }

                await Task.Delay(100);
            }

            return false;
        }

        private static async Task<bool>
            RestaurarMixtaAsync(
                FertilizacionMixtaTabViewModel viewModel,
                FertilizacionMixtaCalcularRequest request,
                FertilizacionMixtaCalculoResponse resultado,
                bool esComplemento)
        {
            FieldInfo? suspendiendo =
                ObtenerCampoMixta(
                    "suspendiendoCambiosTemporales");

            FieldInfo? recalculoPendiente =
                ObtenerCampoMixta(
                    "recalcularComplementoPendiente");

            MethodInfo? construirMatriz =
                ObtenerMetodoMixta(
                    "ConstruirMatrizAportesPorFuente");

            MethodInfo? construirCostos =
                ObtenerMetodoMixta(
                    "ConstruirTablaCostosOrganicos");

            MethodInfo? construirSugerencia =
                ObtenerMetodoMixta(
                    "ConstruirSugerenciaIncremento");

            MethodInfo? calcularBalanceAjustado =
                ObtenerMetodoMixta(
                    "CalcularBalanceAjustadoAsync");

            if (suspendiendo == null ||
                recalculoPendiente == null ||
                construirMatriz == null ||
                construirCostos == null ||
                construirSugerencia == null)
            {
                return false;
            }

            Task<bool>? tareaBalanceAjustado =
                null;

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                suspendiendo.SetValue(
                    viewModel,
                    true);

                foreach (
                    FuenteFertilizacionMixtaItemViewModel fuente
                    in viewModel.FuentesDisponibles)
                {
                    fuente.EstaSeleccionada = false;
                    fuente.CantidadQq = string.Empty;
                    fuente.ErrorCantidad = string.Empty;
                }

                foreach (
                    FuenteFertilizacionMixtaRequest fuenteRequest
                    in request.Fuentes)
                {
                    if (fuenteRequest.FuenteNutrientesId is null or <= 0)
                        continue;

                    FuenteFertilizacionMixtaItemViewModel? fuente =
                        viewModel.FuentesDisponibles
                            .FirstOrDefault(x =>
                                x.FuenteNutrientesId ==
                                    fuenteRequest.FuenteNutrientesId);

                    if (fuente == null)
                        continue;

                    fuente.EstaSeleccionada = true;
                    fuente.CantidadQq =
                        (fuenteRequest.CantidadQq ?? 0)
                            .ToString(
                                "0.00",
                                CultureInfo.InvariantCulture);
                }

                viewModel.ResultadoFertilizacionMixta =
                    resultado;

                construirMatriz.Invoke(
                    viewModel,
                    null);

                construirCostos.Invoke(
                    viewModel,
                    null);

                construirSugerencia.Invoke(
                    viewModel,
                    null);

                recalculoPendiente.SetValue(
                    viewModel,
                    false);

                if (esComplemento &&
                    calcularBalanceAjustado != null)
                {
                    tareaBalanceAjustado =
                        calcularBalanceAjustado.Invoke(
                            viewModel,
                            new object[]
                            {
                                resultado
                            })
                        as Task<bool>;
                }
            });

            bool balanceAjustadoCorrecto =
                !esComplemento;

            try
            {
                if (esComplemento)
                {
                    balanceAjustadoCorrecto =
                        tareaBalanceAjustado != null &&
                        await tareaBalanceAjustado;
                }

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    viewModel.Mensaje =
                        esComplemento
                            ? balanceAjustadoCorrecto
                                ? "Se cargó la fertilización mixta guardada y su balance comercial ajustado."
                                : "Se cargó la fertilización mixta guardada, pero no fue posible reconstruir el balance comercial ajustado."
                            : "Se cargó la fertilización mixta guardada con sus fuentes y su resultado.";
                });

                return
                    viewModel.TieneResultadoFertilizacionMixta &&
                    (
                        !esComplemento ||
                        balanceAjustadoCorrecto
                    );
            }
            finally
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    suspendiendo.SetValue(
                        viewModel,
                        false);

                    recalculoPendiente.SetValue(
                        viewModel,
                        false);
                });
            }
        }

        private static FieldInfo? ObtenerCampoMixta(
            string nombre)
        {
            return typeof(FertilizacionMixtaTabViewModel)
                .GetField(
                    nombre,
                    BindingFlags.Instance |
                    BindingFlags.NonPublic);
        }

        private static MethodInfo? ObtenerMetodoMixta(
            string nombre)
        {
            return typeof(FertilizacionMixtaTabViewModel)
                .GetMethod(
                    nombre,
                    BindingFlags.Instance |
                    BindingFlags.NonPublic);
        }

        private static bool TieneBalanceTemporal()
        {
            BalanceNutricionalRequest? request =
                CalculoAnalisisTemporalService.Instance
                    .ObtenerRequest<BalanceNutricionalRequest>(
                        TipoCalculoTemporal.BalanceFormula);

            BalanceNutricionalResponse? resultado =
                CalculoAnalisisTemporalService.Instance
                    .ObtenerResultado<BalanceNutricionalResponse>(
                        TipoCalculoTemporal.BalanceFormula);

            return
                request?.Items != null &&
                request.Items.Count > 0 &&
                resultado != null;
        }

        private static bool TieneEnmiendaTemporal()
        {
            EnmiendaCalcareaCalcularRequest? request =
                CalculoAnalisisTemporalService.Instance
                    .ObtenerRequest<
                        EnmiendaCalcareaCalcularRequest>(
                            TipoCalculoTemporal.EnmiendaCalcarea);

            EnmiendaCalcareaCalcularResponse? resultado =
                CalculoAnalisisTemporalService.Instance
                    .ObtenerResultado<
                        EnmiendaCalcareaCalcularResponse>(
                            TipoCalculoTemporal.EnmiendaCalcarea);

            return
                request != null &&
                resultado != null;
        }

        private static bool TieneMixtaTemporal()
        {
            FertilizacionMixtaCalcularRequest? request =
                CalculoAnalisisTemporalService.Instance
                    .ObtenerRequest<
                        FertilizacionMixtaCalcularRequest>(
                            TipoCalculoTemporal.FertilizacionMixta);

            FertilizacionMixtaCalculoResponse? resultado =
                CalculoAnalisisTemporalService.Instance
                    .ObtenerResultado<
                        FertilizacionMixtaCalculoResponse>(
                            TipoCalculoTemporal.FertilizacionMixta);

            return
                request != null &&
                resultado != null;
        }

        private static bool RestaurarBalance(
            BalanceFormulaViewModel viewModel,
            AnalisisEdicionContexto contexto)
        {
            BalanceNutricionalRequest? request =
                CalculoAnalisisTemporalService.Instance
                    .ObtenerRequest<BalanceNutricionalRequest>(
                        TipoCalculoTemporal.BalanceFormula);

            BalanceNutricionalResponse? resultado =
                CalculoAnalisisTemporalService.Instance
                    .ObtenerResultado<BalanceNutricionalResponse>(
                        TipoCalculoTemporal.BalanceFormula);

            if (request?.Items == null ||
                request.Items.Count == 0 ||
                resultado == null)
            {
                return false;
            }

            FieldInfo? suspender =
                typeof(BalanceFormulaViewModel)
                    .GetField(
                        "suspenderRecalculoAutomatico",
                        BindingFlags.Instance |
                        BindingFlags.NonPublic);

            MethodInfo? procesar =
                typeof(BalanceFormulaViewModel)
                    .GetMethod(
                        "ProcesarResultadoApi",
                        BindingFlags.Instance |
                        BindingFlags.NonPublic);

            if (suspender == null ||
                procesar == null)
            {
                return false;
            }

            suspender.SetValue(
                viewModel,
                true);

            try
            {
                viewModel.NombreFormula =
                    request.NombreFormula ??
                    resultado.NombreFormula ??
                    string.Empty;

                int plantas =
                    request.TotalPlantas ??
                    resultado.TotalPlantas ??
                    contexto.CantidadPlantas;

                int aplicaciones =
                    request.TotalAplicaciones ??
                    resultado.TotalAplicaciones ??
                    3;

                viewModel.TotalPlantas =
                    plantas.ToString(
                        CultureInfo.InvariantCulture);

                viewModel.TotalAplicaciones =
                    aplicaciones.ToString(
                        CultureInfo.InvariantCulture);

                int fuentesRestauradas = 0;

                foreach (
                    BalanceFormulaElementoViewModel elemento
                    in viewModel.ElementosBalance)
                {
                    BalanceNutricionalItemRequest? item =
                        request.Items.FirstOrDefault(x =>
                            x.ElementoQuimicosId ==
                                elemento.ElementoQuimicosId);

                    if (item?.FuenteNutrientesId is null or <= 0)
                        continue;

                    int fuenteId =
                        item.FuenteNutrientesId.Value;

                    FuenteNutrienteResponse? fuente =
                        elemento.FuentesDisponibles
                            .FirstOrDefault(x =>
                                x.FuenteNutrientesId ==
                                    fuenteId);

                    fuente ??=
                        viewModel.FuentesNutrientes
                            .FirstOrDefault(x =>
                                x.FuenteNutrientesId ==
                                    fuenteId);

                    fuente ??=
                        contexto.FuentesCatalogo
                            .FirstOrDefault(x =>
                                x.FuenteNutrientesId ==
                                    fuenteId);

                    if (fuente == null)
                    {
                        AnalisisGuardadoFormulaDetalle?
                            detalleGuardado =
                                contexto
                                    .Detalle
                                    .BalanceNutricional?
                                    .Detalles
                                    .FirstOrDefault(x =>
                                        x.ElementoQuimicosId ==
                                            elemento
                                                .ElementoQuimicosId &&
                                        x.FuenteNutrientesId ==
                                            fuenteId);

                        fuente =
                            new FuenteNutrienteResponse
                            {
                                FuenteNutrientesId =
                                    fuenteId,

                                NombreNutriente =
                                    detalleGuardado?
                                        .NombreFuente
                                    ??
                                    $"Fuente #{fuenteId}",

                                PrecioNutriente =
                                    detalleGuardado?
                                        .PrecioPorQuintal,

                                Activo = true
                            };
                    }

                    if (!viewModel.FuentesNutrientes.Any(x =>
                            x.FuenteNutrientesId ==
                                fuenteId))
                    {
                        viewModel.FuentesNutrientes.Add(
                            fuente);
                    }

                    if (!elemento.FuentesDisponibles.Any(x =>
                            x.FuenteNutrientesId ==
                                fuenteId))
                    {
                        elemento.FuentesDisponibles.Add(
                            fuente);
                    }

                    elemento.FuenteSeleccionada =
                        fuente;

                    fuentesRestauradas++;
                }

                procesar.Invoke(
                    viewModel,
                    new object[]
                    {
                        resultado,
                        plantas,
                        aplicaciones
                    });

                bool completo =
                    fuentesRestauradas ==
                    request.Items.Count;

                viewModel.Mensaje =
                    completo
                        ? "Se cargó el balance guardado con las " +
                          "fuentes seleccionadas y su resultado."
                        : "Se cargó el resultado del balance, pero " +
                          "una fuente guardada ya no está disponible.";

                return completo;
            }
            finally
            {
                suspender.SetValue(
                    viewModel,
                    false);
            }
        }

        private static bool RestaurarEnmienda(
            EnmiendaCalcareaTabViewModel viewModel,
            AnalisisEdicionContexto contexto)
        {
            EnmiendaCalcareaCalcularRequest? request =
                CalculoAnalisisTemporalService.Instance
                    .ObtenerRequest<
                        EnmiendaCalcareaCalcularRequest>(
                            TipoCalculoTemporal.EnmiendaCalcarea);

            EnmiendaCalcareaCalcularResponse? resultado =
                CalculoAnalisisTemporalService.Instance
                    .ObtenerResultado<
                        EnmiendaCalcareaCalcularResponse>(
                            TipoCalculoTemporal.EnmiendaCalcarea);

            if (request == null ||
                resultado == null)
            {
                return false;
            }

            ParametroEnmiendaCalcareaResponse?
                fuenteSeleccionada =
                    viewModel.EnmiendasCalcareas
                        .FirstOrDefault(x =>
                            x.FuenteNutrientesId ==
                                request.FuenteNutrientesId);

            if (fuenteSeleccionada == null)
            {
                FuenteNutrienteResponse? fuenteCatalogo =
                    contexto.FuentesCatalogo
                        .FirstOrDefault(x =>
                            x.FuenteNutrientesId ==
                                request.FuenteNutrientesId);

                fuenteSeleccionada =
                    new ParametroEnmiendaCalcareaResponse
                    {
                        FuenteNutrientesId =
                            request.FuenteNutrientesId,

                        NombreNutriente =
                            fuenteCatalogo?.NombreNutriente
                            ??
                            resultado.FuenteNutriente
                            ??
                            $"Fuente #{request.FuenteNutrientesId}",

                        PrecioNutriente =
                            fuenteCatalogo?.PrecioNutriente,

                        Prnt =
                            fuenteCatalogo?.Prnt
                            ??
                            resultado.Prnt,

                        DescripcionParametro =
                            fuenteCatalogo?
                                .DescripcionParametro
                    };
            }

            string nombreRespaldo =
                contexto
                    .RequestActual
                    .IdentificadorAnalisisSuelo
                ??
                "Enmienda calcárea";

            viewModel.RestaurarCalculoGuardado(
                fuenteSeleccionada,
                request,
                resultado,
                nombreRespaldo,
                contexto.CantidadPlantas);

            return true;
        }
    }
}
