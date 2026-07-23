using CONATRADEC.Services;
using CONATRADEC.ViewModels;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;
using System;

namespace CONATRADEC.Views
{
    public partial class MainPage : ContentPage
    {
        private readonly MainPageViewModel viewModel = new();
        private readonly View contenidoPrincipal;
        private readonly View contenidoSinPermiso;

        public MainPage()
        {
            Shell.Current.FlyoutBehavior =
                FlyoutBehavior.Disabled;

            InitializeComponent();
            BindingContext = viewModel;

            // Se conserva la vista original para poder restaurarla si los
            // permisos cambian durante una nueva sesión.
            contenidoPrincipal = Content;
            contenidoSinPermiso = CrearContenidoSinPermiso();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            viewModel.LoadPagePermissions("MainPage");
            viewModel.PrepararPantalla();

            if (!viewModel.CanView)
            {
                // No se muestra un diálogo encima de información protegida.
                // Se reemplaza completamente el contenido de MainPage por
                // una pantalla informativa, conservando el menú y Salir.
                viewModel.IsBusy = false;
                MostrarContenidoSinPermiso();
                return;
            }

            MostrarContenidoPrincipal();

            /*
             * La primera consulta sigue siendo manual. Si el usuario ya
             * listó los análisis y vuelve después de guardar una edición,
             * se actualizan las tarjetas automáticamente.
             */
            bool debeActualizar =
                viewModel.SeHaListado ||
                AnalisisListadoEstadoService
                    .HayActualizacionPendiente;

            if (debeActualizar &&
                !viewModel.IsBusy)
            {
                await viewModel.CargarAnalisisAsync(false);

                if (viewModel.SeHaListado)
                {
                    AnalisisListadoEstadoService
                        .ConfirmarActualizacion();
                }
            }
        }

        private void MostrarContenidoPrincipal()
        {
            if (!ReferenceEquals(Content, contenidoPrincipal))
                Content = contenidoPrincipal;
        }

        private void MostrarContenidoSinPermiso()
        {
            if (!ReferenceEquals(Content, contenidoSinPermiso))
                Content = contenidoSinPermiso;
        }

        private static View CrearContenidoSinPermiso()
        {
            var titulo = new Label
            {
                Text = "Sin permisos asignados",
                FontSize = 24,
                FontFamily = "MontserratBold",
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#3B655B"),
                HorizontalTextAlignment = TextAlignment.Center
            };

            var mensaje = new Label
            {
                Text =
                    "Su usuario está activo, pero su rol no tiene permiso " +
                    "para visualizar la pantalla principal.",
                FontSize = 15,
                FontFamily = "MontserratMedium",
                TextColor = Color.FromArgb("#333333"),
                HorizontalTextAlignment = TextAlignment.Center,
                LineBreakMode = LineBreakMode.WordWrap
            };

            var indicacion = new Label
            {
                Text =
                    "Puede ingresar a otra opción disponible desde el menú " +
                    "o comunicarse con un administrador.",
                FontSize = 13,
                FontFamily = "MontserratMedium",
                TextColor = Color.FromArgb("#6B7280"),
                HorizontalTextAlignment = TextAlignment.Center,
                LineBreakMode = LineBreakMode.WordWrap
            };

            var contenidoTarjeta = new VerticalStackLayout
            {
                Padding = new Thickness(24),
                Spacing = 14,
                Children =
                {
                    titulo,
                    mensaje,
                    indicacion
                }
            };

            var tarjeta = new Border
            {
                Content = contenidoTarjeta,
                Margin = new Thickness(18),
                MaximumWidthRequest = 520,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,
                BackgroundColor = Color.FromArgb("#FFFFFF"),
                Stroke = Color.FromArgb("#DDE7E3"),
                StrokeThickness = 1,
                StrokeShape = new RoundRectangle
                {
                    CornerRadius = new CornerRadius(20)
                },
                Shadow = new Shadow
                {
                    Brush = Colors.Black,
                    Offset = new Point(0, 4),
                    Radius = 12,
                    Opacity = 0.10f
                }
            };

            var contenedor = new Grid
            {
                BackgroundColor = Color.FromArgb("#FFF5FF")
            };

            contenedor.Children.Add(tarjeta);

            var vista = new ContentView
            {
                Content = contenedor,
                BackgroundColor = Color.FromArgb("#FFF5FF")
            };

            if (Application.Current?.Resources.TryGetValue(
                    "FooterTemplate",
                    out object? recursoTemplate) == true &&
                recursoTemplate is ControlTemplate footerTemplate)
            {
                vista.ControlTemplate = footerTemplate;
            }

            return vista;
        }

        private async void OnListarAnalisisClicked(
            object? sender,
            EventArgs e)
        {
            if (!viewModel.CanView || viewModel.IsBusy)
                return;

            await viewModel.CargarAnalisisAsync(true);
        }
    }
}