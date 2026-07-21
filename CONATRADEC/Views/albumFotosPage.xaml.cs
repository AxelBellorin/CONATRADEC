using CONATRADEC.Services;
using CONATRADEC.ViewModels;

namespace CONATRADEC.Views
{
    public partial class albumFotosPage : ContentPage
    {
        private readonly AlbumFotosViewModel viewModel = new();

        public albumFotosPage()
        {
            InitializeComponent();

            Shell.Current.FlyoutBehavior =
                FlyoutBehavior.Disabled;

            BindingContext = viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            viewModel.ActualizarPermisos();

            if (!viewModel.CanView)
            {
                await DisplayAlert(
                    "Permiso denegado",
                    "No tiene permisos para consultar el álbum botánico.",
                    "Aceptar");

                await Shell.Current.GoToAsync(
                    AppRoutes.Principal);

                return;
            }

            await viewModel.LoadAsync(true);
        }

        private async void OnBuscarPresionado(
            object sender,
            EventArgs e)
        {
            if (viewModel.IsBusy)
                return;

            await viewModel.BuscarAsync();
            await DesplazarAAsync(GaleriaSection);
        }

        private async void OnLimpiarBusquedaClicked(
            object sender,
            EventArgs e)
        {
            if (viewModel.IsBusy)
                return;

            await viewModel.LimpiarBusquedaAsync();
            await DesplazarAAsync(CapitulosSection);
        }

        private async void OnIncluirInactivosToggled(
            object sender,
            ToggledEventArgs e)
        {
            if (!viewModel.MostrarInactivos ||
                viewModel.IsBusy)
            {
                return;
            }

            await viewModel.AplicarInactivosAsync();
            await DesplazarAAsync(CapitulosSection);
        }

        private async Task DesplazarAAsync(
            Element destino)
        {
            /*
             * Toda la interfaz está en el mismo ScrollView.
             * Después de actualizar los datos esperamos dos ciclos
             * de medición y desplazamos al elemento real, no a una
             * coordenada que pueda quedar obsoleta.
             */
            await Task.Yield();
            await Task.Delay(80);

            await AlbumScrollView.ScrollToAsync(
                destino,
                ScrollToPosition.Start,
                false);

            await Task.Delay(80);

            await AlbumScrollView.ScrollToAsync(
                destino,
                ScrollToPosition.Start,
                false);
        }
    }
}
