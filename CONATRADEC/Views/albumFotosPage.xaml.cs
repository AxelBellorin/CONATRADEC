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
        private bool? ultimoUsoCapitulosCompactos;

        private static bool EsWindows =>
            DeviceInfo.Platform == DevicePlatform.WinUI;

        public albumFotosPage()
        {
            InitializeComponent();

            Shell.Current.FlyoutBehavior =
                FlyoutBehavior.Disabled;

            BindingContext = viewModel;

            /*
             * MeasureAllItems obliga a Android a crear y medir cada tarjeta
             * antes de terminar de mostrar la página. Las tarjetas del álbum
             * tienen una estructura uniforme, por lo que basta medir la
             * primera y reutilizar ese tamaño.
             */
            AlbumCollectionView.ItemSizingStrategy =
                ItemSizingStrategy.MeasureFirstItem;

            /*
             * El rediseño dinámico mediante SizeChanged solamente es
             * necesario en Windows, donde el usuario puede redimensionar la
             * ventana. En Android provocaba mediciones encadenadas mientras
             * cargaban las imágenes y podía terminar en un ANR.
             */
            if (EsWindows)
            {
                AlbumCollectionView.Loaded +=
                    OnAlbumCollectionViewLoaded;

                AlbumCollectionView.SizeChanged +=
                    OnAlbumCollectionViewSizeChanged;
            }
            else
            {
                OptimizarColeccionesMoviles();
            }
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

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

            if (EsWindows)
            {
                AplicarDisenoResponsivo(
                    AlbumCollectionView.Width);
            }
            else
            {
                OptimizarColeccionesMoviles();

                /*
                 * Permite que Android pinte el encabezado y el indicador
                 * antes de comenzar las solicitudes del álbum.
                 */
                await Task.Yield();
            }

            await viewModel.LoadAsync(true);
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();

            /*
             * Libera el foco del buscador antes de navegar. Esto evita que el
             * teclado y una medición pendiente acompañen a la siguiente
             * pantalla en Android.
             */
            if (!EsWindows)
                barraBusqueda?.Unfocus();
        }

        private void OnAlbumCollectionViewLoaded(
            object? sender,
            EventArgs e)
        {
            if (!EsWindows)
                return;

            AplicarDisenoResponsivo(
                AlbumCollectionView.Width);
        }

        private void OnAlbumCollectionViewSizeChanged(
            object? sender,
            EventArgs e)
        {
            if (!EsWindows || aplicandoDiseno)
                return;

            /*
             * Se ejecuta directamente. Antes se acumulaban llamadas con
             * Dispatcher.Dispatch y cada cambio de margen generaba otra
             * medición pendiente.
             */
            AplicarDisenoResponsivo(
                AlbumCollectionView.Width);
        }

        private void OptimizarColeccionesMoviles()
        {
            if (EsWindows)
                return;

            AlbumCollectionView.ItemSizingStrategy =
                ItemSizingStrategy.MeasureFirstItem;

            LocalizarControlesResponsivos();

            if (capitulosCompactos != null)
            {
                capitulosCompactos.ItemSizingStrategy =
                    ItemSizingStrategy.MeasureFirstItem;
            }
        }

        private void AplicarDisenoResponsivo(
            double anchoDisponible)
        {
            if (!EsWindows ||
                aplicandoDiseno ||
                double.IsNaN(anchoDisponible) ||
                anchoDisponible <= 0)
            {
                return;
            }

            LocalizarControlesResponsivos();

            int modo = anchoDisponible switch
            {
                < 520 => 0,
                < 820 => 1,
                _ => 2
            };

            int span = anchoDisponible switch
            {
                < 620 => 1,
                < 1080 => 2,
                _ => 3
            };

            bool usarCapitulosCompactos =
                anchoDisponible < 760;

            bool cambioModo = modo != ultimoModo;
            bool cambioSpan = span != ultimoSpan;
            bool cambioCapitulos =
                ultimoUsoCapitulosCompactos !=
                usarCapitulosCompactos;

            if (!cambioModo &&
                !cambioSpan &&
                !cambioCapitulos)
            {
                return;
            }

            aplicandoDiseno = true;

            try
            {
                if (cambioSpan)
                    AplicarColumnasGaleria(span);

                if (cambioModo)
                {
                    AplicarMargenGaleria(modo);
                    AplicarEncabezado(modo);
                    AplicarBuscador(modo);
                    AplicarTituloGaleria(modo);
                    ultimoModo = modo;
                }

                if (cambioCapitulos)
                {
                    if (capitulosWindows != null)
                    {
                        capitulosWindows.IsVisible =
                            !usarCapitulosCompactos;
                    }

                    if (capitulosCompactos != null)
                    {
                        capitulosCompactos.IsVisible =
                            usarCapitulosCompactos;
                    }

                    ultimoUsoCapitulosCompactos =
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

                List<Button> botones =
                    encabezadoGrid.Children
                        .OfType<Button>()
                        .ToList();

                botonNuevaCategoria =
                    botones.FirstOrDefault(x =>
                        string.Equals(
                            x.Text,
                            "Nueva categoría",
                            StringComparison.OrdinalIgnoreCase));

                botonNuevoRegistro =
                    botones.FirstOrDefault(x =>
                        string.Equals(
                            x.Text,
                            "Nuevo registro",
                            StringComparison.OrdinalIgnoreCase));
            }

            if (header.Children[1]
                is Border bordeBusqueda &&
                bordeBusqueda.Content
                    is Grid gridBusqueda)
            {
                busquedaGrid = gridBusqueda;

                barraBusqueda =
                    gridBusqueda.Children
                        .OfType<SearchBar>()
                        .FirstOrDefault();

                botonLimpiar =
                    gridBusqueda.Children
                        .OfType<Button>()
                        .FirstOrDefault();

                opcionesBusqueda =
                    gridBusqueda.Children
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
                capitulosCompactos != null &&
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
