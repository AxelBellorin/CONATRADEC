using CONATRADEC.Controls;
using CONATRADEC.Models;
using CONATRADEC.Services;
using CONATRADEC.ViewModels;
using System.Collections.Specialized;
using System.ComponentModel;

namespace CONATRADEC.Views
{
    /// <summary>
    /// Presentación y ejecución explícita de la recuperación de análisis IA.
    /// No modifica estados al consultar el expediente: el usuario debe confirmar
    /// la recuperación mediante el botón visible en esta misma página.
    /// </summary>
    public partial class DiagnosticoIAResultadoPage
    {
        private static readonly TimeSpan TiempoMinimoInterrupcionIA =
            TimeSpan.FromMinutes(10);

        private readonly InspeccionFitosanitariaRecuperacionApiService
            recuperacionIAApi = new();

        private Border? recuperacionIAPanel;
        private Label? recuperacionIATitulo;
        private Label? recuperacionIADetalle;
        private Button? recuperacionIAButton;
        private bool recuperacionIASuscrita;
        private bool recuperacionIAEnCurso;

        protected override void OnBindingContextChanged()
        {
            base.OnBindingContextChanged();

            if (BindingContext is not DiagnosticoIAResultadoViewModel vm)
                return;

            if (!recuperacionIASuscrita)
            {
                vm.PropertyChanged += OnRecuperacionIAViewModelPropertyChanged;
                vm.Fotografias.CollectionChanged += OnRecuperacionIACollectionChanged;
                recuperacionIASuscrita = true;
            }

            Dispatcher.Dispatch(() =>
            {
                IntegrarPanelRecuperacionIA();
                ActualizarPanelRecuperacionIA();
            });
        }

        private void IntegrarPanelRecuperacionIA()
        {
            if (recuperacionIAPanel != null)
                return;

            ScrollView? scroll = ResponsiveLayoutUtility.FindDescendant<ScrollView>(
                this,
                item => item.Content is VerticalStackLayout);

            if (scroll?.Content is not VerticalStackLayout contenido)
                return;

            recuperacionIATitulo = new Label
            {
                Text = "Análisis IA interrumpido",
                FontSize = 16,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#6B5710")
            };

            recuperacionIADetalle = new Label
            {
                FontSize = 12,
                TextColor = Color.FromArgb("#6B5710"),
                LineBreakMode = LineBreakMode.WordWrap
            };

            recuperacionIAButton = new Button
            {
                Text = "Recuperar análisis",
                HeightRequest = 44,
                MinimumWidthRequest = 180,
                Padding = new Thickness(14, 7),
                BackgroundColor = Color.FromArgb("#9B552C"),
                TextColor = Colors.White,
                CornerRadius = 10,
                HorizontalOptions = LayoutOptions.Fill
            };
            recuperacionIAButton.Clicked += OnRecuperarAnalisisIAClicked;

            var textos = new VerticalStackLayout
            {
                Spacing = 3,
                Children =
                {
                    recuperacionIATitulo,
                    recuperacionIADetalle
                }
            };

            var layout = new Grid
            {
                RowDefinitions =
                {
                    new RowDefinition(GridLength.Auto),
                    new RowDefinition(GridLength.Auto)
                },
                RowSpacing = 10
            };
            layout.Add(textos, 0, 0);
            layout.Add(recuperacionIAButton, 0, 1);

            recuperacionIAPanel = new Border
            {
                Padding = new Thickness(14),
                Margin = new Thickness(0, 0, 0, 2),
                BackgroundColor = Color.FromArgb("#FFF6E5"),
                Stroke = Color.FromArgb("#E2B93B"),
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle
                {
                    CornerRadius = new CornerRadius(13)
                },
                IsVisible = false,
                Content = layout
            };

            contenido.Children.Insert(0, recuperacionIAPanel);
        }

        private void OnRecuperacionIAViewModelPropertyChanged(
            object? sender,
            PropertyChangedEventArgs e)
        {
            if (e.PropertyName is nameof(DiagnosticoIAResultadoViewModel.Detalle)
                or nameof(DiagnosticoIAResultadoViewModel.SoloConsultaAsignacion)
                or nameof(DiagnosticoIAResultadoViewModel.IsBusy))
            {
                Dispatcher.Dispatch(ActualizarPanelRecuperacionIA);
            }
        }

        private void OnRecuperacionIACollectionChanged(
            object? sender,
            NotifyCollectionChangedEventArgs e) =>
            Dispatcher.Dispatch(ActualizarPanelRecuperacionIA);

        private void ActualizarPanelRecuperacionIA()
        {
            if (recuperacionIAPanel == null ||
                recuperacionIADetalle == null ||
                recuperacionIAButton == null)
            {
                return;
            }

            List<InspeccionFotoV2> recuperables = ObtenerFotosRecuperablesIA();
            bool puedeGestionar =
                viewModel.Detalle?.PuedeGestionarSolicitud == true &&
                viewModel.EsEtapaTecnicaAbierta &&
                !viewModel.SoloConsultaAsignacion;

            recuperacionIAPanel.IsVisible =
                puedeGestionar && recuperables.Count > 0;

            if (!recuperacionIAPanel.IsVisible)
                return;

            int conResultado = recuperables.Count(item => item.ResultadoIA != null);
            int sinResultado = recuperables.Count - conResultado;

            var partes = new List<string>();
            if (conResultado > 0)
            {
                partes.Add(conResultado == 1
                    ? "1 fotografía ya tiene un resultado IA guardado y puede consolidarse sin consumir nuevamente el proveedor"
                    : $"{conResultado} fotografías ya tienen resultado IA guardado y pueden consolidarse sin consumir nuevamente el proveedor");
            }

            if (sinResultado > 0)
            {
                partes.Add(sinResultado == 1
                    ? "1 análisis lleva más de 10 minutos sin resultado y puede liberarse como error recuperable"
                    : $"{sinResultado} análisis llevan más de 10 minutos sin resultado y pueden liberarse como error recuperable");
            }

            recuperacionIADetalle.Text = string.Join(". ", partes) + ".";
            recuperacionIAButton.Text = recuperables.Count == 1
                ? "Recuperar análisis"
                : $"Recuperar {recuperables.Count} análisis";
            recuperacionIAButton.IsEnabled =
                !recuperacionIAEnCurso && !viewModel.IsBusy;
        }

        private List<InspeccionFotoV2> ObtenerFotosRecuperablesIA() =>
            viewModel.Fotografias
                .Where(EsFotoRecuperableIA)
                .OrderBy(item => item.Orden)
                .ToList();

        private static bool EsFotoRecuperableIA(InspeccionFotoV2 foto)
        {
            if (!string.Equals(
                    foto.Estado,
                    InspeccionFotoEstados.AnalizandoIA,
                    StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (foto.ResultadoIA != null)
                return true;

            InspeccionFotoHistorialV2? inicio = (foto.Historial ?? [])
                .Where(item => string.Equals(
                    item.Accion,
                    "ANALISIS_IA_INICIADO",
                    StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(item => item.FechaUtc)
                .FirstOrDefault();

            if (inicio == null)
                return false;

            DateTime fechaUtc = inicio.FechaUtc.Kind == DateTimeKind.Utc
                ? inicio.FechaUtc
                : DateTime.SpecifyKind(inicio.FechaUtc, DateTimeKind.Utc);

            return DateTime.UtcNow - fechaUtc >= TiempoMinimoInterrupcionIA;
        }

        private async void OnRecuperarAnalisisIAClicked(
            object? sender,
            EventArgs e)
        {
            if (recuperacionIAEnCurso ||
                diagnosticoIdActual <= 0 ||
                viewModel.IsBusy)
            {
                return;
            }

            List<InspeccionFotoV2> recuperables = ObtenerFotosRecuperablesIA();
            if (recuperables.Count == 0)
            {
                ActualizarPanelRecuperacionIA();
                return;
            }

            bool confirmar = await DisplayAlert(
                "Recuperar análisis IA",
                recuperables.Any(item => item.ResultadoIA != null)
                    ? "Se consolidarán los resultados IA que ya están guardados y los intentos realmente interrumpidos quedarán disponibles para reintentar. No se ejecutará una nueva llamada a la IA durante esta recuperación."
                    : "Los intentos interrumpidos quedarán marcados como error recuperable para permitir un nuevo análisis. No se ejecutará una nueva llamada a la IA durante esta recuperación.",
                "Recuperar",
                "Cancelar");

            if (!confirmar)
                return;

            recuperacionIAEnCurso = true;
            ActualizarPanelRecuperacionIA();

            try
            {
                InspeccionOperacionMasivaV2 resultado =
                    await recuperacionIAApi.RecuperarAsync(
                        diagnosticoIdActual,
                        recuperables.Select(item => item.FotografiaId).ToArray());

                await viewModel.InicializarAsync();

                string mensaje = resultado.TotalConError == 0
                    ? "La recuperación se completó correctamente."
                    : resultado.TotalExitosas > 0
                        ? $"Se recuperaron {resultado.TotalExitosas} fotografía(s) y {resultado.TotalConError} requieren actualizar o esperar antes de reintentar."
                        : resultado.Resultados.FirstOrDefault()?.Mensaje ??
                          "No fue posible recuperar fotografías en este momento.";

                await DisplayAlert(
                    resultado.TotalExitosas > 0
                        ? "Recuperación completada"
                        : "Recuperación no aplicada",
                    mensaje,
                    "Aceptar");
            }
            catch (Exception ex)
            {
                await DisplayAlert(
                    "No fue posible recuperar el análisis",
                    string.IsNullOrWhiteSpace(ex.Message)
                        ? "Actualice el expediente e inténtelo nuevamente."
                        : ex.Message,
                    "Aceptar");
            }
            finally
            {
                recuperacionIAEnCurso = false;
                ActualizarPanelRecuperacionIA();
            }
        }
    }
}
