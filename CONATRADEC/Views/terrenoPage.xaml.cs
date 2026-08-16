using CONATRADEC.Services;
using CONATRADEC.ViewModels;
using Microsoft.Maui.Devices;
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
        private bool navegandoAConfiguracion;
        private int cantidadAntesCargaManual = -1;
        private int modoFiltrosFecha = -1;

        private Button? cargarMasRespaldoButton;

        public terrenoPage()
        {
            /*
             * El comando debe existir antes de InitializeComponent().
             *
             * El encabezado responsive utiliza x:Reference para enlazar
             * esta propiedad durante la carga del XAML. Si se asigna
             * después, el botón queda visible pero recibe Command = null.
             */
            RegresarConfiguracionCommand =
                new Command(
                    async () =>
                        await RegresarConfiguracionAsync(),
                    () => !navegandoAConfiguracion);

            InitializeComponent();

            BindingContext = viewModel;

            /*
             * Respaldo de carga incremental.
             *
             * RemainingItemsThreshold permanece activo desde el XAML, pero
             * WinUI puede reportar los índices visibles de forma diferente
             * cuando el CollectionView usa GridItemsLayout con 2 o 3 columnas.
             * Escuchamos Scrolled y calculamos el umbral tanto como índice de
             * elementos como índice aproximado de filas para que la siguiente
             * página se solicite de forma confiable en Windows, tablet y móvil.
             */
            /*
             * En Windows conservamos la precarga automática porque WinUI
             * responde bien con el respaldo adicional de Scrolled.
             *
             * En Android/Tablet se deshabilita el umbral automático. Cuando
             * el usuario permanece al final del CollectionView, Android puede
             * volver a disparar la carga incremental inmediatamente después
             * de agregar la página anterior. El botón del footer queda como
             * navegación controlada: un toque = una página.
             */
            if (DeviceInfo.Current.Platform == DevicePlatform.WinUI)
            {
                TerrenosCollectionView.Scrolled +=
                    TerrenosCollectionView_Scrolled;

                TerrenosCollectionView.RemainingItemsThreshold = 10;
            }
            else
            {
                TerrenosCollectionView.RemainingItemsThreshold = -1;
            }

            /*
             * Mientras no existan registros se conserva una sola columna para
             * que EmptyView use todo el ancho. Con datos, las columnas se
             * calculan por el ancho útil real de cada tarjeta.
             */
            viewModel.List.CollectionChanged +=
                (_, _) => AjustarSpanTerrenos();

            TerrenosCollectionView.SizeChanged +=
                (_, _) => AjustarDiseno(Width);

            ConfigurarBotonCargaManual();

            Shell.Current.FlyoutBehavior =
                FlyoutBehavior.Disabled;
        }

        public ICommand RegresarConfiguracionCommand { get; }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            navegandoAConfiguracion = false;
            ActualizarComandoRegresar();

            viewModel.ActualizarPermisos();

            ContenidoPrincipal.IsVisible =
                viewModel.CanView;

            ContenidoSinPermiso.IsVisible =
                !viewModel.CanView;

            if (!viewModel.CanView)
                return;

            AjustarDiseno(Width);

            await viewModel.InicializarAsync();
        }

        protected override void OnDisappearing()
        {
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

            AjustarPaddingContenido(width);
            AjustarAccionesBusqueda(ObtenerAnchoUtilContenido(width));
            AjustarFiltrosAvanzados(ObtenerAnchoUtilContenido(width));
            AjustarSpanTerrenos();
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

            return Math.Max(0, width - paddingHorizontal);
        }

        private void TerrenosCollectionView_Scrolled(
            object? sender,
            ItemsViewScrolledEventArgs e)
        {
            if (!viewModel.CanView ||
                viewModel.CargandoMas ||
                !viewModel.PuedeCargarMas ||
                viewModel.List.Count == 0 ||
                e.LastVisibleItemIndex < 0)
            {
                return;
            }

            /*
             * Solo se precarga mientras el usuario baja. Esto evita solicitudes
             * innecesarias durante reajustes de tamaño o al desplazarse hacia
             * arriba.
             */
            if (e.VerticalDelta <= 0)
                return;

            int span = ObtenerSpanTerrenos();
            int totalElementos = viewModel.List.Count;

            /*
             * En WinUI, LastVisibleItemIndex puede comportarse de forma
             * diferente cuando GridItemsLayout utiliza dos o tres columnas.
             * Por eso se evalúa tanto como índice de elemento como índice
             * aproximado de fila, usando un margen amplio de diez filas.
             */
            int margenElementos =
                Math.Max(
                    12,
                    span * 10);

            int umbralComoElementos =
                Math.Max(
                    0,
                    totalElementos - margenElementos);

            int totalFilas =
                (int)Math.Ceiling(
                    totalElementos / (double)span);

            int umbralComoFilas =
                Math.Max(
                    0,
                    totalFilas - 10);

            bool cercaDelFinal =
                e.LastVisibleItemIndex >= umbralComoElementos ||
                e.LastVisibleItemIndex >= umbralComoFilas;

            if (!cercaDelFinal)
                return;

            EjecutarCargaMasSiDisponible();
        }

        private int ObtenerSpanTerrenos()
        {
            if (TerrenosCollectionView?.ItemsLayout is
                GridItemsLayout gridLayout &&
                gridLayout.Span > 0)
            {
                return gridLayout.Span;
            }

            double width =
                TerrenosCollectionView?.Width ?? 0;

            return CalcularSpanTerrenos(width);
        }

        /// <summary>
        /// Mantiene una sola columna mientras no existen terrenos para que
        /// EmptyView ocupe el ancho completo. Cuando hay datos se usa el ancho
        /// mínimo real de tarjeta en lugar de breakpoints de tipo de dispositivo.
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

        private void EjecutarCargaMasSiDisponible()
        {
            if (!viewModel.CanView ||
                viewModel.CargandoMas ||
                !viewModel.PuedeCargarMas)
            {
                return;
            }

            if (viewModel.CargarMasCommand.CanExecute(null))
                viewModel.CargarMasCommand.Execute(null);
        }

        /// <summary>
        /// Agrega un respaldo manual en el footer del CollectionView.
        /// En Android/Tablet este botón representa el cambio controlado hacia
        /// la siguiente página de resultados.
        /// </summary>
        private void ConfigurarBotonCargaManual()
        {
            if (cargarMasRespaldoButton != null ||
                TerrenosCollectionView?.Footer is not
                    VerticalStackLayout footer)
            {
                return;
            }

            cargarMasRespaldoButton =
                new Button
                {
                    Text = "Cargar más terrenos",
                    FontFamily = "MontserratBold",
                    FontSize = 12,
                    HeightRequest = 46,
                    MinimumWidthRequest = 0,
                    Padding = new Thickness(18, 8),
                    CornerRadius = 12,
                    HorizontalOptions = LayoutOptions.Center,
                    BackgroundColor =
                        Color.FromArgb("#3B655B"),
                    TextColor = Colors.White
                };

            cargarMasRespaldoButton.SetBinding(
                IsVisibleProperty,
                nameof(TerrenoViewModel.PuedeCargarMas));

            cargarMasRespaldoButton.SetBinding(
                Button.CommandProperty,
                nameof(TerrenoViewModel.CargarMasCommand));

            cargarMasRespaldoButton.Pressed +=
                CargarMasRespaldoButton_Pressed;

            cargarMasRespaldoButton.Clicked +=
                CargarMasRespaldoButton_Clicked;

            footer.Children.Insert(
                0,
                cargarMasRespaldoButton);
        }

        private void CargarMasRespaldoButton_Pressed(
            object? sender,
            EventArgs e)
        {
            cantidadAntesCargaManual =
                viewModel.List.Count;
        }

        /// <summary>
        /// Después de cargar manualmente la siguiente página, coloca el primer
        /// registro nuevo al inicio visible. La precarga automática de WinUI no
        /// usa este evento y por tanto conserva un scroll continuo sin saltos.
        /// </summary>
        private async void CargarMasRespaldoButton_Clicked(
            object? sender,
            EventArgs e)
        {
            int primerIndiceNuevaPagina =
                cantidadAntesCargaManual >= 0
                    ? cantidadAntesCargaManual
                    : viewModel.List.Count;

            bool operacionDetectada = false;

            for (int intento = 0; intento < 240; intento++)
            {
                if (viewModel.CargandoMas ||
                    viewModel.List.Count > primerIndiceNuevaPagina)
                {
                    operacionDetectada = true;
                }

                if (operacionDetectada &&
                    !viewModel.CargandoMas)
                {
                    if (viewModel.List.Count > primerIndiceNuevaPagina)
                    {
                        await DesplazarTerrenosAIndiceAsync(
                            primerIndiceNuevaPagina);
                    }

                    cantidadAntesCargaManual = -1;
                    return;
                }

                await Task.Delay(50);
            }

            cantidadAntesCargaManual = -1;
        }

        private async Task DesplazarTerrenosAIndiceAsync(int indice)
        {
            if (TerrenosCollectionView == null ||
                indice < 0 ||
                indice >= viewModel.List.Count)
            {
                return;
            }

            // Da tiempo al CollectionView para materializar el nuevo bloque.
            await Task.Delay(60);

            TerrenosCollectionView.ScrollTo(
                indice,
                position: ScrollToPosition.Start,
                animate: false);
        }

        private async Task RegresarConfiguracionAsync()
        {
            if (navegandoAConfiguracion)
                return;

            try
            {
                navegandoAConfiguracion = true;
                ActualizarComandoRegresar();

                await Shell.Current.GoToAsync(
                    AppRoutes.Configuracion,
                    true);
            }
            catch (Exception ex)
            {
                navegandoAConfiguracion = false;
                ActualizarComandoRegresar();

                await DisplayAlert(
                    "Navegación",
                    $"No fue posible regresar a Configuración. " +
                    $"{ex.Message}",
                    "Aceptar");
            }
        }

        private void ActualizarComandoRegresar()
        {
            if (RegresarConfiguracionCommand is Command command)
                command.ChangeCanExecute();
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
    }
}
