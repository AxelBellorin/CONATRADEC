using CONATRADEC.Models;
using CONATRADEC.ViewModels;
using Microsoft.Maui.ApplicationModel;
using System.Globalization;
using System.Reflection;

namespace CONATRADEC.Services
{
    public sealed class RestaurarCalculosEdicionUiService
    {
        private static readonly Lazy<
            RestaurarCalculosEdicionUiService> instancia =
                new(() =>
                    new RestaurarCalculosEdicionUiService());

        private bool restaurando;

        public static RestaurarCalculosEdicionUiService Instance =>
            instancia.Value;

        private RestaurarCalculosEdicionUiService()
        {
        }

        public async Task RestaurarAsync(
            MultiCalculoViewModel viewModel)
        {
            if (restaurando ||
                !AnalisisEdicionService.Instance.EsModoEdicion ||
                AnalisisEdicionService.Instance.RestauracionUiRealizada)
            {
                return;
            }

            restaurando = true;

            try
            {
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
                 * desde el detalle persistido en la base de datos.
                 *
                 * Solo se reconstruyen cálculos que realmente existen
                 * en el análisis guardado.
                 */
                await AsegurarTemporalesGuardadosAsync(contexto);

                /*
                 * Una pestaña puede mostrarse porque el usuario decidió
                 * crear un cálculo nuevo durante la edición.
                 *
                 * En ese caso, si el cálculo no existía previamente,
                 * no se restaura ningún resultado. El resultado solo
                 * aparecerá después de presionar Calcular.
                 */
                bool debeRestaurarBalance =
                    viewModel.MostrarBalanceFormula &&
                    contexto.TieneBalance;

                bool debeRestaurarEnmienda =
                    viewModel.MostrarEnmiendaCalcarea &&
                    contexto.TieneEnmienda;

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

                bool[] resultados =
                    await Task.WhenAll(
                        tareaBalance,
                        tareaEnmienda);

                if (resultados.All(x => x))
                {
                    AnalisisEdicionService
                        .Instance
                        .RestauracionUiRealizada = true;
                }
            }
            catch (Exception ex)
            {
                /*
                 * No se interrumpe la pantalla completa.
                 * Se deja un mensaje visible para poder recalcular
                 * manualmente en caso de que una fuente ya no exista.
                 */
                viewModel.Mensaje =
                    "No fue posible restaurar completamente los " +
                    $"cálculos guardados: {ex.Message}";
            }
            finally
            {
                restaurando = false;
            }
        }

        private static async Task<bool> EsperarParametrosAsync(
            MultiCalculoViewModel viewModel)
        {
            for (int intento = 0;
                 intento < 120;
                 intento++)
            {
                if (!AnalisisEdicionService.Instance.EsModoEdicion)
                    return false;

                bool recibioParametros =
                    viewModel.EsModoEdicion &&
                    (
                        viewModel.MostrarBalanceFormula ||
                        viewModel.MostrarEnmiendaCalcarea ||
                        viewModel.MostrarFertilizacionMixta
                    );

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
            for (int intento = 0;
                 intento < 240;
                 intento++)
            {
                if (!AnalisisEdicionService.Instance.EsModoEdicion)
                    return false;

                bool interfazLista =
                    viewModel.ElementosBalance.Count > 0;

                if (interfazLista &&
                    TieneBalanceTemporal())
                {
                    bool restaurado = false;

                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        restaurado =
                            RestaurarBalance(
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
            EsperarYRestaurarEnmiendaAsync(
                EnmiendaCalcareaTabViewModel viewModel,
                AnalisisEdicionContexto contexto)
        {
            for (int intento = 0;
                 intento < 240;
                 intento++)
            {
                if (!AnalisisEdicionService.Instance.EsModoEdicion)
                    return false;

                /*
                 * No se exige que el catálogo de enmiendas tenga datos.
                 * El resultado guardado debe mostrarse aunque la fuente
                 * haya sido desactivada después.
                 */
                if (TieneEnmiendaTemporal())
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
                request != null &&
                request.Items != null &&
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

            if (request == null ||
                request.Items == null ||
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

                viewModel.EnmiendasCalcareas.Add(
                    fuenteSeleccionada);
            }

            decimal ph =
                request.Ph != 0
                    ? request.Ph
                    : resultado.Ph ?? 0;

            decimal acidezTotal =
                request.AcidezTotal != 0
                    ? request.AcidezTotal
                    : resultado.AcidezTotal ?? 0;

            decimal ca =
                request.Ca != 0
                    ? request.Ca
                    : resultado.Ca ?? 0;

            decimal mg =
                request.Mg != 0
                    ? request.Mg
                    : resultado.Mg ?? 0;

            decimal k =
                request.K != 0
                    ? request.K
                    : resultado.K ?? 0;

            viewModel.EnmiendaSeleccionada =
                fuenteSeleccionada;

            viewModel.NombreAnalisis =
                string.IsNullOrWhiteSpace(
                    request.NombreAnalisis)
                    ? resultado.NombreAnalisis ??
                      contexto
                          .RequestActual
                          .IdentificadorAnalisisSuelo ??
                      "Enmienda calcárea"
                    : request.NombreAnalisis;

            viewModel.Ph =
                ph.ToString(
                    CultureInfo.InvariantCulture);

            viewModel.Ca =
                ca.ToString(
                    CultureInfo.InvariantCulture);

            viewModel.Mg =
                mg.ToString(
                    CultureInfo.InvariantCulture);

            viewModel.K =
                k.ToString(
                    CultureInfo.InvariantCulture);

            viewModel.AcidezTotal =
                acidezTotal.ToString(
                    CultureInfo.InvariantCulture);

            int plantas =
                request.TotalPlantas > 0
                    ? request.TotalPlantas
                    : resultado.TotalPlantas ??
                      contexto.CantidadPlantas;

            int aplicaciones =
                request.TotalAplicaciones > 0
                    ? request.TotalAplicaciones
                    : resultado.TotalAplicaciones ?? 3;

            viewModel.TotalPlantas =
                plantas.ToString(
                    CultureInfo.InvariantCulture);

            viewModel.TotalAplicaciones =
                aplicaciones.ToString(
                    CultureInfo.InvariantCulture);

            /*
             * Debe asignarse al final. Los cambios en los campos
             * anteriores notifican que la enmienda quedó pendiente.
             * Al poner el resultado al final se conserva como procesada.
             */
            viewModel.ResultadoEnmienda =
                resultado;

            viewModel.ErrorFormulario =
                string.Empty;

            viewModel.Mensaje =
                "Se cargó la enmienda calcárea guardada con " +
                "su fuente y resultado procesado.";

            return true;
        }
    }
}
