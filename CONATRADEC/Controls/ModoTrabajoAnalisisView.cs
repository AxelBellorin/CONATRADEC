using CONATRADEC.Models;
using CONATRADEC.Services;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls.Shapes;

namespace CONATRADEC.Controls
{
    /// <summary>
    /// Selector del modo de trabajo del análisis.
    ///
    /// El modo elegido y el estado de la conexión son independientes:
    /// si el análisis cambió a Sin conexión por una caída, no vuelve
    /// automáticamente a En línea. Cuando la API vuelve a responder,
    /// únicamente se habilita el botón En línea para que el usuario decida.
    /// </summary>
    public sealed class ModoTrabajoAnalisisView : Border
    {
        private readonly Button enLineaButton;
        private readonly Button sinConexionButton;
        private readonly Label estadoLabel;
        private readonly Label versionLabel;

        private readonly SemaphoreSlim reconexionLock =
            new(1, 1);

        private bool activo;
        private bool preparando;
        private bool inicializado;

        public ModoTrabajoAnalisisView()
        {
            Padding = 16;
            Margin = new Thickness(0, 0, 0, 16);
            BackgroundColor = Color.FromArgb("#F8FAF9");
            Stroke = new SolidColorBrush(
                Color.FromArgb("#D6E4DF"));
            StrokeThickness = 1;
            StrokeShape = new RoundRectangle
            {
                CornerRadius = new CornerRadius(16)
            };

            var titulo = new Label
            {
                Text = "Modo del análisis",
                FontFamily = "MontserratBold",
                FontAttributes = FontAttributes.Bold,
                FontSize = 17,
                TextColor = Color.FromArgb("#3B655B")
            };

            var descripcion = new Label
            {
                Text =
                    "Defina si este análisis utilizará el servidor o el motor descargado.",
                FontSize = 12,
                TextColor = Color.FromArgb("#4B5563"),
                LineBreakMode = LineBreakMode.WordWrap
            };

            enLineaButton = CrearBoton("En línea");
            sinConexionButton = CrearBoton("Sin conexión");

            enLineaButton.Clicked += async (_, _) =>
                await SeleccionarAsync(
                    ModoTrabajoAnalisis.EnLinea);

            sinConexionButton.Clicked += async (_, _) =>
                await SeleccionarAsync(
                    ModoTrabajoAnalisis.SinConexion);

            var botones = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition(GridLength.Star),
                    new ColumnDefinition(GridLength.Star)
                },
                ColumnSpacing = 10
            };

            botones.Add(enLineaButton, 0, 0);
            botones.Add(sinConexionButton, 1, 0);

            estadoLabel = new Label
            {
                FontSize = 12,
                TextColor = Color.FromArgb("#374151"),
                LineBreakMode = LineBreakMode.WordWrap
            };

            versionLabel = new Label
            {
                FontSize = 11,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#6B7280")
            };

            Content = new VerticalStackLayout
            {
                Spacing = 9,
                Children =
                {
                    titulo,
                    descripcion,
                    botones,
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
                Suscribir();

                ModoTrabajoAnalisisEstado estado;

                if (!inicializado)
                {
                    estado = await ModoTrabajoAnalisisService
                        .Instance
                        .PrepararNuevoAnalisisAsync();

                    inicializado = true;
                }
                else
                {
                    await ModoTrabajoAnalisisService
                        .Instance
                        .ActualizarDisponibilidadAsync();

                    estado = ModoTrabajoAnalisisService
                        .Instance
                        .EstadoActual;
                }

                ActualizarVista(estado);

                /*
                 * Al regresar a la página se realiza una comprobación real
                 * si el sistema detecta una red disponible pero la API todavía
                 * no ha sido confirmada.
                 */
                if (Microsoft.Maui.Networking.Connectivity
                        .Current.NetworkAccess !=
                    Microsoft.Maui.Networking.NetworkAccess.None &&
                    EstadoConexionService.Instance
                        .ServidorDisponibleConfirmado != true)
                {
                    _ = VerificarReconexionAsync();
                }
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

            ModoTrabajoAnalisisService.Instance.EstadoCambiado -=
                OnEstadoCambiado;

            EstadoConexionService.Instance.EstadoConexionCambiado -=
                OnEstadoConexionCambiado;

            EstadoConexionService.Instance
                .ConexionPotencialmenteRestablecida -=
                OnConexionPotencialmenteRestablecida;

            MotorCalculoPaqueteService.Instance.PaqueteCambiado -=
                OnPaqueteCambiado;

            activo = false;
        }

        private void Suscribir()
        {
            if (activo)
                return;

            ModoTrabajoAnalisisService.Instance.EstadoCambiado +=
                OnEstadoCambiado;

            EstadoConexionService.Instance.EstadoConexionCambiado +=
                OnEstadoConexionCambiado;

            /*
             * Este evento se produce cuando Windows o Android detectan que
             * volvió Wi-Fi, datos o cable. Todavía se debe confirmar la API.
             */
            EstadoConexionService.Instance
                .ConexionPotencialmenteRestablecida +=
                OnConexionPotencialmenteRestablecida;

            MotorCalculoPaqueteService.Instance.PaqueteCambiado +=
                OnPaqueteCambiado;

            activo = true;
        }

        private async Task SeleccionarAsync(
            ModoTrabajoAnalisis modo)
        {
            ModoTrabajoAnalisisEstado estado =
                await ModoTrabajoAnalisisService
                    .Instance
                    .SeleccionarModoAsync(modo);

            ActualizarVista(estado);

            if (modo == ModoTrabajoAnalisis.SinConexion &&
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
                () => ActualizarVista(e.Estado));
        }

        private void OnEstadoConexionCambiado(bool conectado)
        {
            _ = MainThread.InvokeOnMainThreadAsync(
                async () =>
                {
                    if (conectado)
                    {
                        await ModoTrabajoAnalisisService
                            .Instance
                            .ActualizarDisponibilidadAsync();
                    }
                    else
                    {
                        await ModoTrabajoAnalisisService
                            .Instance
                            .AsegurarModoDisponibleAsync();
                    }
                });
        }

        private void OnConexionPotencialmenteRestablecida()
        {
            _ = VerificarReconexionAsync();
        }

        private async Task VerificarReconexionAsync()
        {
            bool entro;

            try
            {
                entro = await reconexionLock.WaitAsync(
                    TimeSpan.Zero);
            }
            catch
            {
                return;
            }

            if (!entro)
                return;

            try
            {
                /*
                 * Se da un breve margen para que el adaptador de red termine
                 * de obtener dirección y DNS.
                 */
                await Task.Delay(800);

                /*
                 * La comprobación es pequeña y solamente valida que la API
                 * responda. Cualquier respuesta HTTP confirma conectividad.
                 * No descarga noticias ni fotografías.
                 */
                bool conectado =
                    await EstadoConexionApiService.Instance
                        .ComprobarAsync("noticias");

                if (!conectado)
                    return;

                await ModoTrabajoAnalisisService
                    .Instance
                    .ActualizarDisponibilidadAsync();
            }
            catch
            {
                /*
                 * El ciclo se volverá a ejecutar ante un nuevo cambio de red
                 * o cuando la página vuelva a aparecer.
                 */
            }
            finally
            {
                reconexionLock.Release();
            }
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
                habilitado: estado.InternetDisponible);

            AplicarBoton(
                sinConexionButton,
                seleccionado: offline,
                habilitado: estado.PaqueteLocalDisponible);

            /*
             * La conexión puede haberse restablecido y el análisis continuar
             * intencionalmente en modo local. Se informa sin cambiarlo solo.
             */
            if (offline && estado.InternetDisponible)
            {
                estadoLabel.Text =
                    "Conexión restablecida. Este análisis continuará usando " +
                    "el motor local hasta que seleccione En línea.";
            }
            else
            {
                estadoLabel.Text = estado.Mensaje;
            }

            estadoLabel.TextColor = offline
                ? Color.FromArgb("#9B552C")
                : Color.FromArgb("#374151");

            versionLabel.Text =
                estado.PaqueteLocalDisponible
                    ? $"Motor local: {estado.VersionPaquete}"
                    : "Motor local: no descargado";
        }

        private static Button CrearBoton(string texto) =>
            new()
            {
                Text = texto,
                HeightRequest = 46,
                CornerRadius = 12,
                FontFamily = "MontserratBold",
                FontAttributes = FontAttributes.Bold,
                FontSize = 13
            };

        private static void AplicarBoton(
            Button button,
            bool seleccionado,
            bool habilitado)
        {
            button.IsEnabled = habilitado;

            button.BackgroundColor = seleccionado
                ? Color.FromArgb("#3B655B")
                : Color.FromArgb(
                    habilitado
                        ? "#E8F0ED"
                        : "#E5E7EB");

            button.TextColor = seleccionado
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
            Page? pagina = Shell.Current?.CurrentPage;

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
