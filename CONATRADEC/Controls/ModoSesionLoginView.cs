using CONATRADEC.Models;
using CONATRADEC.Services;
using Microsoft.Maui.Controls.Shapes;

namespace CONATRADEC.Controls
{
    /// <summary>
    /// Selector global insertado en el login.
    ///
    /// La opción Sin conexión permanece oculta hasta comprobar localmente que
    /// el usuario escrito tiene credencial vigente y una descarga completa
    /// compatible con sus permisos.
    /// </summary>
    public sealed class ModoSesionLoginView : ContentView
    {
        private readonly Grid opcionesGrid;
        private readonly Border onlineBorder;
        private readonly Border offlineBorder;
        private readonly Label detalleLabel;

        private Entry? usuarioEntry;
        private CancellationTokenSource? disponibilidadCts;
        private bool suscritoModo;
        private bool offlineDisponible;
        private string disponibilidadMensaje =
            "Ingrese su usuario para comprobar si este dispositivo permite trabajar sin conexión.";

        public ModoSesionLoginView()
        {
            AutomationId = "ModoSesionLoginView";

            var titulo = new Label
            {
                Text = "¿Cómo desea trabajar?",
                FontFamily = "MontserratBold",
                FontAttributes = FontAttributes.Bold,
                FontSize = 13,
                TextColor = Color.FromArgb("#111827")
            };

            onlineBorder = CrearOpcion(
                "En línea",
                "Usar siempre el servidor",
                ModoSesion.EnLinea);

            offlineBorder = CrearOpcion(
                "Sin conexión",
                "Usar solamente datos descargados",
                ModoSesion.SinConexion);

            offlineBorder.IsVisible = false;

            opcionesGrid = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition(GridLength.Star),
                    new ColumnDefinition(new GridLength(0))
                },
                ColumnSpacing = 8
            };

            opcionesGrid.Add(onlineBorder, 0, 0);
            opcionesGrid.Add(offlineBorder, 1, 0);
            Grid.SetColumnSpan(onlineBorder, 2);

            detalleLabel = new Label
            {
                Text =
                    "Ingrese su usuario para comprobar si este dispositivo permite trabajar sin conexión.",
                FontSize = 10,
                LineBreakMode = LineBreakMode.WordWrap,
                TextColor = Color.FromArgb("#60736B")
            };

            Content = new VerticalStackLayout
            {
                Spacing = 7,
                Children =
                {
                    titulo,
                    opcionesGrid,
                    detalleLabel
                }
            };

            Loaded += OnLoaded;
            Unloaded += OnUnloaded;

            ActualizarSeleccion(
                ModoSesion.EnLinea);
        }

        private Border CrearOpcion(
            string titulo,
            string subtitulo,
            ModoSesion modo)
        {
            var icono = new Label
            {
                Text =
                    modo == ModoSesion.EnLinea
                        ? "☁"
                        : "⇩",
                FontSize = 19,
                HorizontalTextAlignment =
                    TextAlignment.Center,
                VerticalTextAlignment =
                    TextAlignment.Center,
                TextColor =
                    Color.FromArgb("#3B655B")
            };

            var tituloLabel = new Label
            {
                Text = titulo,
                FontFamily = "MontserratBold",
                FontAttributes = FontAttributes.Bold,
                FontSize = 11,
                TextColor =
                    Color.FromArgb("#111827")
            };

            var subtituloLabel = new Label
            {
                Text = subtitulo,
                FontSize = 8.5,
                LineBreakMode =
                    LineBreakMode.WordWrap,
                TextColor =
                    Color.FromArgb("#60736B")
            };

            var contenido = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition(
                        new GridLength(28)),
                    new ColumnDefinition(
                        GridLength.Star)
                },
                ColumnSpacing = 6
            };

            contenido.Add(icono, 0, 0);

            contenido.Add(
                new VerticalStackLayout
                {
                    Spacing = 1,
                    Children =
                    {
                        tituloLabel,
                        subtituloLabel
                    }
                },
                1,
                0);

            var border = new Border
            {
                Padding = new Thickness(8, 7),
                StrokeThickness = 1,
                StrokeShape = new RoundRectangle
                {
                    CornerRadius =
                        new CornerRadius(11)
                },
                Content = contenido
            };

            var tap =
                new TapGestureRecognizer();

            tap.Tapped += (_, _) =>
            {
                if (modo ==
                        ModoSesion.SinConexion &&
                    !offlineDisponible)
                {
                    return;
                }

                ModoSesionService.Instance
                    .SeleccionarParaLogin(modo);
            };

            border.GestureRecognizers.Add(tap);
            return border;
        }

        private void OnLoaded(
            object? sender,
            EventArgs e)
        {
            ModoSesionService.Instance
                .PrepararNuevoLogin();

            SuscribirModo();
            VincularUsuario();

            _ = VerificarDisponibilidadAsync(
                usuarioEntry?.Text);
        }

        private void OnUnloaded(
            object? sender,
            EventArgs e)
        {
            CancelarVerificacion();

            if (usuarioEntry != null)
            {
                usuarioEntry.TextChanged -=
                    UsuarioEntry_TextChanged;
                usuarioEntry = null;
            }

            if (suscritoModo)
            {
                ModoSesionService.Instance
                    .ModoCambiado -=
                    OnModoCambiado;
                suscritoModo = false;
            }
        }

        private void SuscribirModo()
        {
            if (suscritoModo)
                return;

            ModoSesionService.Instance
                .ModoCambiado +=
                OnModoCambiado;

            suscritoModo = true;
        }

        private void VincularUsuario()
        {
            ContentPage? page =
                EncontrarPagina();

            Entry? entry =
                page?.FindByName<Entry>(
                    "UserNameEntry");

            if (ReferenceEquals(
                    entry,
                    usuarioEntry))
            {
                return;
            }

            if (usuarioEntry != null)
            {
                usuarioEntry.TextChanged -=
                    UsuarioEntry_TextChanged;
            }

            usuarioEntry = entry;

            if (usuarioEntry != null)
            {
                usuarioEntry.TextChanged +=
                    UsuarioEntry_TextChanged;
            }
        }

        private ContentPage? EncontrarPagina()
        {
            Element? current = this;

            while (current != null)
            {
                if (current is ContentPage page)
                    return page;

                current = current.Parent;
            }

            return null;
        }

        private void UsuarioEntry_TextChanged(
            object? sender,
            TextChangedEventArgs e)
        {
            _ = VerificarDisponibilidadAsync(
                e.NewTextValue);
        }

        private async Task VerificarDisponibilidadAsync(
            string? usuario)
        {
            CancelarVerificacion();

            var cts =
                new CancellationTokenSource();

            disponibilidadCts = cts;

            try
            {
                await Task.Delay(
                    TimeSpan.FromMilliseconds(350),
                    cts.Token);

                SesionOfflineDisponibilidad resultado =
                    await SesionOfflineService.Instance
                        .ConsultarDisponibilidadAsync(
                            usuario ?? string.Empty);

                cts.Token
                    .ThrowIfCancellationRequested();

                Dispatcher.Dispatch(() =>
                    AplicarDisponibilidad(resultado));
            }
            catch (OperationCanceledException)
            {
            }
            catch
            {
                Dispatcher.Dispatch(() =>
                    AplicarDisponibilidad(
                        new SesionOfflineDisponibilidad
                        {
                            Disponible = false,
                            Message =
                                "No fue posible comprobar la preparación local. Use En línea."
                        }));
            }
            finally
            {
                if (ReferenceEquals(
                        disponibilidadCts,
                        cts))
                {
                    disponibilidadCts = null;
                }

                cts.Dispose();
            }
        }

        private void AplicarDisponibilidad(
            SesionOfflineDisponibilidad resultado)
        {
            offlineDisponible =
                resultado.Disponible;

            offlineBorder.IsVisible =
                offlineDisponible;

            opcionesGrid
                .ColumnDefinitions[1]
                .Width =
                    offlineDisponible
                        ? GridLength.Star
                        : new GridLength(0);

            Grid.SetColumnSpan(
                onlineBorder,
                offlineDisponible
                    ? 1
                    : 2);

            if (!offlineDisponible &&
                ModoSesionService.Instance
                    .ModoSolicitado ==
                    ModoSesion.SinConexion)
            {
                ModoSesionService.Instance
                    .SeleccionarParaLogin(
                        ModoSesion.EnLinea);
            }

            disponibilidadMensaje =
                string.IsNullOrWhiteSpace(
                    resultado.Message)
                    ? "Use En línea para continuar."
                    : resultado.Message;

            detalleLabel.Text =
                disponibilidadMensaje;

            ActualizarSeleccion(
                ModoSesionService.Instance
                    .ModoSolicitado);
        }

        private void CancelarVerificacion()
        {
            CancellationTokenSource? cts =
                Interlocked.Exchange(
                    ref disponibilidadCts,
                    null);

            if (cts == null)
                return;

            try
            {
                cts.Cancel();
            }
            catch
            {
            }
        }

        private void OnModoCambiado(
            object? sender,
            ModoSesionEventArgs e)
        {
            Dispatcher.Dispatch(() =>
                ActualizarSeleccion(e.Modo));
        }

        private void ActualizarSeleccion(
            ModoSesion modo)
        {
            AplicarEstado(
                onlineBorder,
                modo == ModoSesion.EnLinea);

            AplicarEstado(
                offlineBorder,
                offlineDisponible &&
                modo == ModoSesion.SinConexion);

            detalleLabel.Text =
                modo == ModoSesion.SinConexion &&
                offlineDisponible
                    ? "La sesión usará únicamente SQLite y archivos locales. No se realizará ninguna petición a la API."
                    : disponibilidadMensaje;
        }

        private static void AplicarEstado(
            Border border,
            bool seleccionado)
        {
            border.BackgroundColor =
                seleccionado
                    ? Color.FromArgb("#EEF5F2")
                    : Colors.White;

            border.Stroke =
                new SolidColorBrush(
                    Color.FromArgb(
                        seleccionado
                            ? "#3B655B"
                            : "#DCE5E1"));

            border.StrokeThickness =
                seleccionado
                    ? 2
                    : 1;
        }
    }
}
