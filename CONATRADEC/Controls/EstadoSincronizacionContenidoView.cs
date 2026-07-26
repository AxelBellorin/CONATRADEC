using CONATRADEC.Models;
using CONATRADEC.Services;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;

namespace CONATRADEC.Controls
{
    /// <summary>
    /// Indicador de conexión y origen de datos para Noticias y Álbum.
    ///
    /// - Comprueba la API cada 15 segundos.
    /// - Comprueba contenido nuevo cada minuto.
    /// - Indica claramente si la vista usa servidor o SQLite.
    /// </summary>
    public sealed class EstadoSincronizacionContenidoView : ContentView
    {
        private static readonly TimeSpan IntervaloConexion =
            TimeSpan.FromSeconds(10);

        private static readonly TimeSpan IntervaloContenido =
            TimeSpan.FromMinutes(1);

        private static readonly TimeSpan EsperaReconexion =
            TimeSpan.FromSeconds(1);

        private static readonly TimeSpan SeparacionMinimaIntentos =
            TimeSpan.FromSeconds(10);

        private readonly Border container;
        private readonly Label statusPoint;
        private readonly Label titleLabel;
        private readonly Label detailLabel;
        private readonly ActivityIndicator activityIndicator;
        private readonly Button syncButton;

        private readonly SemaphoreSlim synchronizationLock =
            new(1, 1);

        private readonly SemaphoreSlim connectionLock =
            new(1, 1);

        private CancellationTokenSource? automaticCancellation;
        private bool subscribed;
        private bool active;
        private DateTime lastAutomaticAttemptUtc;
        private string module = string.Empty;

        public string Modulo
        {
            get => module;
            set
            {
                module = (value ?? string.Empty)
                    .Trim()
                    .ToLowerInvariant();

                RefreshView(
                    ContenidoEstadoService.Instance
                        .Obtener(module));
            }
        }

        public Func<bool, CancellationToken, Task>?
            SincronizarAsync { get; set; }

        public EstadoSincronizacionContenidoView()
        {
            statusPoint = new Label
            {
                Text = "●",
                FontSize = 16,
                VerticalTextAlignment = TextAlignment.Center,
                HorizontalTextAlignment = TextAlignment.Center,
                TextColor = Color.FromArgb("#6B7280")
            };

            activityIndicator = new ActivityIndicator
            {
                IsVisible = false,
                IsRunning = false,
                WidthRequest = 18,
                HeightRequest = 18,
                Color = Color.FromArgb("#3B655B")
            };

            var iconGrid = new Grid
            {
                WidthRequest = 22,
                HeightRequest = 22,
                VerticalOptions = LayoutOptions.Center
            };

            iconGrid.Children.Add(statusPoint);
            iconGrid.Children.Add(activityIndicator);

            titleLabel = new Label
            {
                Text = "Verificando estado...",
                FontFamily = "MontserratBold",
                FontAttributes = FontAttributes.Bold,
                FontSize = 12,
                TextColor = Color.FromArgb("#374151")
            };

            detailLabel = new Label
            {
                Text = "Origen pendiente de confirmar.",
                FontFamily = "MontserratMedium",
                FontSize = 10,
                TextColor = Color.FromArgb("#6B7280"),
                LineBreakMode = LineBreakMode.WordWrap
            };

            var textStack = new VerticalStackLayout
            {
                Spacing = 1,
                VerticalOptions = LayoutOptions.Center
            };

            textStack.Children.Add(titleLabel);
            textStack.Children.Add(detailLabel);

            syncButton = new Button
            {
                Text = "Sincronizar",
                FontFamily = "MontserratBold",
                FontSize = 10,
                Padding = new Thickness(10, 6),
                CornerRadius = 9,
                BackgroundColor = Color.FromArgb("#3B655B"),
                TextColor = Colors.White,
                VerticalOptions = LayoutOptions.Center,
                Command = new Command(
                    async () => await EjecutarSincronizacionAsync(
                        manual: true,
                        CancellationToken.None))
            };

            var grid = new Grid
            {
                ColumnSpacing = 9
            };

            grid.ColumnDefinitions.Add(
                new ColumnDefinition(GridLength.Auto));
            grid.ColumnDefinitions.Add(
                new ColumnDefinition(GridLength.Star));
            grid.ColumnDefinitions.Add(
                new ColumnDefinition(GridLength.Auto));

            Grid.SetColumn(iconGrid, 0);
            Grid.SetColumn(textStack, 1);
            Grid.SetColumn(syncButton, 2);

            grid.Children.Add(iconGrid);
            grid.Children.Add(textStack);
            grid.Children.Add(syncButton);

            container = new Border
            {
                Padding = new Thickness(11, 8),
                Background = new SolidColorBrush(
                    Color.FromArgb("#F8FAF9")),
                Stroke = new SolidColorBrush(
                    Color.FromArgb("#DDE7E3")),
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

        public void Activar()
        {
            if (active)
                return;

            active = true;
            Subscribe();

            automaticCancellation =
                new CancellationTokenSource();

            CancellationToken token =
                automaticCancellation.Token;

            _ = InicializarAsync(token);
            _ = EjecutarCicloConexionAsync(token);
            _ = EjecutarCicloContenidoAsync(token);
        }

        public void Desactivar()
        {
            if (!active)
                return;

            active = false;

            CancellationTokenSource? cancellation =
                automaticCancellation;

            automaticCancellation = null;

            try
            {
                cancellation?.Cancel();
            }
            catch
            {
            }
            finally
            {
                cancellation?.Dispose();
            }

            Unsubscribe();
        }

        private void OnLoaded(object? sender, EventArgs e) =>
            Activar();

        private void OnUnloaded(object? sender, EventArgs e) =>
            Desactivar();

        private async Task InicializarAsync(
            CancellationToken cancellationToken)
        {
            try
            {
                await ContenidoEstadoService.Instance
                    .CargarPersistidoAsync(Modulo);

                bool conectado =
                    await ComprobarConexionAsync(
                        cancellationToken);

                /*
                 * La acción se ejecuta aun sin conexión para que el handler
                 * entregue la copia SQLite disponible.
                 */
                await EjecutarSincronizacionAsync(
                    manual: false,
                    cancellationToken);

                if (!conectado)
                    ActualizarEstadoSinConexion();
            }
            catch (OperationCanceledException)
            {
            }
        }

        private async Task EjecutarCicloConexionAsync(
            CancellationToken cancellationToken)
        {
            try
            {
                using var timer =
                    new PeriodicTimer(IntervaloConexion);

                while (await timer.WaitForNextTickAsync(
                           cancellationToken))
                {
                    bool conectado =
                        await ComprobarConexionAsync(
                            cancellationToken);

                    if (!conectado)
                        ActualizarEstadoSinConexion();
                }
            }
            catch (OperationCanceledException)
            {
            }
        }

        private async Task EjecutarCicloContenidoAsync(
            CancellationToken cancellationToken)
        {
            try
            {
                using var timer =
                    new PeriodicTimer(IntervaloContenido);

                while (await timer.WaitForNextTickAsync(
                           cancellationToken))
                {
                    await EjecutarSincronizacionAsync(
                        manual: false,
                        cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
            }
        }

        private async Task<bool> ComprobarConexionAsync(
            CancellationToken cancellationToken)
        {
            bool entered;

            try
            {
                entered = await connectionLock.WaitAsync(
                    TimeSpan.Zero,
                    cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return false;
            }

            if (!entered)
                return EstadoConexionService.Instance.HayInternet;

            try
            {
                return await EstadoConexionApiService.Instance
                    .ComprobarAsync(
                        Modulo,
                        cancellationToken);
            }
            finally
            {
                connectionLock.Release();
            }
        }

        private async Task EjecutarSincronizacionAsync(
            bool manual,
            CancellationToken cancellationToken)
        {
            Func<bool, CancellationToken, Task>? action =
                SincronizarAsync;

            if (action == null)
                return;

            DateTime now = DateTime.UtcNow;

            if (!manual &&
                now - lastAutomaticAttemptUtc <
                SeparacionMinimaIntentos)
            {
                return;
            }

            bool entered;

            try
            {
                entered = await synchronizationLock.WaitAsync(
                    TimeSpan.Zero,
                    cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            if (!entered)
                return;

            try
            {
                if (!manual)
                    lastAutomaticAttemptUtc = DateTime.UtcNow;

                EstadoSincronizacionContenido current =
                    ContenidoEstadoService.Instance
                        .Obtener(Modulo);

                ContenidoEstadoService.Instance.Actualizar(
                    Modulo,
                    TipoEstadoSincronizacionContenido.Verificando,
                    manual
                        ? "Sincronizando..."
                        : "Verificando contenido...",
                    "Origen pendiente · " +
                    ContenidoEstadoService.ConstruirDetalleFecha(
                        current.UltimaSincronizacionUtc),
                    current.Version,
                    current.UltimaSincronizacionUtc);

                await action(manual, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                if (manual)
                    ActualizarEstadoSinConexion();
            }
            catch (Exception ex)
            {
                EstadoSincronizacionContenido current =
                    ContenidoEstadoService.Instance
                        .Obtener(Modulo);

                bool hayLocal =
                    !string.IsNullOrWhiteSpace(current.Version);

                ContenidoEstadoService.Instance.Actualizar(
                    Modulo,
                    hayLocal
                        ? TipoEstadoSincronizacionContenido.SinConexionLocal
                        : TipoEstadoSincronizacionContenido.SinDatos,
                    hayLocal
                        ? "Sin conexión · usando datos sincronizados"
                        : "Sin conexión · sin copia local",
                    hayLocal
                        ? "Datos sincronizados anteriormente · " +
                          ContenidoEstadoService.ConstruirDetalleFecha(
                              current.UltimaSincronizacionUtc)
                        : "Origen: ninguno · " + ex.Message,
                    current.Version,
                    current.UltimaSincronizacionUtc);
            }
            finally
            {
                synchronizationLock.Release();
            }
        }

        private void ActualizarEstadoSinConexion()
        {
            EstadoSincronizacionContenido current =
                ContenidoEstadoService.Instance
                    .Obtener(Modulo);

            bool hayLocal =
                !string.IsNullOrWhiteSpace(current.Version);

            ContenidoEstadoService.Instance.Actualizar(
                Modulo,
                hayLocal
                    ? TipoEstadoSincronizacionContenido.SinConexionLocal
                    : TipoEstadoSincronizacionContenido.SinDatos,
                hayLocal
                    ? "Sin conexión · usando datos sincronizados"
                    : "Sin conexión · sin copia local",
                hayLocal
                    ? "Datos sincronizados anteriormente · " +
                      ContenidoEstadoService.ConstruirDetalleFecha(
                          current.UltimaSincronizacionUtc)
                    : "Origen: ninguno · conecte el dispositivo para sincronizar.",
                current.Version,
                current.UltimaSincronizacionUtc);
        }

        private void Subscribe()
        {
            if (subscribed)
                return;

            ContenidoEstadoService.Instance.EstadoCambiado +=
                OnSyncStateChanged;

            EstadoConexionService.Instance.EstadoConexionCambiado +=
                OnConnectionChanged;

            EstadoConexionService.Instance
                .ConexionPotencialmenteRestablecida +=
                OnPossibleReconnection;

            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!subscribed)
                return;

            ContenidoEstadoService.Instance.EstadoCambiado -=
                OnSyncStateChanged;

            EstadoConexionService.Instance.EstadoConexionCambiado -=
                OnConnectionChanged;

            EstadoConexionService.Instance
                .ConexionPotencialmenteRestablecida -=
                OnPossibleReconnection;

            subscribed = false;
        }

        private void OnSyncStateChanged(
            object? sender,
            EstadoSincronizacionContenidoEventArgs e)
        {
            if (!string.Equals(
                    e.Estado.Modulo,
                    Modulo,
                    StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            MainThread.BeginInvokeOnMainThread(
                () => RefreshView(e.Estado));
        }

        private void OnConnectionChanged(bool connected)
        {
            if (!connected)
            {
                MainThread.BeginInvokeOnMainThread(
                    ActualizarEstadoSinConexion);

                return;
            }

            ProgramarReintentoPorConexion();
        }

        private void OnPossibleReconnection()
        {
            ProgramarReintentoPorConexion();
        }

        private void ProgramarReintentoPorConexion()
        {
            CancellationToken token =
                automaticCancellation?.Token ??
                CancellationToken.None;

            _ = ReintentarDespuesAsync(token);
        }

        private async Task ReintentarDespuesAsync(
            CancellationToken cancellationToken)
        {
            try
            {
                await Task.Delay(
                    EsperaReconexion,
                    cancellationToken);

                bool conectado =
                    await ComprobarConexionAsync(
                        cancellationToken);

                if (conectado)
                {
                    await EjecutarSincronizacionAsync(
                        manual: false,
                        cancellationToken);
                }
                else
                {
                    ActualizarEstadoSinConexion();
                }
            }
            catch (OperationCanceledException)
            {
            }
        }

        private void RefreshView(
            EstadoSincronizacionContenido state)
        {
            titleLabel.Text = state.Mensaje;
            detailLabel.Text = state.Detalle;

            bool synchronizing =
                state.Tipo ==
                TipoEstadoSincronizacionContenido.Verificando;

            syncButton.IsEnabled = !synchronizing;
            syncButton.Text =
                synchronizing
                    ? "Sincronizando..."
                    : "Sincronizar";

            activityIndicator.IsVisible = synchronizing;
            activityIndicator.IsRunning = synchronizing;
            statusPoint.IsVisible = !synchronizing;

            string background;
            string stroke;
            string point;

            switch (state.Tipo)
            {
                case TipoEstadoSincronizacionContenido.Servidor:
                    background = "#EEF8F2";
                    stroke = "#B7DDC5";
                    point = "#2E7D4F";
                    break;

                case TipoEstadoSincronizacionContenido.Local:
                    background = "#F3F7FF";
                    stroke = "#C9D7F2";
                    point = "#3B82F6";
                    break;

                case TipoEstadoSincronizacionContenido.SinConexionLocal:
                    background = "#FFF8E8";
                    stroke = "#F2D48A";
                    point = "#C47B00";
                    break;

                case TipoEstadoSincronizacionContenido.Error:
                case TipoEstadoSincronizacionContenido.SinDatos:
                    background = "#FFF1F1";
                    stroke = "#F2B8B8";
                    point = "#C43D3D";
                    break;

                default:
                    background = "#F8FAF9";
                    stroke = "#DDE7E3";
                    point = "#6B7280";
                    break;
            }

            container.Background = new SolidColorBrush(
                Color.FromArgb(background));

            container.Stroke = new SolidColorBrush(
                Color.FromArgb(stroke));

            statusPoint.TextColor = Color.FromArgb(point);
        }
    }
}
