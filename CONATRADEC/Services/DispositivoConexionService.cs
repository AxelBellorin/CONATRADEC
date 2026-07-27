using CONATRADEC.Models;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.Networking;
using Microsoft.Maui.Storage;
using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;

namespace CONATRADEC.Services
{
    /// <summary>
    /// Reporta la instalación al portal administrativo únicamente durante una
    /// sesión global En línea.
    ///
    /// Al confirmar una sesión Sin conexión se cancela completamente el ciclo:
    /// no hay temporizador, GPS, comprobaciones de red, latidos ni intentos de
    /// desconexión contra la API.
    /// </summary>
    public sealed class DispositivoConexionService
    {
        private const string KeyInstalacionId =
            "conexion_dispositivo.instalacion_id";

        private const string KeyPermisoUbicacionSolicitado =
            "conexion_dispositivo.ubicacion_permiso_solicitado";

        private static readonly TimeSpan IntervaloLatido =
            TimeSpan.FromSeconds(45);

        private static readonly TimeSpan IntervaloUbicacion =
            TimeSpan.FromMinutes(15);

        private static readonly TimeSpan TiempoMaximoPeticion =
            TimeSpan.FromSeconds(10);

        private static readonly TimeSpan TiempoMaximoUbicacion =
            TimeSpan.FromSeconds(8);

        private static readonly Lazy<DispositivoConexionService> lazy =
            new(() => new DispositivoConexionService());

        private static readonly JsonSerializerOptions JsonOptions =
            new(JsonSerializerDefaults.Web);

        private readonly SemaphoreSlim operationLock = new(1, 1);
        private readonly object cycleLock = new();
        private readonly HttpClient httpClient;

        private CancellationTokenSource? cycleCancellation;
        private Task? cycleTask;
        private Shell? linkedShell;
        private string sessionId = Guid.NewGuid().ToString("N");
        private int? lastReportedUserId;
        private bool connectionReported;
        private bool background;
        private bool started;
        private bool locationPermissionRequestedThisRun;
        private DateTime nextLocationUtc = DateTime.MinValue;
        private LocationSnapshot location = LocationSnapshot.Empty();

        public static DispositivoConexionService Instance => lazy.Value;

        private DispositivoConexionService()
        {
            var urlApiService = new UrlApiService();

            if (!Uri.TryCreate(
                    urlApiService.BaseUrlApi,
                    UriKind.Absolute,
                    out Uri? baseAddress))
            {
                throw new InvalidOperationException(
                    "La URL configurada para la API no es válida.");
            }

            httpClient = new HttpClient
            {
                BaseAddress = baseAddress,
                Timeout = TiempoMaximoPeticion
            };
        }

        public void VincularShell(Shell shell)
        {
            ArgumentNullException.ThrowIfNull(shell);

            if (ReferenceEquals(linkedShell, shell))
                return;

            if (linkedShell != null)
                linkedShell.Navigated -= Shell_Navigated;

            linkedShell = shell;
            linkedShell.Navigated += Shell_Navigated;
        }

        public void Iniciar()
        {
            if (started)
                return;

            started = true;
            background = false;

            ModoSesionService.Instance.ModoCambiado +=
                OnSessionModeChanged;

            /*
             * La aplicación inicia mostrando el login. El ciclo no se crea
             * hasta que el usuario autentica una sesión En línea.
             */
            ApplyCurrentMode();
        }

        public async Task ReanudarAsync()
        {
            background = false;
            CreateNewLocalSession();

            if (!CanUseServer())
                return;

            StartCycleIfNeeded();
            await ActualizarEstadoActualAsync();
        }

        public async Task SuspenderAsync()
        {
            background = true;

            if (CanUseServer())
            {
                await MarcarDesconexionAsync(
                    "Aplicación en segundo plano");
            }

            StopCycle();
            CreateNewLocalSession();
        }

        public async Task DetenerAsync()
        {
            background = true;

            if (CanUseServer())
            {
                await MarcarDesconexionAsync(
                    "Aplicación cerrada");
            }

            StopCycle();

            if (linkedShell != null)
                linkedShell.Navigated -= Shell_Navigated;

            ModoSesionService.Instance.ModoCambiado -=
                OnSessionModeChanged;

            Task? pending;
            lock (cycleLock)
                pending = cycleTask;

            try
            {
                if (pending != null)
                    await pending;
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                lock (cycleLock)
                {
                    cycleCancellation?.Dispose();
                    cycleCancellation = null;
                    cycleTask = null;
                }

                started = false;
            }
        }

        public async Task ActualizarEstadoActualAsync()
        {
            if (!CanUseServer() || background)
                return;

            if (!await operationLock.WaitAsync(0))
                return;

            try
            {
                int? userId = GetVisibleUserId();

                if (!userId.HasValue)
                {
                    if (connectionReported)
                    {
                        await SendDisconnectAsync(
                            "Sesión cerrada o pantalla de acceso");
                    }

                    lastReportedUserId = null;
                    connectionReported = false;
                    return;
                }

                if (lastReportedUserId.HasValue &&
                    lastReportedUserId.Value != userId.Value)
                {
                    await SendDisconnectAsync(
                        "Cambio de usuario en la instalación");

                    CreateNewLocalSession();
                    nextLocationUtc = DateTime.MinValue;
                }

                if (!CanUseServer())
                    return;

                LocationSnapshot currentLocation =
                    await GetApproximateLocationAsync();

                var payload = new ReportarDispositivoConexionRequest
                {
                    InstalacionId = GetInstallationId(),
                    SesionId = sessionId,
                    UsuarioId = userId.Value,
                    Plataforma = GetPlatform(),
                    TipoDispositivo =
                        DeviceInfo.Current.Idiom.ToString(),
                    Fabricante =
                        DeviceInfo.Current.Manufacturer ?? string.Empty,
                    Modelo = DeviceInfo.Current.Model ?? string.Empty,
                    NombreDispositivo =
                        DeviceInfo.Current.Name ?? string.Empty,
                    SistemaOperativo = GetPlatform(),
                    VersionSistema =
                        DeviceInfo.Current.VersionString ?? string.Empty,
                    VersionApp =
                        AppInfo.Current.VersionString ?? string.Empty,
                    BuildApp =
                        AppInfo.Current.BuildString ?? string.Empty,
                    Idioma = CultureInfo.CurrentUICulture.Name,
                    TipoConexion = GetConnectionType(),
                    PaginaActual = GetCurrentPage(),
                    Latitud = currentLocation.Latitude,
                    Longitud = currentLocation.Longitude,
                    PrecisionMetros = currentLocation.AccuracyMeters,
                    FechaUbicacionUtc = currentLocation.DateUtc,
                    OrigenUbicacion = currentLocation.Source,
                    EstadoPermisoUbicacion =
                        currentLocation.PermissionStatus,
                    UbicacionSimulada = currentLocation.IsMock
                };

                using var timeout = new CancellationTokenSource(
                    TiempoMaximoPeticion);

                using HttpResponseMessage response =
                    await httpClient.PostAsJsonAsync(
                        "conectividad/dispositivos/reportar",
                        payload,
                        JsonOptions,
                        timeout.Token);

                if (!response.IsSuccessStatusCode)
                    return;

                lastReportedUserId = userId.Value;
                connectionReported = true;
            }
            catch (OperationCanceledException)
            {
            }
            catch (HttpRequestException)
            {
            }
            catch
            {
                /* La telemetría nunca debe afectar el uso de la app. */
            }
            finally
            {
                operationLock.Release();
            }
        }

        private void OnSessionModeChanged(
            object? sender,
            ModoSesionEventArgs e)
        {
            ApplyCurrentMode();
        }

        private void ApplyCurrentMode()
        {
            if (!CanUseServer())
            {
                /*
                 * Una sesión offline no informa desconexión al servidor porque
                 * hacerlo sería precisamente una solicitud de red.
                 */
                StopCycle();
                connectionReported = false;
                lastReportedUserId = null;
                return;
            }

            if (background)
                return;

            StartCycleIfNeeded();
            _ = ActualizarEstadoActualAsync();
        }

        private void StartCycleIfNeeded()
        {
            if (!CanUseServer() || background)
                return;

            lock (cycleLock)
            {
                if (cycleTask != null && !cycleTask.IsCompleted)
                    return;

                cycleCancellation?.Dispose();
                cycleCancellation = new CancellationTokenSource();
                cycleTask = RunCycleAsync(cycleCancellation.Token);
            }
        }

        private void StopCycle()
        {
            lock (cycleLock)
            {
                cycleCancellation?.Cancel();
            }
        }

        private async Task RunCycleAsync(
            CancellationToken cancellationToken)
        {
            try
            {
                using var timer = new PeriodicTimer(IntervaloLatido);

                while (await timer.WaitForNextTickAsync(
                    cancellationToken))
                {
                    if (!CanUseServer() || background)
                        break;

                    await ActualizarEstadoActualAsync();
                }
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                lock (cycleLock)
                {
                    /*
                     * Solamente limpia el ciclo que finalizó. Un ciclo nuevo
                     * creado después de un cambio de sesión no se cancela aquí.
                     */
                    if (cycleCancellation?.Token == cancellationToken)
                    {
                        cycleCancellation.Dispose();
                        cycleCancellation = null;
                        cycleTask = null;
                    }
                }
            }
        }

        private async void Shell_Navigated(
            object? sender,
            ShellNavigatedEventArgs e)
        {
            if (!CanUseServer() || background)
                return;

            await ActualizarEstadoActualAsync();
        }

        private async Task MarcarDesconexionAsync(string reason)
        {
            if (!CanUseServer() || !connectionReported)
                return;

            await SendDisconnectAsync(reason);
            connectionReported = false;
            lastReportedUserId = null;
        }

        private async Task SendDisconnectAsync(string reason)
        {
            if (!CanUseServer())
                return;

            try
            {
                var payload = new DesconectarDispositivoConexionRequest
                {
                    InstalacionId = GetInstallationId(),
                    SesionId = sessionId,
                    Motivo = reason
                };

                using var timeout = new CancellationTokenSource(
                    TiempoMaximoPeticion);

                using HttpResponseMessage response =
                    await httpClient.PostAsJsonAsync(
                        "conectividad/dispositivos/desconectar",
                        payload,
                        JsonOptions,
                        timeout.Token);
            }
            catch
            {
            }
        }

        private async Task<LocationSnapshot>
            GetApproximateLocationAsync()
        {
            if (!CanUseServer())
                return LocationSnapshot.Empty();

#if ANDROID || WINDOWS
            DateTime now = DateTime.UtcNow;
            if (now < nextLocationUtc)
                return location;

            nextLocationUtc = now.Add(IntervaloUbicacion);

            try
            {
                PermissionStatus permission =
                    await Permissions.CheckStatusAsync<
                        Permissions.LocationWhenInUse>();

                bool requestedBefore = Preferences.Get(
                    KeyPermisoUbicacionSolicitado,
                    false);

                if ((permission == PermissionStatus.Unknown ||
                     permission == PermissionStatus.Denied) &&
                    !locationPermissionRequestedThisRun &&
                    !requestedBefore)
                {
                    locationPermissionRequestedThisRun = true;
                    Preferences.Set(
                        KeyPermisoUbicacionSolicitado,
                        true);

                    permission = await Permissions.RequestAsync<
                        Permissions.LocationWhenInUse>();
                }

                if (permission != PermissionStatus.Granted)
                {
                    location = LocationSnapshot.WithoutCoordinates(
                        permission.ToString().ToUpperInvariant());
                    return location;
                }

                Location? value =
                    await Geolocation.Default
                        .GetLastKnownLocationAsync();

                if (value == null ||
                    DateTime.UtcNow - value.Timestamp.UtcDateTime >
                    TimeSpan.FromMinutes(30))
                {
                    var request = new GeolocationRequest(
                        GeolocationAccuracy.Low,
                        TiempoMaximoUbicacion);

                    using var timeout =
                        new CancellationTokenSource(
                            TiempoMaximoUbicacion);

                    value = await Geolocation.Default.GetLocationAsync(
                        request,
                        timeout.Token);
                }

                if (value == null)
                {
                    location = LocationSnapshot.WithoutCoordinates(
                        "NO_DISPONIBLE");
                    return location;
                }

                location = new LocationSnapshot
                {
                    Latitude = Math.Round(value.Latitude, 3),
                    Longitude = Math.Round(value.Longitude, 3),
                    AccuracyMeters = Math.Round(
                        Math.Max(100d, value.Accuracy ?? 1000d),
                        0),
                    DateUtc = value.Timestamp.UtcDateTime,
                    Source = "DISPOSITIVO",
                    PermissionStatus = "CONCEDIDO_APROXIMADO",
                    IsMock = value.IsFromMockProvider
                };

                return location;
            }
            catch
            {
                location = LocationSnapshot.WithoutCoordinates(
                    "NO_DISPONIBLE");
                return location;
            }
#else
            return LocationSnapshot.WithoutCoordinates("NO_APLICA");
#endif
        }

        private static bool CanUseServer() =>
            ModoSesionService.Instance.SesionConfirmada &&
            ModoSesionService.EsEnLinea;

        private static int? GetVisibleUserId()
        {
            string route = Shell.Current?
                .CurrentState?
                .Location?
                .OriginalString ?? string.Empty;

            if (route.Contains(
                    "login",
                    StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            return int.TryParse(
                Preferences.Get(
                    SessionKeys.KeyUserId,
                    string.Empty),
                out int userId) &&
                userId > 0
                    ? userId
                    : null;
        }

        private static string GetCurrentPage() =>
            Shell.Current?
                .CurrentState?
                .Location?
                .OriginalString ?? string.Empty;

        private static string GetInstallationId()
        {
            string value = Preferences.Get(
                KeyInstalacionId,
                string.Empty);

            if (!string.IsNullOrWhiteSpace(value))
                return value;

            value = Guid.NewGuid().ToString("N");
            Preferences.Set(KeyInstalacionId, value);
            return value;
        }

        private static string GetPlatform() =>
            $"{DeviceInfo.Current.Platform} " +
            $"{DeviceInfo.Current.VersionString}";

        private static string GetConnectionType()
        {
            if (Connectivity.Current.NetworkAccess ==
                NetworkAccess.None)
            {
                return "SIN_CONEXION";
            }

            IEnumerable<ConnectionProfile> profiles =
                Connectivity.Current.ConnectionProfiles;

            if (profiles.Contains(ConnectionProfile.WiFi))
                return "WIFI";

            if (profiles.Contains(ConnectionProfile.Cellular))
                return "DATOS_MOVILES";

            if (profiles.Contains(ConnectionProfile.Ethernet))
                return "ETHERNET";

            return "OTRA";
        }

        private void CreateNewLocalSession()
        {
            sessionId = Guid.NewGuid().ToString("N");
            connectionReported = false;
            lastReportedUserId = null;
        }

        private sealed class LocationSnapshot
        {
            public double? Latitude { get; init; }
            public double? Longitude { get; init; }
            public double? AccuracyMeters { get; init; }
            public DateTime? DateUtc { get; init; }
            public string Source { get; init; } = string.Empty;
            public string PermissionStatus { get; init; } = string.Empty;
            public bool? IsMock { get; init; }

            public static LocationSnapshot Empty() =>
                WithoutCoordinates("NO_REPORTADA");

            public static LocationSnapshot WithoutCoordinates(
                string status) =>
                new()
                {
                    Source = "NO_REPORTADA",
                    PermissionStatus = status
                };
        }
    }
}
