using CONATRADEC.Models;
using CONATRADEC.ViewModels;
using CONATRADEC.Views;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using System.Globalization;
using System.Reflection;
using System.Threading;

namespace CONATRADEC.Services
{
    /// <summary>
    /// Corrige la restauración del flujo de edición sin depender del orden en
    /// que terminan las cargas asíncronas de Balance y Fertilización Mixta.
    ///
    /// La fuente de verdad siempre es AnalisisEdicionContexto.Detalle, que
    /// contiene lo guardado por la API o por el caché offline.
    /// </summary>
    public sealed class AnalisisEdicionCalculosDeterministaService
    {
        private static readonly Lazy<
            AnalisisEdicionCalculosDeterministaService> instancia =
                new(() => new AnalisisEdicionCalculosDeterministaService());

        private Shell? shellVinculado;
        private CancellationTokenSource? restauracionCts;

        private AnalisisEdicionCalculosDeterministaService()
        {
        }

        public static AnalisisEdicionCalculosDeterministaService Instance =>
            instancia.Value;

        public void VincularShell(Shell shell)
        {
            if (ReferenceEquals(shellVinculado, shell))
                return;

            if (shellVinculado != null)
                shellVinculado.Navigated -= Shell_Navigated;

            shellVinculado = shell;
            shellVinculado.Navigated += Shell_Navigated;
        }

        private void Shell_Navigated(
            object? sender,
            ShellNavigatedEventArgs e)
        {
            CancellationTokenSource nuevaCts = new();

            CancellationTokenSource? anterior =
                Interlocked.Exchange(
                    ref restauracionCts,
                    nuevaCts);

            if (anterior != null)
            {
                try
                {
                    anterior.Cancel();
                }
                catch (ObjectDisposedException)
                {
                }
                finally
                {
                    anterior.Dispose();
                }
            }

            _ = ProcesarPaginaActualAsync(nuevaCts.Token);
        }

        private async Task ProcesarPaginaActualAsync(
            CancellationToken cancellationToken)
        {
            try
            {
                /*
                 * Shell.Navigated puede ejecutarse antes de ApplyQueryAttributes
                 * y antes de OnAppearing. Se espera únicamente a que la página
                 * actual tenga su ViewModel y el contexto de edición.
                 */
                for (int intento = 0; intento < 180; intento++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    Page? pagina = shellVinculado?.CurrentPage;

                    if (pagina is ResultadoAnalisisSueloPage &&
                        pagina.BindingContext
                            is ResultadoAnalisisSueloViewModel resultadoVm)
                    {
                        if (!resultadoVm.EsModoEdicion ||
                            resultadoVm.Elementos.Count == 0 ||
                            AnalisisEdicionService.Instance.ContextoActual == null)
                        {
                            await Task.Delay(50, cancellationToken);
                            continue;
                        }

                        await MainThread.InvokeOnMainThreadAsync(() =>
                            AplicarSeleccionPersistida(resultadoVm));

                        return;
                    }

                    if (pagina is MultiCalculoPage &&
                        pagina.BindingContext
                            is MultiCalculoViewModel multiVm)
                    {
                        bool listo =
                            multiVm.EsModoEdicion &&
                            multiVm.ResultadoCalculo != null &&
                            multiVm.RequestGuardarAnalisis != null &&
                            AnalisisEdicionService.Instance.ContextoActual != null;

                        if (!listo)
                        {
                            await Task.Delay(50, cancellationToken);
                            continue;
                        }

                        await RestaurarMultiCalculoAsync(
                            multiVm,
                            cancellationToken);

                        return;
                    }

                    return;
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch
            {
                /*
                 * Este servicio es una recuperación visual. Una excepción no
                 * debe cerrar la pantalla ni impedir que el usuario navegue.
                 */
            }
        }

        private static void AplicarSeleccionPersistida(
            ResultadoAnalisisSueloViewModel viewModel)
        {
            AnalisisEdicionContexto? contexto =
                AnalisisEdicionService.Instance.ContextoActual;

            if (contexto == null)
                return;

            Dictionary<int, bool> seleccion =
                contexto.Detalle.RequerimientoAnual.Elementos
                    .Where(x => x.ElementoQuimicosId > 0)
                    .GroupBy(x => x.ElementoQuimicosId)
                    .ToDictionary(
                        grupo => grupo.Key,
                        grupo => grupo.Last()
                            .IncluirCalculosComplementarios);

            /*
             * Respaldo para análisis creados con versiones anteriores que no
             * hubieran persistido la bandera de inclusión.
             */
            if (seleccion.Count == 0)
            {
                HashSet<int> usados = new();

                foreach (AnalisisGuardadoFormulaDetalle item
                         in contexto.Detalle.BalanceNutricional?.Detalles
                            ?? new List<AnalisisGuardadoFormulaDetalle>())
                {
                    if (item.ElementoQuimicosId > 0)
                        usados.Add(item.ElementoQuimicosId);
                }

                foreach (AnalisisGuardadoMixtaDetalle item
                         in contexto.Detalle.FertilizacionMixta?.Detalles
                            ?? new List<AnalisisGuardadoMixtaDetalle>())
                {
                    if (item.ElementoQuimicosId > 0)
                        usados.Add(item.ElementoQuimicosId);
                }

                foreach (int id in usados)
                    seleccion[id] = true;
            }

            foreach (ElementoResultadoCalculoResponse elemento
                     in viewModel.Elementos)
            {
                if (elemento.ElementoQuimicosId is not int id)
                    continue;

                if (seleccion.TryGetValue(id, out bool incluir))
                {
                    elemento.IncluirEnCalculosComplementarios =
                        incluir;
                }
            }

            /*
             * Resultado y la colección visual comparten normalmente las mismas
             * instancias. Se aplica también por ID para cubrir cualquier clon.
             */
            foreach (ElementoResultadoCalculoResponse elemento
                     in viewModel.Resultado?.Elementos
                        ?? new List<ElementoResultadoCalculoResponse>())
            {
                if (elemento.ElementoQuimicosId is int id &&
                    seleccion.TryGetValue(id, out bool incluir))
                {
                    elemento.IncluirEnCalculosComplementarios =
                        incluir;
                }
            }

            FieldInfo? campoInicial =
                typeof(ResultadoAnalisisSueloViewModel)
                    .GetField(
                        "elementosIncluidosInicialmente",
                        BindingFlags.Instance |
                        BindingFlags.NonPublic);

            if (campoInicial?.GetValue(viewModel)
                is HashSet<int> iniciales)
            {
                iniciales.Clear();

                foreach (KeyValuePair<int, bool> item
                         in seleccion.Where(x => x.Value))
                {
                    iniciales.Add(item.Key);
                }
            }
        }

        private static async Task RestaurarMultiCalculoAsync(
            MultiCalculoViewModel viewModel,
            CancellationToken cancellationToken)
        {
            AnalisisEdicionContexto? contexto =
                AnalisisEdicionService.Instance.ContextoActual;

            if (contexto == null ||
                viewModel.ResultadoCalculo == null ||
                viewModel.RequestGuardarAnalisis == null)
            {
                return;
            }

            /*
             * Se deja terminar la inicialización propia de cada pestaña.
             * Después se reconstruye el temporal desde el detalle persistido,
             * por lo que una carga tardía ya no puede reemplazarlo.
             */
            await EsperarPestanasAsync(
                viewModel,
                cancellationToken);

            await Task.Delay(250, cancellationToken);

            int plantas =
                viewModel.CantidadPlantas is > 0
                    ? viewModel.CantidadPlantas.Value
                    : contexto.CantidadPlantas;

            await AnalisisEdicionService.Instance.RestaurarTemporalAsync(
                viewModel.ResultadoCalculo,
                viewModel.RequestGuardarAnalisis,
                plantas,
                requerimientoCambio: false,
                incluirBalance:
                    viewModel.MostrarBalanceFormula,
                incluirEnmienda:
                    viewModel.MostrarEnmiendaCalcarea,
                incluirMixta:
                    viewModel.MostrarFertilizacionMixta);

            AnalisisEdicionService.Instance.RestauracionUiRealizada =
                false;

            await RestaurarCalculosEdicionUiService.Instance
                .RestaurarAsync(viewModel);

            bool balanceRestaurado =
                await AsegurarBalanceVisualAsync(
                    viewModel.BalanceFormula,
                    contexto,
                    cancellationToken);

            bool complementoGuardado =
                contexto.Detalle.BalanceNutricional?
                    .Formula.EsComplementoFertilizacionMixta == true ||
                contexto.Detalle.FertilizacionMixta?
                    .Mixta.EsComplementoBalance == true;

            bool activarComplemento =
                complementoGuardado &&
                balanceRestaurado &&
                viewModel.MostrarBalanceFormula &&
                viewModel.MostrarFertilizacionMixta;

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                viewModel.BalanceFormula
                    .ComplementarConFertilizacionMixta =
                        activarComplemento;
            });

            if (activarComplemento)
            {
                await EsperarContextoMixtaAsync(
                    viewModel.FertilizacionMixta,
                    cancellationToken);
            }

            bool mixtaRestaurada =
                await AsegurarMixtaVisualAsync(
                    viewModel.FertilizacionMixta,
                    contexto,
                    activarComplemento,
                    cancellationToken);

            /*
             * Un último pase permite que el servicio existente reconstruya
             * tablas derivadas después de que el check ya quedó vinculado.
             */
            AnalisisEdicionService.Instance.RestauracionUiRealizada =
                false;

            await RestaurarCalculosEdicionUiService.Instance
                .RestaurarAsync(viewModel);

            AnalisisEdicionService.Instance.RestauracionUiRealizada =
                balanceRestaurado &&
                (!viewModel.MostrarFertilizacionMixta ||
                 !contexto.TieneMixta ||
                 mixtaRestaurada);

            if (!balanceRestaurado &&
                viewModel.MostrarBalanceFormula &&
                contexto.TieneBalance)
            {
                viewModel.Mensaje =
                    "Se encontraron los datos guardados, pero no fue posible " +
                    "restaurar completamente el Balance.";
            }
            else if (!mixtaRestaurada &&
                     viewModel.MostrarFertilizacionMixta &&
                     contexto.TieneMixta)
            {
                viewModel.Mensaje =
                    "Se restauró el Balance, pero no fue posible reconstruir " +
                    "completamente Fertilización Mixta.";
            }
        }

        private static async Task EsperarPestanasAsync(
            MultiCalculoViewModel viewModel,
            CancellationToken cancellationToken)
        {
            for (int intento = 0; intento < 200; intento++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                bool balanceListo =
                    !viewModel.MostrarBalanceFormula ||
                    !viewModel.BalanceFormula.IsBusy;

                bool mixtaLista =
                    !viewModel.MostrarFertilizacionMixta ||
                    (
                        !viewModel.FertilizacionMixta.IsBusy &&
                        (
                            viewModel.FertilizacionMixta
                                .TieneFuentesDisponibles ||
                            viewModel.FertilizacionMixta
                                .TieneErrorFuentes
                        )
                    );

                if (balanceListo && mixtaLista)
                    return;

                await Task.Delay(50, cancellationToken);
            }
        }

        private static async Task<bool> AsegurarBalanceVisualAsync(
            BalanceFormulaViewModel viewModel,
            AnalisisEdicionContexto contexto,
            CancellationToken cancellationToken)
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

            for (int intento = 0; intento < 120; intento++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!viewModel.IsBusy &&
                    viewModel.ElementosBalance.Count > 0)
                {
                    break;
                }

                await Task.Delay(50, cancellationToken);
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

            if (suspender == null || procesar == null)
                return false;

            int restauradas = 0;

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                suspender.SetValue(viewModel, true);

                try
                {
                    int plantas =
                        request.TotalPlantas ??
                        resultado.TotalPlantas ??
                        contexto.CantidadPlantas;

                    int aplicaciones =
                        request.TotalAplicaciones ??
                        resultado.TotalAplicaciones ??
                        3;

                    viewModel.NombreFormula =
                        request.NombreFormula ??
                        resultado.NombreFormula ??
                        string.Empty;

                    viewModel.TotalPlantas =
                        plantas.ToString(
                            CultureInfo.InvariantCulture);

                    viewModel.TotalAplicaciones =
                        aplicaciones.ToString(
                            CultureInfo.InvariantCulture);

                    foreach (BalanceNutricionalItemRequest item
                             in request.Items)
                    {
                        if (item.ElementoQuimicosId is null or <= 0 ||
                            item.FuenteNutrientesId is null or <= 0)
                        {
                            continue;
                        }

                        BalanceFormulaElementoViewModel? elemento =
                            viewModel.ElementosBalance
                                .FirstOrDefault(x =>
                                    x.ElementoQuimicosId ==
                                    item.ElementoQuimicosId);

                        if (elemento == null)
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
                                guardada =
                                    contexto.Detalle
                                        .BalanceNutricional?
                                        .Detalles
                                        .FirstOrDefault(x =>
                                            x.ElementoQuimicosId ==
                                                item.ElementoQuimicosId &&
                                            x.FuenteNutrientesId ==
                                                fuenteId);

                            fuente =
                                new FuenteNutrienteResponse
                                {
                                    FuenteNutrientesId =
                                        fuenteId,
                                    NombreNutriente =
                                        guardada?.NombreFuente ??
                                        $"Fuente #{fuenteId}",
                                    PrecioNutriente =
                                        guardada?.PrecioPorQuintal,
                                    Activo = true
                                };
                        }

                        if (!viewModel.FuentesNutrientes.Any(x =>
                                x.FuenteNutrientesId == fuenteId))
                        {
                            viewModel.FuentesNutrientes.Add(fuente);
                        }

                        if (!elemento.FuentesDisponibles.Any(x =>
                                x.FuenteNutrientesId == fuenteId))
                        {
                            elemento.FuentesDisponibles.Add(fuente);
                        }

                        elemento.FuenteSeleccionada = fuente;
                        restauradas++;
                    }

                    procesar.Invoke(
                        viewModel,
                        new object[]
                        {
                            resultado,
                            plantas,
                            aplicaciones
                        });

                    viewModel.Mensaje =
                        restauradas > 0
                            ? "Se cargó el Balance guardado con sus fuentes y resultado."
                            : "Se cargó el resultado del Balance, pero no se encontraron sus fuentes.";
                }
                finally
                {
                    suspender.SetValue(viewModel, false);
                }
            });

            return
                viewModel.TieneResultadoBalance &&
                restauradas > 0;
        }

        private static async Task EsperarContextoMixtaAsync(
            FertilizacionMixtaTabViewModel viewModel,
            CancellationToken cancellationToken)
        {
            for (int intento = 0; intento < 120; intento++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (viewModel.EsComplementoBalance &&
                    viewModel.TieneContextoBalance)
                {
                    return;
                }

                await Task.Delay(50, cancellationToken);
            }
        }

        private static async Task<bool> AsegurarMixtaVisualAsync(
            FertilizacionMixtaTabViewModel viewModel,
            AnalisisEdicionContexto contexto,
            bool esComplemento,
            CancellationToken cancellationToken)
        {
            FertilizacionMixtaCalcularRequest? request =
                CalculoAnalisisTemporalService.Instance
                    .ObtenerRequest<FertilizacionMixtaCalcularRequest>(
                        TipoCalculoTemporal.FertilizacionMixta);

            FertilizacionMixtaCalculoResponse? resultado =
                CalculoAnalisisTemporalService.Instance
                    .ObtenerResultado<FertilizacionMixtaCalculoResponse>(
                        TipoCalculoTemporal.FertilizacionMixta);

            if (request == null || resultado == null)
                return false;

            for (int intento = 0; intento < 120; intento++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!viewModel.IsBusy &&
                    viewModel.FuentesDisponibles.Count > 0)
                {
                    break;
                }

                await Task.Delay(50, cancellationToken);
            }

            FieldInfo? suspendiendo =
                typeof(FertilizacionMixtaTabViewModel)
                    .GetField(
                        "suspendiendoCambiosTemporales",
                        BindingFlags.Instance |
                        BindingFlags.NonPublic);

            FieldInfo? recalculoPendiente =
                typeof(FertilizacionMixtaTabViewModel)
                    .GetField(
                        "recalcularComplementoPendiente",
                        BindingFlags.Instance |
                        BindingFlags.NonPublic);

            MethodInfo? construirMatriz =
                ObtenerMetodoMixta(
                    "ConstruirMatrizAportesPorFuente");

            MethodInfo? construirCostos =
                ObtenerMetodoMixta(
                    "ConstruirTablaCostosOrganicos");

            MethodInfo? construirSugerencia =
                ObtenerMetodoMixta(
                    "ConstruirSugerenciaIncremento");

            MethodInfo? balanceAjustado =
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

            Task<bool>? tareaBalanceAjustado = null;
            int fuentesRestauradas = 0;

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                suspendiendo.SetValue(viewModel, true);

                foreach (FuenteFertilizacionMixtaItemViewModel fuente
                         in viewModel.FuentesDisponibles)
                {
                    fuente.EstaSeleccionada = false;
                    fuente.CantidadQq = string.Empty;
                    fuente.ErrorCantidad = string.Empty;
                }

                foreach (FuenteFertilizacionMixtaRequest item
                         in request.Fuentes ??
                            new List<FuenteFertilizacionMixtaRequest>())
                {
                    if (item.FuenteNutrientesId is null or <= 0)
                        continue;

                    int fuenteId =
                        item.FuenteNutrientesId.Value;

                    FuenteFertilizacionMixtaItemViewModel? fuente =
                        viewModel.FuentesDisponibles
                            .FirstOrDefault(x =>
                                x.FuenteNutrientesId ==
                                fuenteId);

                    if (fuente == null)
                    {
                        FuenteNutrienteResponse? catalogo =
                            contexto.FuentesCatalogo
                                .FirstOrDefault(x =>
                                    x.FuenteNutrientesId ==
                                    fuenteId);

                        AnalisisGuardadoMixtaFuente? guardada =
                            contexto.Detalle
                                .FertilizacionMixta?
                                .Fuentes
                                .FirstOrDefault(x =>
                                    x.FuenteNutrientesId ==
                                    fuenteId);

                        fuente =
                            new FuenteFertilizacionMixtaItemViewModel
                            {
                                FuenteNutrientesId =
                                    fuenteId,
                                NombreFuente =
                                    catalogo?.NombreNutriente ??
                                    guardada?.NombreFuente ??
                                    $"Fuente #{fuenteId}",
                                DescripcionFuente =
                                    catalogo?.DescripcionNutriente ??
                                    string.Empty,
                                PrecioFuente =
                                    catalogo?.PrecioNutriente,
                                ElementosTexto =
                                    catalogo?.AportesMostrar ??
                                    string.Empty
                            };

                        viewModel.FuentesDisponibles.Add(fuente);
                    }

                    fuente.EstaSeleccionada = true;
                    fuente.CantidadQq =
                        (item.CantidadQq ?? 0)
                            .ToString(
                                "0.00",
                                CultureInfo.InvariantCulture);

                    fuentesRestauradas++;
                }

                viewModel.ResultadoFertilizacionMixta =
                    resultado;

                construirMatriz.Invoke(viewModel, null);
                construirCostos.Invoke(viewModel, null);
                construirSugerencia.Invoke(viewModel, null);

                recalculoPendiente.SetValue(
                    viewModel,
                    false);

                if (esComplemento &&
                    balanceAjustado != null)
                {
                    tareaBalanceAjustado =
                        balanceAjustado.Invoke(
                            viewModel,
                            new object[] { resultado })
                        as Task<bool>;
                }
            });

            bool ajustadoCorrecto = !esComplemento;

            try
            {
                if (esComplemento)
                {
                    ajustadoCorrecto =
                        tareaBalanceAjustado != null &&
                        await tareaBalanceAjustado;
                }

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    viewModel.Mensaje =
                        esComplemento
                            ? ajustadoCorrecto
                                ? "Se cargó Fertilización Mixta guardada y el Balance ajustado."
                                : "Se cargó Fertilización Mixta, pero no se reconstruyó el Balance ajustado."
                            : "Se cargó Fertilización Mixta con sus fuentes, cantidades y resultado.";
                });

                return
                    viewModel.TieneResultadoFertilizacionMixta &&
                    fuentesRestauradas > 0 &&
                    ajustadoCorrecto;
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

        private static MethodInfo? ObtenerMetodoMixta(
            string nombre) =>
            typeof(FertilizacionMixtaTabViewModel)
                .GetMethod(
                    nombre,
                    BindingFlags.Instance |
                    BindingFlags.NonPublic);
    }
}
