using CONATRADEC.Models;
using Microsoft.Maui.Devices;
using Microsoft.Maui.Layouts;

namespace CONATRADEC.Views
{
    /// <summary>
    /// Presenta una fotografía durante la revisión guiada del analizador.
    /// La fotografía conserva un único estado de flujo, aunque la valoración
    /// IA pueda contener varias afectaciones independientes.
    /// </summary>
    public partial class RevisionAnalizadorFotografiaPage : ContentPage
    {
        private static readonly string[] ColoresDiagnostico =
        [
            "#E53935",
            "#43A047",
            "#FB8C00",
            "#8E24AA",
            "#00897B",
            "#6D4C41"
        ];

        private readonly IReadOnlyList<InspeccionFotoV2> fotografias;
        private readonly int indice;
        private readonly List<InspeccionDiagnosticoVisualV2>
            diagnosticosRevision = [];
        private readonly TaskCompletionSource<RevisionAnalizadorAccion>
            resultadoSource = new(
                TaskCreationOptions.RunContinuationsAsynchronously);

        private bool resultadoResuelto;
        private bool cierreEnCurso;

        public RevisionAnalizadorFotografiaPage(
            IReadOnlyList<InspeccionFotoV2> fotografias,
            int indice)
        {
            ArgumentNullException.ThrowIfNull(fotografias);

            if (fotografias.Count == 0)
            {
                throw new ArgumentException(
                    "Debe existir al menos una fotografía para iniciar la revisión.",
                    nameof(fotografias));
            }

            if (indice < 0 || indice >= fotografias.Count)
                throw new ArgumentOutOfRangeException(nameof(indice));

            this.fotografias = fotografias;
            this.indice = indice;

            InitializeComponent();
            CargarFotografia();
        }

        /// <summary>
        /// Tarea esperada por el flujo principal para conocer la decisión tomada.
        /// </summary>
        public Task<RevisionAnalizadorAccion> ResultadoTask =>
            resultadoSource.Task;

        private InspeccionFotoV2 Fotografia => fotografias[indice];

        private void CargarFotografia()
        {
            InspeccionFotoV2 foto = Fotografia;
            InspeccionFotoResultadoIAV2? resultadoIa = foto.ResultadoIA;

            TituloRevisionLabel.Text =
                $"Revisión guiada · Fotografía {indice + 1} de {fotografias.Count}";
            SubtituloFotografiaLabel.Text = foto.Titulo;
            FotografiaImage.Source = CrearOrigenImagen(foto.UrlImagen);

            DiagnosticoIaLabel.Text = resultadoIa?.DiagnosticoVisible ??
                "Sin diagnóstico preliminar de IA";

            ResumenIaLabel.Text = string.IsNullOrWhiteSpace(
                resultadoIa?.ResumenImagen)
                    ? "Revise visualmente la evidencia antes de tomar una decisión."
                    : resultadoIa!.ResumenImagen;

            var detalles = new List<string>();

            if (!string.IsNullOrWhiteSpace(resultadoIa?.NivelCerteza))
                detalles.Add($"Certeza principal: {resultadoIa.NivelCerteza}");

            if (!string.IsNullOrWhiteSpace(resultadoIa?.SeveridadVisual))
                detalles.Add($"Severidad principal: {resultadoIa.SeveridadVisual}");

            if (!string.IsNullOrWhiteSpace(resultadoIa?.CategoriaPrincipal))
            {
                detalles.Add(
                    $"Categoría principal: {resultadoIa.CategoriaPrincipal.Replace('_', ' ')}");
            }

            DetalleIaLabel.Text = string.Join(" · ", detalles);
            DetalleIaLabel.IsVisible = detalles.Count > 0;

            PrepararDiagnosticosRevision(resultadoIa);
            RenderizarDiagnosticos();

            LocalizacionIaLabel.Text = foto.TieneMarcadaIA
                ? $"La valoración IA vigente dispone de una imagen marcada (revisión {foto.VersionImagenMarcadaIA ?? resultadoIa?.VersionVisual ?? 0}). Ábrala para comprobar visualmente dónde se sustenta cada afectación."
                : foto.TieneResultadoIA
                    ? "Localización visual no disponible para esta valoración. La fotografía original continúa siendo válida y no necesita reprocesarse si corresponde a un registro anterior."
                    : "Todavía no existe una valoración IA para esta fotografía.";
        }

        private void PrepararDiagnosticosRevision(
            InspeccionFotoResultadoIAV2? resultado)
        {
            diagnosticosRevision.Clear();

            foreach (InspeccionDiagnosticoVisualV2 diagnostico in
                     resultado?.Diagnosticos ?? [])
            {
                diagnosticosRevision.Add(CopiarDiagnostico(diagnostico));
            }

            AgregarDiagnosticoButton.IsVisible = resultado != null;
        }

        private void RenderizarDiagnosticos()
        {
            DiagnosticosIaStack.Children.Clear();

            if (diagnosticosRevision.Count == 0)
            {
                DiagnosticosIaStack.Children.Add(new Label
                {
                    Text = "Esta valoración no contiene afectaciones diferenciadas. Si observa una lesión que la IA no detectó, puede agregar un diagnóstico manual.",
                    FontSize = 12,
                    TextColor = Color.FromArgb("#66736E"),
                    LineBreakMode = LineBreakMode.WordWrap
                });
                return;
            }

            DiagnosticosIaStack.Children.Add(new Label
            {
                Text = diagnosticosRevision.Count == 1
                    ? "Afectación diferenciada"
                    : $"Afectaciones diferenciadas ({diagnosticosRevision.Count})",
                FontSize = 12,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#52625D")
            });

            foreach (InspeccionDiagnosticoVisualV2 diagnostico in
                     diagnosticosRevision)
            {
                DiagnosticosIaStack.Children.Add(
                    CrearTarjetaDiagnostico(diagnostico));
            }
        }

        private View CrearTarjetaDiagnostico(
            InspeccionDiagnosticoVisualV2 diagnostico)
        {
            bool descartado = string.Equals(
                diagnostico.AccionHumana,
                "DESCARTAR",
                StringComparison.OrdinalIgnoreCase);

            var encabezado = new Grid
            {
                ColumnSpacing = 9
            };
            encabezado.ColumnDefinitions.Add(
                new ColumnDefinition { Width = GridLength.Auto });
            encabezado.ColumnDefinitions.Add(
                new ColumnDefinition { Width = GridLength.Star });

            var indicador = new Border
            {
                WidthRequest = 13,
                HeightRequest = 13,
                Padding = 0,
                Margin = new Thickness(0, 4, 0, 0),
                StrokeThickness = 0,
                BackgroundColor = ResolverColor(diagnostico.ColorMarcador),
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle
                {
                    CornerRadius = new CornerRadius(3)
                }
            };

            var textos = new VerticalStackLayout
            {
                Spacing = 2
            };

            textos.Children.Add(new Label
            {
                Text = diagnostico.EsPrincipal && !descartado
                    ? $"{diagnostico.Diagnostico} · Principal"
                    : diagnostico.Diagnostico,
                FontSize = 13,
                FontAttributes = FontAttributes.Bold,
                TextDecorations = descartado
                    ? TextDecorations.Strikethrough
                    : TextDecorations.None,
                TextColor = Color.FromArgb(descartado
                    ? "#8A9490"
                    : "#263A35"),
                LineBreakMode = LineBreakMode.WordWrap
            });

            var detalle = new List<string>
            {
                diagnostico.TotalLesiones == 1
                    ? "1 región localizada"
                    : $"{diagnostico.TotalLesiones} regiones localizadas"
            };

            if (!string.IsNullOrWhiteSpace(diagnostico.NivelCerteza))
                detalle.Add($"certeza {diagnostico.NivelCerteza}");

            if (!string.IsNullOrWhiteSpace(diagnostico.Severidad))
                detalle.Add($"severidad {diagnostico.Severidad}");

            textos.Children.Add(new Label
            {
                Text = string.Join(" · ", detalle),
                FontSize = 11,
                TextColor = Color.FromArgb("#66736E"),
                LineBreakMode = LineBreakMode.WordWrap
            });

            string textoDiferenciales = CrearTextoDiferenciales(diagnostico);
            if (!string.IsNullOrWhiteSpace(textoDiferenciales))
            {
                textos.Children.Add(new Label
                {
                    Text = textoDiferenciales,
                    FontSize = 11,
                    TextColor = Color.FromArgb("#315B86"),
                    LineBreakMode = LineBreakMode.WordWrap
                });
            }

            string estadoAccion = ObtenerTextoAccion(diagnostico);
            textos.Children.Add(new Label
            {
                Text = estadoAccion,
                FontSize = 11,
                FontAttributes = FontAttributes.Bold,
                TextColor = ResolverColorAccion(diagnostico.AccionHumana),
                LineBreakMode = LineBreakMode.WordWrap
            });

            encabezado.Add(indicador, 0, 0);
            encabezado.Add(textos, 1, 0);

            Button confirmar = CrearBotonAccion(
                "Confirmar",
                "#E7F2EE",
                "#315E52");
            confirmar.Clicked += (_, _) =>
            {
                diagnostico.AccionHumana = string.IsNullOrWhiteSpace(
                    diagnostico.IdOrigenIA)
                        ? "AGREGAR"
                        : "CONFIRMAR";
                RenderizarDiagnosticos();
            };

            Button corregir = CrearBotonAccion(
                "Corregir",
                "#FFF4EA",
                "#8A4325");
            corregir.Clicked += async (_, _) =>
                await CorregirDiagnosticoIndividualAsync(diagnostico);

            Button descartar = CrearBotonAccion(
                descartado ? "Restaurar" : "Descartar",
                descartado ? "#EEF5F2" : "#FDECEC",
                descartado ? "#315E52" : "#B42318");
            descartar.Clicked += (_, _) =>
            {
                if (descartado)
                {
                    diagnostico.AccionHumana = string.IsNullOrWhiteSpace(
                        diagnostico.IdOrigenIA)
                            ? "AGREGAR"
                            : "CONFIRMAR";
                }
                else
                {
                    bool eraPrincipal = diagnostico.EsPrincipal;
                    diagnostico.AccionHumana = "DESCARTAR";
                    diagnostico.EsPrincipal = false;

                    if (eraPrincipal)
                    {
                        Fotografia.JerarquiaAlbum = null;
                        AsignarPrincipalSiSoloQuedaUno();
                    }
                }

                RenderizarDiagnosticos();
            };

            Button principal = CrearBotonAccion(
                diagnostico.EsPrincipal && !descartado
                    ? "Principal ✓"
                    : "Hacer principal",
                "#FFF9E8",
                "#7A5A13");
            principal.IsEnabled = !descartado;
            principal.Clicked += (_, _) =>
            {
                if (descartado)
                    return;

                foreach (InspeccionDiagnosticoVisualV2 item in
                         diagnosticosRevision)
                {
                    item.EsPrincipal = ReferenceEquals(item, diagnostico);
                }

                Fotografia.JerarquiaAlbum = null;
                RenderizarDiagnosticos();
            };

            var acciones = new FlexLayout
            {
                Direction = FlexDirection.Row,
                Wrap = FlexWrap.Wrap,
                JustifyContent = FlexJustify.Start,
                AlignItems = FlexAlignItems.Center
            };

            foreach (Button boton in new[]
                     {
                         confirmar,
                         corregir,
                         descartar,
                         principal
                     })
            {
                boton.Margin = new Thickness(0, 4, 6, 0);
                acciones.Children.Add(boton);
            }

            var contenido = new VerticalStackLayout
            {
                Spacing = 7
            };
            contenido.Children.Add(encabezado);
            contenido.Children.Add(acciones);

            return new Border
            {
                Padding = new Thickness(10),
                BackgroundColor = Color.FromArgb(descartado
                    ? "#F4F5F5"
                    : "#F8FBFA"),
                Stroke = Color.FromArgb(descartado
                    ? "#D8DDDB"
                    : "#C8DED6"),
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle
                {
                    CornerRadius = new CornerRadius(10)
                },
                Content = contenido
            };
        }

        private async void OnAgregarDiagnosticoClicked(
            object? sender,
            EventArgs e)
        {
            if (diagnosticosRevision.Count >= 8)
            {
                await DisplayAlert(
                    "Límite de diagnósticos",
                    "La fotografía admite como máximo 8 diagnósticos durante la revisión humana.",
                    "Aceptar");
                return;
            }

            string? nombre = await DisplayPromptAsync(
                "Agregar diagnóstico",
                "Escriba la afectación adicional observada por el analizador.",
                "Agregar",
                "Cancelar",
                string.Empty,
                300,
                Keyboard.Text);

            if (string.IsNullOrWhiteSpace(nombre))
                return;

            bool existe = diagnosticosRevision.Any(item =>
                !string.Equals(
                    item.AccionHumana,
                    "DESCARTAR",
                    StringComparison.OrdinalIgnoreCase) &&
                string.Equals(
                    item.Diagnostico?.Trim(),
                    nombre.Trim(),
                    StringComparison.OrdinalIgnoreCase));

            if (existe)
            {
                await DisplayAlert(
                    "Diagnóstico existente",
                    "La afectación ya está incluida en esta revisión.",
                    "Aceptar");
                return;
            }

            int consecutivo = diagnosticosRevision.Count + 1;
            var nuevo = new InspeccionDiagnosticoVisualV2
            {
                Id = $"H{consecutivo}",
                IdOrigenIA = string.Empty,
                AccionHumana = "AGREGAR",
                Diagnostico = nombre.Trim(),
                Categoria = "AFECTACION_NO_DETERMINADA",
                TipoDiagnostico = string.Empty,
                EsPrincipal = diagnosticosRevision.All(item =>
                    string.Equals(
                        item.AccionHumana,
                        "DESCARTAR",
                        StringComparison.OrdinalIgnoreCase)),
                NivelCerteza = "NO_DETERMINADO",
                Severidad = "NO_EVALUABLE",
                ColorMarcador = ColoresDiagnostico[
                    diagnosticosRevision.Count % ColoresDiagnostico.Length]
            };

            if (nuevo.EsPrincipal)
            {
                foreach (InspeccionDiagnosticoVisualV2 item in
                         diagnosticosRevision)
                {
                    item.EsPrincipal = false;
                }
            }

            diagnosticosRevision.Add(nuevo);
            Fotografia.JerarquiaAlbum = null;
            RenderizarDiagnosticos();
        }

        private async Task CorregirDiagnosticoIndividualAsync(
            InspeccionDiagnosticoVisualV2 diagnostico)
        {
            if (string.Equals(
                    diagnostico.AccionHumana,
                    "DESCARTAR",
                    StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            string? correccion = await DisplayPromptAsync(
                "Corregir diagnóstico",
                "Indique el diagnóstico que corresponde a esta afectación visual.",
                "Guardar",
                "Cancelar",
                diagnostico.Diagnostico,
                300,
                Keyboard.Text);

            if (string.IsNullOrWhiteSpace(correccion))
                return;

            string anterior = diagnostico.Diagnostico;
            diagnostico.Diagnostico = correccion.Trim();
            diagnostico.AccionHumana = string.IsNullOrWhiteSpace(
                diagnostico.IdOrigenIA)
                    ? "AGREGAR"
                    : "CORREGIR";

            if (diagnostico.EsPrincipal &&
                !string.Equals(
                    anterior,
                    diagnostico.Diagnostico,
                    StringComparison.OrdinalIgnoreCase))
            {
                Fotografia.JerarquiaAlbum = null;
            }

            RenderizarDiagnosticos();
        }

        private void AsignarPrincipalSiSoloQuedaUno()
        {
            List<InspeccionDiagnosticoVisualV2> activos =
                diagnosticosRevision
                    .Where(item => !string.Equals(
                        item.AccionHumana,
                        "DESCARTAR",
                        StringComparison.OrdinalIgnoreCase))
                    .ToList();

            if (activos.Count != 1)
                return;

            foreach (InspeccionDiagnosticoVisualV2 item in diagnosticosRevision)
                item.EsPrincipal = ReferenceEquals(item, activos[0]);
        }

        private void PrepararDiagnosticosParaGuardar()
        {
            foreach (InspeccionDiagnosticoVisualV2 diagnostico in
                     diagnosticosRevision)
            {
                if (!string.IsNullOrWhiteSpace(diagnostico.AccionHumana))
                    continue;

                diagnostico.AccionHumana = string.IsNullOrWhiteSpace(
                    diagnostico.IdOrigenIA)
                        ? "AGREGAR"
                        : "CONFIRMAR";
            }

            InspeccionDiagnosticosRevisionStore.Guardar(
                Fotografia.FotografiaId,
                diagnosticosRevision);
        }

        private static InspeccionDiagnosticoVisualV2 CopiarDiagnostico(
            InspeccionDiagnosticoVisualV2 origen) =>
            new()
            {
                Id = origen.Id,
                IdOrigenIA = string.IsNullOrWhiteSpace(origen.IdOrigenIA)
                    ? origen.Id
                    : origen.IdOrigenIA,
                AccionHumana = string.Empty,
                Diagnostico = origen.Diagnostico,
                Categoria = origen.Categoria,
                TipoDiagnostico = origen.TipoDiagnostico,
                EsPrincipal = origen.EsPrincipal,
                NivelCerteza = origen.NivelCerteza,
                Severidad = origen.Severidad,
                DiagnosticosDiferenciales =
                    (origen.DiagnosticosDiferenciales ?? []).ToList(),
                DiferencialesLocalizados = (origen.DiferencialesLocalizados ?? [])
                    .Select(diferencial => new InspeccionDiferencialVisualV2
                    {
                        Diagnostico = diferencial.Diagnostico,
                        ColorMarcador = string.IsNullOrWhiteSpace(diferencial.ColorMarcador)
                            ? "#1E88E5"
                            : diferencial.ColorMarcador,
                        Lesiones = (diferencial.Lesiones ?? [])
                            .Select(lesion => new InspeccionLesionVisualV2
                            {
                                Id = lesion.Id,
                                Descripcion = lesion.Descripcion,
                                Box2d = (lesion.Box2d ?? []).ToList()
                            })
                            .ToList()
                    })
                    .ToList(),
                Lesiones = (origen.Lesiones ?? [])
                    .Select(lesion => new InspeccionLesionVisualV2
                    {
                        Id = lesion.Id,
                        Descripcion = lesion.Descripcion,
                        Box2d = (lesion.Box2d ?? []).ToList()
                    })
                    .ToList(),
                ColorMarcador = origen.ColorMarcador
            };

        private static Button CrearBotonAccion(
            string texto,
            string fondo,
            string colorTexto) =>
            new()
            {
                Text = texto,
                HeightRequest = 38,
                MinimumWidthRequest = DeviceInfo.Idiom == DeviceIdiom.Phone
                    ? 112
                    : 125,
                Padding = new Thickness(10, 5),
                CornerRadius = 9,
                FontSize = 11,
                FontAttributes = FontAttributes.Bold,
                BackgroundColor = Color.FromArgb(fondo),
                TextColor = Color.FromArgb(colorTexto)
            };

        private static string ObtenerTextoAccion(
            InspeccionDiagnosticoVisualV2 diagnostico) =>
            (diagnostico.AccionHumana ?? string.Empty).ToUpperInvariant() switch
            {
                "CONFIRMAR" => "Decisión: confirmar",
                "CORREGIR" => "Decisión: corregir",
                "DESCARTAR" => "Decisión: descartar",
                "AGREGAR" => "Decisión: agregar manualmente",
                _ => "Decisión pendiente · se confirmará si continúa sin cambios"
            };

        private static Color ResolverColorAccion(string? accion) =>
            (accion ?? string.Empty).ToUpperInvariant() switch
            {
                "CONFIRMAR" => Color.FromArgb("#315E52"),
                "CORREGIR" => Color.FromArgb("#8A4325"),
                "DESCARTAR" => Color.FromArgb("#B42318"),
                "AGREGAR" => Color.FromArgb("#7A5A13"),
                _ => Color.FromArgb("#66736E")
            };

        private async void OnAmpliarImagenTapped(
            object? sender,
            TappedEventArgs e) =>
            await AbrirVisorAsync();

        private async void OnAmpliarImagenClicked(
            object? sender,
            EventArgs e) =>
            await AbrirVisorAsync();

        private async Task AbrirVisorAsync()
        {
            var visor = new VisorFotografiaFitosanitariaPage(
                fotografias,
                indice);

            await Navigation.PushModalAsync(visor, animated: false);
        }

        private async void OnConfirmarClicked(object? sender, EventArgs e)
        {
            PrepararDiagnosticosParaGuardar();
            await CerrarAsync(RevisionAnalizadorAccion.Confirmar);
        }

        private async void OnCorregirClicked(object? sender, EventArgs e)
        {
            PrepararDiagnosticosParaGuardar();
            await CerrarAsync(RevisionAnalizadorAccion.Corregir);
        }

        private async void OnDevolverTecnicoClicked(
            object? sender,
            EventArgs e)
        {
            InspeccionDiagnosticosRevisionStore.Limpiar(
                Fotografia.FotografiaId);
            await CerrarAsync(RevisionAnalizadorAccion.DevolverTecnico);
        }

        private async void OnOmitirClicked(object? sender, EventArgs e)
        {
            InspeccionDiagnosticosRevisionStore.Limpiar(
                Fotografia.FotografiaId);
            await CerrarAsync(RevisionAnalizadorAccion.Omitir);
        }

        private async void OnCancelarClicked(object? sender, EventArgs e)
        {
            InspeccionDiagnosticosRevisionStore.Limpiar(
                Fotografia.FotografiaId);
            await CerrarAsync(RevisionAnalizadorAccion.Cancelar);
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
            InspeccionDiagnosticosRevisionStore.Limpiar(
                Fotografia.FotografiaId);
            _ = CerrarAsync(RevisionAnalizadorAccion.Cancelar);
            return true;
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();

            if (!resultadoResuelto && !Navigation.ModalStack.Contains(this))
            {
                InspeccionDiagnosticosRevisionStore.Limpiar(
                    Fotografia.FotografiaId);
                resultadoResuelto = true;
                resultadoSource.TrySetResult(
                    RevisionAnalizadorAccion.Cancelar);
            }
        }

        private async Task CerrarAsync(RevisionAnalizadorAccion accion)
        {
            if (cierreEnCurso || resultadoResuelto)
                return;

            cierreEnCurso = true;
            resultadoResuelto = true;

            try
            {
                IReadOnlyList<Page> modales = Navigation.ModalStack;

                if (modales.Count > 0 && ReferenceEquals(modales[^1], this))
                    await Navigation.PopModalAsync(animated: false);
            }
            finally
            {
                resultadoSource.TrySetResult(accion);
                cierreEnCurso = false;
            }
        }
    }
}
