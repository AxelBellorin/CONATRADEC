using CONATRADEC.Models;
using CONATRADEC.Services;
using CONATRADEC.ViewModels;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls.Shapes;
using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

namespace CONATRADEC.Views
{
    public partial class MultiCalculoPage :
        ContentPage
    {
        private const string TabBalanceFormula =
            "BALANCE_FORMULA";

        private readonly MultiCalculoViewModel viewModel =
            new MultiCalculoViewModel();

        private BalanceFormulaTabView? balanceView;
        private EnmiendaCalcareaTabView? enmiendaView;
        private FertilizacionMixtaTabView?
            fertilizacionView;

        /*
         * Permite distinguir una pestaña Mixta seleccionada
         * originalmente de otra creada únicamente por activar
         * "Complementar el balance con fertilización mixta".
         */
        private bool estadoInicialMixtaCapturado;
        private bool mixtaSeleccionadaOriginalmente;
        private bool mixtaActivadaPorComplemento;

        private int? analisisCapturadoId;
        private string identificadorCapturado =
            string.Empty;

        private AnalisisSueloCalculoDataResponse?
            resultadoCapturado;

        private int versionCargaVisual;

        /*
         * Indicador visual independiente del IsBusy utilizado para guardar.
         * Su única responsabilidad es cubrir la carga inicial y la construcción
         * visual de una pestaña.
         */
        private Grid? indicadorCargaVisual;
        private Label? textoCargaVisual;
        private ActivityIndicator? actividadCargaVisual;
        private CancellationTokenSource?
            cambioTabCancellationTokenSource;

        private bool cargaInicialEnCurso;
        private bool paginaVisible;

        public MultiCalculoPage()
        {
            Shell.Current.FlyoutBehavior =
                FlyoutBehavior.Disabled;

            InitializeComponent();

            BindingContext = viewModel;

            CrearIndicadorCargaVisual();

            viewModel.PropertyChanged +=
                ViewModel_PropertyChanged;

            /*
             * MultiCalculoViewModel ya registra su callback interno para
             * inicializar y configurar Fertilización mixta.
             *
             * Esta página agrega su comportamiento visual sin reemplazar
             * el callback del ViewModel: primero espera la lógica interna
             * y después controla si la pestaña Mixta debe permanecer o
             * desaparecer.
             */
            Func<
                BalanceFertilizacionMixtaChangedEventArgs,
                Task>? callbackViewModel =
                    viewModel
                        .BalanceFormula
                        .ComplementoFertilizacionMixtaCambiadoAsync;

            viewModel
                .BalanceFormula
                .ComplementoFertilizacionMixtaCambiadoAsync =
                    async argumentos =>
                    {
                        if (callbackViewModel != null)
                        {
                            await callbackViewModel(
                                argumentos);
                        }

                        await ManejarCambioComplementoFertilizacionMixtaAsync(
                            argumentos);
                    };
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            paginaVisible = true;
            cargaInicialEnCurso = true;

            viewModel.LoadPagePermissions(
                "ResultadoAnalisisSueloPage");

            if (!viewModel.CanView)
            {
                cargaInicialEnCurso = false;
                OcultarIndicadorCargaVisual();

                await GlobalService.MostrarToastAsync(
                    "No tiene permisos para ver los cálculos complementarios.");

                await Shell.Current
                    .GoToAsync("//MainPage");

                return;
            }

            /*
             * El indicador se muestra antes de construir la pestaña activa.
             * El pequeño Yield/Delay de CompletarCargaVisualAsync permite que
             * MAUI pinte primero la rueda y luego realice el trabajo visual.
             */
            MostrarIndicadorCargaVisual(
                "Preparando los datos...");

            int version =
                ++versionCargaVisual;

            _ = CompletarCargaVisualAsync(
                version);
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();

            paginaVisible = false;
            cargaInicialEnCurso = false;

            /*
             * Invalida una espera visual anterior si el usuario abandona la
             * página antes de que finalice la restauración.
             */
            versionCargaVisual++;

            CancelarCambioTab();
            OcultarIndicadorCargaVisual();
        }

        private async Task CompletarCargaVisualAsync(
            int version)
        {
            try
            {
                /*
                 * Permite que el indicador quede visible antes de crear o medir
                 * la primera interfaz de cálculo.
                 */
                await Task.Yield();
                await Task.Delay(60);

                if (version != versionCargaVisual ||
                    !paginaVisible)
                {
                    return;
                }

                await MainThread.InvokeOnMainThreadAsync(
                    ActualizarVistaTab);

                await EsperarInicializacionActualAsync();

                /*
                 * Fertilización mixta inicia su carga mediante una tarea
                 * independiente. En edición esa tarea también inicializa el
                 * estado temporal y puede terminar después de que Balance ya
                 * dibujó sus elementos.
                 *
                 * Se espera únicamente a que Mixta termine de cargar su
                 * catálogo antes de restaurar los cálculos guardados. Así una
                 * inicialización tardía no vuelve a limpiar las fuentes del
                 * Balance, el checkbox del complemento ni el resultado Mixta.
                 */
                await EsperarInicializacionMixtaAntesDeRestaurarAsync();

                if (version != versionCargaVisual ||
                    !paginaVisible)
                {
                    return;
                }

                /*
                 * ApplyQueryAttributes es async void. Por eso la selección
                 * original de Mixta debe capturarse después de que el ViewModel
                 * haya recibido el análisis y las pestañas seleccionadas.
                 */
                PrepararCapturaSeleccionOriginalMixta();

                await MainThread.InvokeOnMainThreadAsync(
                    ActualizarVistaTab);

                await RestaurarCalculosEdicionUiService
                    .Instance
                    .RestaurarAsync(viewModel);

                if (version != versionCargaVisual ||
                    !paginaVisible)
                {
                    return;
                }

                await MainThread.InvokeOnMainThreadAsync(
                    ActualizarVistaTab);

                /*
                 * Si el Balance restaurado inicia su recálculo determinista,
                 * se mantiene el indicador hasta que termine. En las demás
                 * pestañas se espera su carga real de catálogo.
                 */
                await EsperarTabActualListaAsync(
                    CancellationToken.None);

                await Task.Delay(80);
            }
            catch (Exception ex)
            {
                if (version != versionCargaVisual)
                    return;

                viewModel.Mensaje =
                    "No fue posible completar la carga visual de los " +
                    $"cálculos: {ex.Message}";
            }
            finally
            {
                if (version == versionCargaVisual)
                {
                    cargaInicialEnCurso = false;
                    OcultarIndicadorCargaVisual();
                }
            }
        }

        private void
            PrepararCapturaSeleccionOriginalMixta()
        {
            int? idActual =
                viewModel
                    .AnalisisSueloCalculoIdEdicion;

            string identificadorActual =
                viewModel
                    .NombreAnalisisSuelo;

            AnalisisSueloCalculoDataResponse?
                resultadoActual =
                    viewModel.ResultadoCalculo;

            bool correspondeMismaNavegacion =
                estadoInicialMixtaCapturado &&
                analisisCapturadoId == idActual &&
                string.Equals(
                    identificadorCapturado,
                    identificadorActual,
                    StringComparison.Ordinal) &&
                ReferenceEquals(
                    resultadoCapturado,
                    resultadoActual);

            if (correspondeMismaNavegacion)
                return;

            estadoInicialMixtaCapturado = false;
            mixtaSeleccionadaOriginalmente = false;
            mixtaActivadaPorComplemento = false;

            analisisCapturadoId = idActual;
            identificadorCapturado =
                identificadorActual;
            resultadoCapturado =
                resultadoActual;

            CapturarSeleccionOriginalMixta();
        }

        private void CapturarSeleccionOriginalMixta()
        {
            if (estadoInicialMixtaCapturado)
                return;

            estadoInicialMixtaCapturado = true;

            /*
             * En un análisis nuevo, MostrarFertilizacionMixta ya
             * contiene la selección realizada en la pantalla anterior.
             * Si todavía no existe complemento, esa pestaña fue
             * seleccionada directamente por el usuario.
             */
            mixtaSeleccionadaOriginalmente =
                viewModel.MostrarFertilizacionMixta &&
                !viewModel
                    .BalanceFormula
                    .ComplementarConFertilizacionMixta;

            mixtaActivadaPorComplemento = false;
        }

        private async Task ManejarCambioComplementoFertilizacionMixtaAsync(
            BalanceFertilizacionMixtaChangedEventArgs e)
        {
            if (!estadoInicialMixtaCapturado)
                CapturarSeleccionOriginalMixta();

            if (e.Activado)
            {
                if (!mixtaSeleccionadaOriginalmente)
                {
                    mixtaActivadaPorComplemento = true;
                }

                return;
            }

            /*
             * Si Mixta fue seleccionada desde la pantalla de cálculos
             * opcionales, al quitar el complemento debe continuar
             * disponible como cálculo independiente.
             */
            if (mixtaSeleccionadaOriginalmente)
            {
                mixtaActivadaPorComplemento = false;
                return;
            }

            /*
             * Si apareció únicamente por activar el complemento,
             * al quitar el check se oculta, se limpia su resultado
             * temporal y se regresa a Balance si estaba seleccionada.
             */
            if (!mixtaActivadaPorComplemento)
                return;

            mixtaActivadaPorComplemento = false;

            viewModel.MostrarFertilizacionMixta =
                false;

            if (viewModel.EsFertilizacionSeleccionada)
            {
                viewModel.TabSeleccionada =
                    TabBalanceFormula;
            }

            await CalculoAnalisisTemporalService
                .Instance
                .ReiniciarCalculoAsync(
                    TipoCalculoTemporal
                        .FertilizacionMixta,
                    "Fertilización mixta retirada porque se desactivó el complemento del balance.");

            viewModel.Mensaje =
                "Se desactivó el complemento. La pestaña Mixta fue retirada porque no había sido seleccionada originalmente.";
        }

        private async Task
            EsperarInicializacionActualAsync()
        {
            if (!AnalisisEdicionService
                    .Instance
                    .EsModoEdicion)
            {
                return;
            }

            AnalisisEdicionContexto? contexto =
                AnalisisEdicionService
                    .Instance
                    .ContextoActual;

            if (contexto == null)
                return;

            /*
             * Solo se espera a que MultiCalculo reciba los parámetros del
             * análisis actual. La carga de fuentes y la restauración de cada
             * pestaña se ejecutan en segundo plano por su servicio específico.
             * Así se evita repetir una espera de treinta segundos antes de
             * comenzar la restauración real.
             */
            for (int intento = 0;
                 intento < 40;
                 intento++)
            {
                if (!ReferenceEquals(
                        contexto,
                        AnalisisEdicionService
                            .Instance
                            .ContextoActual))
                {
                    return;
                }

                bool parametrosRecibidos =
                    viewModel.EsModoEdicion &&
                    viewModel.ResultadoCalculo != null &&
                    viewModel.RequestGuardarAnalisis != null;

                if (parametrosRecibidos)
                    return;

                await Task.Delay(50);
            }
        }

        private async Task
            EsperarInicializacionMixtaAntesDeRestaurarAsync()
        {
            if (!AnalisisEdicionService
                    .Instance
                    .EsModoEdicion ||
                !viewModel.MostrarFertilizacionMixta)
            {
                return;
            }

            AnalisisEdicionContexto? contexto =
                AnalisisEdicionService
                    .Instance
                    .ContextoActual;

            if (contexto == null)
                return;

            /*
             * Antes de iniciar, FuentesDisponibles está vacío, IsBusy es
             * false y todavía no existe error. Por eso no basta con esperar
             * solamente a que IsBusy sea false. La carga se considera
             * finalizada cuando ya existe al menos una fuente o se registró
             * explícitamente un error de catálogo.
             */
            for (int intento = 0;
                 intento < 200;
                 intento++)
            {
                if (!ReferenceEquals(
                        contexto,
                        AnalisisEdicionService
                            .Instance
                            .ContextoActual))
                {
                    return;
                }

                FertilizacionMixtaTabViewModel mixta =
                    viewModel.FertilizacionMixta;

                bool cargaFinalizada =
                    !mixta.IsBusy &&
                    (mixta.TieneFuentesDisponibles ||
                     mixta.TieneErrorFuentes);

                if (cargaFinalizada)
                {
                    /*
                     * Permite que finalice el bloque finally de la tarea de
                     * inicialización antes de reconstruir los temporales.
                     */
                    await Task.Delay(75);
                    return;
                }

                await Task.Delay(50);
            }
        }

        private void ViewModel_PropertyChanged(
            object? sender,
            PropertyChangedEventArgs e)
        {
            /*
             * TabSeleccionada también notifica las tres
             * propiedades Es...Seleccionado. Escuchar las cuatro
             * provocaba múltiples intentos de redibujar.
             */
            if (e.PropertyName !=
                nameof(
                    MultiCalculoViewModel
                        .TabSeleccionada))
            {
                return;
            }

            if (!paginaVisible)
                return;

            /*
             * La carga inicial ya mantiene su propio indicador y realizará
             * el dibujo final. No se crea una segunda espera simultánea.
             */
            if (cargaInicialEnCurso)
                return;

            _ = CambiarVistaTabConIndicadorAsync();
        }

        private async Task
            CambiarVistaTabConIndicadorAsync()
        {
            CancellationTokenSource nueva =
                new();

            CancellationTokenSource? anterior =
                Interlocked.Exchange(
                    ref cambioTabCancellationTokenSource,
                    nueva);

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

            CancellationToken token =
                nueva.Token;

            try
            {
                MostrarIndicadorCargaVisual(
                    "Preparando los datos...");

                /*
                 * Garantiza que el overlay se pinte antes de construir por
                 * primera vez la vista seleccionada.
                 */
                await Task.Yield();
                await Task.Delay(
                    55,
                    token);

                await MainThread.InvokeOnMainThreadAsync(
                    ActualizarVistaTab);

                await EsperarTabActualListaAsync(
                    token);

                /*
                 * Concede un ciclo adicional para que MAUI termine de medir
                 * la nueva vista antes de retirar el overlay.
                 */
                await Task.Delay(
                    90,
                    token);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                viewModel.Mensaje =
                    "No fue posible mostrar la pestaña seleccionada: " +
                    ex.Message;
            }
            finally
            {
                if (ReferenceEquals(
                        cambioTabCancellationTokenSource,
                        nueva))
                {
                    Interlocked.Exchange(
                        ref cambioTabCancellationTokenSource,
                        null);

                    nueva.Dispose();

                    OcultarIndicadorCargaVisual();
                }
            }
        }

        private async Task EsperarTabActualListaAsync(
            CancellationToken cancellationToken)
        {
            /*
             * Máximo de quince segundos para no dejar la interfaz cubierta
             * indefinidamente si una API devolvió error. Los ViewModels
             * conservarán su mensaje de error debajo del overlay.
             */
            for (int intento = 0;
                 intento < 300;
                 intento++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!paginaVisible)
                    return;

                if (TabActualEstaLista())
                    return;

                await Task.Delay(
                    50,
                    cancellationToken);
            }
        }

        private bool TabActualEstaLista()
        {
            if (viewModel.EsBalanceSeleccionado)
            {
                BalanceFormulaViewModel balance =
                    viewModel.BalanceFormula;

                return
                    !balance.IsBusy &&
                    (
                        balance.ElementosBalance.Count > 0 ||
                        !string.IsNullOrWhiteSpace(
                            balance.Mensaje)
                    );
            }

            if (viewModel.EsEnmiendaSeleccionada)
            {
                EnmiendaCalcareaTabViewModel enmienda =
                    viewModel.EnmiendaCalcarea;

                return
                    !enmienda.IsBusy &&
                    enmienda.CargaEnmiendasFinalizada;
            }

            if (viewModel.EsFertilizacionSeleccionada)
            {
                FertilizacionMixtaTabViewModel mixta =
                    viewModel.FertilizacionMixta;

                return
                    !mixta.IsBusy &&
                    (
                        mixta.TieneFuentesDisponibles ||
                        mixta.TieneErrorFuentes
                    );
            }

            return true;
        }

        private void ActualizarVistaTab()
        {
            if (viewModel.EsBalanceSeleccionado)
            {
                AsegurarVistaBalance();
            }
            else if (
                viewModel.EsEnmiendaSeleccionada)
            {
                AsegurarVistaEnmienda();
            }
            else if (
                viewModel.EsFertilizacionSeleccionada)
            {
                AsegurarVistaFertilizacion();
            }

            if (balanceView != null)
            {
                balanceView.IsVisible =
                    viewModel
                        .EsBalanceSeleccionado;
            }

            if (enmiendaView != null)
            {
                enmiendaView.IsVisible =
                    viewModel
                        .EsEnmiendaSeleccionada;
            }

            if (fertilizacionView != null)
            {
                fertilizacionView.IsVisible =
                    viewModel
                        .EsFertilizacionSeleccionada;
            }
        }

        private void AsegurarVistaBalance()
        {
            if (balanceView != null)
                return;

            balanceView =
                new BalanceFormulaTabView
                {
                    BindingContext =
                        viewModel.BalanceFormula,
                    IsVisible = false
                };

            ContenidoTabActual
                .Children
                .Add(balanceView);
        }

        private void AsegurarVistaEnmienda()
        {
            if (enmiendaView != null)
                return;

            enmiendaView =
                new EnmiendaCalcareaTabView
                {
                    BindingContext =
                        viewModel.EnmiendaCalcarea,
                    IsVisible = false
                };

            ContenidoTabActual
                .Children
                .Add(enmiendaView);
        }

        private void AsegurarVistaFertilizacion()
        {
            if (fertilizacionView != null)
                return;

            fertilizacionView =
                new FertilizacionMixtaTabView
                {
                    BindingContext =
                        viewModel
                            .FertilizacionMixta,
                    IsVisible = false
                };

            ContenidoTabActual
                .Children
                .Add(fertilizacionView);
        }

        private void CrearIndicadorCargaVisual()
        {
            if (Content is not Grid contenedorRaiz)
                return;

            actividadCargaVisual =
                new ActivityIndicator
                {
                    // No consume render mientras el overlay está oculto.
                    IsRunning = false,
                    WidthRequest = 46,
                    HeightRequest = 46,
                    Color = Color.FromArgb("#3B655B"),
                    HorizontalOptions = LayoutOptions.Center
                };

            textoCargaVisual =
                new Label
                {
                    Text = "Preparando los datos...",
                    FontFamily = "MontserratBold",
                    FontSize = 15,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Color.FromArgb("#17201D"),
                    HorizontalTextAlignment = TextAlignment.Center,
                    LineBreakMode = LineBreakMode.WordWrap
                };

            var tarjeta =
                new Border
                {
                    BackgroundColor = Colors.White,
                    Stroke = Color.FromArgb("#DCE6E1"),
                    StrokeThickness = 1,
                    StrokeShape =
                        new RoundRectangle
                        {
                            CornerRadius = new CornerRadius(20)
                        },
                    Padding = new Thickness(24, 20),
                    Margin = 24,
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center,
                    MaximumWidthRequest = 420,
                    Content =
                        new VerticalStackLayout
                        {
                            Spacing = 11,
                            Children =
                            {
                                actividadCargaVisual,
                                textoCargaVisual
                            }
                        }
                };

            indicadorCargaVisual =
                new Grid
                {
                    BackgroundColor = Color.FromArgb("#66000000"),
                    IsVisible = false,
                    InputTransparent = false,
                    ZIndex = 2000
                };

            Grid.SetRowSpan(indicadorCargaVisual, 3);

            indicadorCargaVisual.Children.Add(tarjeta);
            contenedorRaiz.Children.Add(indicadorCargaVisual);
        }

        private void MostrarIndicadorCargaVisual(string mensaje)
        {
            void Mostrar()
            {
                if (textoCargaVisual != null)
                    textoCargaVisual.Text = mensaje;

                if (actividadCargaVisual != null)
                    actividadCargaVisual.IsRunning = true;

                if (indicadorCargaVisual != null)
                    indicadorCargaVisual.IsVisible = true;
            }

            if (MainThread.IsMainThread)
            {
                Mostrar();
            }
            else
            {
                MainThread.BeginInvokeOnMainThread(Mostrar);
            }
        }

        private void OcultarIndicadorCargaVisual()
        {
            void Ocultar()
            {
                if (actividadCargaVisual != null)
                    actividadCargaVisual.IsRunning = false;

                if (indicadorCargaVisual != null)
                    indicadorCargaVisual.IsVisible = false;
            }

            if (MainThread.IsMainThread)
            {
                Ocultar();
            }
            else
            {
                MainThread.BeginInvokeOnMainThread(Ocultar);
            }
        }

        private void CancelarCambioTab()
        {
            CancellationTokenSource? anterior =
                Interlocked.Exchange(
                    ref cambioTabCancellationTokenSource,
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
