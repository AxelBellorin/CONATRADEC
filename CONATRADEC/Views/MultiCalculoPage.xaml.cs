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
            if (e.PropertyName != nameof(
                    MultiCalculoViewModel.TabSeleccionada) &&
                e.PropertyName != nameof(
                    MultiCalculoViewModel.EsBalanceSeleccionado) &&
                e.PropertyName != nameof(
                    MultiCalculoViewModel.EsEnmiendaSeleccionada) &&
                e.PropertyName != nameof(
                    MultiCalculoViewModel.EsFertilizacionSeleccionada))
            {
                return;
            }

            Dispatcher.Dispatch(ActualizarVistaTab);
        }

        private void ActualizarVistaTab()
        {
            View? vista = null;

            if (viewModel.EsBalanceSeleccionado)
            {
                balanceView ??= new BalanceFormulaTabView
                {
                    BindingContext = viewModel.BalanceFormula
                };

                vista = balanceView;
            }
            else if (viewModel.EsEnmiendaSeleccionada)
            {
                enmiendaView ??= new EnmiendaCalcareaTabView
                {
                    BindingContext = viewModel.EnmiendaCalcarea
                };

                vista = enmiendaView;
            }
            else if (viewModel.EsFertilizacionSeleccionada)
            {
                fertilizacionView ??=
                    new FertilizacionMixtaTabView
                    {
                        BindingContext =
                            viewModel.FertilizacionMixta
                    };

                vista = fertilizacionView;
            }

            if (vista == null ||
                ContenidoTabActual.Children.Contains(vista))
            {
                return;
            }

            ContenidoTabActual.Children.Clear();
            ContenidoTabActual.Children.Add(vista);
        }
    }
}
