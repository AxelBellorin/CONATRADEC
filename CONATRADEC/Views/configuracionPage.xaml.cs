using CONATRADEC.Services;
using CONATRADEC.ViewModels;

namespace CONATRADEC.Views
{
    public partial class configuracionPage : ContentPage
    {
        private static bool rutasBitacoraRegistradas;
        private readonly ConfiguracionViewModel viewModel = new();
        private readonly Button botonBitacora;

        public configuracionPage()
        {
            InitializeComponent();
            Shell.Current.FlyoutBehavior = FlyoutBehavior.Disabled;
            BindingContext = viewModel;

            RegistrarRutasBitacora();
            botonBitacora = CrearBotonBitacora();
            AgregarBotonFlotante(botonBitacora);
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            viewModel.ActualizarVisibilidad();

            botonBitacora.IsVisible =
                PermissionService.Instance.HasRead("bitacoraPage");
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
