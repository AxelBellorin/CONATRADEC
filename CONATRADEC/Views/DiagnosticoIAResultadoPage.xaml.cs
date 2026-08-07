using CONATRADEC.Models;
using CONATRADEC.Services;
using CONATRADEC.ViewModels;

namespace CONATRADEC.Views
{
    public partial class DiagnosticoIAResultadoPage :
        ContentPage,
        IQueryAttributable
    {
        private readonly DiagnosticoIAResultadoViewModel viewModel;
        private readonly InspeccionFitosanitariaBandejaApiService bandejaApi =
            InspeccionFitosanitariaBandejaApiService.Instance;
        private readonly Label tecnicoResponsableLabel;
        private readonly Border tecnicoResponsableBanner;

        private int diagnosticoIdActual;
        private string origenActual = DiagnosticoIARoutes.ModoMisInspecciones;

        public DiagnosticoIAResultadoPage()
        {
            InitializeComponent();

            (tecnicoResponsableBanner, tecnicoResponsableLabel) =
                CrearBannerTecnicoResponsable();
            IntegrarBannerTecnicoResponsable();

            viewModel = new DiagnosticoIAResultadoViewModel();
            BindingContext = viewModel;
            InicializarCapaRevision();
        }

        public void ApplyQueryAttributes(
            IDictionary<string, object> query)
        {
            int id = 0;
            string? origen = null;

            if (query.TryGetValue("diagnosticoId", out object? valor))
                int.TryParse(valor?.ToString(), out id);

            if (query.TryGetValue("origen", out object? origenValor))
                origen = origenValor?.ToString();

            diagnosticoIdActual = id;
            origenActual = DiagnosticoIARoutes.NormalizarModo(origen);
            ConfigurarFlujoRevision(diagnosticoIdActual, origenActual);
            viewModel.AplicarParametros(id, origen);
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await viewModel.InicializarAsync();
            await CargarTecnicoResponsableAsync();
            await AplicarFlujoRevisionAsync();
        }

        protected override void OnDisappearing()
        {
            DesconectarFlujoRevision();
            viewModel.DetenerSeguimiento();
            base.OnDisappearing();
        }

        private static (Border Banner, Label Texto)
            CrearBannerTecnicoResponsable()
        {
            var etiqueta = new Label
            {
                FontSize = 12,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#315E52"),
                LineBreakMode = LineBreakMode.WordWrap,
                VerticalTextAlignment = TextAlignment.Center
            };

            var titulo = new Label
            {
                Text = "Usuario que registró la inspección",
                FontSize = 11,
                TextColor = Color.FromArgb("#5E6B67"),
                VerticalTextAlignment = TextAlignment.Center
            };

            var contenido = new VerticalStackLayout
            {
                Spacing = 1,
                Children =
                {
                    titulo,
                    etiqueta
                }
            };

            var banner = new Border
            {
                IsVisible = false,
                Padding = new Thickness(14, 9),
                Margin = new Thickness(12, 8, 12, 0),
                BackgroundColor = Color.FromArgb("#EAF3EF"),
                Stroke = Color.FromArgb("#C8DED6"),
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle
                {
                    CornerRadius = 12
                },
                MaximumWidthRequest = 1250,
                HorizontalOptions = LayoutOptions.Fill,
                Content = contenido
            };

            return (banner, etiqueta);
        }

        private void IntegrarBannerTecnicoResponsable()
        {
            View? contenidoOriginal = Content;
            if (contenidoOriginal == null)
                return;

            Content = null;

            var contenedor = new Grid
            {
                RowDefinitions =
                {
                    new RowDefinition { Height = GridLength.Auto },
                    new RowDefinition { Height = GridLength.Star }
                }
            };

            Grid.SetRow(tecnicoResponsableBanner, 0);
            Grid.SetRow(contenidoOriginal, 1);
            contenedor.Children.Add(tecnicoResponsableBanner);
            contenedor.Children.Add(contenidoOriginal);
            Content = contenedor;
        }

        private async Task CargarTecnicoResponsableAsync()
        {
            if (diagnosticoIdActual <= 0)
            {
                tecnicoResponsableBanner.IsVisible = false;
                return;
            }

            try
            {
                TecnicoInspeccionFiltroItem tecnico =
                    await bandejaApi.ObtenerTecnicoResponsableAsync(
                        diagnosticoIdActual);

                tecnicoResponsableLabel.Text =
                    !string.IsNullOrWhiteSpace(tecnico.NombreCompleto)
                        ? tecnico.NombreCompleto.Trim()
                        : !string.IsNullOrWhiteSpace(tecnico.NombreUsuario)
                            ? tecnico.NombreUsuario.Trim()
                            : tecnico.UsuarioTecnicoId > 0
                                ? $"Usuario #{tecnico.UsuarioTecnicoId}"
                                : "Usuario no disponible";
                tecnicoResponsableBanner.IsVisible = true;
            }
            catch
            {
                /*
                 * El dato es informativo. Un problema al cargar el nombre del
                 * usuario creador no debe bloquear el expediente ni sus decisiones.
                 */
                tecnicoResponsableBanner.IsVisible = false;
            }
        }
    }
}
