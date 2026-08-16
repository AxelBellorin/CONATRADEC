using CONATRADEC.Services;
using CONATRADEC.ViewModels;
using System.Windows.Input;

namespace CONATRADEC.Views
{
    public partial class terrenoPage : ContentPage
    {
        /*
         * Cada tarjeta necesita un ancho útil suficiente para que métricas,
         * textos y acciones no se recorten en WinUI reducido ni en tablet.
         */
        private const double AnchoMinimoTarjeta = 430;
        private const double EspaciadoTarjetas = 12;

        private readonly TerrenoViewModel viewModel = new();

        private bool accionesCompactas;
        private bool navegacionShellSuscrita;
        private bool salidaExternaPendiente;
        private int modoFiltrosFecha = -1;

        public terrenoPage()
        {
            InitializeComponent();

            BindingContext = viewModel;

            /*
             * Mientras no existan registros se conserva una sola columna para
             * que EmptyView use todo el ancho. Con datos, las columnas se
             * calculan por el ancho útil real de cada tarjeta.
             */
            viewModel.List.CollectionChanged +=
                (_, _) => AjustarSpanTerrenos();

            TerrenosCollectionView.SizeChanged +=
                (_, _) => AjustarDiseno(Width);

            Shell.Current.FlyoutBehavior =
                FlyoutBehavior.Disabled;
        }

        /*
         * El encabezado utiliza x:Reference sobre la Page. Se expone el comando
         * real del ViewModel para conservar la finalización correcta de la
         * visita al regresar a Configuración.
         */
        public ICommand RegresarConfiguracionCommand =>
            viewModel.RegresarConfiguracionCommand;

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            salidaExternaPendiente = false;
            SuscribirNavegacionShell();

            viewModel.ActualizarPermisos();

            ContenidoPrincipal.IsVisible =
                viewModel.CanView;

            ContenidoSinPermiso.IsVisible =
                !viewModel.CanView;

            if (!viewModel.CanView)
                return;

            AjustarDiseno(Width);

            /*
             * Cada entrada real a Terrenos inicia con datos frescos. Regresar
             * desde Crear/Editar/Ver pertenece a la misma visita y reutiliza
             * únicamente los catálogos ya consultados.
             */
            bool nuevaVisita =
                TerrenoVisitaService.AsegurarVisita();

            if (nuevaVisita)
            {
                await viewModel.IniciarNuevaVisitaAsync();
                return;
            }

            /*
             * Una operación interna puede marcar que el listado cambió.
             * En ese caso se renueva solamente la página actualmente visible.
             */
            if (TerrenoVisitaService.ConsumirRecargaListado())
            {
                await viewModel.RecargarPaginaActualAsync();
                return;
            }

            if (!viewModel.TienePaginaCargada)
                await viewModel.InicializarAsync();
        }

        protected override void OnDisappearing()
        {
            /*
             * Si se abandona Terrenos hacia otro módulo mediante la navegación
             * inferior/lateral, se finaliza la visita. El formulario de Terreno
             * continúa siendo parte de la misma visita para reutilizar catálogos.
             */
            if (salidaExternaPendiente)
            {
                TerrenoVisitaService.FinalizarVisita();
                salidaExternaPendiente = false;
            }

            DesuscribirNavegacionShell();
            viewModel.CancelarCarga();

            base.OnDisappearing();
        }

        protected override void OnSizeAllocated(
            double width,
            double height)
        {
            base.OnSizeAllocated(width, height);

            AjustarDiseno(width);
        }

        private void AjustarDiseno(double width)
        {
            if (width <= 0)
                return;

            double anchoUtil =
                ObtenerAnchoUtilContenido(width);

            AjustarPaddingContenido(width);
            AjustarAccionesBusqueda(anchoUtil);
            AjustarFiltrosAvanzados(anchoUtil);
            AjustarSpanTerrenos();
            AjustarPaginacion(width);
        }

        private void AjustarPaddingContenido(double width)
        {
            if (ContenidoPrincipal != null)
            {
                ContenidoPrincipal.Padding =
                    width < 600
                        ? new Thickness(12, 12, 12, 20)
                        : width < 900
                            ? new Thickness(18, 16, 18, 24)
                            : new Thickness(24, 20, 24, 28);
            }

            if (ContenidoSinPermiso != null)
            {
                double padding =
                    width < 600
                        ? 16
                        : width < 900
                            ? 24
                            : 28;

                ContenidoSinPermiso.Padding =
                    new Thickness(padding);
            }
        }

        private double ObtenerAnchoUtilContenido(double width)
        {
            if (TerrenosCollectionView?.Width > 0)
                return TerrenosCollectionView.Width;

            double paddingHorizontal =
                width < 600
                    ? 24
                    : width < 900
                        ? 36
                        : 48;

            return Math.Max(
                0,
                width - paddingHorizontal);
        }

        private void AjustarPaginacion(double width)
        {
            if (PaginacionTerrenos == null)
                return;

            double anchoDisponible =
                ObtenerAnchoUtilContenido(width);

            PaginacionTerrenos.WidthRequest =
                Math.Min(
                    560,
                    Math.Max(0, anchoDisponible));
        }

        /// <summary>
        /// Mantiene una sola columna mientras no existen terrenos para que
        /// EmptyView ocupe el ancho completo. Cuando hay datos se usa el ancho
        /// mínimo real de tarjeta en lugar de breakpoints por dispositivo.
        /// </summary>
        private void AjustarSpanTerrenos()
        {
            if (TerrenosCollectionView?.ItemsLayout is not
                GridItemsLayout gridLayout)
            {
                return;
            }

            double width =
                TerrenosCollectionView.Width > 0
                    ? TerrenosCollectionView.Width
                    : ObtenerAnchoUtilContenido(Width);

            if (width <= 0)
                return;

            int span =
                viewModel.List.Count == 0
                    ? 1
                    : CalcularSpanTerrenos(width);

            if (gridLayout.Span != span)
                gridLayout.Span = span;
        }

        private static int CalcularSpanTerrenos(double width)
        {
            if (width <= 0)
                return 1;

            double requeridoTres =
                (AnchoMinimoTarjeta * 3) +
                (EspaciadoTarjetas * 2);

            if (width >= requeridoTres)
                return 3;

            double requeridoDos =
                (AnchoMinimoTarjeta * 2) +
                EspaciadoTarjetas;

            return width >= requeridoDos
                ? 2
                : 1;
        }

        /// <summary>
        /// Después de cambiar de página espera a que termine la consulta y
        /// coloca el primer terreno de la nueva página al inicio visible.
        /// El Command del ViewModel continúa siendo el único responsable de
        /// solicitar la página al servidor.
        /// </summary>
        private async void PaginacionTerrenos_Clicked(
            object? sender,
            EventArgs e)
        {
            int paginaAnterior =
                viewModel.PaginaActual;

            bool operacionDetectada = false;

            for (int intento = 0; intento < 240; intento++)
            {
                if (viewModel.IsBusy ||
                    viewModel.PaginaActual != paginaAnterior)
                {
                    operacionDetectada = true;
                }

                if (operacionDetectada &&
                    !viewModel.IsBusy)
                {
                    if (viewModel.PaginaActual != paginaAnterior &&
                        viewModel.List.Count > 0)
                    {
                        await DesplazarTerrenosAlInicioAsync();
                    }

                    return;
                }

                await Task.Delay(50);
            }
        }

        private async Task DesplazarTerrenosAlInicioAsync()
        {
            if (TerrenosCollectionView == null ||
                viewModel.List.Count == 0)
            {
                return;
            }

            // Permite que CollectionView termine de materializar la nueva página.
            await Task.Delay(60);

            TerrenosCollectionView.ScrollTo(
                0,
                position: ScrollToPosition.Start,
                animate: false);
        }

        /// <summary>
        /// En ancho estrecho, Buscar ocupa toda la primera fila y las acciones
        /// Filtros/Limpiar comparten una segunda fila.
        /// </summary>
        private void AjustarAccionesBusqueda(double width)
        {
            if (width <= 0 ||
                BusquedaAccionesGrid == null)
            {
                return;
            }

            bool compacto = width < 640;

            if (accionesCompactas == compacto)
                return;

            accionesCompactas = compacto;

            BusquedaAccionesGrid
                .ColumnDefinitions
                .Clear();

            BusquedaAccionesGrid
                .RowDefinitions
                .Clear();

            if (BusquedaAccionesGrid.Children.Count < 3)
                return;

            View buscar =
                (View)BusquedaAccionesGrid.Children[0];

            View filtros =
                (View)BusquedaAccionesGrid.Children[1];

            View limpiar =
                (View)BusquedaAccionesGrid.Children[2];

            if (compacto)
            {
                BusquedaAccionesGrid.ColumnDefinitions.Add(
                    new ColumnDefinition(GridLength.Star));

                BusquedaAccionesGrid.ColumnDefinitions.Add(
                    new ColumnDefinition(GridLength.Star));

                BusquedaAccionesGrid.RowDefinitions.Add(
                    new RowDefinition(GridLength.Auto));

                BusquedaAccionesGrid.RowDefinitions.Add(
                    new RowDefinition(GridLength.Auto));

                Grid.SetRow(buscar, 0);
                Grid.SetColumn(buscar, 0);
                Grid.SetColumnSpan(buscar, 2);

                Grid.SetRow(filtros, 1);
                Grid.SetColumn(filtros, 0);
                Grid.SetColumnSpan(filtros, 1);

                Grid.SetRow(limpiar, 1);
                Grid.SetColumn(limpiar, 1);
                Grid.SetColumnSpan(limpiar, 1);

                return;
            }

            BusquedaAccionesGrid.ColumnDefinitions.Add(
                new ColumnDefinition(GridLength.Star));

            BusquedaAccionesGrid.ColumnDefinitions.Add(
                new ColumnDefinition(GridLength.Auto));

            BusquedaAccionesGrid.ColumnDefinitions.Add(
                new ColumnDefinition(GridLength.Auto));

            BusquedaAccionesGrid.RowDefinitions.Add(
                new RowDefinition(GridLength.Auto));

            Grid.SetRow(buscar, 0);
            Grid.SetColumn(buscar, 0);
            Grid.SetColumnSpan(buscar, 1);

            Grid.SetRow(filtros, 0);
            Grid.SetColumn(filtros, 1);
            Grid.SetColumnSpan(filtros, 1);

            Grid.SetRow(limpiar, 0);
            Grid.SetColumn(limpiar, 2);
            Grid.SetColumnSpan(limpiar, 1);
        }

        /// <summary>
        /// Reorganiza todos los filtros usando el ancho útil real de la página.
        /// Esto evita que OnIdiom Desktop mantenga varias columnas en una
        /// ventana WinUI pequeña y cubre también tablet vertical/dividida.
        /// </summary>
        private void AjustarFiltrosAvanzados(double width)
        {
            if (width <= 0)
                return;

            int columnasTexto =
                width >= 700
                    ? 2
                    : 1;

            ConfigurarGridUniforme(
                FiltrosTextoGrid,
                new View[]
                {
                    FiltroCodigoSection,
                    FiltroPropietarioSection,
                    FiltroIdentificacionSection,
                    FiltroDireccionSection
                },
                columnasTexto);

            int columnasUbicacion =
                width >= 900
                    ? 3
                    : width >= 620
                        ? 2
                        : 1;

            ConfigurarGridUniforme(
                FiltrosUbicacionGrid,
                new View[]
                {
                    FiltroPaisBorder,
                    FiltroDepartamentoBorder,
                    FiltroMunicipioBorder
                },
                columnasUbicacion);

            AjustarGridFecha(width);

            int columnasExtension =
                width >= 900
                    ? 3
                    : width >= 620
                        ? 2
                        : 1;

            ConfigurarGridUniforme(
                FiltrosExtensionGrid,
                new View[]
                {
                    FiltroExtensionMinimaSection,
                    FiltroExtensionMaximaSection,
                    FiltroOrdenSection
                },
                columnasExtension);

            if (AplicarFiltrosButton != null)
            {
                AplicarFiltrosButton.HorizontalOptions =
                    width < 620
                        ? LayoutOptions.Fill
                        : LayoutOptions.End;
            }
        }

        private void AjustarGridFecha(double width)
        {
            if (FiltrosFechaGrid == null)
                return;

            int nuevoModo =
                width >= 900
                    ? 2
                    : width < 430
                        ? 0
                        : 1;

            if (modoFiltrosFecha == nuevoModo)
                return;

            modoFiltrosFecha = nuevoModo;

            FiltrosFechaGrid.ColumnDefinitions.Clear();
            FiltrosFechaGrid.RowDefinitions.Clear();

            RestablecerPosicion(FiltroFechaSwitch);
            RestablecerPosicion(FiltroFechaLabel);
            RestablecerPosicion(FiltroFechaDesdeBorder);
            RestablecerPosicion(FiltroFechaHastaBorder);

            if (nuevoModo == 2)
            {
                FiltrosFechaGrid.ColumnDefinitions.Add(
                    new ColumnDefinition(GridLength.Auto));
                FiltrosFechaGrid.ColumnDefinitions.Add(
                    new ColumnDefinition(GridLength.Star));
                FiltrosFechaGrid.ColumnDefinitions.Add(
                    new ColumnDefinition(GridLength.Star));
                FiltrosFechaGrid.ColumnDefinitions.Add(
                    new ColumnDefinition(GridLength.Star));
                FiltrosFechaGrid.RowDefinitions.Add(
                    new RowDefinition(GridLength.Auto));

                Grid.SetColumn(FiltroFechaSwitch, 0);
                Grid.SetColumn(FiltroFechaLabel, 1);
                Grid.SetColumn(FiltroFechaDesdeBorder, 2);
                Grid.SetColumn(FiltroFechaHastaBorder, 3);
                return;
            }

            FiltrosFechaGrid.ColumnDefinitions.Add(
                new ColumnDefinition(GridLength.Star));
            FiltrosFechaGrid.ColumnDefinitions.Add(
                new ColumnDefinition(GridLength.Star));
            FiltrosFechaGrid.RowDefinitions.Add(
                new RowDefinition(GridLength.Auto));
            FiltrosFechaGrid.RowDefinitions.Add(
                new RowDefinition(GridLength.Auto));

            Grid.SetRow(FiltroFechaSwitch, 0);
            Grid.SetColumn(FiltroFechaSwitch, 0);

            Grid.SetRow(FiltroFechaLabel, 0);
            Grid.SetColumn(FiltroFechaLabel, 1);

            Grid.SetRow(FiltroFechaDesdeBorder, 1);
            Grid.SetColumn(FiltroFechaDesdeBorder, 0);

            Grid.SetRow(FiltroFechaHastaBorder, 1);
            Grid.SetColumn(FiltroFechaHastaBorder, 1);

            if (nuevoModo == 0)
            {
                FiltrosFechaGrid.RowDefinitions.Add(
                    new RowDefinition(GridLength.Auto));

                Grid.SetColumn(FiltroFechaDesdeBorder, 0);
                Grid.SetColumnSpan(FiltroFechaDesdeBorder, 2);

                Grid.SetRow(FiltroFechaHastaBorder, 2);
                Grid.SetColumn(FiltroFechaHastaBorder, 0);
                Grid.SetColumnSpan(FiltroFechaHastaBorder, 2);
            }
        }

        private static void ConfigurarGridUniforme(
            Grid grid,
            IReadOnlyList<View> elementos,
            int columnas)
        {
            if (grid == null || elementos.Count == 0)
                return;

            columnas = Math.Clamp(
                columnas,
                1,
                elementos.Count);

            int filas =
                (int)Math.Ceiling(
                    elementos.Count / (double)columnas);

            bool estructuraCorrecta =
                grid.ColumnDefinitions.Count == columnas &&
                grid.RowDefinitions.Count == filas;

            if (estructuraCorrecta)
                return;

            grid.ColumnDefinitions.Clear();
            grid.RowDefinitions.Clear();

            for (int columna = 0; columna < columnas; columna++)
            {
                grid.ColumnDefinitions.Add(
                    new ColumnDefinition(GridLength.Star));
            }

            for (int fila = 0; fila < filas; fila++)
            {
                grid.RowDefinitions.Add(
                    new RowDefinition(GridLength.Auto));
            }

            for (int indice = 0; indice < elementos.Count; indice++)
            {
                View elemento = elementos[indice];
                RestablecerPosicion(elemento);

                Grid.SetRow(
                    elemento,
                    indice / columnas);

                Grid.SetColumn(
                    elemento,
                    indice % columnas);
            }

            /*
             * Cuando quedan tres controles sobre dos columnas, el último ocupa
             * el ancho completo para no dejar media fila vacía innecesariamente.
             */
            if (columnas == 2 &&
                elementos.Count == 3)
            {
                Grid.SetColumn(
                    elementos[2],
                    0);
                Grid.SetColumnSpan(
                    elementos[2],
                    2);
            }
        }

        private static void RestablecerPosicion(View view)
        {
            Grid.SetRow(view, 0);
            Grid.SetColumn(view, 0);
            Grid.SetRowSpan(view, 1);
            Grid.SetColumnSpan(view, 1);
        }

        private void TerrenoMetricasGrid_SizeChanged(
            object? sender,
            EventArgs e)
        {
            if (sender is not Grid grid ||
                grid.Width <= 0)
            {
                return;
            }

            List<View> metricas =
                grid.Children
                    .OfType<View>()
                    .ToList();

            if (metricas.Count == 0)
                return;

            int columnas =
                grid.Width >= 520
                    ? 3
                    : grid.Width >= 340
                        ? 2
                        : 1;

            ConfigurarGridUniforme(
                grid,
                metricas,
                columnas);
        }

        private void TerrenoAccionesGrid_SizeChanged(
            object? sender,
            EventArgs e)
        {
            if (sender is not Grid grid ||
                grid.Width <= 0)
            {
                return;
            }

            List<View> acciones =
                grid.Children
                    .OfType<Button>()
                    .Where(button => button.IsVisible)
                    .Cast<View>()
                    .ToList();

            if (acciones.Count == 0)
                return;

            int columnas =
                grid.Width < 360 &&
                acciones.Count >= 3
                    ? 2
                    : acciones.Count;

            ConfigurarGridUniforme(
                grid,
                acciones,
                columnas);

            if (grid.Width < 360 &&
                acciones.Count == 3)
            {
                Grid.SetRow(acciones[0], 0);
                Grid.SetColumn(acciones[0], 0);
                Grid.SetColumnSpan(acciones[0], 2);

                Grid.SetRow(acciones[1], 1);
                Grid.SetColumn(acciones[1], 0);
                Grid.SetColumnSpan(acciones[1], 1);

                Grid.SetRow(acciones[2], 1);
                Grid.SetColumn(acciones[2], 1);
                Grid.SetColumnSpan(acciones[2], 1);
            }
        }

        private void SuscribirNavegacionShell()
        {
            if (navegacionShellSuscrita ||
                Shell.Current == null)
            {
                return;
            }

            Shell.Current.Navigating +=
                Shell_Navigating;

            navegacionShellSuscrita = true;
        }

        private void DesuscribirNavegacionShell()
        {
            if (!navegacionShellSuscrita ||
                Shell.Current == null)
            {
                return;
            }

            Shell.Current.Navigating -=
                Shell_Navigating;

            navegacionShellSuscrita = false;
        }

        private void Shell_Navigating(
            object? sender,
            ShellNavigatingEventArgs e)
        {
            string rutaDestino =
                e.Target?
                    .Location?
                    .OriginalString ??
                string.Empty;

            if (string.IsNullOrWhiteSpace(rutaDestino))
                return;

            salidaExternaPendiente =
                !EsRutaInternaTerrenos(rutaDestino);
        }

        private static bool EsRutaInternaTerrenos(
            string ruta)
        {
            return
                ruta.Contains(
                    "TerrenoPage",
                    StringComparison.OrdinalIgnoreCase) ||
                ruta.Contains(
                    "TerrenoFormPage",
                    StringComparison.OrdinalIgnoreCase) ||
                ruta.Contains(
                    "MapaSeleccionPage",
                    StringComparison.OrdinalIgnoreCase) ||
                ruta.Contains(
                    "FotosTerrenoGaleriaPage",
                    StringComparison.OrdinalIgnoreCase);
        }
    }
}
