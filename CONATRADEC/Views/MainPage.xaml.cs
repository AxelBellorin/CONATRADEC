using CONATRADEC.Services;
using CONATRADEC.ViewModels;

namespace CONATRADEC.Views
{
    public partial class MainPage : ContentPage
    {
        private readonly MainPageViewModel viewModel = new();

        public MainPage()
        {
            Shell.Current.FlyoutBehavior = FlyoutBehavior.Disabled;
            InitializeComponent();
            BindingContext = viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            viewModel.LoadPagePermissions("MainPage");
            viewModel.PrepararPantalla();

            ContenidoPrincipal.IsVisible = viewModel.CanView;
            ContenidoSinPermiso.IsVisible = !viewModel.CanView;

            if (!viewModel.CanView)
            {
                viewModel.CancelarCarga();
                viewModel.IsBusy = false;
                return;
            }

            // Permite que Android pinte primero la estructura de la página.
            await Task.Yield();

            /*
             * La primera página se carga automáticamente. Solo contiene
             * registros activos y mantiene la paginación de 6 elementos en
             * Android y 12 en Windows.
             */
            if (!viewModel.SeHaListado &&
                !viewModel.CargandoListado)
            {
                await viewModel.CargarAnalisisAsync(
                    mostrarIndicador: true,
                    reiniciar: true);

                return;
            }

            if (viewModel.SeHaListado &&
                AnalisisListadoEstadoService.HayActualizacionPendiente &&
                !viewModel.IsBusy &&
                !viewModel.CargandoListado)
            {
                await viewModel.CargarAnalisisAsync(
                    mostrarIndicador: true,
                    reiniciar: true);

                if (viewModel.UltimaCargaExitosa)
                {
                    AnalisisListadoEstadoService
                        .ConfirmarActualizacion();
                }
            }
        }

        protected override void OnDisappearing()
        {
            viewModel.CancelarCarga();
            base.OnDisappearing();
        }
    }
}
