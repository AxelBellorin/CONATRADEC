using CONATRADEC.Services;
using CONATRADEC.ViewModels;
using Microsoft.Maui;
using Microsoft.Maui.Controls.Shapes;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CONATRADEC.Views
{
    public partial class ResultadoAnalisisSueloPage : ContentPage
    {
        private readonly ResultadoAnalisisSueloEdicionViewModel viewModel =
            new();

        private Grid? indicadorProcesamiento;
        private Label? textoProcesamiento;
        private Button? botonContinuar;
        private CancellationTokenSource? indicadorCts;

        public ResultadoAnalisisSueloPage()
        {
            Shell.Current.FlyoutBehavior = FlyoutBehavior.Disabled;

            InitializeComponent();

            BindingContext = viewModel;

            CrearIndicadorProcesamiento();

            Loaded += ResultadoAnalisisSueloPage_Loaded;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            OcultarIndicadorProcesamiento();

            viewModel.LoadPagePermissions("ResultadoAnalisisSueloPage");

            if (!viewModel.CanView)
            {
                await GlobalService.MostrarToastAsync(
                    "No tiene permisos para ver el resultado del análisis de suelo.");

                await Shell.Current.GoToAsync("//MainPage");
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();

            CancelarVigilanciaIndicador();
            OcultarIndicadorProcesamiento();
        }

        private void ResultadoAnalisisSueloPage_Loaded(
            object? sender,
            EventArgs e)
        {
            VincularBotonContinuar();
        }

        private void CrearIndicadorProcesamiento()
        {
            View? contenidoOriginal =
                Content;

            if (contenidoOriginal == null)
                return;

            Content = null;

            Grid contenedorRaiz =
                new();

            contenedorRaiz.Children.Add(
                contenidoOriginal);

            var actividad =
                new ActivityIndicator
                {
                    IsRunning = true,
                    WidthRequest = 44,
                    HeightRequest = 44,
                    Color = Color.FromArgb("#3B655B"),
                    HorizontalOptions =
                        LayoutOptions.Center
                };

            textoProcesamiento =
                new Label
                {
                    Text =
                        "Preparando los cálculos complementarios...",
                    FontSize = 15,
                    FontAttributes =
                        FontAttributes.Bold,
                    TextColor =
                        Color.FromArgb("#1F2937"),
                    HorizontalTextAlignment =
                        TextAlignment.Center,
                    LineBreakMode =
                        LineBreakMode.WordWrap
                };

            Label detalle =
                new()
                {
                    Text =
                        "Espere un momento. La información se está preparando en el dispositivo.",
                    FontSize = 12,
                    TextColor =
                        Color.FromArgb("#6B7280"),
                    HorizontalTextAlignment =
                        TextAlignment.Center,
                    LineBreakMode =
                        LineBreakMode.WordWrap
                };

            Border tarjeta =
                new()
                {
                    BackgroundColor = Colors.White,
                    Stroke =
                        Color.FromArgb("#D1D5DB"),
                    StrokeThickness = 1,
                    StrokeShape =
                        new RoundRectangle
                        {
                            CornerRadius =
                                new CornerRadius(18)
                        },
                    Padding =
                        new Thickness(24, 20),
                    Margin = 24,
                    HorizontalOptions =
                        LayoutOptions.Center,
                    VerticalOptions =
                        LayoutOptions.Center,
                    MaximumWidthRequest = 420,
                    Content =
                        new VerticalStackLayout
                        {
                            Spacing = 12,
                            Children =
                            {
                                actividad,
                                textoProcesamiento,
                                detalle
                            }
                        }
                };

            indicadorProcesamiento =
                new Grid
                {
                    BackgroundColor =
                        Color.FromArgb("#80000000"),
                    IsVisible = false,
                    InputTransparent = false,
                    ZIndex = 1000
                };

            indicadorProcesamiento.Children.Add(
                tarjeta);

            contenedorRaiz.Children.Add(
                indicadorProcesamiento);

            Content = contenedorRaiz;
        }

        private void VincularBotonContinuar()
        {
            Button? encontrado =
                BuscarBotonContinuar(this);

            if (ReferenceEquals(
                    botonContinuar,
                    encontrado))
            {
                return;
            }

            if (botonContinuar != null)
            {
                botonContinuar.Clicked -=
                    BotonContinuar_Clicked;
            }

            botonContinuar = encontrado;

            if (botonContinuar != null)
            {
                botonContinuar.Clicked +=
                    BotonContinuar_Clicked;
            }
        }

        private void BotonContinuar_Clicked(
            object? sender,
            EventArgs e)
        {
            if (indicadorProcesamiento?.IsVisible == true)
                return;

            string mensaje =
                viewModel.TieneSeleccionCalculo
                    ? "Preparando los cálculos complementarios..."
                    : viewModel.EsModoEdicion
                        ? "Actualizando el requerimiento anual..."
                        : "Guardando el requerimiento anual...";

            MostrarIndicadorProcesamiento(
                mensaje);

            CancelarVigilanciaIndicador();

            indicadorCts =
                new CancellationTokenSource();

            _ = VigilarProcesamientoAsync(
                indicadorCts.Token);
        }

        private async Task VigilarProcesamientoAsync(
            CancellationToken cancellationToken)
        {
            try
            {
                for (int intento = 0;
                     intento < 100;
                     intento++)
                {
                    await Task.Delay(
                        100,
                        cancellationToken);

                    if (Shell.Current?.CurrentPage != this)
                        return;

                    /*
                     * Guardar únicamente el requerimiento utiliza IsBusy.
                     * Al finalizar esa operación sin navegar, se libera el
                     * indicador para permitir corregir cualquier validación.
                     */
                    if (intento > 10 &&
                        viewModel.IsBusy)
                    {
                        continue;
                    }

                    /*
                     * Si una validación mantiene al usuario en esta pantalla,
                     * el indicador se retira. La navegación normal debería
                     * completarse en mucho menos de ocho segundos offline.
                     */
                    if (intento >= 80)
                    {
                        OcultarIndicadorProcesamiento();
                        return;
                    }
                }
            }
            catch (OperationCanceledException)
            {
            }
        }

        private void MostrarIndicadorProcesamiento(
            string mensaje)
        {
            if (textoProcesamiento != null)
                textoProcesamiento.Text = mensaje;

            if (indicadorProcesamiento != null)
                indicadorProcesamiento.IsVisible = true;
        }

        private void OcultarIndicadorProcesamiento()
        {
            if (indicadorProcesamiento != null)
                indicadorProcesamiento.IsVisible = false;
        }

        private void CancelarVigilanciaIndicador()
        {
            CancellationTokenSource? anterior =
                Interlocked.Exchange(
                    ref indicadorCts,
                    null);

            if (anterior == null)
                return;

            try
            {
                anterior.Cancel();
            }
            catch (ObjectDisposedException)
            {
            }
            finally
            {
                anterior.Dispose();
            }
        }

        private Button? BuscarBotonContinuar(
            IVisualTreeElement elemento)
        {
            if (elemento is Button boton)
            {
                string texto =
                    boton.Text ??
                    string.Empty;

                if (ReferenceEquals(
                        boton.Command,
                        viewModel.ProcesarSeleccionCommand) ||
                    texto.Contains(
                        "Continuar",
                        StringComparison.OrdinalIgnoreCase) ||
                    texto.Contains(
                        "Guardar requerimiento",
                        StringComparison.OrdinalIgnoreCase) ||
                    texto.Contains(
                        "Actualizar requerimiento",
                        StringComparison.OrdinalIgnoreCase))
                {
                    return boton;
                }
            }

            foreach (IVisualTreeElement hijo
                     in elemento.GetVisualChildren())
            {
                Button? encontrado =
                    BuscarBotonContinuar(hijo);

                if (encontrado != null)
                    return encontrado;
            }

            return null;
        }
    }
}
