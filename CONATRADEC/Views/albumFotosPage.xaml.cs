using CONATRADEC.Services;
using CONATRADEC.ViewModels;
using System.Diagnostics;
using Microsoft.Maui.ApplicationModel;

namespace CONATRADEC.Views
{
    public partial class albumFotosPage : ContentPage
    {
        private const double HeroCompactoBreakpoint = 900;
        private const double BusquedaCompactaBreakpoint = 620;
        private const double DosColumnasBreakpoint = 900;
        private const double TresColumnasBreakpoint = 1280;

        private readonly AlbumFotosViewModel viewModel = new();
        private int spanActual = -1;
        private bool? heroCompacto;
        private bool? busquedaCompacta;

        public albumFotosPage()
        {
            InitializeComponent();
            Shell.Current.FlyoutBehavior = FlyoutBehavior.Disabled;
            BindingContext = viewModel;

            Loaded += (_, _) => AplicarDisenoResponsivo();
            SizeChanged += (_, _) => AplicarDisenoResponsivo();
            AlbumCollectionView.SizeChanged +=
                (_, _) => AplicarDisenoResponsivo();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            try
            {
                AplicarDisenoResponsivo();
                viewModel.ActualizarPermisos();

                if (!viewModel.CanView)
                {
                    AlbumBotanicoVisitaService.FinalizarVisita();
                    viewModel.CancelarConsultas();

                    await DisplayAlert(
                        "Permiso denegado",
                        "No tiene permisos para consultar el álbum botánico.",
                        "Aceptar");

                    await Shell.Current.GoToAsync(AppRoutes.Principal);
                    return;
                }

                bool nuevaVisita =
                    AlbumBotanicoVisitaService.AsegurarVisita();

                await Task.Yield();

                if (nuevaVisita)
                {
                    await viewModel.IniciarNuevaVisitaAsync();
                }
                else if (viewModel.SeHaListado &&
                         viewModel.RequiereRecargaPorCambios &&
                         !viewModel.IsBusy)
                {
                    await viewModel.RecargarContextoActualAsync();
                }
                else if (!viewModel.SeHaListado && !viewModel.IsBusy)
                {
                    await viewModel.InicializarAsync();
                }

                AplicarDisenoResponsivo();
            }
            catch (OperationCanceledException)
            {
                // La navegación canceló una consulta activa.
            }
            catch (ObjectDisposedException)
            {
                // La respuesta HTTP se cerró durante una navegación rápida.
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error al abrir el Álbum Botánico: {ex}");

                try
                {
                    await GlobalService.MostrarErrorAsync(
                        "No fue posible abrir el Álbum Botánico.");
                }
                catch
                {
                    // OnAppearing es async void: nunca se propaga la excepción.
                }
            }
        }

        protected override void OnDisappearing()
        {
            try
            {
                /*
                 * Se cancela únicamente la solicitud activa. La visita termina
                 * cuando AlbumBotanicoVisitaService detecta una ruta externa.
                 */
                viewModel.CancelarConsultas();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    $"No fue posible cancelar la consulta del álbum: {ex}");
            }

            base.OnDisappearing();
        }

        protected override void OnSizeAllocated(double width, double height)
        {
            base.OnSizeAllocated(width, height);
            AplicarDisenoResponsivo();
        }

        private void AplicarDisenoResponsivo()
        {
            AjustarMargenes();
            AplicarHero();
            AplicarAccionesHero();
            AplicarBusqueda();
            AplicarColumnas();
            AplicarPaginacion();
        }

        private void AjustarMargenes()
        {
            double ancho = Width;
            if (ancho <= 0)
                return;

            if (ancho < 600)
            {
                AlbumHeaderStack.Margin = new Thickness(0, 0, 0, 14);
                AlbumCollectionView.Margin = new Thickness(12, 12, 12, 20);
            }
            else if (ancho < 950)
            {
                AlbumHeaderStack.Margin = new Thickness(0, 0, 0, 14);
                AlbumCollectionView.Margin = new Thickness(20, 18, 20, 26);
            }
            else
            {
                AlbumHeaderStack.Margin = new Thickness(0, 0, 0, 14);
                AlbumCollectionView.Margin = new Thickness(28, 22, 28, 30);
            }
        }

        private void AplicarHero()
        {
            /*
             * El encabezado toma una única referencia estable: el ancho real
             * de la página. No se utiliza HeroAlbumGrid.Width ni el ancho del
             * panel de acciones porque ambos cambian como consecuencia del
             * propio diseño y en WinUI podían provocar alternancia entre dos
             * composiciones durante las pasadas de medición.
             */
            double ancho = Width;

            if (ancho <= 0)
                return;

            bool compacto = ancho < HeroCompactoBreakpoint;
            if (heroCompacto == compacto)
                return;

            heroCompacto = compacto;
            HeroAlbumGrid.RowDefinitions.Clear();
            HeroAlbumGrid.ColumnDefinitions.Clear();

            if (compacto)
            {
                HeroAlbumGrid.RowDefinitions.Add(
                    new RowDefinition(GridLength.Auto));
                HeroAlbumGrid.RowDefinitions.Add(
                    new RowDefinition(GridLength.Auto));
                HeroAlbumGrid.ColumnDefinitions.Add(
                    new ColumnDefinition(GridLength.Star));

                Ubicar(HeroAlbumTexto, 0, 0, 1);
                Ubicar(HeroAlbumAcciones, 1, 0, 1);
                HeroAlbumAcciones.HorizontalOptions = LayoutOptions.Fill;
            }
            else
            {
                HeroAlbumGrid.RowDefinitions.Add(
                    new RowDefinition(GridLength.Auto));
                HeroAlbumGrid.ColumnDefinitions.Add(
                    new ColumnDefinition(GridLength.Star));
                HeroAlbumGrid.ColumnDefinitions.Add(
                    new ColumnDefinition(GridLength.Auto));

                Ubicar(HeroAlbumTexto, 0, 0, 1);
                Ubicar(HeroAlbumAcciones, 0, 1, 1);
                HeroAlbumAcciones.HorizontalOptions = LayoutOptions.End;
            }
        }

        private void AplicarAccionesHero()
        {
            /*
             * Los tres botones siempre conservan la misma estructura vertical.
             * Únicamente cambia la posición del bloque completo: a la derecha
             * en pantallas amplias o debajo del título en pantallas estrechas.
             * Esto elimina la dependencia circular de mediciones en WinUI.
             */
            if (HeroAlbumAcciones.RowDefinitions.Count == 3 &&
                HeroAlbumAcciones.ColumnDefinitions.Count == 1 &&
                Grid.GetRow(NuevaCategoriaButton) == 0 &&
                Grid.GetRow(NuevaSubcategoriaButton) == 1 &&
                Grid.GetRow(EliminadosButton) == 2)
            {
                return;
            }

            HeroAlbumAcciones.RowDefinitions.Clear();
            HeroAlbumAcciones.ColumnDefinitions.Clear();

            for (int i = 0; i < 3; i++)
            {
                HeroAlbumAcciones.RowDefinitions.Add(
                    new RowDefinition(GridLength.Auto));
            }

            HeroAlbumAcciones.ColumnDefinitions.Add(
                new ColumnDefinition(GridLength.Star));

            Ubicar(NuevaCategoriaButton, 0, 0, 1);
            Ubicar(NuevaSubcategoriaButton, 1, 0, 1);
            Ubicar(EliminadosButton, 2, 0, 1);
        }

        private void AplicarBusqueda()
        {
            double ancho = BusquedaAlbumGrid.Width > 0
                ? BusquedaAlbumGrid.Width
                : AlbumCollectionView.Width;

            if (ancho <= 0)
                return;

            bool compacta = ancho < BusquedaCompactaBreakpoint;
            if (busquedaCompacta == compacta)
                return;

            busquedaCompacta = compacta;
            BusquedaAlbumGrid.RowDefinitions.Clear();
            BusquedaAlbumGrid.ColumnDefinitions.Clear();

            if (compacta)
            {
                BusquedaAlbumGrid.RowDefinitions.Add(
                    new RowDefinition(GridLength.Auto));
                BusquedaAlbumGrid.RowDefinitions.Add(
                    new RowDefinition(GridLength.Auto));
                BusquedaAlbumGrid.ColumnDefinitions.Add(
                    new ColumnDefinition(GridLength.Star));

                Ubicar(BusquedaAlbum, 0, 0, 1);
                Ubicar(LimpiarBusquedaButton, 1, 0, 1);
            }
            else
            {
                BusquedaAlbumGrid.RowDefinitions.Add(
                    new RowDefinition(GridLength.Auto));
                BusquedaAlbumGrid.ColumnDefinitions.Add(
                    new ColumnDefinition(GridLength.Star));
                BusquedaAlbumGrid.ColumnDefinitions.Add(
                    new ColumnDefinition(GridLength.Auto));

                Ubicar(BusquedaAlbum, 0, 0, 1);
                Ubicar(LimpiarBusquedaButton, 0, 1, 1);
            }
        }

        private void AplicarColumnas()
        {
            double ancho = AlbumCollectionView.Width > 0
                ? AlbumCollectionView.Width
                : Width;

            if (ancho <= 0)
                return;

            int span = ancho >= TresColumnasBreakpoint
                ? 3
                : ancho >= DosColumnasBreakpoint
                    ? 2
                    : 1;

            if (spanActual == span && AlbumGrid.Span == span)
                return;

            spanActual = span;
            AlbumGrid.Span = span;
        }

        private void AplicarPaginacion()
        {
            double ancho = AlbumCollectionView.Width > 0
                ? AlbumCollectionView.Width
                : Width;

            if (ancho <= 0)
                return;

            PaginacionAlbum.WidthRequest = Math.Min(
                580,
                Math.Max(0, ancho - (ancho < 480 ? 8 : 24)));
        }

        private static void Ubicar(
            View control,
            int row,
            int column,
            int columnSpan)
        {
            Grid.SetRow(control, row);
            Grid.SetColumn(control, column);
            Grid.SetColumnSpan(control, columnSpan);
        }

        private async void OnBuscarPresionado(object? sender, EventArgs e)
        {
            await viewModel.BuscarAsync();
        }

        private async void OnLimpiarBusquedaClicked(object? sender, EventArgs e)
        {
            await viewModel.LimpiarBusquedaAsync();
        }

        private async void OnPaginaAnteriorClicked(object? sender, EventArgs e)
        {
            bool cambio = await viewModel.IrPaginaAnteriorAsync();
            if (cambio)
                await DesplazarGaleriaAlInicioAsync();
        }

        private async void OnPaginaSiguienteClicked(object? sender, EventArgs e)
        {
            bool cambio = await viewModel.IrPaginaSiguienteAsync();
            if (cambio)
                await DesplazarGaleriaAlInicioAsync();
        }

        private async Task DesplazarGaleriaAlInicioAsync()
        {
            if (viewModel.Registros.Count == 0)
                return;

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                AlbumCollectionView.ScrollTo(
                    0,
                    position: ScrollToPosition.Start,
                    animate: false);
            });
        }

        private async void OnAbrirEliminadosClicked(
            object? sender,
            EventArgs e)
        {
            if (!viewModel.MostrarEliminados || Shell.Current?.Navigation == null)
                return;

            var pagina = new albumEliminadosPage();
            await Shell.Current.Navigation.PushModalAsync(
                new NavigationPage(pagina));

            await Task.Yield();
            await pagina.InicializarDespuesDeMostrarAsync();
        }
    }
}
