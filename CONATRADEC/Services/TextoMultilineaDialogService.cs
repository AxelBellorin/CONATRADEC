using Microsoft.Maui.Devices;

namespace CONATRADEC.Services
{
    /// <summary>
    /// Diálogo reutilizable para capturar textos largos sin depender del
    /// DisplayPrompt nativo de una sola línea. Se adapta a teléfono, tablet
    /// y escritorio y mantiene el ajuste de palabras mientras el usuario escribe.
    /// </summary>
    public static class TextoMultilineaDialogService
    {
        public static async Task<string?> SolicitarAsync(
            string titulo,
            string descripcion,
            string textoAceptar,
            string textoCancelar,
            string valorInicial = "",
            int maximoCaracteres = 2000,
            int minimoCaracteres = 0)
        {
            INavigation? navegacion = Shell.Current?.Navigation;
            if (navegacion == null)
                return null;

            maximoCaracteres = Math.Max(1, maximoCaracteres);
            minimoCaracteres = Math.Clamp(
                minimoCaracteres,
                0,
                maximoCaracteres);

            var pagina = new TextoMultilineaDialogPage(
                titulo,
                descripcion,
                textoAceptar,
                textoCancelar,
                valorInicial,
                maximoCaracteres,
                minimoCaracteres);

            await navegacion.PushModalAsync(pagina);
            return await pagina.ResultadoTask;
        }

        private sealed class TextoMultilineaDialogPage : ContentPage
        {
            private readonly TaskCompletionSource<string?> resultadoSource =
                new(TaskCreationOptions.RunContinuationsAsynchronously);

            private readonly Editor editor;
            private readonly Label contadorLabel;
            private readonly Label validacionLabel;
            private readonly Button aceptarButton;
            private bool cerrando;

            public TextoMultilineaDialogPage(
                string titulo,
                string descripcion,
                string textoAceptar,
                string textoCancelar,
                string valorInicial,
                int maximoCaracteres,
                int minimoCaracteres)
            {
                Shell.SetNavBarIsVisible(this, false);
                BackgroundColor = Color.FromArgb("#66000000");

                bool telefono = DeviceInfo.Idiom == DeviceIdiom.Phone;
                double anchoMaximo = telefono ? 560 : 720;
                double altoEditor = telefono ? 170 : 210;
                double altoMaximoEditor = telefono ? 260 : 340;

                var tituloLabel = new Label
                {
                    Text = string.IsNullOrWhiteSpace(titulo)
                        ? "Detalle para la IA"
                        : titulo.Trim(),
                    FontSize = telefono ? 20 : 22,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Color.FromArgb("#263A35"),
                    LineBreakMode = LineBreakMode.WordWrap
                };

                var descripcionLabel = new Label
                {
                    Text = descripcion?.Trim() ?? string.Empty,
                    FontSize = telefono ? 13 : 14,
                    TextColor = Color.FromArgb("#4F5D59"),
                    LineBreakMode = LineBreakMode.WordWrap
                };

                editor = new Editor
                {
                    Text = valorInicial ?? string.Empty,
                    Placeholder = "Describa lo que desea que la IA observe o tenga en cuenta...",
                    MaxLength = maximoCaracteres,
                    AutoSize = EditorAutoSizeOption.TextChanges,
                    MinimumHeightRequest = altoEditor,
                    MaximumHeightRequest = altoMaximoEditor,
                    FontSize = telefono ? 14 : 15,
                    TextColor = Color.FromArgb("#263A35"),
                    BackgroundColor = Colors.White,
                    HorizontalOptions = LayoutOptions.Fill,
                    VerticalOptions = LayoutOptions.Fill
                };

                contadorLabel = new Label
                {
                    FontSize = 11,
                    TextColor = Color.FromArgb("#65736F"),
                    HorizontalTextAlignment = TextAlignment.End
                };

                validacionLabel = new Label
                {
                    FontSize = 12,
                    TextColor = Color.FromArgb("#B42318"),
                    LineBreakMode = LineBreakMode.WordWrap,
                    IsVisible = false
                };

                aceptarButton = new Button
                {
                    Text = string.IsNullOrWhiteSpace(textoAceptar)
                        ? "Aceptar"
                        : textoAceptar.Trim(),
                    HeightRequest = 46,
                    BackgroundColor = Color.FromArgb("#3B655B"),
                    TextColor = Colors.White,
                    CornerRadius = 10,
                    FontAttributes = FontAttributes.Bold,
                    HorizontalOptions = LayoutOptions.Fill
                };

                var cancelarButton = new Button
                {
                    Text = string.IsNullOrWhiteSpace(textoCancelar)
                        ? "Cancelar"
                        : textoCancelar.Trim(),
                    HeightRequest = 46,
                    BackgroundColor = Color.FromArgb("#EEF2F0"),
                    TextColor = Color.FromArgb("#263A35"),
                    CornerRadius = 10,
                    HorizontalOptions = LayoutOptions.Fill
                };

                editor.TextChanged += (_, _) =>
                {
                    ActualizarEstado(maximoCaracteres, minimoCaracteres);
                };

                aceptarButton.Clicked += async (_, _) =>
                {
                    string texto = (editor.Text ?? string.Empty).Trim();
                    if (texto.Length < minimoCaracteres)
                    {
                        validacionLabel.Text = minimoCaracteres <= 1
                            ? "Debe ingresar una descripción."
                            : $"Ingrese al menos {minimoCaracteres} caracteres.";
                        validacionLabel.IsVisible = true;
                        return;
                    }

                    await CerrarAsync(texto);
                };

                cancelarButton.Clicked += async (_, _) =>
                    await CerrarAsync(null);

                var botones = new Grid
                {
                    ColumnDefinitions =
                    {
                        new ColumnDefinition(GridLength.Star),
                        new ColumnDefinition(GridLength.Star)
                    },
                    ColumnSpacing = 10
                };
                botones.Add(cancelarButton, 0, 0);
                botones.Add(aceptarButton, 1, 0);

                var cuerpo = new VerticalStackLayout
                {
                    Spacing = 12,
                    Children =
                    {
                        tituloLabel,
                        descripcionLabel,
                        editor,
                        contadorLabel,
                        validacionLabel,
                        botones
                    }
                };

                var contenidoScroll = new ScrollView
                {
                    Content = cuerpo,
                    VerticalScrollBarVisibility = ScrollBarVisibility.Default
                };

                var tarjeta = new Border
                {
                    Padding = new Thickness(
                        telefono ? 16 : 22),
                    Margin = new Thickness(
                        telefono ? 14 : 24),
                    BackgroundColor = Colors.White,
                    Stroke = Color.FromArgb("#D6E2DE"),
                    StrokeThickness = 1,
                    StrokeShape =
                        new Microsoft.Maui.Controls.Shapes.RoundRectangle
                        {
                            CornerRadius = new CornerRadius(16)
                        },
                    MaximumWidthRequest = anchoMaximo,
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center,
                    Content = contenidoScroll
                };

                Content = new Grid
                {
                    Padding = new Thickness(0),
                    Children = { tarjeta }
                };

                SizeChanged += (_, _) =>
                {
                    if (Width > 0)
                    {
                        tarjeta.WidthRequest = Math.Min(
                            anchoMaximo,
                            Math.Max(280, Width - (telefono ? 28 : 48)));
                    }

                    /*
                     * En móvil el teclado puede reducir mucho el alto útil.
                     * El contenido interno se vuelve desplazable en lugar de
                     * ocultar los botones al final del diálogo.
                     */
                    if (Height > 0)
                    {
                        tarjeta.MaximumHeightRequest = Math.Max(
                            300,
                            Height - (telefono ? 28 : 48));
                    }
                };

                ActualizarEstado(maximoCaracteres, minimoCaracteres);

                Loaded += (_, _) =>
                    Dispatcher.Dispatch(() => editor.Focus());
            }

            public Task<string?> ResultadoTask => resultadoSource.Task;

            private void ActualizarEstado(
                int maximoCaracteres,
                int minimoCaracteres)
            {
                int cantidad = (editor.Text ?? string.Empty).Length;
                contadorLabel.Text =
                    $"{cantidad:N0} / {maximoCaracteres:N0} caracteres";

                if (validacionLabel.IsVisible &&
                    cantidad >= minimoCaracteres)
                {
                    validacionLabel.IsVisible = false;
                    validacionLabel.Text = string.Empty;
                }

                aceptarButton.IsEnabled = cantidad >= minimoCaracteres;
                aceptarButton.Opacity = aceptarButton.IsEnabled ? 1d : 0.6d;
            }

            private async Task CerrarAsync(string? resultado)
            {
                if (cerrando)
                    return;

                cerrando = true;

                /*
                 * Primero liberamos al código que espera ResultadoTask y luego
                 * cerramos visualmente el modal. En WinUI una transición modal
                 * no debe poder dejar bloqueado indefinidamente el inicio del
                 * análisis IA.
                 */
                resultadoSource.TrySetResult(resultado);

                try
                {
                    if (Navigation.ModalStack.Count > 0)
                        await Navigation.PopModalAsync(animated: false);
                }
                catch
                {
                    // El resultado ya fue entregado. Una falla visual al cerrar
                    // el modal no debe cancelar ni bloquear la operación elegida.
                }
            }

            protected override void OnDisappearing()
            {
                base.OnDisappearing();

                if (!cerrando)
                    resultadoSource.TrySetResult(null);
            }

            protected override bool OnBackButtonPressed()
            {
                _ = CerrarAsync(null);
                return true;
            }
        }
    }
}
