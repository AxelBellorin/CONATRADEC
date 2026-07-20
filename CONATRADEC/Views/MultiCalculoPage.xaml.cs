using CONATRADEC.Models;
using CONATRADEC.Services;
using CONATRADEC.ViewModels;
using System;
using System.ComponentModel;
using System.Threading.Tasks;

namespace CONATRADEC.Views
{
    public partial class MultiCalculoPage : ContentPage
    {
        private readonly MultiCalculoViewModel viewModel =
            new MultiCalculoViewModel();

        private BalanceFormulaTabView? balanceView;
        private EnmiendaCalcareaTabView? enmiendaView;
        private FertilizacionMixtaTabView? fertilizacionView;

        public MultiCalculoPage()
        {
            Shell.Current.FlyoutBehavior = FlyoutBehavior.Disabled;
            InitializeComponent();
            BindingContext = viewModel;

            viewModel.PropertyChanged +=
                ViewModel_PropertyChanged;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            viewModel.LoadPagePermissions("ResultadoAnalisisSueloPage");

            if (!viewModel.CanView)
            {
                await GlobalService.MostrarToastAsync(
                    "No tiene permisos para ver los cálculos complementarios.");

                await Shell.Current.GoToAsync("//MainPage");
                return;
            }

            /*
             * MultiCalculoPage es un ShellContent y MAUI conserva la misma
             * instancia. Al editar otro análisis, durante unos milisegundos
             * todavía pueden existir los elementos del balance anterior.
             *
             * Se espera a que MultiCalculoViewModel y BalanceFormulaViewModel
             * hayan recibido el resultado temporal del análisis actual y que
             * la carga asíncrona del balance haya terminado. Solo entonces se
             * restauran las fuentes, el resultado y el checkbox guardados.
             */
            await EsperarInicializacionActualAsync();

            await RestaurarCalculosEdicionUiService.Instance
                .RestaurarAsync(viewModel);
        }

        private async Task EsperarInicializacionActualAsync()
        {
            if (!AnalisisEdicionService.Instance.EsModoEdicion)
                return;

            AnalisisEdicionContexto? contexto =
                AnalisisEdicionService.Instance.ContextoActual;

            if (contexto == null)
                return;

            for (int intento = 0;
                 intento < 300;
                 intento++)
            {
                if (!ReferenceEquals(
                        contexto,
                        AnalisisEdicionService.Instance.ContextoActual))
                {
                    return;
                }

                CalculoAnalisisTemporalState estadoTemporal =
                    CalculoAnalisisTemporalService.Instance
                        .ObtenerEstadoActual();

                AnalisisSueloCalculoDataResponse? resultadoActual =
                    estadoTemporal.ResultadoAnalisisSuelo;

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
                 * Si el análisis guardado no tiene balance, no es necesario
                 * esperar esa pestaña. La enmienda conserva su propia espera
                 * mediante CargaEnmiendasFinalizada dentro del restaurador.
                 */
                if (!contexto.TieneBalance ||
                    !viewModel.MostrarBalanceFormula)
                {
                    return;
                }

                bool balanceActual =
                    ReferenceEquals(
                        viewModel.BalanceFormula.ResultadoCalculo,
                        resultadoActual) &&
                    !viewModel.BalanceFormula.IsBusy &&
                    viewModel.BalanceFormula
                        .ElementosBalance.Count > 0;

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
             * TabSeleccionada también notifica las tres propiedades
             * Es...Seleccionado. Escuchar las cuatro provocaba cuatro
             * despachos y hasta cuatro intentos de redibujar la pestaña.
             */
            if (e.PropertyName != nameof(
                    MultiCalculoViewModel.TabSeleccionada))
            {
                return;
            }

            Dispatcher.Dispatch(ActualizarVistaTab);
        }

        private void ActualizarVistaTab()
        {
            if (viewModel.EsBalanceSeleccionado)
            {
                AsegurarVistaBalance();
            }
            else if (viewModel.EsEnmiendaSeleccionada)
            {
                AsegurarVistaEnmienda();
            }
            else if (viewModel.EsFertilizacionSeleccionada)
            {
                AsegurarVistaFertilizacion();
            }

            if (balanceView != null)
            {
                balanceView.IsVisible =
                    viewModel.EsBalanceSeleccionado;
            }

            if (enmiendaView != null)
            {
                enmiendaView.IsVisible =
                    viewModel.EsEnmiendaSeleccionada;
            }

            if (fertilizacionView != null)
            {
                fertilizacionView.IsVisible =
                    viewModel.EsFertilizacionSeleccionada;
            }
        }

        private void AsegurarVistaBalance()
        {
            if (balanceView != null)
                return;

            balanceView = new BalanceFormulaTabView
            {
                BindingContext = viewModel.BalanceFormula,
                IsVisible = false
            };

            ContenidoTabActual.Children.Add(balanceView);
        }

        private void AsegurarVistaEnmienda()
        {
            if (enmiendaView != null)
                return;

            enmiendaView = new EnmiendaCalcareaTabView
            {
                BindingContext = viewModel.EnmiendaCalcarea,
                IsVisible = false
            };

            ContenidoTabActual.Children.Add(enmiendaView);
        }

        private void AsegurarVistaFertilizacion()
        {
            if (fertilizacionView != null)
                return;

            fertilizacionView = new FertilizacionMixtaTabView
            {
                BindingContext = viewModel.FertilizacionMixta,
                IsVisible = false
            };

            ContenidoTabActual.Children.Add(fertilizacionView);
        }
    }
}
