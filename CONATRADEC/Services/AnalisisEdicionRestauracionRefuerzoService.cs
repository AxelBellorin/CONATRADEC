using CONATRADEC.Models;
using CONATRADEC.ViewModels;
using CONATRADEC.Views;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace CONATRADEC.Services
{
    /// <summary>
    /// Realiza una verificación final de la restauración visual de un análisis.
    ///
    /// Balance y Fertilización Mixta cargan catálogos mediante tareas distintas.
    /// Una de esas tareas puede terminar después de la primera restauración y
    /// volver a limpiar la fuente seleccionada, el checkbox o la tabla Mixta.
    /// Este servicio espera a que la página esté estable, repite el restaurado
    /// existente y comprueba por ID que lo guardado quedó realmente visible.
    /// </summary>
    public sealed class AnalisisEdicionRestauracionRefuerzoService
    {
        private static readonly Lazy<
            AnalisisEdicionRestauracionRefuerzoService> instancia =
                new(() =>
                    new AnalisisEdicionRestauracionRefuerzoService());

        private Shell? shellVinculado;
        private CancellationTokenSource? restauracionCts;

        private AnalisisEdicionRestauracionRefuerzoService()
        {
        }

        public static AnalisisEdicionRestauracionRefuerzoService Instance =>
            instancia.Value;

        public void VincularShell(
            Shell shell)
        {
            ArgumentNullException.ThrowIfNull(shell);

            if (ReferenceEquals(
                    shellVinculado,
                    shell))
            {
                return;
            }

            if (shellVinculado != null)
            {
                shellVinculado.Navigated -=
                    Shell_Navigated;
            }

            shellVinculado = shell;
            shellVinculado.Navigated +=
                Shell_Navigated;
        }

        private void Shell_Navigated(
            object? sender,
            ShellNavigatedEventArgs e)
        {
            var nuevaCts =
                new CancellationTokenSource();

            CancellationTokenSource? anterior =
                Interlocked.Exchange(
                    ref restauracionCts,
                    nuevaCts);

            CancelarSeguro(anterior);

            _ = ProcesarPaginaActualAsync(
                nuevaCts.Token);
        }

        private async Task ProcesarPaginaActualAsync(
            CancellationToken cancellationToken)
        {
            try
            {
                MultiCalculoPage? pagina = null;
                MultiCalculoViewModel? viewModel = null;

                /*
                 * Shell.Navigated puede ocurrir antes de ApplyQueryAttributes.
                 * Se espera a que MultiCálculo haya recibido el contexto real
                 * de edición y sus parámetros principales.
                 */
                for (int intento = 0;
                     intento < 240;
                     intento++)
                {
                    cancellationToken
                        .ThrowIfCancellationRequested();

                    Page? actual =
                        shellVinculado?
                            .CurrentPage;

                    if (actual is not MultiCalculoPage multiPage)
                        return;

                    if (multiPage.BindingContext
                            is MultiCalculoViewModel multiVm &&
                        multiVm.EsModoEdicion &&
                        multiVm.ResultadoCalculo != null &&
                        multiVm.RequestGuardarAnalisis != null &&
                        AnalisisEdicionService.Instance
                            .ContextoActual != null)
                    {
                        pagina = multiPage;
                        viewModel = multiVm;
                        break;
                    }

                    await Task.Delay(
                        50,
                        cancellationToken);
                }

                if (pagina == null ||
                    viewModel == null)
                {
                    return;
                }

                int? calculoId =
                    viewModel
                        .AnalisisSueloCalculoIdEdicion;

                int[] esperas =
                [
                    300,
                    650,
                    1200,
                    1800
                ];

                foreach (int espera in esperas)
                {
                    cancellationToken
                        .ThrowIfCancellationRequested();

                    await Task.Delay(
                        espera,
                        cancellationToken);

                    if (!ReferenceEquals(
                            shellVinculado?.CurrentPage,
                            pagina) ||
                        !viewModel.EsModoEdicion ||
                        viewModel.AnalisisSueloCalculoIdEdicion !=
                            calculoId)
                    {
                        return;
                    }

                    AnalisisEdicionContexto? contexto =
                        AnalisisEdicionService.Instance
                            .ContextoActual;

                    if (contexto == null ||
                        contexto.AnalisisSueloCalculoId !=
                            calculoId)
                    {
                        return;
                    }

                    /*
                     * El servicio existente es la fuente única de la lógica de
                     * restauración. Aquí solo se permite un nuevo pase después
                     * de que las colecciones visuales terminaron de cargar.
                     */
                    AnalisisEdicionService.Instance
                        .RestauracionUiRealizada = false;

                    await RestaurarCalculosEdicionUiService
                        .Instance
                        .RestaurarAsync(viewModel);

                    await AsegurarCheckboxAsync(
                        viewModel,
                        contexto);

                    /*
                     * Activar el complemento puede inicializar nuevamente la
                     * pestaña Mixta. Se realiza un segundo pase breve para
                     * colocar fuentes, cantidades y resultado sobre ese estado.
                     */
                    AnalisisEdicionService.Instance
                        .RestauracionUiRealizada = false;

                    await RestaurarCalculosEdicionUiService
                        .Instance
                        .RestaurarAsync(viewModel);

                    if (EstaCompletamenteRestaurado(
                            viewModel,
                            contexto))
                    {
                        AnalisisEdicionService.Instance
                            .RestauracionUiRealizada = true;

                        return;
                    }
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch
            {
                /*
                 * Es un refuerzo visual. Nunca debe cerrar la pantalla ni
                 * impedir que el usuario continúe trabajando.
                 */
            }
        }

        private static async Task AsegurarCheckboxAsync(
            MultiCalculoViewModel viewModel,
            AnalisisEdicionContexto contexto)
        {
            bool complementoGuardado =
                contexto.Detalle.BalanceNutricional?
                    .Formula
                    .EsComplementoFertilizacionMixta == true ||
                contexto.Detalle.FertilizacionMixta?
                    .Mixta
                    .EsComplementoBalance == true;

            bool valorEsperado =
                complementoGuardado &&
                viewModel.MostrarBalanceFormula &&
                viewModel.MostrarFertilizacionMixta;

            if (viewModel.BalanceFormula
                    .ComplementarConFertilizacionMixta ==
                valorEsperado)
            {
                return;
            }

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                viewModel.BalanceFormula
                    .ComplementarConFertilizacionMixta =
                        valorEsperado;
            });

            await Task.Delay(180);
        }

        private static bool EstaCompletamenteRestaurado(
            MultiCalculoViewModel viewModel,
            AnalisisEdicionContexto contexto)
        {
            if (!BalanceRestaurado(
                    viewModel,
                    contexto))
            {
                return false;
            }

            if (!MixtaRestaurada(
                    viewModel,
                    contexto))
            {
                return false;
            }

            bool complementoGuardado =
                contexto.Detalle.BalanceNutricional?
                    .Formula
                    .EsComplementoFertilizacionMixta == true ||
                contexto.Detalle.FertilizacionMixta?
                    .Mixta
                    .EsComplementoBalance == true;

            bool complementoEsperado =
                complementoGuardado &&
                viewModel.MostrarBalanceFormula &&
                viewModel.MostrarFertilizacionMixta;

            return viewModel.BalanceFormula
                       .ComplementarConFertilizacionMixta ==
                   complementoEsperado;
        }

        private static bool BalanceRestaurado(
            MultiCalculoViewModel viewModel,
            AnalisisEdicionContexto contexto)
        {
            if (!viewModel.MostrarBalanceFormula ||
                !contexto.TieneBalance)
            {
                return true;
            }

            List<AnalisisGuardadoFormulaDetalle> guardados =
                contexto.Detalle
                    .BalanceNutricional!
                    .Detalles
                    .Where(x =>
                        x.ElementoQuimicosId > 0 &&
                        x.FuenteNutrientesId > 0)
                    .ToList();

            if (guardados.Count == 0)
                return true;

            return guardados.All(guardado =>
            {
                BalanceFormulaElementoViewModel? elemento =
                    viewModel.BalanceFormula
                        .ElementosBalance
                        .FirstOrDefault(x =>
                            x.ElementoQuimicosId ==
                                guardado.ElementoQuimicosId);

                return elemento?
                           .FuenteSeleccionada?
                           .FuenteNutrientesId ==
                       guardado.FuenteNutrientesId;
            });
        }

        private static bool MixtaRestaurada(
            MultiCalculoViewModel viewModel,
            AnalisisEdicionContexto contexto)
        {
            if (!viewModel.MostrarFertilizacionMixta ||
                !contexto.TieneMixta)
            {
                return true;
            }

            List<AnalisisGuardadoMixtaFuente> guardadas =
                contexto.Detalle
                    .FertilizacionMixta!
                    .Fuentes
                    .Where(x =>
                        x.FuenteNutrientesId > 0)
                    .ToList();

            bool fuentesCorrectas =
                guardadas.All(guardada =>
                    viewModel.FertilizacionMixta
                        .FuentesDisponibles
                        .Any(fuente =>
                            fuente.FuenteNutrientesId ==
                                guardada.FuenteNutrientesId &&
                            fuente.EstaSeleccionada));

            return fuentesCorrectas &&
                   viewModel.FertilizacionMixta
                       .TieneResultadoFertilizacionMixta;
        }

        private static void CancelarSeguro(
            CancellationTokenSource? source)
        {
            if (source == null)
                return;

            try
            {
                source.Cancel();
            }
            catch (ObjectDisposedException)
            {
            }
            finally
            {
                source.Dispose();
            }
        }
    }
}
