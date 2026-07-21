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

            /*
             * La página ahora es una instancia nueva.
             * Solo se asegura la posición inicial una vez.
             */
            await Task.Yield();
            await AlbumScrollView.ScrollToAsync(
                0,
                0,
                false);
        }

        private async void OnBuscarPresionado(
            object sender,
            EventArgs e)
        {
            if (viewModel.IsBusy)
                return;

            await viewModel.BuscarAsync();
        }

        private async void OnLimpiarBusquedaClicked(
            object sender,
            EventArgs e)
        {
            if (viewModel.IsBusy)
                return;

            await viewModel.LimpiarBusquedaAsync();
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
        }
    }
}
