using CONATRADEC.Models;
using CONATRADEC.Services;
using CONATRADEC.ViewModels;
using System;
using System.ComponentModel;
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

        public MultiCalculoPage()
        {
            Shell.Current.FlyoutBehavior =
                FlyoutBehavior.Disabled;

            InitializeComponent();

            BindingContext = viewModel;

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

            viewModel.LoadPagePermissions(
                "ResultadoAnalisisSueloPage");

            if (!viewModel.CanView)
            {
                await GlobalService.MostrarToastAsync(
                    "No tiene permisos para ver los cálculos complementarios.");

                await Shell.Current
                    .GoToAsync("//MainPage");

                return;
            }

            /*
             * La restauración de edición ya no se espera dentro de
             * OnAppearing. La página se muestra inmediatamente y el trabajo
             * continúa de forma asíncrona. Esto evita que Windows o Android
             * parezcan congelados mientras se cargan catálogos y resultados.
             */
            int version =
                ++versionCargaVisual;

            ActualizarVistaTab();

            _ = CompletarCargaVisualAsync(
                version);
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();

            /*
             * Invalida una espera visual anterior si el usuario abandona la
             * página antes de que finalice la restauración.
             */
            versionCargaVisual++;
        }

        private async Task CompletarCargaVisualAsync(
            int version)
        {
            try
            {
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

                if (version != versionCargaVisual)
                    return;

                /*
                 * ApplyQueryAttributes es async void. Por eso la selección
                 * original de Mixta debe capturarse después de que el ViewModel
                 * haya recibido el análisis y las pestañas seleccionadas.
                 */
                PrepararCapturaSeleccionOriginalMixta();

                Dispatcher.Dispatch(
                    ActualizarVistaTab);

                await RestaurarCalculosEdicionUiService
                    .Instance
                    .RestaurarAsync(viewModel);

                if (version != versionCargaVisual)
                    return;

                Dispatcher.Dispatch(
                    ActualizarVistaTab);
            }
            catch (Exception ex)
            {
                if (version != versionCargaVisual)
                    return;

                viewModel.Mensaje =
                    "No fue posible completar la carga visual de los " +
                    $"cálculos: {ex.Message}";
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

            Dispatcher.Dispatch(
                ActualizarVistaTab);
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
    }
}
