using CONATRADEC.Services;
using CONATRADEC.ViewModels;
using Microsoft.Maui.Devices;

namespace CONATRADEC.Views
{
    public partial class albumFotosPage : ContentPage
    {
        private AlbumFotosViewModel viewModel = new();
        private bool paginaMostrada;

        public albumFotosPage()
        {
            InitializeComponent();
            Shell.Current.FlyoutBehavior = FlyoutBehavior.Disabled;
            BindingContext = viewModel;

            AlbumCollectionView.ItemSizingStrategy =
                ItemSizingStrategy.MeasureFirstItem;

            if (DeviceInfo.Platform == DevicePlatform.WinUI)
            {
                AlbumCollectionView.SizeChanged +=
                    OnAlbumCollectionViewSizeChanged;
            }
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            /*
             * El álbum permanece debajo de detalles y formularios dinámicos.
             * Al volver se descarta exclusivamente su estado de consulta para
             * reconstruir categorías, filtros y primera página desde la API.
             */
            if (paginaMostrada)
            {
                viewModel.CancelarConsultas();
                viewModel = new AlbumFotosViewModel();
                BindingContext = viewModel;
            }
            else
            {
                paginaMostrada = true;
            }

            viewModel.ActualizarPermisos();

            if (!viewModel.CanView)
            {
                await DisplayAlert(
                    "Permiso denegado",
                    "No tiene permisos para consultar el álbum botánico.",
                    "Aceptar");
                await Shell.Current.GoToAsync(AppRoutes.Principal);
                return;
            }

            AplicarColumnas(AlbumCollectionView.Width);
            await Task.Yield();
            await viewModel.AsegurarCargaAsync(true);
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            viewModel.CancelarConsultas();
        }

        private void OnAlbumCollectionViewSizeChanged(
            object? sender,
            EventArgs e) =>
            AplicarColumnas(AlbumCollectionView.Width);

        private void AplicarColumnas(double ancho)
        {
            if (AlbumCollectionView.ItemsLayout is not GridItemsLayout layout ||
                double.IsNaN(ancho) || ancho <= 0)
            {
                return;
            }

            int span = DeviceInfo.Platform == DevicePlatform.WinUI
                ? ancho switch
                {
                    < 650 => 1,
                    < 1080 => 2,
                    _ => 3
                }
                : 1;

            if (layout.Span != span)
                layout.Span = span;
        }

        private async void OnBuscarPresionado(
            object? sender,
            EventArgs e) =>
            await viewModel.BuscarAsync();

        private async void OnLimpiarBusquedaClicked(
            object? sender,
            EventArgs e) =>
            await viewModel.LimpiarBusquedaAsync();

        private async void OnIncluirInactivosToggled(
            object? sender,
            ToggledEventArgs e) =>
            await viewModel.AplicarInactivosAsync();
    }
}
