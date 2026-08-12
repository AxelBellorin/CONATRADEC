using CONATRADEC.Services;
using CONATRADEC.ViewModels;
using System.Windows.Input;
using Microsoft.Maui.Devices;

namespace CONATRADEC.Views
{
    public partial class terrenoPage : ContentPage
    {
        private readonly TerrenoViewModel viewModel = new();

        private bool accionesCompactas;
        private bool navegandoAConfiguracion;

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

            AjustarAccionesBusqueda(Width);

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

            AjustarAccionesBusqueda(width);
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

            /*
             * Respaldo por ancho para los casos en que WinUI todavía no haya
             * terminado de aplicar el ResponsiveGridItemsLayoutBehavior.
             */
            double width =
                TerrenosCollectionView?.Width ?? 0;

            if (width >= 1380)
                return 3;

            if (width >= 760)
                return 2;

            return 1;
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
        /// Si WinUI no vuelve a disparar correctamente el evento de scroll,
        /// el usuario siempre puede solicitar la siguiente página sin perder
        /// filtros, ordenamiento ni posición actual.
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
                    MinimumWidthRequest = 190,
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

            footer.Children.Insert(
                0,
                cargarMasRespaldoButton);
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
        /// En teléfono, Buscar ocupa toda la primera fila y las acciones
        /// Filtros/Limpiar comparten una segunda fila.
        /// </summary>
        private void AjustarAccionesBusqueda(double width)
        {
            if (width <= 0 ||
                BusquedaAccionesGrid == null)
            {
                return;
            }

            bool compacto = width < 600;

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
    }
}
