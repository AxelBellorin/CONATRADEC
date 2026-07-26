using CONATRADEC.Models;
using CONATRADEC.Services;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;

namespace CONATRADEC.Controls
{
    public sealed class DescargaDatosOfflineView : ContentView
    {
        private readonly Label titulo;
        private readonly Label detalle;
        private readonly ProgressBar progreso;
        private readonly Button boton;
        private readonly Border container;
        private bool subscribed;

        public DescargaDatosOfflineView()
        {
            titulo = new Label
            {
                Text = "Datos para trabajar sin conexión",
                FontFamily = "MontserratBold",
                FontAttributes = FontAttributes.Bold,
                FontSize = 12,
                TextColor = Color.FromArgb("#374151")
            };

            detalle = new Label
            {
                Text =
                    "Todavía no se han descargado todos los datos necesarios.",
                FontFamily = "MontserratMedium",
                FontSize = 10,
                TextColor = Color.FromArgb("#6B7280"),
                LineBreakMode = LineBreakMode.WordWrap
            };

            progreso = new ProgressBar
            {
                Progress = 0,
                HeightRequest = 5,
                IsVisible = false,
                ProgressColor = Color.FromArgb("#3B655B")
            };

            var texto = new VerticalStackLayout { Spacing = 3 };
            texto.Children.Add(titulo);
            texto.Children.Add(detalle);
            texto.Children.Add(progreso);

            boton = new Button
            {
                Text = "Descargar datos",
                FontFamily = "MontserratBold",
                FontSize = 10,
                Padding = new Thickness(12, 7),
                CornerRadius = 9,
                BackgroundColor = Color.FromArgb("#3B655B"),
                TextColor = Colors.White,
                Command = new Command(
                    async () => await DescargarAsync())
            };

            var grid = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition(GridLength.Star),
                    new ColumnDefinition(GridLength.Auto)
                },
                ColumnSpacing = 12
            };

            Grid.SetColumn(texto, 0);
            Grid.SetColumn(boton, 1);
            grid.Children.Add(texto);
            grid.Children.Add(boton);

            container = new Border
            {
                Padding = new Thickness(12, 9),
                Stroke = new SolidColorBrush(
                    Color.FromArgb("#C9D7F2")),
                Background = new SolidColorBrush(
                    Color.FromArgb("#F3F7FF")),
                StrokeThickness = 1,
                StrokeShape = new RoundRectangle
                {
                    CornerRadius = new CornerRadius(12)
                },
                Content = grid
            };

            Content = container;
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        private async void OnLoaded(object? sender, EventArgs e)
        {
            Suscribir();

            EstadoPaqueteOffline estado =
                await PaqueteCatalogosOfflineService.Instance
                    .ObtenerEstadoAsync();

            Actualizar(estado);

            PaqueteCatalogosOfflineService.Instance
                .VerificarActualizacionEnSegundoPlano();
        }

        private void OnUnloaded(object? sender, EventArgs e)
        {
            Desuscribir();
        }

        private async Task DescargarAsync()
        {
            boton.IsEnabled = false;

            ResultadoDescargaOffline resultado =
                await PaqueteCatalogosOfflineService.Instance
                    .DescargarTodoAsync(forzar: true);

            boton.IsEnabled = true;

            if (!resultado.Success)
            {
                Page? page =
                    Application.Current?
                        .Windows
                        .FirstOrDefault()?
                        .Page ??
                    Application.Current?.MainPage;

                if (page != null)
                {
                    await page.DisplayAlert(
                        "Descarga incompleta",
                        resultado.Message,
                        "Aceptar");
                }
            }
        }

        private void Suscribir()
        {
            if (subscribed)
                return;

            PaqueteCatalogosOfflineService.Instance.EstadoCambiado +=
                OnEstadoCambiado;

            subscribed = true;
        }

        private void Desuscribir()
        {
            if (!subscribed)
                return;

            PaqueteCatalogosOfflineService.Instance.EstadoCambiado -=
                OnEstadoCambiado;

            subscribed = false;
        }

        private void OnEstadoCambiado(
            object? sender,
            EstadoPaqueteOfflineEventArgs e)
        {
            MainThread.BeginInvokeOnMainThread(
                () => Actualizar(e.Estado));
        }

        private void Actualizar(EstadoPaqueteOffline estado)
        {
            progreso.IsVisible = estado.EstaDescargando;
            progreso.Progress = Math.Clamp(
                estado.ProgresoPorcentaje / 100d,
                0,
                1);

            boton.IsEnabled = !estado.EstaDescargando;

            if (estado.EstaDescargando)
            {
                titulo.Text =
                    $"Descargando datos ({estado.ProgresoPorcentaje}%)";
                detalle.Text = estado.Mensaje;
                boton.Text = "Descargando...";
                AplicarColores("#FFF8E8", "#F2D48A");
                return;
            }

            if (estado.HayActualizacion)
            {
                titulo.Text = "Hay datos nuevos disponibles";
                detalle.Text =
                    "Actualice la copia guardada antes de trabajar sin conexión.";
                boton.Text = "Actualizar datos";
                AplicarColores("#FFF8E8", "#F2D48A");
                return;
            }

            if (estado.TienePaqueteCompleto)
            {
                titulo.Text =
                    "Datos listos para trabajar sin conexión";

                string fecha = estado.UltimaDescargaCompletaUtc?
                    .ToLocalTime()
                    .ToString("dd/MM/yyyy h:mm tt")
                    ?? "sin fecha";

                detalle.Text =
                    $"{estado.TotalRegistros} registros guardados · " +
                    $"Última descarga: {fecha}";

                boton.Text = "Actualizar datos";
                AplicarColores("#EEF8F2", "#B7DDC5");
                return;
            }

            titulo.Text =
                "Descargar datos para trabajar sin conexión";
            detalle.Text = estado.Mensaje;
            boton.Text = "Descargar datos";
            AplicarColores("#F3F7FF", "#C9D7F2");
        }

        private void AplicarColores(string fondo, string borde)
        {
            container.Background = new SolidColorBrush(
                Color.FromArgb(fondo));
            container.Stroke = new SolidColorBrush(
                Color.FromArgb(borde));
        }
    }
}
