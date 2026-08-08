using CONATRADEC.Services;
using CONATRADEC.ViewModels;
using Microsoft.Maui;
using Microsoft.Maui.Controls.Shapes;
using System;
using System.ComponentModel;
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
        private ActivityIndicator? actividadProcesamiento;
        private Button? botonContinuar;
        private CancellationTokenSource? indicadorCts;
        private bool procesamientoDetectado;

        public ResultadoAnalisisSueloPage()
        {
            Shell.Current.FlyoutBehavior = FlyoutBehavior.Disabled;

            InitializeComponent();

            BindingContext = viewModel;

            CrearIndicadorProcesamiento();

            viewModel.PropertyChanged += ViewModel_PropertyChanged;
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
            View? contenidoOriginal = Content;

            if (contenidoOriginal == null)
                return;

            Content = null;

            Grid contenedorRaiz = new();

            contenedorRaiz.Children.Add(contenidoOriginal);

            actividadProcesamiento =
                new ActivityIndicator
                {
                    // Permanece detenido mientras el overlay está oculto.
                    IsRunning = false,
                    WidthRequest = 44,
                    HeightRequest = 44,
                    Color = Color.FromArgb("#3B655B"),
                    HorizontalOptions = LayoutOptions.Center
                };

            textoProcesamiento =
                new Label
                {
                    Text = "Preparando los cálculos complementarios...",
                    FontFamily = "MontserratBold",
                    FontSize = 15,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Color.FromArgb("#17201D"),
                    HorizontalTextAlignment = TextAlignment.Center,
                    LineBreakMode = LineBreakMode.WordWrap
                };

            Label detalle =
                new()
                {
                    Text = "Espere un momento. La información se está preparando en el dispositivo.",
                    FontFamily = "MontserratMedium",
                    FontSize = 12,
                    TextColor = Color.FromArgb("#607069"),
                    HorizontalTextAlignment = TextAlignment.Center,
                    LineBreakMode = LineBreakMode.WordWrap
                };

            Border tarjeta =
                new()
                {
                    BackgroundColor = Colors.White,
                    Stroke = Color.FromArgb("#DCE6E1"),
                    StrokeThickness = 1,
                    StrokeShape =
                        new RoundRectangle
                        {
                            CornerRadius = new CornerRadius(20)
                        },
                    Padding = new Thickness(24, 20),
                    Margin = 24,
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center,
                    MaximumWidthRequest = 420,
                    Content =
                        new VerticalStackLayout
                        {
                            Spacing = 11,
                            Children =
                            {
                                actividadProcesamiento,
                                textoProcesamiento,
                                detalle
                            }
                        }
                };

            indicadorProcesamiento =
                new Grid
                {
                    BackgroundColor = Color.FromArgb("#66000000"),
                    IsVisible = false,
                    InputTransparent = false,
                    ZIndex = 1000
                };

            indicadorProcesamiento.Children.Add(tarjeta);
            contenedorRaiz.Children.Add(indicadorProcesamiento);

            Content = contenedorRaiz;
        }

        private void VincularBotonContinuar()
        {
            Button? encontrado = BuscarBotonContinuar(this);

            if (ReferenceEquals(botonContinuar, encontrado))
                return;

            if (botonContinuar != null)
            {
                botonContinuar.Clicked -= BotonContinuar_Clicked;
            }

            botonContinuar = encontrado;

            if (botonContinuar != null)
            {
                botonContinuar.Clicked += BotonContinuar_Clicked;
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

            procesamientoDetectado = viewModel.IsBusy;
            MostrarIndicadorProcesamiento(mensaje);

            CancelarVigilanciaIndicador();

            indicadorCts = new CancellationTokenSource();

            /*
             * El comando puede validar antes de activar IsBusy. Se utiliza un
             * único fallback corto en vez de consultar el estado cada 100 ms.
             * Si el proceso real comienza, PropertyChanged se encarga del relay.
             */
            _ = OcultarSiOperacionNoInicioAsync(indicadorCts.Token);
        }

        private void ViewModel_PropertyChanged(
            object? sender,
            PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(GlobalService.IsBusy))
                return;

            if (viewModel.IsBusy)
            {
                procesamientoDetectado = true;
                return;
            }

            if (procesamientoDetectado &&
                Shell.Current?.CurrentPage == this)
            {
                Dispatcher.Dispatch(OcultarIndicadorProcesamiento);
            }
        }

        private async Task OcultarSiOperacionNoInicioAsync(
            CancellationToken cancellationToken)
        {
            try
            {
                await Task.Delay(1200, cancellationToken);

                if (Shell.Current?.CurrentPage != this)
                    return;

                if (!viewModel.IsBusy && !procesamientoDetectado)
                {
                    OcultarIndicadorProcesamiento();
                }
            }
            catch (OperationCanceledException)
            {
            }
        }

        private void MostrarIndicadorProcesamiento(string mensaje)
        {
            if (textoProcesamiento != null)
                textoProcesamiento.Text = mensaje;

            if (actividadProcesamiento != null)
                actividadProcesamiento.IsRunning = true;

            if (indicadorProcesamiento != null)
                indicadorProcesamiento.IsVisible = true;
        }

        private void OcultarIndicadorProcesamiento()
        {
            procesamientoDetectado = false;

            if (actividadProcesamiento != null)
                actividadProcesamiento.IsRunning = false;

            if (indicadorProcesamiento != null)
                indicadorProcesamiento.IsVisible = false;
        }

        private void CancelarVigilanciaIndicador()
        {
            CancellationTokenSource? anterior =
                Interlocked.Exchange(ref indicadorCts, null);

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

        private Button? BuscarBotonContinuar(IVisualTreeElement elemento)
        {
            if (elemento is Button boton)
            {
                string texto = boton.Text ?? string.Empty;

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

            foreach (IVisualTreeElement hijo in elemento.GetVisualChildren())
            {
                Button? encontrado = BuscarBotonContinuar(hijo);

                if (encontrado != null)
                    return encontrado;
            }

            return null;
        }
    }
}
