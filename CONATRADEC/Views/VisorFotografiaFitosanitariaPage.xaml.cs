using CONATRADEC.Models;

namespace CONATRADEC.Views
{
    /// <summary>
    /// Visor modal reutilizable para fotografías fitosanitarias.
    /// La evidencia original permanece intacta y, cuando existe, puede
    /// compararse con la copia marcada correspondiente a la valoración IA
    /// vigente. La navegación continúa siendo por fotografía, no por enfermedad.
    /// </summary>
    public partial class VisorFotografiaFitosanitariaPage : ContentPage
    {
        private static readonly Color FondoSeleccionado =
            Color.FromArgb("#3B655B");
        private static readonly Color FondoNoSeleccionado =
            Color.FromArgb("#262D2A");
        private static readonly Color TextoDisponible = Colors.White;
        private static readonly Color TextoNoDisponible =
            Color.FromArgb("#7F8985");

        private const double ZoomMinimo = 1d;
        private const double ZoomMaximo = 6d;
        private const double PasoZoom = 0.5d;

        private readonly IReadOnlyList<InspeccionFotoV2> fotografias;
        private int indiceActual;
        private bool mostrandoMarcada;
        private bool hallazgosExpandidos;
        private bool cierreEnCurso;
        private double escalaInicioPinch = ZoomMinimo;
        private double traslacionInicioX;
        private double traslacionInicioY;

        public VisorFotografiaFitosanitariaPage(
            IReadOnlyList<InspeccionFotoV2> fotografias,
            int indiceInicial)
        {
            ArgumentNullException.ThrowIfNull(fotografias);

            if (fotografias.Count == 0)
            {
                throw new ArgumentException(
                    "El visor necesita al menos una fotografía.",
                    nameof(fotografias));
            }

            if (indiceInicial < 0 || indiceInicial >= fotografias.Count)
                throw new ArgumentOutOfRangeException(nameof(indiceInicial));

            this.fotografias = fotografias;
            indiceActual = indiceInicial;

            InitializeComponent();
            MostrarFotografiaActual();
        }

        private InspeccionFotoV2 FotografiaActual =>
            fotografias[indiceActual];

        private void OnAnteriorClicked(object? sender, EventArgs e) =>
            CambiarIndice(-1);

        private void OnSiguienteClicked(object? sender, EventArgs e) =>
            CambiarIndice(1);

        private void OnLimpiaClicked(object? sender, EventArgs e)
        {
            mostrandoMarcada = false;
            ActualizarVistaImagen();
        }

        private void OnMarcadaClicked(object? sender, EventArgs e)
        {
            if (!FotografiaActual.TieneMarcadaIA)
                return;

            mostrandoMarcada = true;
            ActualizarVistaImagen();
        }

        private async void OnCerrarClicked(object? sender, EventArgs e) =>
            await CerrarAsync();

        private void CambiarIndice(int desplazamiento)
        {
            int nuevoIndice = indiceActual + desplazamiento;

            if (nuevoIndice < 0 || nuevoIndice >= fotografias.Count)
                return;

            indiceActual = nuevoIndice;

            /*
             * Se conserva la preferencia Marcada IA al navegar solamente si la
             * nueva fotografía dispone de un derivado vigente. En registros
             * antiguos el visor vuelve automáticamente a la evidencia limpia.
             */
            if (mostrandoMarcada && !FotografiaActual.TieneMarcadaIA)
                mostrandoMarcada = false;

            RestablecerZoom();
            MostrarFotografiaActual();
        }

        private void MostrarFotografiaActual()
        {
            InspeccionFotoV2 foto = FotografiaActual;
            InspeccionFotoResultadoIAV2? resultado = foto.ResultadoIA;

            TituloFotografiaLabel.Text = foto.Titulo;
            ContadorFotografiaLabel.Text =
                $"{indiceActual + 1}/{fotografias.Count}";
            DiagnosticoFotografiaLabel.Text =
                CrearResumenDiagnosticos(foto);

            LocalizacionVisualLabel.Text = CrearTextoLocalizacion(foto);
            VersionMarcadaLabel.Text = foto.TieneMarcadaIA
                ? $"Revisión IA {foto.VersionImagenMarcadaIA ?? resultado?.VersionVisual ?? 0}"
                : string.Empty;

            CrearLeyendaDiagnosticos(foto);
            ContraerHallazgos();
            ActualizarVistaImagen();

            AnteriorButton.IsEnabled = indiceActual > 0;
            SiguienteButton.IsEnabled =
                indiceActual < fotografias.Count - 1;

            AnteriorButton.Opacity = AnteriorButton.IsEnabled ? 1 : 0.45;
            SiguienteButton.Opacity = SiguienteButton.IsEnabled ? 1 : 0.45;
        }

        private void ActualizarVistaImagen()
        {
            InspeccionFotoV2 foto = FotografiaActual;

            bool puedeMostrarMarcada =
                mostrandoMarcada && foto.TieneMarcadaIA;

            if (!puedeMostrarMarcada)
                mostrandoMarcada = false;

            string ruta = puedeMostrarMarcada
                ? foto.UrlImagenMarcadaIA
                : foto.UrlImagen;

            FotografiaImage.Source = CrearOrigenImagen(ruta);
            IndicadorVistaLabel.Text = puedeMostrarMarcada
                ? "Marcada por IA"
                : "Foto limpia · evidencia original";

            LimpiaButton.BackgroundColor = mostrandoMarcada
                ? FondoNoSeleccionado
                : FondoSeleccionado;
            LimpiaButton.TextColor = TextoDisponible;

            MarcadaButton.IsEnabled = foto.TieneMarcadaIA;
            MarcadaButton.Opacity = foto.TieneMarcadaIA ? 1 : 0.5;
            MarcadaButton.BackgroundColor = mostrandoMarcada
                ? FondoSeleccionado
                : FondoNoSeleccionado;
            MarcadaButton.TextColor = foto.TieneMarcadaIA
                ? TextoDisponible
                : TextoNoDisponible;
        }

        private void OnHallazgosToggleClicked(object? sender, EventArgs e)
        {
            hallazgosExpandidos = !hallazgosExpandidos;
            HallazgosDetalleScroll.IsVisible = hallazgosExpandidos;
            HallazgosToggleButton.Text = hallazgosExpandidos
                ? "Ocultar detalles ▴"
                : "Ver detalles ▾";
        }

        private void ContraerHallazgos()
        {
            hallazgosExpandidos = false;

            if (HallazgosDetalleScroll != null)
                HallazgosDetalleScroll.IsVisible = false;

            if (HallazgosToggleButton != null)
                HallazgosToggleButton.Text = "Ver detalles ▾";
        }

        private void OnZoomMasClicked(object? sender, EventArgs e) =>
            AplicarZoom(FotografiaImage.Scale + PasoZoom);

        private void OnZoomMenosClicked(object? sender, EventArgs e) =>
            AplicarZoom(FotografiaImage.Scale - PasoZoom);

        private void OnRestablecerZoomClicked(object? sender, EventArgs e) =>
            RestablecerZoom();

        private void OnImagenDobleTap(object? sender, TappedEventArgs e)
        {
            if (FotografiaImage.Scale <= ZoomMinimo + 0.05d)
                AplicarZoom(2d);
            else
                RestablecerZoom();
        }

        private void OnImagenPinchUpdated(
            object? sender,
            PinchGestureUpdatedEventArgs e)
        {
            switch (e.Status)
            {
                case GestureStatus.Started:
                    escalaInicioPinch = FotografiaImage.Scale;
                    break;

                case GestureStatus.Running:
                    AplicarZoom(
                        escalaInicioPinch * e.Scale,
                        centrarAlMinimo: false);
                    break;

                case GestureStatus.Completed:
                case GestureStatus.Canceled:
                    LimitarTraslacion();
                    ActualizarZoomPorcentaje();
                    break;
            }
        }

        private void OnImagenPanUpdated(
            object? sender,
            PanUpdatedEventArgs e)
        {
            if (FotografiaImage.Scale <= ZoomMinimo + 0.01d)
            {
                FotografiaImage.TranslationX = 0;
                FotografiaImage.TranslationY = 0;
                return;
            }

            switch (e.StatusType)
            {
                case GestureStatus.Started:
                    traslacionInicioX = FotografiaImage.TranslationX;
                    traslacionInicioY = FotografiaImage.TranslationY;
                    break;

                case GestureStatus.Running:
                    FotografiaImage.TranslationX =
                        traslacionInicioX + e.TotalX;
                    FotografiaImage.TranslationY =
                        traslacionInicioY + e.TotalY;
                    LimitarTraslacion();
                    break;

                case GestureStatus.Completed:
                case GestureStatus.Canceled:
                    LimitarTraslacion();
                    break;
            }
        }

        private void AplicarZoom(
            double escalaSolicitada,
            bool centrarAlMinimo = true)
        {
            double escala = Math.Clamp(
                escalaSolicitada,
                ZoomMinimo,
                ZoomMaximo);

            FotografiaImage.Scale = escala;

            if (centrarAlMinimo && escala <= ZoomMinimo + 0.01d)
            {
                FotografiaImage.TranslationX = 0;
                FotografiaImage.TranslationY = 0;
            }
            else
            {
                LimitarTraslacion();
            }

            ActualizarZoomPorcentaje();
        }

        private void RestablecerZoom()
        {
            FotografiaImage.Scale = ZoomMinimo;
            FotografiaImage.TranslationX = 0;
            FotografiaImage.TranslationY = 0;
            escalaInicioPinch = ZoomMinimo;
            traslacionInicioX = 0;
            traslacionInicioY = 0;
            ActualizarZoomPorcentaje();
        }

        private void LimitarTraslacion()
        {
            if (FotografiaImage.Scale <= ZoomMinimo + 0.01d)
            {
                FotografiaImage.TranslationX = 0;
                FotografiaImage.TranslationY = 0;
                return;
            }

            double ancho = ContenedorImagenGrid.Width;
            double alto = ContenedorImagenGrid.Height;

            if (ancho <= 0 || alto <= 0)
                return;

            double maximoX =
                ancho * (FotografiaImage.Scale - ZoomMinimo) / 2d;
            double maximoY =
                alto * (FotografiaImage.Scale - ZoomMinimo) / 2d;

            FotografiaImage.TranslationX = Math.Clamp(
                FotografiaImage.TranslationX,
                -maximoX,
                maximoX);
            FotografiaImage.TranslationY = Math.Clamp(
                FotografiaImage.TranslationY,
                -maximoY,
                maximoY);
        }

        private void ActualizarZoomPorcentaje()
        {
            if (ZoomPorcentajeLabel == null)
                return;

            ZoomPorcentajeLabel.Text =
                $"{Math.Round(FotografiaImage.Scale * 100d):0}%";
        }

        private void CrearLeyendaDiagnosticos(InspeccionFotoV2 foto)
        {
            DiagnosticosStack.Children.Clear();

            IReadOnlyList<InspeccionDiagnosticoVisualV2> diagnosticos =
                ObtenerDiagnosticosVigentes(foto);

            if (diagnosticos.Count == 0)
                return;

            foreach (InspeccionDiagnosticoVisualV2 diagnostico in diagnosticos)
            {
                var fila = new Grid
                {
                    ColumnSpacing = 9,
                    RowSpacing = 2
                };

                fila.ColumnDefinitions.Add(
                    new ColumnDefinition { Width = GridLength.Auto });
                fila.ColumnDefinitions.Add(
                    new ColumnDefinition { Width = GridLength.Star });

                var marcador = new Border
                {
                    WidthRequest = 13,
                    HeightRequest = 13,
                    Padding = 0,
                    Margin = new Thickness(0, 3, 0, 0),
                    StrokeThickness = 0,
                    BackgroundColor = ResolverColor(
                        diagnostico.ColorMarcador),
                    StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle
                    {
                        CornerRadius = new CornerRadius(3)
                    },
                    HorizontalOptions = LayoutOptions.Start,
                    VerticalOptions = LayoutOptions.Start
                };

                var textos = new VerticalStackLayout
                {
                    Spacing = 1
                };

                string nombre = string.IsNullOrWhiteSpace(
                    diagnostico.Diagnostico)
                        ? "Afectación sin nombre"
                        : diagnostico.Diagnostico.Trim();

                textos.Children.Add(new Label
                {
                    Text = diagnostico.EsPrincipal
                        ? $"{nombre} · Principal"
                        : nombre,
                    FontSize = 13,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Colors.White,
                    LineBreakMode = LineBreakMode.WordWrap
                });

                var detalles = new List<string>();

                if (diagnostico.TotalLesiones > 0)
                {
                    detalles.Add(
                        $"{diagnostico.TotalLesiones} " +
                        (diagnostico.TotalLesiones == 1
                            ? "región localizada"
                            : "regiones localizadas"));
                }
                else if (string.Equals(
                             diagnostico.AccionHumana,
                             "AGREGAR",
                             StringComparison.OrdinalIgnoreCase))
                {
                    detalles.Add("agregado por revisión humana");
                }

                if (!string.IsNullOrWhiteSpace(diagnostico.NivelCerteza))
                    detalles.Add($"certeza {diagnostico.NivelCerteza}");

                if (!string.IsNullOrWhiteSpace(diagnostico.Severidad))
                    detalles.Add($"severidad {diagnostico.Severidad}");

                textos.Children.Add(new Label
                {
                    Text = detalles.Count == 0
                        ? "Sin localización visual asociada."
                        : string.Join(" · ", detalles),
                    FontSize = 11,
                    TextColor = Color.FromArgb("#B7C2BE"),
                    LineBreakMode = LineBreakMode.WordWrap
                });

                string textoDiferenciales = CrearTextoDiferenciales(diagnostico);
                if (!string.IsNullOrWhiteSpace(textoDiferenciales))
                {
                    textos.Children.Add(new Label
                    {
                        Text = textoDiferenciales,
                        FontSize = 11,
                        TextColor = Color.FromArgb("#9FC9FF"),
                        LineBreakMode = LineBreakMode.WordWrap
                    });
                }

                fila.Add(marcador, 0, 0);
                fila.Add(textos, 1, 0);
                DiagnosticosStack.Children.Add(fila);
            }
        }

        private static IReadOnlyList<InspeccionDiagnosticoVisualV2>
            ObtenerDiagnosticosVigentes(InspeccionFotoV2 foto)
        {
            IEnumerable<InspeccionDiagnosticoVisualV2> origen =
                foto.UltimaAprobacion?.DiagnosticosFinales?.Count > 0
                    ? foto.UltimaAprobacion.DiagnosticosFinales
                    : foto.UltimoAnalisisHumano?.Diagnosticos?.Count > 0
                        ? foto.UltimoAnalisisHumano.Diagnosticos
                        : foto.ResultadoIA?.Diagnosticos ?? [];

            return origen
                .Where(item => !string.Equals(
                    item.AccionHumana,
                    "DESCARTAR",
                    StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        private static string CrearResumenDiagnosticos(InspeccionFotoV2 foto)
        {
            IReadOnlyList<InspeccionDiagnosticoVisualV2> diagnosticos =
                ObtenerDiagnosticosVigentes(foto);

            string origen = foto.UltimaAprobacion?.DiagnosticosFinales?.Count > 0
                ? "Clasificación final"
                : foto.UltimoAnalisisHumano?.Diagnosticos?.Count > 0
                    ? "Clasificación humana"
                    : "Valoración IA";

            if (diagnosticos.Count == 0)
            {
                return foto.ResultadoIA == null
                    ? "Sin diagnóstico preliminar"
                    : $"{origen}: sin afectaciones activas";
            }

            InspeccionDiagnosticoVisualV2 principal =
                diagnosticos.FirstOrDefault(item => item.EsPrincipal) ??
                diagnosticos[0];

            return diagnosticos.Count == 1
                ? $"{origen}: {principal.Diagnostico}"
                : $"{origen}: {principal.Diagnostico} + " +
                  $"{diagnosticos.Count - 1} afectación(es) secundaria(s)";
        }

        private static string CrearTextoLocalizacion(InspeccionFotoV2 foto)
        {
            if (foto.TieneMarcadaIA)
            {
                return "La imagen marcada es una copia derivada de esta valoración. " +
                       "La fotografía limpia permanece intacta como evidencia oficial. " +
                       "Una zona sin marca no se considera automáticamente sana: significa " +
                       "que la IA no la vinculó con suficiente certeza a una localización diagnóstica.";
            }

            if (foto.ResultadoIA?.Diagnosticos.Count > 0)
            {
                return "Esta valoración contiene diagnósticos, pero no dispone " +
                       "de una imagen marcada válida.";
            }

            return foto.TieneResultadoIA
                ? "Localización visual no disponible para esta valoración. " +
                  "Los registros anteriores continúan siendo válidos sin reprocesarse."
                : "La fotografía todavía no tiene una valoración IA.";
        }

        private static string CrearTextoDiferenciales(
            InspeccionDiagnosticoVisualV2 diagnostico)
        {
            if (diagnostico.DiagnosticosDiferenciales?.Count > 0 &&
                diagnostico.DiferencialesLocalizados?.Count > 0)
            {
                string detalle = string.Join(
                    "; ",
                    diagnostico.DiferencialesLocalizados.Select(item =>
                    {
                        string regiones = $"{item.TotalLesiones} " +
                            (item.TotalLesiones == 1
                                ? "región azul"
                                : "regiones azules");

                        string evidencia = item.Lesiones?
                            .FirstOrDefault(lesion =>
                                !string.IsNullOrWhiteSpace(lesion.Descripcion))?
                            .Descripcion?.Trim() ?? string.Empty;

                        return string.IsNullOrWhiteSpace(evidencia)
                            ? $"{item.Diagnostico} ({regiones})"
                            : $"{item.Diagnostico} ({regiones}): {evidencia}";
                    }));

                return "Diferenciales no confirmados (azul en imagen): " + detalle;
            }

            if (diagnostico.DiagnosticosDiferenciales?.Count > 0)
            {
                return "Diferenciales no confirmados · Sin localización específica: " +
                       string.Join(", ", diagnostico.DiagnosticosDiferenciales);
            }

            return string.Empty;
        }

        private static Color ResolverColor(string? hexadecimal)
        {
            if (string.IsNullOrWhiteSpace(hexadecimal))
                return Color.FromArgb("#E53935");

            try
            {
                return Color.FromArgb(hexadecimal.Trim());
            }
            catch
            {
                return Color.FromArgb("#E53935");
            }
        }

        private static ImageSource? CrearOrigenImagen(string? ruta)
        {
            if (string.IsNullOrWhiteSpace(ruta))
                return null;

            string valor = ruta.Trim();

            return Uri.TryCreate(valor, UriKind.Absolute, out Uri? uri)
                ? ImageSource.FromUri(uri)
                : ImageSource.FromFile(valor);
        }

        protected override bool OnBackButtonPressed()
        {
            _ = CerrarAsync();
            return true;
        }

        private async Task CerrarAsync()
        {
            if (cierreEnCurso)
                return;

            cierreEnCurso = true;

            try
            {
                IReadOnlyList<Page> modales = Navigation.ModalStack;

                if (modales.Count > 0 && ReferenceEquals(modales[^1], this))
                    await Navigation.PopModalAsync(animated: false);
            }
            finally
            {
                cierreEnCurso = false;
            }
        }
    }
}
