using CONATRADEC.Services;
using CONATRADEC.ViewModels;
using Microsoft.Maui.Controls.Shapes;

namespace CONATRADEC.Views
{
    public partial class configuracionPage : ContentPage
    {
        private static bool rutasBitacoraRegistradas;
        private readonly ConfiguracionViewModel viewModel = new();
        private readonly Button botonBitacora;
        private VerticalStackLayout? seccionContenidoComunicacion;

        public configuracionPage()
        {
            InitializeComponent();
            Shell.Current.FlyoutBehavior = FlyoutBehavior.Disabled;
            BindingContext = viewModel;

            RegistrarRutasBitacora();
            AgregarSeccionTiposPublicacion();

            botonBitacora = CrearBotonBitacora();
            AgregarBotonFlotante(botonBitacora);
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            viewModel.ActualizarVisibilidad();

            botonBitacora.IsVisible =
                PermissionService.Instance.HasRead("bitacoraPage");

            if (seccionContenidoComunicacion != null)
            {
                seccionContenidoComunicacion.IsVisible =
                    viewModel.MostrarContenidoComunicacion;
            }
        }

        private static void RegistrarRutasBitacora()
        {
            if (rutasBitacoraRegistradas)
                return;

            Routing.RegisterRoute(
                AppRoutes.Bitacora,
                typeof(bitacoraPage));

            Routing.RegisterRoute(
                AppRoutes.BitacoraDetalle,
                typeof(bitacoraDetallePage));

            rutasBitacoraRegistradas = true;
        }

        private void AgregarSeccionTiposPublicacion()
        {
            VerticalStackLayout? contenedorPrincipal =
                BuscarContenedorPrincipal();

            if (contenedorPrincipal == null)
                return;

            seccionContenidoComunicacion =
                CrearSeccionTiposPublicacion();

            int posicion = Math.Max(
                0,
                contenedorPrincipal.Children.Count - 1);

            contenedorPrincipal.Children.Insert(
                posicion,
                seccionContenidoComunicacion);
        }

        private VerticalStackLayout? BuscarContenedorPrincipal()
        {
            if (Content is not ContentView contentView ||
                contentView.Content is not ScrollView scrollView ||
                scrollView.Content is not Grid grid)
            {
                return null;
            }

            return grid.Children
                .OfType<VerticalStackLayout>()
                .FirstOrDefault();
        }

        private VerticalStackLayout CrearSeccionTiposPublicacion()
        {
            Label titulo = new()
            {
                Text = "Contenido y comunicación",
                FontSize = 20,
                FontFamily = "MontserratBold",
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#3B655B")
            };

            Label subtitulo = new()
            {
                Text = "Catálogos utilizados por el centro de noticias.",
                FontSize = 13,
                TextColor = Color.FromArgb("#4B5563")
            };

            VerticalStackLayout encabezado = new()
            {
                Spacing = 2,
                Children =
                {
                    titulo,
                    subtitulo
                }
            };

            Image icono = new()
            {
                Source = "iconnews.png",
                WidthRequest = 28,
                HeightRequest = 28,
                Aspect = Aspect.AspectFit,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center
            };

            Border fondoIcono = new()
            {
                WidthRequest = 48,
                HeightRequest = 48,
                Padding = 10,
                BackgroundColor = Color.FromArgb("#EEF5F2"),
                StrokeThickness = 0,
                StrokeShape = new RoundRectangle
                {
                    CornerRadius = 14
                },
                Content = icono
            };

            Label nombre = new()
            {
                Text = "Tipos de publicación",
                FontSize = 16,
                FontFamily = "MontserratBold",
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#111827")
            };

            Label descripcion = new()
            {
                Text = "Noticias, ofertas, eventos y nuevas categorías.",
                FontSize = 12,
                TextColor = Color.FromArgb("#4B5563")
            };

            VerticalStackLayout textos = new()
            {
                Spacing = 2,
                VerticalOptions = LayoutOptions.Center,
                Children =
                {
                    nombre,
                    descripcion
                }
            };

            Label flecha = new()
            {
                Text = "›",
                FontSize = 28,
                TextColor = Color.FromArgb("#3B655B"),
                VerticalOptions = LayoutOptions.Center
            };

            Grid contenidoTarjeta = new()
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition(GridLength.Auto),
                    new ColumnDefinition(GridLength.Star),
                    new ColumnDefinition(GridLength.Auto)
                },
                ColumnSpacing = 12
            };

            Grid.SetColumn(fondoIcono, 0);
            Grid.SetColumn(textos, 1);
            Grid.SetColumn(flecha, 2);

            contenidoTarjeta.Children.Add(fondoIcono);
            contenidoTarjeta.Children.Add(textos);
            contenidoTarjeta.Children.Add(flecha);

            Border tarjeta = new()
            {
                Padding = 14,
                BackgroundColor = Colors.White,
                Stroke = new SolidColorBrush(
                    Color.FromArgb("#DDE7E3")),
                StrokeThickness = 1,
                StrokeShape = new RoundRectangle
                {
                    CornerRadius = 16
                },
                MinimumWidthRequest = 270,
                MaximumWidthRequest = 430,
                HorizontalOptions = LayoutOptions.Fill,
                Content = contenidoTarjeta
            };

            TapGestureRecognizer toque = new();
            toque.Tapped += async (_, _) =>
            {
                if (!PermissionService.Instance.HasRead(
                        "categoriaPublicacionPage"))
                {
                    await GlobalService.MostrarAdvertenciaAsync(
                        "No tiene permiso para consultar los tipos de publicación.");
                    return;
                }

                await Shell.Current.GoToAsync(
                    AppRoutes.CategoriasPublicacion,
                    false);
            };

            tarjeta.GestureRecognizers.Add(toque);

            return new VerticalStackLayout
            {
                IsVisible = false,
                Spacing = 11,
                Children =
                {
                    encabezado,
                    tarjeta
                }
            };
        }

        private Button CrearBotonBitacora()
        {
            var boton = new Button
            {
                Text = "≡  Bitácora",
                Padding = new Thickness(18, 11),
                Margin = new Thickness(20, 20, 20, 86),
                CornerRadius = 24,
                BackgroundColor = Color.FromArgb("#3B655B"),
                TextColor = Colors.White,
                FontFamily = "MontserratBold",
                FontSize = 14,
                HorizontalOptions = LayoutOptions.End,
                VerticalOptions = LayoutOptions.End,
                IsVisible = false,
                ZIndex = 20,
                Shadow = new Shadow
                {
                    Brush = new SolidColorBrush(Colors.Black),
                    Opacity = 0.22f,
                    Radius = 10,
                    Offset = new Point(0, 4)
                }
            };

            boton.Clicked += async (_, _) =>
            {
                if (!PermissionService.Instance.HasRead("bitacoraPage"))
                {
                    await GlobalService.MostrarAdvertenciaAsync(
                        "No tiene permiso para consultar la bitácora.");
                    return;
                }

                await Shell.Current.GoToAsync(AppRoutes.Bitacora, false);
            };

            return boton;
        }

        private void AgregarBotonFlotante(Button boton)
        {
            View? contenidoOriginal = Content;
            if (contenidoOriginal == null)
                return;

            Content = null;

            var contenedor = new Grid();
            contenedor.Children.Add(contenidoOriginal);
            contenedor.Children.Add(boton);
            Content = contenedor;
        }
    }
}
