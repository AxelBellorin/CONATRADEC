using CONATRADEC.Services;
using CONATRADEC.ViewModels;
using System.Diagnostics;

namespace CONATRADEC.Views
{
    public partial class noticiasPage : ContentPage
    {
        private readonly NoticiasViewModel viewModel = new();

        public noticiasPage()
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
                ContenidoPrincipal.IsVisible = viewModel.CanView;
                ContenidoSinPermiso.IsVisible = !viewModel.CanView;

                if (!viewModel.CanView)
                    return;

                // Permite que Android dibuje primero la estructura de la página.
                await Task.Yield();
                await viewModel.InicializarAsync();
            }
            catch (OperationCanceledException)
            {
                // La navegación canceló la carga de la página.
            }
            catch (ObjectDisposedException)
            {
                // El stream HTTP se cerró porque el usuario cambió de página.
            }
            catch (Exception ex)
            {
                /*
                 * OnAppearing es async void. Una excepción que salga de este
                 * método finaliza la aplicación en Release; por eso se captura
                 * en el límite del ciclo de vida de la página.
                 */
                Debug.WriteLine(
                    $"Error al abrir la página de noticias: {ex}");

                try
                {
                    await GlobalService.MostrarErrorAsync(
                        "No fue posible abrir el centro de noticias.");
                }
                catch
                {
                    // Nunca se vuelve a propagar desde un método async void.
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
                    $"No fue posible cancelar la carga de noticias: {ex}");
            }

            base.OnDisappearing();
        }
    }
}
