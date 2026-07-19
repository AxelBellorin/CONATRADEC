using CONATRADEC.Services;
using CONATRADEC.ViewModels;
using System.ComponentModel;

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

            await RestaurarCalculosEdicionUiService.Instance
                .RestaurarAsync(viewModel);
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
