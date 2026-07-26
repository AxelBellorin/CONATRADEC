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

        public MultiCalculoPage()
        {
            Shell.Current.FlyoutBehavior =
                FlyoutBehavior.Disabled;

            InitializeComponent();

            BindingContext = viewModel;

            viewModel.PropertyChanged +=
                ViewModel_PropertyChanged;

            viewModel
                .BalanceFormula
                .ComplementoFertilizacionMixtaCambiado +=
                    BalanceFormula_
                        ComplementoFertilizacionMixtaCambiado;
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

            CapturarSeleccionOriginalMixta();

            /*
             * MultiCalculoPage es un ShellContent y MAUI conserva
             * la misma instancia. Al editar otro análisis, durante
             * unos milisegundos todavía pueden existir los elementos
             * del balance anterior.
             *
             * Se espera a que MultiCalculoViewModel y
             * BalanceFormulaViewModel hayan recibido el resultado
             * temporal del análisis actual. Solo entonces se restauran
             * las fuentes, el resultado y el checkbox guardados.
             */
            await EsperarInicializacionActualAsync();

            await RestaurarCalculosEdicionUiService
                .Instance
                .RestaurarAsync(viewModel);
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

        private async void
            BalanceFormula_
                ComplementoFertilizacionMixtaCambiado(
                    object? sender,
                    BalanceFertilizacionMixtaChangedEventArgs
                        e)
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

            for (int intento = 0;
                 intento < 300;
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

                CalculoAnalisisTemporalState
                    estadoTemporal =
                        CalculoAnalisisTemporalService
                            .Instance
                            .ObtenerEstadoActual();

                AnalisisSueloCalculoDataResponse?
                    resultadoActual =
                        estadoTemporal
                            .ResultadoAnalisisSuelo;

                bool multiCalculoActual =
                    resultadoActual != null &&
                    viewModel.EsModoEdicion &&
                    ReferenceEquals(
                        viewModel.ResultadoCalculo,
                        resultadoActual);

                if (!multiCalculoActual)
                {
                    await Task.Delay(100);
                    continue;
                }

                /*
                 * Si el análisis guardado no tiene balance,
                 * no es necesario esperar esa pestaña.
                 */
                if (!contexto.TieneBalance ||
                    !viewModel
                        .MostrarBalanceFormula)
                {
                    return;
                }

                bool balanceActual =
                    ReferenceEquals(
                        viewModel
                            .BalanceFormula
                            .ResultadoCalculo,
                        resultadoActual) &&
                    !viewModel
                        .BalanceFormula
                        .IsBusy &&
                    viewModel
                        .BalanceFormula
                        .ElementosBalance
                        .Count > 0;

                if (balanceActual)
                    return;

                await Task.Delay(100);
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
