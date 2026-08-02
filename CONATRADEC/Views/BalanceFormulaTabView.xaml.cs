using CONATRADEC.Services;
using CONATRADEC.ViewModels;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace CONATRADEC.Views
{
    /// <summary>
    /// Vista del Balance de fórmula.
    ///
    /// La selección de fuentes continúa administrándose únicamente mediante
    /// el binding SelectedItem definido en BalanceFormulaTabView.xaml.
    ///
    /// Durante la edición, las fuentes guardadas se restauran sin disparar
    /// cambios de usuario. Después de que toda la restauración termina, esta
    /// vista ejecuta una sola vez el cálculo real del Balance con:
    ///
    /// - Las fuentes restauradas.
    /// - El requerimiento anual actual.
    /// - La cantidad de plantas actual.
    /// - El número de aplicaciones guardado.
    ///
    /// Esto evita mostrar una fórmula calculada con valores temporales antiguos
    /// y elimina la necesidad de cambiar manualmente una fuente para corregirla.
    /// </summary>
    public partial class BalanceFormulaTabView : ContentView
    {
        private static readonly MethodInfo?
            metodoRecalcularBalance =
                typeof(BalanceFormulaViewModel)
                    .GetMethod(
                        "RecalcularBalanceAsync",
                        BindingFlags.Instance |
                        BindingFlags.NonPublic);

        private CancellationTokenSource?
            verificacionRestauracionCancellationTokenSource;

        private bool recalculoRestauradoEjecutado;
        private bool recalculoRestauradoEnCurso;

        public BalanceFormulaTabView()
        {
            InitializeComponent();

            Loaded += BalanceFormulaTabView_Loaded;
            Unloaded += BalanceFormulaTabView_Unloaded;
            BindingContextChanged +=
                BalanceFormulaTabView_BindingContextChanged;
        }

        private void BalanceFormulaTabView_Loaded(
            object? sender,
            EventArgs e)
        {
            IniciarVerificacionRestauracion();
        }

        private void BalanceFormulaTabView_Unloaded(
            object? sender,
            EventArgs e)
        {
            CancelarVerificacionRestauracion();
        }

        private void BalanceFormulaTabView_BindingContextChanged(
            object? sender,
            EventArgs e)
        {
            /*
             * Una nueva instancia del ViewModel representa una nueva entrada
             * al proceso de Balance. Se permite nuevamente un único recálculo.
             */
            recalculoRestauradoEjecutado = false;
            recalculoRestauradoEnCurso = false;

            IniciarVerificacionRestauracion();
        }

        private void IniciarVerificacionRestauracion()
        {
            CancelarVerificacionRestauracion();

            if (recalculoRestauradoEjecutado ||
                BindingContext is not BalanceFormulaViewModel viewModel ||
                !AnalisisEdicionService.Instance.EsModoEdicion)
            {
                return;
            }

            verificacionRestauracionCancellationTokenSource =
                new CancellationTokenSource();

            CancellationToken token =
                verificacionRestauracionCancellationTokenSource.Token;

            _ = EsperarYRecalcularBalanceRestauradoAsync(
                viewModel,
                token);
        }

        /// <summary>
        /// Mantiene una sola espera por instancia de la vista.
        ///
        /// No modifica bindings, SelectedItem, SelectedIndex ni las colecciones
        /// del Picker. Solamente espera la señal global de que la restauración
        /// de edición terminó.
        /// </summary>
        private async Task EsperarYRecalcularBalanceRestauradoAsync(
            BalanceFormulaViewModel viewModel,
            CancellationToken cancellationToken)
        {
            try
            {
                /*
                 * RestaurarCalculosEdicionUiService puede esperar la carga de
                 * Balance, Enmienda y Mixta. Se concede hasta treinta segundos,
                 * igual que el tiempo máximo utilizado por esa restauración.
                 */
                for (int intento = 0;
                     intento < 300;
                     intento++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (!AnalisisEdicionService.Instance.EsModoEdicion ||
                        !ReferenceEquals(
                            BindingContext,
                            viewModel))
                    {
                        return;
                    }

                    bool tieneFuentesRestauradas =
                        viewModel.ElementosBalance.Any(
                            elemento =>
                                elemento.FuenteSeleccionada != null);

                    bool restauracionLista =
                        AnalisisEdicionService.Instance
                            .RestauracionUiRealizada &&
                        !viewModel.IsBusy &&
                        viewModel.TieneResultadoBalance &&
                        tieneFuentesRestauradas;

                    if (restauracionLista)
                    {
                        await EjecutarRecalculoRestauradoAsync(
                            viewModel,
                            cancellationToken);

                        return;
                    }

                    await Task.Delay(
                        100,
                        cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                // La vista salió de pantalla o cambió su BindingContext.
            }
            catch (Exception ex)
            {
                /*
                 * La restauración guardada permanece visible como respaldo.
                 * El usuario todavía puede cambiar una fuente manualmente.
                 */
                System.Diagnostics.Debug.WriteLine(
                    "No fue posible recalcular automáticamente el Balance " +
                    $"restaurado: {ex}");
            }
        }

        private async Task EjecutarRecalculoRestauradoAsync(
            BalanceFormulaViewModel viewModel,
            CancellationToken cancellationToken)
        {
            if (recalculoRestauradoEjecutado ||
                recalculoRestauradoEnCurso ||
                metodoRecalcularBalance == null)
            {
                return;
            }

            recalculoRestauradoEnCurso = true;

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                await MainThread.InvokeOnMainThreadAsync(
                    async () =>
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        if (!ReferenceEquals(
                                BindingContext,
                                viewModel) ||
                            viewModel.IsBusy)
                        {
                            return;
                        }

                        /*
                         * Se invoca exactamente el mismo cálculo privado que
                         * ProgramarRecalculoAutomatico utiliza después de que el
                         * usuario cambia una fuente en el Picker.
                         */
                        Task? tareaRecalculo =
                            metodoRecalcularBalance.Invoke(
                                viewModel,
                                new object[]
                                {
                                    false
                                })
                            as Task;

                        if (tareaRecalculo != null)
                            await tareaRecalculo;
                    });

                /*
                 * Se marca aunque la API haya rechazado el cálculo para impedir
                 * solicitudes repetidas. El resultado restaurado o el mensaje
                 * de la API queda disponible como respaldo.
                 */
                recalculoRestauradoEjecutado = true;
            }
            finally
            {
                recalculoRestauradoEnCurso = false;
            }
        }

        private void CancelarVerificacionRestauracion()
        {
            CancellationTokenSource? anterior =
                Interlocked.Exchange(
                    ref verificacionRestauracionCancellationTokenSource,
                    null);

            if (anterior == null)
                return;

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
    }
}
