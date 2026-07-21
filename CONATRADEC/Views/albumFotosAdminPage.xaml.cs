using CONATRADEC.Services;
using CONATRADEC.ViewModels;
using System.Diagnostics;

namespace CONATRADEC.Views
{
    [QueryProperty(nameof(RegistroId), "RegistroId")]
    public partial class albumFotosAdminPage : ContentPage
    {
        private readonly AlbumFotosAdminViewModel
            viewModel = new();

        private bool regresando;

        public int RegistroId
        {
            set => viewModel.Id = value;
        }

        public albumFotosAdminPage()
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

            bool denied =
                !viewModel.CanView ||
                (
                    !viewModel.CanAdd &&
                    !viewModel.CanEdit &&
                    !viewModel.CanDelete
                );

            if (denied)
            {
                await DisplayAlert(
                    "Permiso denegado",
                    "No tiene permisos para administrar fotografías.",
                    "Aceptar");

                await Shell.Current.GoToAsync(
                    AppRoutes.Regresar,
                    false);

                return;
            }

            await viewModel.LoadAsync(true);
            await RestablecerAlInicioAsync();
        }

        private async void OnRegresarClicked(
            object sender,
            EventArgs e)
        {
            if (regresando || viewModel.IsBusy)
                return;

            regresando = true;
            RegresarButton.IsEnabled = false;

            try
            {
                /*
                 * Retroceso real en la pila.
                 * No crea otra instancia de albumDetallePage.
                 */
                await Shell.Current.GoToAsync(
                    AppRoutes.Regresar,
                    false);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    $"No fue posible regresar desde fotografías: {ex}");

                await DisplayAlert(
                    "No fue posible regresar",
                    "Ocurrió un problema al volver a la pantalla anterior.",
                    "Aceptar");
            }
            finally
            {
                regresando = false;

                if (Handler != null)
                    RegresarButton.IsEnabled = true;
            }
        }

        private async Task RestablecerAlInicioAsync()
        {
            /*
             * WinUI puede enfocar automáticamente un Entry de una tarjeta
             * y desplazar el contenido. El foco queda en el botón fijo.
             */
            await Task.Delay(120);

#if WINDOWS
            await Microsoft.Maui.ApplicationModel.MainThread
                .InvokeOnMainThreadAsync(() =>
                {
                    RegresarButton.Focus();

                    if (FotosCollectionView.Handler?.PlatformView
                        is Microsoft.UI.Xaml.DependencyObject nativeView)
                    {
                        Microsoft.UI.Xaml.Controls.ScrollViewer?
                            scrollViewer =
                                BuscarScrollViewer(nativeView);

                        scrollViewer?.ChangeView(
                            null,
                            0,
                            null,
                            true);
                    }
                });
#endif
        }

#if WINDOWS
        private static Microsoft.UI.Xaml.Controls.ScrollViewer?
            BuscarScrollViewer(
                Microsoft.UI.Xaml.DependencyObject elemento)
        {
            if (elemento
                is Microsoft.UI.Xaml.Controls.ScrollViewer scrollViewer)
            {
                return scrollViewer;
            }

            int cantidad =
                Microsoft.UI.Xaml.Media.VisualTreeHelper
                    .GetChildrenCount(elemento);

            for (int i = 0; i < cantidad; i++)
            {
                Microsoft.UI.Xaml.DependencyObject hijo =
                    Microsoft.UI.Xaml.Media.VisualTreeHelper
                        .GetChild(elemento, i);

                Microsoft.UI.Xaml.Controls.ScrollViewer?
                    encontrado =
                        BuscarScrollViewer(hijo);

                if (encontrado != null)
                    return encontrado;
            }

            return null;
        }
#endif
    }
}
