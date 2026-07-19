using CONATRADEC.Models;
using CONATRADEC.ViewModels;
using Microsoft.Maui.ApplicationModel;
using System.Globalization;
using System.Reflection;

namespace CONATRADEC.Services
{
    public sealed class RestaurarCalculosEdicionUiService
    {
        private static readonly Lazy<RestaurarCalculosEdicionUiService> instancia =
            new(() => new RestaurarCalculosEdicionUiService());

        public static RestaurarCalculosEdicionUiService Instance =>
            instancia.Value;

        private RestaurarCalculosEdicionUiService()
        {
        }

        public async Task RestaurarAsync(MultiCalculoViewModel viewModel)
        {
            if (!AnalisisEdicionService.Instance.EsModoEdicion ||
                AnalisisEdicionService.Instance.RestauracionUiRealizada)
            {
                return;
            }

            bool inicializacionCompleta =
                await EsperarInicializacionAsync(viewModel);

            if (!inicializacionCompleta)
                return;

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                if (viewModel.MostrarBalanceFormula)
                    RestaurarBalance(viewModel.BalanceFormula);

                if (viewModel.MostrarEnmiendaCalcarea)
                    RestaurarEnmienda(viewModel.EnmiendaCalcarea);
            });

            AnalisisEdicionService.Instance.RestauracionUiRealizada = true;
        }

        private static async Task<bool> EsperarInicializacionAsync(
            MultiCalculoViewModel viewModel)
        {
            for (int intento = 0; intento < 100; intento++)
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
                    break;

                await Task.Delay(100);
            }

            if (!viewModel.EsModoEdicion)
                return false;

            for (int intento = 0; intento < 150; intento++)
            {
                bool balanceListo =
                    !viewModel.MostrarBalanceFormula ||
                    (
                        viewModel.BalanceFormula.FuentesNutrientes.Count > 0 &&
                        viewModel.BalanceFormula.ElementosBalance.Count > 0 &&
                        TieneBalanceTemporal()
                    );

                bool enmiendaLista =
                    !viewModel.MostrarEnmiendaCalcarea ||
                    (
                        viewModel.EnmiendaCalcarea.EnmiendasCalcareas.Count > 0 &&
                        TieneEnmiendaTemporal()
                    );

                bool mixtaLista =
                    !viewModel.MostrarFertilizacionMixta ||
                    viewModel.FertilizacionMixta.FuentesDisponibles.Count > 0;

                if (balanceListo && enmiendaLista && mixtaLista)
                    return true;

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

            return request != null &&
                   request.Items != null &&
                   request.Items.Count > 0 &&
                   resultado != null;
        }

        private static bool TieneEnmiendaTemporal()
        {
            EnmiendaCalcareaCalcularRequest? request =
                CalculoAnalisisTemporalService.Instance
                    .ObtenerRequest<EnmiendaCalcareaCalcularRequest>(
                        TipoCalculoTemporal.EnmiendaCalcarea);

            EnmiendaCalcareaCalcularResponse? resultado =
                CalculoAnalisisTemporalService.Instance
                    .ObtenerResultado<EnmiendaCalcareaCalcularResponse>(
                        TipoCalculoTemporal.EnmiendaCalcarea);

            return request != null && resultado != null;
        }

        private static void RestaurarBalance(
            BalanceFormulaViewModel viewModel)
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
                return;
            }

            FieldInfo? suspender =
                typeof(BalanceFormulaViewModel)
                    .GetField(
                        "suspenderRecalculoAutomatico",
                        BindingFlags.Instance |
                        BindingFlags.NonPublic);

            suspender?.SetValue(viewModel, true);

            try
            {
                viewModel.NombreFormula =
                    request.NombreFormula ??
                    resultado.NombreFormula ??
                    string.Empty;

                int plantas =
                    request.TotalPlantas ??
                    resultado.TotalPlantas ??
                    0;

                int aplicaciones =
                    request.TotalAplicaciones ??
                    resultado.TotalAplicaciones ??
                    3;

                viewModel.TotalPlantas =
                    plantas.ToString(CultureInfo.InvariantCulture);

                viewModel.TotalAplicaciones =
                    aplicaciones.ToString(CultureInfo.InvariantCulture);

                foreach (BalanceFormulaElementoViewModel elemento
                         in viewModel.ElementosBalance)
                {
                    BalanceNutricionalItemRequest? item =
                        request.Items.FirstOrDefault(x =>
                            x.ElementoQuimicosId ==
                            elemento.ElementoQuimicosId);

                    if (item?.FuenteNutrientesId == null)
                        continue;

                    FuenteNutrienteResponse? fuente =
                        elemento.FuentesDisponibles.FirstOrDefault(x =>
                            x.FuenteNutrientesId ==
                            item.FuenteNutrientesId);

                    if (fuente != null)
                        elemento.FuenteSeleccionada = fuente;
                }

                MethodInfo? procesar =
                    typeof(BalanceFormulaViewModel)
                        .GetMethod(
                            "ProcesarResultadoApi",
                            BindingFlags.Instance |
                            BindingFlags.NonPublic);

                procesar?.Invoke(
                    viewModel,
                    new object[]
                    {
                        resultado,
                        plantas,
                        aplicaciones
                    });

                viewModel.Mensaje =
                    "Se cargó el balance guardado. Puede cambiar una fuente " +
                    "o presionar Reiniciar cuando necesite modificarlo.";
            }
            finally
            {
                suspender?.SetValue(viewModel, false);
            }
        }

        private static void RestaurarEnmienda(
            EnmiendaCalcareaTabViewModel viewModel)
        {
            EnmiendaCalcareaCalcularRequest? request =
                CalculoAnalisisTemporalService.Instance
                    .ObtenerRequest<EnmiendaCalcareaCalcularRequest>(
                        TipoCalculoTemporal.EnmiendaCalcarea);

            EnmiendaCalcareaCalcularResponse? resultado =
                CalculoAnalisisTemporalService.Instance
                    .ObtenerResultado<EnmiendaCalcareaCalcularResponse>(
                        TipoCalculoTemporal.EnmiendaCalcarea);

            if (request == null || resultado == null)
                return;

            viewModel.EnmiendaSeleccionada =
                viewModel.EnmiendasCalcareas.FirstOrDefault(x =>
                    x.FuenteNutrientesId ==
                    request.FuenteNutrientesId);

            viewModel.NombreAnalisis = request.NombreAnalisis;
            viewModel.Ph = request.Ph.ToString(CultureInfo.InvariantCulture);
            viewModel.Ca = request.Ca.ToString(CultureInfo.InvariantCulture);
            viewModel.Mg = request.Mg.ToString(CultureInfo.InvariantCulture);
            viewModel.K = request.K.ToString(CultureInfo.InvariantCulture);
            viewModel.AcidezTotal =
                request.AcidezTotal.ToString(CultureInfo.InvariantCulture);
            viewModel.TotalPlantas =
                request.TotalPlantas.ToString(CultureInfo.InvariantCulture);
            viewModel.TotalAplicaciones =
                request.TotalAplicaciones.ToString(
                    CultureInfo.InvariantCulture);

            viewModel.ResultadoEnmienda = resultado;
            viewModel.Mensaje =
                "Se cargó la enmienda guardada. Solo deberá calcular " +
                "nuevamente si modifica sus datos.";
        }
    }
}
