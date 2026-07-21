using CONATRADEC.Services;
using CONATRADEC.ViewModels;
using Microsoft.Maui.ApplicationModel;

namespace CONATRADEC.Views
{
    public partial class albumFotosPage : ContentPage
    {
        private readonly AlbumFotosViewModel viewModel = new();
        private CancellationTokenSource? scrollCancellationTokenSource;

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
             * AlbumFotosPage está declarada como ShellContent, por lo que
             * la misma instancia puede conservar el foco y el ScrollY de
             * una visita anterior. El contenido se espera y luego se lleva
             * de forma comprobada al inicio.
             */
            await RestablecerVistaAlInicioAsync();
        }

        protected override void OnDisappearing()
        {
            scrollCancellationTokenSource?.Cancel();
            base.OnDisappearing();
        }

        private async void OnBuscarPresionado(
            object sender,
            EventArgs e)
        {
            if (viewModel.IsBusy)
                return;

            await viewModel.BuscarAsync();
            await RestablecerVistaAlInicioAsync();
        }

        private async void OnLimpiarBusquedaClicked(
            object sender,
            EventArgs e)
        {
            if (viewModel.IsBusy)
                return;

            await viewModel.LimpiarBusquedaAsync();
            await RestablecerVistaAlInicioAsync();
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
            await RestablecerVistaAlInicioAsync();
        }

        private async Task RestablecerVistaAlInicioAsync()
        {
            scrollCancellationTokenSource?.Cancel();
            scrollCancellationTokenSource =
                new CancellationTokenSource();

            CancellationToken token =
                scrollCancellationTokenSource.Token;

            try
            {
                await EsperarContenidoEstableAsync(token);

                /*
                 * El ScrollView recibe el foco para quitarlo de cualquier
                 * botón de una tarjeta anterior. WinUI puede desplazar
                 * automáticamente hasta el control que conserva el foco.
                 */
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    AlbumScrollView.Focus();
                });

                for (int intento = 0;
                     intento < 8;
                     intento++)
                {
                    token.ThrowIfCancellationRequested();

                    await MainThread.InvokeOnMainThreadAsync(
                        () => AlbumScrollView.ScrollToAsync(
                            0,
                            0,
                            false));

                    await Task.Delay(75, token);

                    if (AlbumScrollView.ScrollY <= 1)
                        break;
                }
            }
            catch (OperationCanceledException)
            {
                // Una operación nueva reemplazó el ajuste anterior.
            }
        }

        private async Task EsperarContenidoEstableAsync(
            CancellationToken token)
        {
            double alturaAnterior = -1;
            int medicionesEstables = 0;

            for (int intento = 0;
                 intento < 24;
                 intento++)
            {
                token.ThrowIfCancellationRequested();
                await Task.Delay(50, token);

                double alturaActual =
                    AlbumScrollView.Content?.Height ?? 0;

                if (alturaActual > 0 &&
                    Math.Abs(
                        alturaActual -
                        alturaAnterior) < 0.5)
                {
                    medicionesEstables++;
                }
                else
                {
                    medicionesEstables = 0;
                }

                alturaAnterior = alturaActual;

                if (medicionesEstables >= 3)
                    return;
            }
        }
    }
}
