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
        private bool avisoLocalMostrado;

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

            /*
             * Los identificadores negativos pertenecen exclusivamente a la cola
             * fitosanitaria local. Aún no existe un expediente central que pueda
             * analizarse; la tarjeta se conserva en Mis inspecciones hasta que
             * una sesión en línea complete la sincronización.
             */
            if (diagnosticoIdActual < 0 && ModoSesionService.EsOffline)
            {
                if (!avisoLocalMostrado)
                {
                    avisoLocalMostrado = true;
                    await DisplayAlert(
                        "Inspección pendiente de sincronización",
                        "Esta inspección fue guardada en el dispositivo. Sus fotografías se enviarán al servidor cuando vuelva a iniciar una sesión en línea; después podrá continuar con el análisis IA y el resto del flujo.",
                        "Aceptar");
                }

                if (Shell.Current != null)
                    await Shell.Current.GoToAsync("..");
                return;
            }

            /*
             * Analizador y aprobador deben reservar la ficha antes de cargar
             * sus herramientas de trabajo. Si otra sesión ya la tiene abierta,
             * esta página regresa a la bandeja sin permitir una revisión doble.
             */
            if (!await PrepararBloqueoRevisionAsync())
                return;

            await viewModel.InicializarAsync();
            await CargarTecnicoResponsableAsync();
            await AplicarFlujoRevisionAsync();
        }

        protected override async void OnDisappearing()
        {
            /*
             * Las ventanas modales que forman parte de la misma revisión no
             * liberan el bloqueo. Al abandonar realmente el expediente sí se
             * libera de inmediato; si la aplicación termina abruptamente, el
             * backend lo libera automáticamente al vencer el lease.
             */
            if (!DebeMantenerBloqueoRevisionAlOcultarse)
                await LiberarBloqueoRevisionAsync();

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
