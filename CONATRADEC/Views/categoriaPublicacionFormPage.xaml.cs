using CONATRADEC.Models;
using CONATRADEC.ViewModels;

namespace CONATRADEC.Views
{
    public partial class categoriaPublicacionFormPage :
        ContentPage,
        IQueryAttributable
    {
        private readonly CategoriaPublicacionFormViewModel
            viewModel = new();

        public categoriaPublicacionFormPage()
        {
            InitializeComponent();

            Shell.Current.FlyoutBehavior =
                FlyoutBehavior.Disabled;

            BindingContext = viewModel;
        }

        public void ApplyQueryAttributes(
            IDictionary<string, object> query)
        {
            int categoriaId = 0;

            if (query.TryGetValue(
                    "CategoriaId",
                    out object? valorId))
            {
                categoriaId = ConvertirId(valorId);
            }
            else if (query.TryGetValue(
                         "Categoria",
                         out object? valorCategoria) &&
                     valorCategoria is
                         CategoriaPublicacionCatalogoResponse categoria)
            {
                /*
                 * Compatibilidad con navegaciones anteriores. Solo se conserva
                 * el identificador y la edición siempre vuelve a consultar el
                 * registro fresco desde la API administrativa.
                 */
                categoriaId =
                    categoria.CategoriaPublicacionId;
            }

            viewModel.Preparar(categoriaId);
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            viewModel.ActualizarPermisos();
            AjustarDiseno(Width);

            bool tienePermiso =
                viewModel.PuedeAcceder;

            ContenidoPrincipal.IsVisible =
                tienePermiso;

            ContenidoSinPermiso.IsVisible =
                !tienePermiso;

            if (!tienePermiso)
                return;

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

            FormularioLayout.Padding =
                width < 600
                    ? new Thickness(12, 12, 12, 28)
                    : width < 900
                        ? new Thickness(20, 18, 20, 32)
                        : new Thickness(28, 22, 28, 36);

            bool compacto =
                width < 650;

            ConfigurarHero(compacto);
            ConfigurarColor(compacto);
            ConfigurarAcciones(
                width < 480);
        }

        private void ConfigurarHero(bool compacto)
        {
            HeroGrid.RowDefinitions.Clear();
            HeroGrid.ColumnDefinitions.Clear();

            if (compacto)
            {
                HeroGrid.RowDefinitions.Add(
                    new RowDefinition(GridLength.Auto));

                HeroGrid.RowDefinitions.Add(
                    new RowDefinition(GridLength.Auto));

                HeroGrid.ColumnDefinitions.Add(
                    new ColumnDefinition(GridLength.Star));

                Grid.SetRow(
                    GuardarSuperiorButton,
                    1);

                Grid.SetColumn(
                    GuardarSuperiorButton,
                    0);

                GuardarSuperiorButton
                    .HorizontalOptions =
                        LayoutOptions.Fill;

                GuardarSuperiorButton
                    .MinimumWidthRequest = 0;

                return;
            }

            HeroGrid.RowDefinitions.Add(
                new RowDefinition(GridLength.Auto));

            HeroGrid.ColumnDefinitions.Add(
                new ColumnDefinition(GridLength.Star));

            HeroGrid.ColumnDefinitions.Add(
                new ColumnDefinition(GridLength.Auto));

            Grid.SetRow(
                GuardarSuperiorButton,
                0);

            Grid.SetColumn(
                GuardarSuperiorButton,
                1);

            GuardarSuperiorButton
                .HorizontalOptions =
                    LayoutOptions.End;

            GuardarSuperiorButton
                .MinimumWidthRequest = 150;
        }

        private void ConfigurarColor(bool compacto)
        {
            ColorGrid.RowDefinitions.Clear();
            ColorGrid.ColumnDefinitions.Clear();

            if (compacto)
            {
                ColorGrid.RowDefinitions.Add(
                    new RowDefinition(GridLength.Auto));

                ColorGrid.RowDefinitions.Add(
                    new RowDefinition(GridLength.Auto));

                ColorGrid.ColumnDefinitions.Add(
                    new ColumnDefinition(GridLength.Star));

                Grid.SetRow(
                    ColorHexContainer,
                    1);

                Grid.SetColumn(
                    ColorHexContainer,
                    0);

                return;
            }

            ColorGrid.RowDefinitions.Add(
                new RowDefinition(GridLength.Auto));

            ColorGrid.ColumnDefinitions.Add(
                new ColumnDefinition(GridLength.Star));

            ColorGrid.ColumnDefinitions.Add(
                new ColumnDefinition(GridLength.Star));

            Grid.SetRow(
                ColorHexContainer,
                0);

            Grid.SetColumn(
                ColorHexContainer,
                1);
        }

        private void ConfigurarAcciones(
            bool unaColumna)
        {
            AccionesGrid.RowDefinitions.Clear();
            AccionesGrid.ColumnDefinitions.Clear();

            if (unaColumna)
            {
                AccionesGrid.RowDefinitions.Add(
                    new RowDefinition(GridLength.Auto));

                AccionesGrid.RowDefinitions.Add(
                    new RowDefinition(GridLength.Auto));

                AccionesGrid.ColumnDefinitions.Add(
                    new ColumnDefinition(GridLength.Star));

                Grid.SetRow(
                    CancelarInferiorButton,
                    0);

                Grid.SetColumn(
                    CancelarInferiorButton,
                    0);

                Grid.SetRow(
                    GuardarInferiorButton,
                    1);

                Grid.SetColumn(
                    GuardarInferiorButton,
                    0);

                return;
            }

            AccionesGrid.RowDefinitions.Add(
                new RowDefinition(GridLength.Auto));

            AccionesGrid.ColumnDefinitions.Add(
                new ColumnDefinition(GridLength.Star));

            AccionesGrid.ColumnDefinitions.Add(
                new ColumnDefinition(GridLength.Star));

            Grid.SetRow(
                CancelarInferiorButton,
                0);

            Grid.SetColumn(
                CancelarInferiorButton,
                0);

            Grid.SetRow(
                GuardarInferiorButton,
                0);

            Grid.SetColumn(
                GuardarInferiorButton,
                1);
        }

        private static int ConvertirId(object? valor)
        {
            if (valor is int id)
                return id;

            return int.TryParse(
                    valor?.ToString(),
                    out int convertido)
                ? convertido
                : 0;
        }
    }
}
