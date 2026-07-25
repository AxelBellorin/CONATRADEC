using CONATRADEC.Services;
using CONATRADEC.ViewModels;
using System.Diagnostics;

namespace CONATRADEC.Views
{
    public partial class publicacionesAdminPage : ContentPage
    {
        private readonly PublicacionesAdminViewModel viewModel = new();

        public publicacionesAdminPage()
        {
            InitializeComponent();
            Shell.Current.FlyoutBehavior = FlyoutBehavior.Disabled;
            BindingContext = viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            try
            {
                viewModel.ActualizarPermisos();
                ContenidoPrincipal.IsVisible = viewModel.CanAdministrar;
                ContenidoSinPermiso.IsVisible = !viewModel.CanAdministrar;

                if (!viewModel.CanAdministrar)
                    return;

                // Primero se renderiza la página y después inicia la consulta.
                await Task.Yield();
                await viewModel.InicializarAsync();
            }
            catch (OperationCanceledException)
            {
                // La navegación canceló la carga de la página.
            }
            catch (ObjectDisposedException)
            {
                // El stream HTTP se cerró durante una navegación rápida.
            }
            catch (Exception ex)
            {
                /*
                 * OnAppearing es async void. En Release una excepción no
                 * capturada aquí cierra completamente la aplicación.
                 */
                Debug.WriteLine(
                    $"Error al abrir la administración de publicaciones: {ex}");

                try
                {
                    await GlobalService.MostrarErrorAsync(
                        "No fue posible abrir la administración de publicaciones.");
                }
                catch
                {
                    // Nunca se propaga una excepción desde este async void.
                }
            }
        }

        protected override void OnDisappearing()
        {
            try
            {
                viewModel.CancelarCarga();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    $"No fue posible cancelar la carga administrativa: {ex}");
            }

            base.OnDisappearing();
        }
    }
}
