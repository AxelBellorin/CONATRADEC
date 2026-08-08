using CONATRADEC.Models;
using CONATRADEC.Services;
using CONATRADEC.ViewModels;
using Microsoft.Maui.Devices;
using System.Linq;

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
                Text = "Registrado por",
                FontSize = 10,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#6B7773"),
                VerticalTextAlignment = TextAlignment.Center
            };

            View contenido;

            if (DeviceInfo.Current.Idiom == DeviceIdiom.Phone)
            {
                contenido = new VerticalStackLayout
                {
                    Spacing = 2,
                    Children =
                    {
                        titulo,
                        etiqueta
                    }
                };
            }
            else
            {
                var fila = new Grid
                {
                    ColumnDefinitions =
                    {
                        new ColumnDefinition
                        {
                            Width = GridLength.Auto
                        },
                        new ColumnDefinition
                        {
                            Width = GridLength.Star
                        }
                    },
                    ColumnSpacing = 12
                };

                fila.Add(titulo, 0, 0);
                fila.Add(etiqueta, 1, 0);
                contenido = fila;
            }

            var banner = new Border
            {
                IsVisible = false,
                Padding = new Thickness(11, 7),
                Margin = new Thickness(0, 6, 0, 0),
                BackgroundColor = Color.FromArgb("#EAF3EF"),
                Stroke = Color.FromArgb("#C8DED6"),
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle
                {
                    CornerRadius = 10
                },
                MaximumWidthRequest = 520,
                HorizontalOptions = LayoutOptions.Start,
                Content = contenido
            };

            return (banner, etiqueta);
        }

        private void IntegrarBannerTecnicoResponsable()
        {
            /*
             * El dato del usuario pertenece al encabezado del expediente.
             * Antes se insertaba por encima de todo el ContentView, lo que en
             * Windows creaba una franja independiente incluso sobre el menú
             * lateral. Ahora se integra debajo del título/subtítulo y comparte
             * exactamente el mismo ancho y alineación del encabezado.
             */
            if (Content is not ContentView contentView ||
                contentView.Content is not Grid contenedorPrincipal)
            {
                return;
            }

            Grid? encabezado =
                contenedorPrincipal.Children
                    .OfType<Grid>()
                    .FirstOrDefault(x => Grid.GetRow(x) == 0);

            if (encabezado == null)
                return;

            VerticalStackLayout? bloqueTitulo =
                encabezado.Children
                    .OfType<VerticalStackLayout>()
                    .FirstOrDefault();

            if (bloqueTitulo == null)
                return;

            bloqueTitulo.Children.Add(
                tecnicoResponsableBanner);
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
