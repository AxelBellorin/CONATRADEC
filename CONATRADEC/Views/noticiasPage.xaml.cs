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
                {
                    NoticiasVisitaService.FinalizarVisita();
                    viewModel.CancelarCarga();
                    return;
                }

                AplicarResponsiveNoticias();

                bool nuevaVisita =
                    NoticiasVisitaService.AsegurarVisita();

                // Permite que Android dibuje primero la estructura de la página.
                await Task.Yield();

                if (nuevaVisita)
                {
                    await viewModel.IniciarNuevaVisitaAsync();
                    return;
                }

                /*
                 * Regresar desde Detalle sin cambios mantiene exactamente la
                 * página y filtros de la visita y no consulta el feed.
                 *
                 * Si desde Detalle se editó la publicación, el formulario ya
                 * incrementó PublicacionListadoEstadoService y solamente en ese
                 * caso se renueva la página actual con los filtros aplicados.
                 */
                if (viewModel.SeHaListado &&
                    viewModel.RequiereRecargaPorCambios &&
                    !viewModel.IsBusy &&
                    !viewModel.CargandoListado)
                {
                    await viewModel.RecargarPaginaActualAsync();
                    return;
                }

                if (!viewModel.SeHaListado &&
                    !viewModel.CargandoListado)
                {
                    await viewModel.InicializarAsync();
                }
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
                /*
                 * Cancelar una solicitud no finaliza la visita. El servicio de
                 * visita decide si la navegación sigue dentro del módulo o si
                 * realmente se abandonó Noticias.
                 */
                viewModel.CancelarCarga();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    $"No fue posible cancelar la carga de noticias: {ex}");
            }

            base.OnDisappearing();
        }

        /// <summary>
        /// Espera el reemplazo real de la página y posiciona la primera noticia
        /// al inicio. No se ejecuta cuando la consulta falla o no cambia página.
        /// </summary>
        private async void PaginacionNoticias_Clicked(
            object? sender,
            EventArgs e)
        {
            int paginaAnterior = viewModel.PaginaActual;
            bool operacionDetectada = false;

            for (int intento = 0; intento < 240; intento++)
            {
                if (viewModel.CargandoListado ||
                    viewModel.PaginaActual != paginaAnterior)
                {
                    operacionDetectada = true;
                }

                if (operacionDetectada &&
                    !viewModel.CargandoListado)
                {
                    if (viewModel.PaginaActual != paginaAnterior &&
                        viewModel.Publicaciones.Count > 0)
                    {
                        await DesplazarNoticiasAlInicioAsync();
                    }

                    return;
                }

                await Task.Delay(50);
            }
        }

        private async Task DesplazarNoticiasAlInicioAsync()
        {
            if (NoticiasCollectionView == null ||
                viewModel.Publicaciones.Count == 0)
            {
                return;
            }

            await Task.Delay(60);

            NoticiasCollectionView.ScrollTo(
                0,
                position: ScrollToPosition.Start,
                animate: false);
        }
    }
}
