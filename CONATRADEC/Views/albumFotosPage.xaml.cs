using CONATRADEC.Services;
using CONATRADEC.ViewModels;
using Microsoft.Maui.Devices;
using System.Linq;

namespace CONATRADEC.Views
{
    public partial class albumFotosPage : ContentPage
    {
        private readonly AlbumFotosViewModel viewModel = new();

        private Grid? encabezadoGrid;
        private VerticalStackLayout? encabezadoTexto;
        private Button? botonNuevaCategoria;
        private Button? botonNuevoRegistro;

        private Grid? busquedaGrid;
        private SearchBar? barraBusqueda;
        private Button? botonLimpiar;
        private HorizontalStackLayout? opcionesBusqueda;

        private Grid? capitulosWindows;
        private CollectionView? capitulosCompactos;

        private Grid? tituloGaleriaGrid;
        private VerticalStackLayout? tituloGaleriaTexto;
        private Label? totalRegistrosLabel;

        private bool controlesResponsivosLocalizados;
        private bool aplicandoDiseno;
        private int ultimoSpan = -1;
        private int ultimoModo = -1;

        public albumFotosPage()
        {
            InitializeComponent();

            Shell.Current.FlyoutBehavior =
                FlyoutBehavior.Disabled;

            BindingContext = viewModel;

            AlbumCollectionView.Loaded +=
                OnAlbumCollectionViewLoaded;
            AlbumCollectionView.SizeChanged +=
                OnAlbumCollectionViewSizeChanged;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            AplicarDisenoResponsivo(
                AlbumCollectionView.Width);

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
        }

        private void OnAlbumCollectionViewLoaded(
            object? sender,
            EventArgs e)
        {
            Dispatcher.Dispatch(() =>
                AplicarDisenoResponsivo(
                    AlbumCollectionView.Width));
        }

        private void OnAlbumCollectionViewSizeChanged(
            object? sender,
            EventArgs e)
        {
            if (aplicandoDiseno)
                return;

            Dispatcher.Dispatch(() =>
                AplicarDisenoResponsivo(
                    AlbumCollectionView.Width));
        }

        private void AplicarDisenoResponsivo(double anchoDisponible)
        {
            if (aplicandoDiseno ||
                double.IsNaN(anchoDisponible) ||
                anchoDisponible <= 0)
            {
                return;
            }

            aplicandoDiseno = true;

            try
            {
                LocalizarControlesResponsivos();

                bool esWindows =
                    DeviceInfo.Platform == DevicePlatform.WinUI;

                // Se usa el ancho real del área de contenido, no el ancho
                // completo de la ventana. Así se descuenta automáticamente
                // el espacio ocupado por el menú lateral.
                int modo = anchoDisponible switch
                {
                    < 520 => 0, // compacto
                    < 820 => 1, // mediano
                    _ => 2      // amplio
                };

                int span = anchoDisponible switch
                {
                    < 620 => 1,
                    < 1080 => 2,
                    _ => 3
                };

                AplicarColumnasGaleria(span);
                AplicarMargenGaleria(modo);

                if (modo != ultimoModo)
                {
                    AplicarEncabezado(modo);
                    AplicarBuscador(modo);
                    AplicarTituloGaleria(modo);
                    ultimoModo = modo;
                }

                bool usarCapitulosCompactos =
                    !esWindows || anchoDisponible < 760;

                if (capitulosWindows != null)
                {
                    capitulosWindows.IsVisible =
                        esWindows && !usarCapitulosCompactos;
                }

                if (capitulosCompactos != null)
                {
                    capitulosCompactos.IsVisible =
                        usarCapitulosCompactos;
                }
            }
            finally
            {
                aplicandoDiseno = false;
            }
        }

        private void LocalizarControlesResponsivos()
        {
            if (controlesResponsivosLocalizados)
                return;

            if (AlbumCollectionView.Header
                is not VerticalStackLayout header ||
                header.Children.Count < 4)
            {
                return;
            }

            encabezadoGrid =
                header.Children[0] as Grid;

            if (encabezadoGrid != null)
            {
                encabezadoTexto =
                    encabezadoGrid.Children
                        .OfType<VerticalStackLayout>()
                        .FirstOrDefault();

                var botones = encabezadoGrid.Children
                    .OfType<Button>()
                    .ToList();

                botonNuevaCategoria = botones
                    .FirstOrDefault(x =>
                        string.Equals(
                            x.Text,
                            "Nueva categoría",
                            StringComparison.OrdinalIgnoreCase));

                botonNuevoRegistro = botones
                    .FirstOrDefault(x =>
                        string.Equals(
                            x.Text,
                            "Nuevo registro",
                            StringComparison.OrdinalIgnoreCase));
            }

            if (header.Children[1]
                is Border bordeBusqueda &&
                bordeBusqueda.Content is Grid gridBusqueda)
            {
                busquedaGrid = gridBusqueda;
                barraBusqueda = gridBusqueda.Children
                    .OfType<SearchBar>()
                    .FirstOrDefault();
                botonLimpiar = gridBusqueda.Children
                    .OfType<Button>()
                    .FirstOrDefault();
                opcionesBusqueda = gridBusqueda.Children
                    .OfType<HorizontalStackLayout>()
                    .FirstOrDefault();
            }

            if (header.Children[2]
                is VerticalStackLayout seccionCapitulos &&
                seccionCapitulos.Children.Count >= 3)
            {
                capitulosWindows =
                    seccionCapitulos.Children[1] as Grid;
                capitulosCompactos =
                    seccionCapitulos.Children[2]
                    as CollectionView;
            }

            tituloGaleriaGrid =
                header.Children[3] as Grid;

            if (tituloGaleriaGrid != null)
            {
                tituloGaleriaTexto =
                    tituloGaleriaGrid.Children
                        .OfType<VerticalStackLayout>()
                        .FirstOrDefault();

                totalRegistrosLabel =
                    tituloGaleriaGrid.Children
                        .OfType<Label>()
                        .FirstOrDefault();
            }

            controlesResponsivosLocalizados =
                encabezadoGrid != null &&
                busquedaGrid != null &&
                tituloGaleriaGrid != null;
        }

        private void AplicarColumnasGaleria(int span)
        {
            if (span == ultimoSpan)
                return;

            if (AlbumCollectionView.ItemsLayout
                is GridItemsLayout layout)
            {
                layout.Span = span;
                layout.HorizontalItemSpacing =
                    span == 1 ? 0 : 14;
                layout.VerticalItemSpacing = 14;
                ultimoSpan = span;
            }
        }

        private void AplicarMargenGaleria(int modo)
        {
            AlbumCollectionView.Margin = modo switch
            {
                0 => new Thickness(12, 14, 12, 18),
                1 => new Thickness(20, 18, 20, 22),
                _ => new Thickness(30, 24, 30, 28)
            };
        }

        private void AplicarEncabezado(int modo)
        {
            if (encabezadoGrid == null ||
                encabezadoTexto == null ||
                botonNuevaCategoria == null ||
                botonNuevoRegistro == null)
            {
                return;
            }

            encabezadoGrid.ColumnDefinitions.Clear();
            encabezadoGrid.RowDefinitions.Clear();

            if (modo == 2)
            {
                encabezadoGrid.ColumnDefinitions.Add(
                    new ColumnDefinition(GridLength.Star));
                encabezadoGrid.ColumnDefinitions.Add(
                    new ColumnDefinition(GridLength.Auto));
                encabezadoGrid.ColumnDefinitions.Add(
                    new ColumnDefinition(GridLength.Auto));
                encabezadoGrid.RowDefinitions.Add(
                    new RowDefinition(GridLength.Auto));

                Posicionar(encabezadoTexto, 0, 0, 1);
                Posicionar(botonNuevaCategoria, 0, 1, 1);
                Posicionar(botonNuevoRegistro, 0, 2, 1);

                botonNuevaCategoria.HorizontalOptions =
                    LayoutOptions.End;
                botonNuevoRegistro.HorizontalOptions =
                    LayoutOptions.End;
                return;
            }

            if (modo == 1)
            {
                encabezadoGrid.ColumnDefinitions.Add(
                    new ColumnDefinition(GridLength.Star));
                encabezadoGrid.ColumnDefinitions.Add(
                    new ColumnDefinition(GridLength.Star));
                encabezadoGrid.RowDefinitions.Add(
                    new RowDefinition(GridLength.Auto));
                encabezadoGrid.RowDefinitions.Add(
                    new RowDefinition(GridLength.Auto));

                Posicionar(encabezadoTexto, 0, 0, 2);
                Posicionar(botonNuevaCategoria, 1, 0, 1);
                Posicionar(botonNuevoRegistro, 1, 1, 1);

                botonNuevaCategoria.HorizontalOptions =
                    LayoutOptions.Fill;
                botonNuevoRegistro.HorizontalOptions =
                    LayoutOptions.Fill;
                return;
            }

            encabezadoGrid.ColumnDefinitions.Add(
                new ColumnDefinition(GridLength.Star));
            encabezadoGrid.RowDefinitions.Add(
                new RowDefinition(GridLength.Auto));
            encabezadoGrid.RowDefinitions.Add(
                new RowDefinition(GridLength.Auto));
            encabezadoGrid.RowDefinitions.Add(
                new RowDefinition(GridLength.Auto));

            Posicionar(encabezadoTexto, 0, 0, 1);
            Posicionar(botonNuevaCategoria, 1, 0, 1);
            Posicionar(botonNuevoRegistro, 2, 0, 1);

            botonNuevaCategoria.HorizontalOptions =
                LayoutOptions.Fill;
            botonNuevoRegistro.HorizontalOptions =
                LayoutOptions.Fill;
        }

        private void AplicarBuscador(int modo)
        {
            if (busquedaGrid == null ||
                barraBusqueda == null ||
                botonLimpiar == null ||
                opcionesBusqueda == null)
            {
                return;
            }

            busquedaGrid.ColumnDefinitions.Clear();
            busquedaGrid.RowDefinitions.Clear();

            if (modo > 0)
            {
                busquedaGrid.ColumnDefinitions.Add(
                    new ColumnDefinition(GridLength.Star));
                busquedaGrid.ColumnDefinitions.Add(
                    new ColumnDefinition(GridLength.Auto));
                busquedaGrid.RowDefinitions.Add(
                    new RowDefinition(GridLength.Auto));
                busquedaGrid.RowDefinitions.Add(
                    new RowDefinition(GridLength.Auto));

                Posicionar(barraBusqueda, 0, 0, 1);
                Posicionar(botonLimpiar, 0, 1, 1);
                Posicionar(opcionesBusqueda, 1, 0, 2);

                botonLimpiar.HorizontalOptions =
                    LayoutOptions.End;
                return;
            }

            busquedaGrid.ColumnDefinitions.Add(
                new ColumnDefinition(GridLength.Star));
            busquedaGrid.RowDefinitions.Add(
                new RowDefinition(GridLength.Auto));
            busquedaGrid.RowDefinitions.Add(
                new RowDefinition(GridLength.Auto));
            busquedaGrid.RowDefinitions.Add(
                new RowDefinition(GridLength.Auto));

            Posicionar(barraBusqueda, 0, 0, 1);
            Posicionar(botonLimpiar, 1, 0, 1);
            Posicionar(opcionesBusqueda, 2, 0, 1);

            botonLimpiar.HorizontalOptions =
                LayoutOptions.Fill;
        }

        private void AplicarTituloGaleria(int modo)
        {
            if (tituloGaleriaGrid == null ||
                tituloGaleriaTexto == null ||
                totalRegistrosLabel == null)
            {
                return;
            }

            tituloGaleriaGrid.ColumnDefinitions.Clear();
            tituloGaleriaGrid.RowDefinitions.Clear();

            if (modo > 0)
            {
                tituloGaleriaGrid.ColumnDefinitions.Add(
                    new ColumnDefinition(GridLength.Star));
                tituloGaleriaGrid.ColumnDefinitions.Add(
                    new ColumnDefinition(GridLength.Auto));
                tituloGaleriaGrid.RowDefinitions.Add(
                    new RowDefinition(GridLength.Auto));

                Posicionar(tituloGaleriaTexto, 0, 0, 1);
                Posicionar(totalRegistrosLabel, 0, 1, 1);

                totalRegistrosLabel.HorizontalOptions =
                    LayoutOptions.End;
                totalRegistrosLabel.VerticalOptions =
                    LayoutOptions.Center;
                return;
            }

            tituloGaleriaGrid.ColumnDefinitions.Add(
                new ColumnDefinition(GridLength.Star));
            tituloGaleriaGrid.RowDefinitions.Add(
                new RowDefinition(GridLength.Auto));
            tituloGaleriaGrid.RowDefinitions.Add(
                new RowDefinition(GridLength.Auto));

            Posicionar(tituloGaleriaTexto, 0, 0, 1);
            Posicionar(totalRegistrosLabel, 1, 0, 1);

            totalRegistrosLabel.HorizontalOptions =
                LayoutOptions.Start;
            totalRegistrosLabel.VerticalOptions =
                LayoutOptions.Start;
        }

        private static void Posicionar(
            View vista,
            int fila,
            int columna,
            int columnas)
        {
            Grid.SetRow(vista, fila);
            Grid.SetColumn(vista, columna);
            Grid.SetColumnSpan(vista, columnas);
        }

        private async void OnBuscarPresionado(
            object sender,
            EventArgs e)
        {
            if (viewModel.IsBusy)
                return;

            await viewModel.BuscarAsync();
        }

        private async void OnLimpiarBusquedaClicked(
            object sender,
            EventArgs e)
        {
            if (viewModel.IsBusy)
                return;

            await viewModel.LimpiarBusquedaAsync();
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
        }
    }
}
