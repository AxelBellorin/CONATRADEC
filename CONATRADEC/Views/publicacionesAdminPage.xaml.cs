using CONATRADEC.Services;
using CONATRADEC.ViewModels;
using System.Diagnostics;

namespace CONATRADEC.Views
{
    public partial class publicacionesAdminPage : ContentPage
    {
        private const double HeroCompactoBreakpoint = 720;
        private const double AccionesHeroApiladasBreakpoint = 520;
        private const double FiltrosUnaColumnaBreakpoint = 680;
        private const double FiltrosDosFilasBreakpoint = 980;
        private const double DosColumnasBreakpoint = 900;
        private const double TresColumnasBreakpoint = 1280;

        private readonly PublicacionesAdminViewModel viewModel = new();

        private int spanActual = -1;
        private bool? heroCompacto;
        private int modoAccionesHero = -1;
        private int modoFiltros = -1;
        private int paginaAntesCambio = -1;

        public publicacionesAdminPage()
        {
            InitializeComponent();
            Shell.Current.FlyoutBehavior = FlyoutBehavior.Disabled;
            BindingContext = viewModel;

            viewModel.Publicaciones.CollectionChanged +=
                (_, _) => AplicarColumnas();

            Loaded += (_, _) => AplicarDisenoResponsivo();
            SizeChanged += (_, _) => AplicarDisenoResponsivo();
            PublicacionesCollection.SizeChanged +=
                (_, _) => AplicarDisenoResponsivo();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            try
            {
                AplicarDisenoResponsivo();
                viewModel.ActualizarPermisos();

                ContenidoPrincipal.IsVisible = viewModel.CanAdministrar;
                ContenidoSinPermiso.IsVisible = !viewModel.CanAdministrar;

                if (!viewModel.CanAdministrar)
                {
                    PublicacionesAdminVisitaService.FinalizarVisita();
                    viewModel.CancelarCarga();
                    return;
                }

                bool nuevaVisita =
                    PublicacionesAdminVisitaService.AsegurarVisita();

                // Primero permite que Android/WinUI dibujen la estructura.
                await Task.Yield();

                if (nuevaVisita)
                {
                    await viewModel.IniciarNuevaVisitaAsync();
                }
                else if (viewModel.SeHaListado &&
                         viewModel.RequiereRecargaPorCambios &&
                         !viewModel.IsBusy)
                {
                    /*
                     * Crear/Editar/Eliminadas son subflujos de esta visita.
                     * Sólo una mutación obliga a consultar nuevamente la página
                     * que el usuario estaba viendo y conserva filtros aplicados.
                     */
                    await viewModel.RecargarPaginaActualAsync();
                }
                else if (!viewModel.SeHaListado &&
                         !viewModel.IsBusy)
                {
                    await viewModel.InicializarAsync();
                }

                AplicarDisenoResponsivo();
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
                /*
                 * Cancelar la solicitud no finaliza la visita. El servicio de
                 * visita determina si la navegación sigue en Crear/Editar o si
                 * realmente se abandonó la administración.
                 */
                viewModel.CancelarCarga();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    $"No fue posible cancelar la carga administrativa: {ex}");
            }

            base.OnDisappearing();
        }

        protected override void OnSizeAllocated(
            double width,
            double height)
        {
            base.OnSizeAllocated(width, height);
            AplicarDisenoResponsivo();
        }

        private void AplicarDisenoResponsivo()
        {
            AjustarMargenes();
            AplicarHero();
            AplicarAccionesHero();
            AplicarFiltros();
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
                EncabezadoAdminStack.Margin =
                    new Thickness(12, 12, 12, 10);

                PublicacionesCollection.Margin =
                    new Thickness(12, 0, 12, 20);
            }
            else if (ancho < 950)
            {
                EncabezadoAdminStack.Margin =
                    new Thickness(20, 18, 20, 12);

                PublicacionesCollection.Margin =
                    new Thickness(20, 0, 20, 26);
            }
            else
            {
                EncabezadoAdminStack.Margin =
                    new Thickness(28, 22, 28, 14);

                PublicacionesCollection.Margin =
                    new Thickness(28, 0, 28, 30);
            }
        }

        private void AplicarHero()
        {
            double ancho =
                HeroAdminGrid.Width > 0
                    ? HeroAdminGrid.Width
                    : Width;

            if (ancho <= 0)
                return;

            bool compacto =
                ancho < HeroCompactoBreakpoint;

            if (heroCompacto == compacto)
                return;

            heroCompacto = compacto;
            HeroAdminGrid.RowDefinitions.Clear();
            HeroAdminGrid.ColumnDefinitions.Clear();

            if (compacto)
            {
                HeroAdminGrid.RowDefinitions.Add(
                    new RowDefinition(GridLength.Auto));
                HeroAdminGrid.RowDefinitions.Add(
                    new RowDefinition(GridLength.Auto));
                HeroAdminGrid.ColumnDefinitions.Add(
                    new ColumnDefinition(GridLength.Star));

                Grid.SetRow(HeroAdminTexto, 0);
                Grid.SetColumn(HeroAdminTexto, 0);
                Grid.SetColumnSpan(HeroAdminTexto, 1);

                Grid.SetRow(HeroAdminAcciones, 1);
                Grid.SetColumn(HeroAdminAcciones, 0);
                Grid.SetColumnSpan(HeroAdminAcciones, 1);

                HeroAdminAcciones.HorizontalOptions =
                    LayoutOptions.Fill;
            }
            else
            {
                HeroAdminGrid.RowDefinitions.Add(
                    new RowDefinition(GridLength.Auto));
                HeroAdminGrid.ColumnDefinitions.Add(
                    new ColumnDefinition(GridLength.Star));
                HeroAdminGrid.ColumnDefinitions.Add(
                    new ColumnDefinition(GridLength.Auto));

                Grid.SetRow(HeroAdminTexto, 0);
                Grid.SetColumn(HeroAdminTexto, 0);
                Grid.SetColumnSpan(HeroAdminTexto, 1);

                Grid.SetRow(HeroAdminAcciones, 0);
                Grid.SetColumn(HeroAdminAcciones, 1);
                Grid.SetColumnSpan(HeroAdminAcciones, 1);

                HeroAdminAcciones.HorizontalOptions =
                    LayoutOptions.End;
            }

            modoAccionesHero = -1;
        }

        private void AplicarAccionesHero()
        {
            double ancho =
                heroCompacto == true
                    ? HeroAdminGrid.Width
                    : HeroAdminAcciones.Width;

            if (ancho <= 0)
                ancho = Width;

            if (ancho <= 0)
                return;

            bool apiladas =
                ancho < AccionesHeroApiladasBreakpoint;

            bool llenar =
                heroCompacto == true;

            int nuevoModo =
                apiladas
                    ? 0
                    : llenar
                        ? 1
                        : 2;

            if (modoAccionesHero == nuevoModo)
                return;

            modoAccionesHero = nuevoModo;
            HeroAdminAcciones.RowDefinitions.Clear();
            HeroAdminAcciones.ColumnDefinitions.Clear();

            if (apiladas)
            {
                HeroAdminAcciones.RowDefinitions.Add(
                    new RowDefinition(GridLength.Auto));
                HeroAdminAcciones.RowDefinitions.Add(
                    new RowDefinition(GridLength.Auto));
                HeroAdminAcciones.ColumnDefinitions.Add(
                    new ColumnDefinition(GridLength.Star));

                Grid.SetRow(EliminadasButton, 0);
                Grid.SetColumn(EliminadasButton, 0);
                Grid.SetColumnSpan(EliminadasButton, 1);

                Grid.SetRow(NuevaPublicacionButton, 1);
                Grid.SetColumn(NuevaPublicacionButton, 0);
                Grid.SetColumnSpan(NuevaPublicacionButton, 1);
            }
            else
            {
                HeroAdminAcciones.RowDefinitions.Add(
                    new RowDefinition(GridLength.Auto));
                HeroAdminAcciones.ColumnDefinitions.Add(
                    new ColumnDefinition(
                        llenar
                            ? GridLength.Star
                            : GridLength.Auto));
                HeroAdminAcciones.ColumnDefinitions.Add(
                    new ColumnDefinition(
                        llenar
                            ? GridLength.Star
                            : GridLength.Auto));

                Grid.SetRow(EliminadasButton, 0);
                Grid.SetColumn(EliminadasButton, 0);
                Grid.SetColumnSpan(EliminadasButton, 1);

                Grid.SetRow(NuevaPublicacionButton, 0);
                Grid.SetColumn(NuevaPublicacionButton, 1);
                Grid.SetColumnSpan(NuevaPublicacionButton, 1);
            }

            EliminadasButton.MinimumWidthRequest = 0;
            NuevaPublicacionButton.MinimumWidthRequest = 0;
        }

        private void AplicarFiltros()
        {
            double ancho =
                PublicacionesCollection.Width > 0
                    ? PublicacionesCollection.Width
                    : Width;

            if (ancho <= 0)
                return;

            int nuevoModo =
                ancho < FiltrosUnaColumnaBreakpoint
                    ? 0
                    : ancho < FiltrosDosFilasBreakpoint
                        ? 1
                        : 2;

            if (modoFiltros == nuevoModo)
                return;

            modoFiltros = nuevoModo;
            FiltrosAdminGrid.RowDefinitions.Clear();
            FiltrosAdminGrid.ColumnDefinitions.Clear();

            if (nuevoModo == 0)
            {
                FiltrosAdminGrid.RowDefinitions.Add(
                    new RowDefinition(GridLength.Auto));
                FiltrosAdminGrid.RowDefinitions.Add(
                    new RowDefinition(GridLength.Auto));
                FiltrosAdminGrid.RowDefinitions.Add(
                    new RowDefinition(GridLength.Auto));
                FiltrosAdminGrid.ColumnDefinitions.Add(
                    new ColumnDefinition(GridLength.Star));
                FiltrosAdminGrid.ColumnDefinitions.Add(
                    new ColumnDefinition(GridLength.Star));

                Ubicar(
                    CategoriaFiltroBorder,
                    row: 0,
                    column: 0,
                    columnSpan: 2);

                Ubicar(
                    EstadoFiltroBorder,
                    row: 1,
                    column: 0,
                    columnSpan: 2);

                Ubicar(BuscarFiltroButton, 2, 0, 1);
                Ubicar(LimpiarFiltroButton, 2, 1, 1);
            }
            else if (nuevoModo == 1)
            {
                FiltrosAdminGrid.RowDefinitions.Add(
                    new RowDefinition(GridLength.Auto));
                FiltrosAdminGrid.RowDefinitions.Add(
                    new RowDefinition(GridLength.Auto));
                FiltrosAdminGrid.ColumnDefinitions.Add(
                    new ColumnDefinition(GridLength.Star));
                FiltrosAdminGrid.ColumnDefinitions.Add(
                    new ColumnDefinition(GridLength.Star));

                Ubicar(CategoriaFiltroBorder, 0, 0, 1);
                Ubicar(EstadoFiltroBorder, 0, 1, 1);
                Ubicar(BuscarFiltroButton, 1, 0, 1);
                Ubicar(LimpiarFiltroButton, 1, 1, 1);
            }
            else
            {
                FiltrosAdminGrid.RowDefinitions.Add(
                    new RowDefinition(GridLength.Auto));
                FiltrosAdminGrid.ColumnDefinitions.Add(
                    new ColumnDefinition(GridLength.Star));
                FiltrosAdminGrid.ColumnDefinitions.Add(
                    new ColumnDefinition(GridLength.Star));
                FiltrosAdminGrid.ColumnDefinitions.Add(
                    new ColumnDefinition(GridLength.Auto));
                FiltrosAdminGrid.ColumnDefinitions.Add(
                    new ColumnDefinition(GridLength.Auto));

                Ubicar(CategoriaFiltroBorder, 0, 0, 1);
                Ubicar(EstadoFiltroBorder, 0, 1, 1);
                Ubicar(BuscarFiltroButton, 0, 2, 1);
                Ubicar(LimpiarFiltroButton, 0, 3, 1);
            }

            BuscarFiltroButton.MinimumWidthRequest = 0;
            LimpiarFiltroButton.MinimumWidthRequest = 0;
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

        private void AplicarColumnas()
        {
            double ancho =
                PublicacionesCollection.Width > 0
                    ? PublicacionesCollection.Width
                    : Width;

            if (ancho <= 0)
                return;

            int nuevoSpan =
                viewModel.Publicaciones.Count == 0
                    ? 1
                    : ancho >= TresColumnasBreakpoint
                        ? 3
                        : ancho >= DosColumnasBreakpoint
                            ? 2
                            : 1;

            if (spanActual == nuevoSpan &&
                PublicacionesGrid.Span == nuevoSpan)
            {
                return;
            }

            spanActual = nuevoSpan;
            PublicacionesGrid.Span = nuevoSpan;
        }

        private void AplicarPaginacion()
        {
            double ancho =
                PublicacionesCollection.Width > 0
                    ? PublicacionesCollection.Width
                    : Width;

            if (ancho <= 0)
                return;

            double margenHorizontal =
                ancho < 480
                    ? 8
                    : ancho < 800
                        ? 20
                        : 32;

            PaginacionAdmin.WidthRequest =
                Math.Min(
                    560,
                    Math.Max(0, ancho - margenHorizontal));
        }

        private void PaginacionAdmin_Pressed(
            object? sender,
            EventArgs e)
        {
            paginaAntesCambio =
                viewModel.PaginaActual;
        }

        private async void PaginacionAdmin_Clicked(
            object? sender,
            EventArgs e)
        {
            int paginaOrigen =
                paginaAntesCambio > 0
                    ? paginaAntesCambio
                    : viewModel.PaginaActual;

            bool operacionDetectada = false;

            for (int intento = 0; intento < 240; intento++)
            {
                if (viewModel.IsBusy ||
                    viewModel.PaginaActual != paginaOrigen)
                {
                    operacionDetectada = true;
                }

                if (operacionDetectada &&
                    !viewModel.IsBusy)
                {
                    if (viewModel.PaginaActual != paginaOrigen &&
                        viewModel.Publicaciones.Count > 0)
                    {
                        await DesplazarPublicacionesAlInicioAsync();
                    }

                    paginaAntesCambio = -1;
                    return;
                }

                await Task.Delay(50);
            }

            paginaAntesCambio = -1;
        }

        private async Task DesplazarPublicacionesAlInicioAsync()
        {
            if (PublicacionesCollection == null ||
                viewModel.Publicaciones.Count == 0)
            {
                return;
            }

            await Task.Delay(60);

            PublicacionesCollection.ScrollTo(
                0,
                position: ScrollToPosition.Start,
                animate: false);
        }

        private async void AbrirEliminadas_Clicked(
            object? sender,
            EventArgs e)
        {
            if (!viewModel.CanAdministrar || viewModel.IsBusy)
                return;

            try
            {
                await PublicacionesEliminadasLauncher.AbrirAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    $"No fue posible abrir las publicaciones eliminadas: {ex}");

                try
                {
                    await GlobalService.MostrarErrorAsync(
                        "No fue posible abrir las publicaciones eliminadas.");
                }
                catch
                {
                    // Nunca se propaga una excepción desde este async void.
                }
            }
        }
    }
}
