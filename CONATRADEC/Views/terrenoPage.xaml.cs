using CONATRADEC.Services;
using CONATRADEC.ViewModels;
using Microsoft.Maui.Graphics;

namespace CONATRADEC.Views
{
    public partial class terrenoPage : ContentPage
    {
        private readonly TerrenoViewModel viewModel = new();
        private bool botonRegresarAgregado;

        public terrenoPage()
        {
            InitializeComponent();
            BindingContext = viewModel;
            Shell.Current.FlyoutBehavior = FlyoutBehavior.Disabled;

            AgregarBotonRegresarConfiguracion();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            viewModel.ActualizarPermisos();

            ContenidoPrincipal.IsVisible = viewModel.CanView;
            ContenidoSinPermiso.IsVisible = !viewModel.CanView;

            if (!viewModel.CanView)
                return;

            await viewModel.InicializarAsync();
        }

        protected override void OnDisappearing()
        {
            viewModel.CancelarCarga();
            base.OnDisappearing();
        }

        /// <summary>
        /// Reorganiza únicamente el encabezado actual:
        ///
        /// [Configuración] [Título y total] [Nuevo]
        ///
        /// De esta manera no se altera el listado, los filtros ni la
        /// optimización existente de Terrenos.
        /// </summary>
        private void AgregarBotonRegresarConfiguracion()
        {
            if (botonRegresarAgregado ||
                ContenidoPrincipal == null)
            {
                return;
            }

            Grid? encabezado =
                ContenidoPrincipal.Children
                    .OfType<Grid>()
                    .FirstOrDefault(item =>
                        Grid.GetRow(item) == 0);

            if (encabezado == null)
                return;

            View? titulo =
                encabezado.Children
                    .OfType<View>()
                    .FirstOrDefault(item =>
                        Grid.GetColumn(item) == 0);

            View? botonNuevo =
                encabezado.Children
                    .OfType<View>()
                    .FirstOrDefault(item =>
                        Grid.GetColumn(item) == 1);

            encabezado.ColumnDefinitions.Clear();
            encabezado.ColumnDefinitions.Add(
                new ColumnDefinition(GridLength.Auto));
            encabezado.ColumnDefinitions.Add(
                new ColumnDefinition(GridLength.Star));
            encabezado.ColumnDefinitions.Add(
                new ColumnDefinition(GridLength.Auto));

            if (titulo != null)
                Grid.SetColumn(titulo, 1);

            if (botonNuevo != null)
                Grid.SetColumn(botonNuevo, 2);

            var botonRegresar =
                new Button
                {
                    Text = "← Configuración",
                    Padding = new Thickness(14, 9),
                    CornerRadius = 11,
                    FontFamily = "MontserratBold",
                    FontSize = 12,
                    BackgroundColor =
                        Color.FromArgb("#EEF5F2"),
                    TextColor =
                        Color.FromArgb("#3B655B"),
                    VerticalOptions =
                        LayoutOptions.Center
                };

            botonRegresar.Clicked +=
                async (_, _) =>
                    await Shell.Current.GoToAsync(
                        AppRoutes.Configuracion);

            Grid.SetColumn(botonRegresar, 0);
            encabezado.Children.Add(botonRegresar);

            botonRegresarAgregado = true;
        }
    }
}
