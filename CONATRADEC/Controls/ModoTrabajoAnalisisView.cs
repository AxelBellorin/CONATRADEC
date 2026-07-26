using CONATRADEC.Models;
using CONATRADEC.Services;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls.Shapes;

namespace CONATRADEC.Controls
{
    /// <summary>
    /// Selector visual insertado automáticamente al inicio de Nuevo análisis.
    /// No requiere modificar el ViewModel existente.
    /// </summary>
    public sealed class ModoTrabajoAnalisisView :
        Border
    {
        private readonly Button enLineaButton;
        private readonly Button sinConexionButton;
        private readonly Label estadoLabel;
        private readonly Label versionLabel;

        private bool activo;
        private bool preparando;
        private bool inicializado;

        public ModoTrabajoAnalisisView()
        {
            Padding = 16;
            Margin = new Thickness(0, 0, 0, 16);
            BackgroundColor =
                Color.FromArgb("#F8FAF9");
            Stroke =
                new SolidColorBrush(
                    Color.FromArgb("#D6E4DF"));
            StrokeThickness = 1;
            StrokeShape =
                new RoundRectangle
                {
                    CornerRadius =
                        new CornerRadius(16)
                };

            var title =
                new Label
                {
                    Text = "Modo del análisis",
                    FontFamily =
                        "MontserratBold",
                    FontAttributes =
                        FontAttributes.Bold,
                    FontSize = 17,
                    TextColor =
                        Color.FromArgb(
                            "#3B655B")
                };

            var description =
                new Label
                {
                    Text =
                        "Defina desde el inicio si el cálculo utilizará el servidor o el motor descargado.",
                    FontSize = 12,
                    TextColor =
                        Color.FromArgb(
                            "#4B5563"),
                    LineBreakMode =
                        LineBreakMode.WordWrap
                };

            enLineaButton =
                CrearBoton(
                    "En línea");

            sinConexionButton =
                CrearBoton(
                    "Sin conexión");

            enLineaButton.Clicked +=
                async (_, _) =>
                    await SeleccionarAsync(
                        ModoTrabajoAnalisis
                            .EnLinea);

            sinConexionButton.Clicked +=
                async (_, _) =>
                    await SeleccionarAsync(
                        ModoTrabajoAnalisis
                            .SinConexion);

            var buttons =
                new Grid
                {
                    ColumnDefinitions =
                    {
                        new ColumnDefinition(
                            GridLength.Star),
                        new ColumnDefinition(
                            GridLength.Star)
                    },
                    ColumnSpacing = 10
                };

            buttons.Add(
                enLineaButton,
                0,
                0);

            buttons.Add(
                sinConexionButton,
                1,
                0);

            estadoLabel =
                new Label
                {
                    FontSize = 12,
                    TextColor =
                        Color.FromArgb(
                            "#374151"),
                    LineBreakMode =
                        LineBreakMode.WordWrap
                };

            versionLabel =
                new Label
                {
                    FontSize = 11,
                    FontAttributes =
                        FontAttributes.Bold,
                    TextColor =
                        Color.FromArgb(
                            "#6B7280")
                };

            Content =
                new VerticalStackLayout
                {
                    Spacing = 9,
                    Children =
                    {
                        title,
                        description,
                        buttons,
                        estadoLabel,
                        versionLabel
                    }
                };
        }

        public async Task ActivarAsync()
        {
            if (preparando)
                return;

            preparando = true;

            try
            {
                if (!activo)
                {
                    ModoTrabajoAnalisisService
                        .Instance
                        .EstadoCambiado +=
                        OnEstadoCambiado;

                    EstadoConexionService
                        .Instance
                        .EstadoConexionCambiado +=
                        OnEstadoConexionCambiado;

                    MotorCalculoPaqueteService
                        .Instance
                        .PaqueteCambiado +=
                        OnPaqueteCambiado;

                    activo = true;
                }

                ModoTrabajoAnalisisEstado estado;

                if (!inicializado)
                {
                    estado =
                        await ModoTrabajoAnalisisService
                            .Instance
                            .PrepararNuevoAnalisisAsync();

                    inicializado = true;
                }
                else
                {
                    await ModoTrabajoAnalisisService
                        .Instance
                        .ActualizarDisponibilidadAsync();

                    estado =
                        ModoTrabajoAnalisisService
                            .Instance
                            .EstadoActual;
                }

                ActualizarVista(estado);
            }
            finally
            {
                preparando = false;
            }
        }

        public void Desactivar()
        {
            if (!activo)
                return;

            ModoTrabajoAnalisisService
                .Instance
                .EstadoCambiado -=
                OnEstadoCambiado;

            EstadoConexionService
                .Instance
                .EstadoConexionCambiado -=
                OnEstadoConexionCambiado;

            MotorCalculoPaqueteService
                .Instance
                .PaqueteCambiado -=
                OnPaqueteCambiado;

            activo = false;
        }

        private async Task SeleccionarAsync(
            ModoTrabajoAnalisis modo)
        {
            ModoTrabajoAnalisisEstado estado =
                await ModoTrabajoAnalisisService
                    .Instance
                    .SeleccionarModoAsync(
                        modo);

            ActualizarVista(estado);

            if (modo ==
                    ModoTrabajoAnalisis
                        .SinConexion &&
                !estado.PaqueteLocalDisponible)
            {
                await MostrarMensajeAsync(
                    "Motor no disponible",
                    estado.Mensaje);
            }
        }

        private void OnEstadoCambiado(
            object? sender,
            ModoTrabajoAnalisisEventArgs e)
        {
            MainThread.BeginInvokeOnMainThread(
                () => ActualizarVista(
                    e.Estado));
        }

        private void OnEstadoConexionCambiado(
            bool conectado)
        {
            _ = MainThread.InvokeOnMainThreadAsync(
                async () =>
                {
                    await ModoTrabajoAnalisisService
                        .Instance
                        .AsegurarModoDisponibleAsync();
                });
        }

        private void OnPaqueteCambiado(
            object? sender,
            EventArgs e)
        {
            _ = MainThread.InvokeOnMainThreadAsync(
                async () =>
                {
                    await ModoTrabajoAnalisisService
                        .Instance
                        .ActualizarDisponibilidadAsync();
                });
        }

        private void ActualizarVista(
            ModoTrabajoAnalisisEstado estado)
        {
            bool offline =
                estado.Modo ==
                ModoTrabajoAnalisis.SinConexion;

            AplicarBoton(
                enLineaButton,
                seleccionado: !offline,
                habilitado:
                    estado.InternetDisponible);

            AplicarBoton(
                sinConexionButton,
                seleccionado: offline,
                habilitado:
                    estado.PaqueteLocalDisponible);

            estadoLabel.Text =
                estado.Mensaje;

            estadoLabel.TextColor =
                offline
                    ? Color.FromArgb(
                        "#9B552C")
                    : Color.FromArgb(
                        "#374151");

            versionLabel.Text =
                estado.PaqueteLocalDisponible
                    ? $"Motor local: {estado.VersionPaquete}"
                    : "Motor local: no descargado";
        }

        private static Button CrearBoton(
            string texto) =>
            new()
            {
                Text = texto,
                HeightRequest = 46,
                CornerRadius = 12,
                FontFamily =
                    "MontserratBold",
                FontAttributes =
                    FontAttributes.Bold,
                FontSize = 13
            };

        private static void AplicarBoton(
            Button button,
            bool seleccionado,
            bool habilitado)
        {
            button.IsEnabled = habilitado;
            button.BackgroundColor =
                seleccionado
                    ? Color.FromArgb(
                        "#3B655B")
                    : Color.FromArgb(
                        habilitado
                            ? "#E8F0ED"
                            : "#E5E7EB");

            button.TextColor =
                seleccionado
                    ? Colors.White
                    : Color.FromArgb(
                        habilitado
                            ? "#3B655B"
                            : "#9CA3AF");
        }

        private static async Task MostrarMensajeAsync(
            string titulo,
            string mensaje)
        {
            Page? pagina =
                Shell.Current?
                    .CurrentPage;

            if (pagina != null)
            {
                await pagina.DisplayAlert(
                    titulo,
                    mensaje,
                    "Aceptar");
            }
        }
    }
}
