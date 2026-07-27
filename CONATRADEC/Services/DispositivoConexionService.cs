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
    /// Mantiene el estado de conexión de esta instalación ante la API.
    /// Reporta un latido cada 45 segundos mientras existe una sesión iniciada.
    /// La ubicación aproximada se actualiza como máximo cada 15 minutos y solo
    /// cuando el usuario concede permiso mientras la aplicación está en uso.
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

        private static readonly TimeSpan AntiguedadUbicacionCache =
            TimeSpan.FromMinutes(30);

        private static readonly TimeSpan TiempoMaximoPeticion =
            TimeSpan.FromSeconds(10);

        private static readonly TimeSpan TiempoMaximoUbicacion =
            TimeSpan.FromSeconds(8);

        private static readonly Lazy<DispositivoConexionService> lazy =
            new(() => new DispositivoConexionService());

        private static readonly JsonSerializerOptions jsonOptions =
            new(JsonSerializerDefaults.Web);

        private readonly SemaphoreSlim bloqueo = new(1, 1);
        private readonly HttpClient httpClient;

        private CancellationTokenSource? cicloCancellationTokenSource;
        private Task? tareaCiclo;
        private Shell? shellVinculado;
        private string sesionId = Guid.NewGuid().ToString("N");
        private int? ultimoUsuarioReportado;
        private bool conexionReportada;
        private bool segundoPlano;
        private bool iniciado;
        private bool permisoSolicitadoEnEstaEjecucion;
        private DateTime proximaActualizacionUbicacionUtc = DateTime.MinValue;
        private UbicacionSnapshot ubicacionActual =
            UbicacionSnapshot.NoReportada();

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

        /// <summary>
        /// Vincula el servicio a Shell para detectar inmediatamente el login,
        /// el cierre de sesión y los cambios de página.
        /// </summary>
        public void VincularShell(Shell shell)
        {
            ArgumentNullException.ThrowIfNull(shell);

            if (ReferenceEquals(shellVinculado, shell))
                return;

            if (shellVinculado != null)
                shellVinculado.Navigated -= Shell_Navigated;

            shellVinculado = shell;
            shellVinculado.Navigated += Shell_Navigated;
        }

        public void Iniciar()
        {
            if (iniciado)
                return;

            iniciado = true;
            segundoPlano = false;

            Connectivity.Current.ConnectivityChanged +=
                Connectivity_ConnectivityChanged;

            cicloCancellationTokenSource =
                new CancellationTokenSource();

            tareaCiclo = EjecutarCicloAsync(
                cicloCancellationTokenSource.Token);

            _ = ActualizarEstadoActualAsync();
        }

        public async Task ReanudarAsync()
        {
            segundoPlano = false;
            ForzarActualizacionUbicacion();
            CrearNuevaSesionLocal();
            await ActualizarEstadoActualAsync();
        }

        public async Task SuspenderAsync()
        {
            segundoPlano = true;
            await MarcarDesconexionAsync("Aplicación en segundo plano");
            CrearNuevaSesionLocal();
        }

        public async Task DetenerAsync()
        {
            segundoPlano = true;

            cicloCancellationTokenSource?.Cancel();
            await MarcarDesconexionAsync("Aplicación cerrada");

            Connectivity.Current.ConnectivityChanged -=
                Connectivity_ConnectivityChanged;

            if (shellVinculado != null)
                shellVinculado.Navigated -= Shell_Navigated;

            try
            {
                if (tareaCiclo != null)
                    await tareaCiclo;
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                cicloCancellationTokenSource?.Dispose();
                cicloCancellationTokenSource = null;
                tareaCiclo = null;
                iniciado = false;
            }
        }

        public async Task ActualizarEstadoActualAsync()
        {
            if (!await bloqueo.WaitAsync(0))
                return;

            try
            {
                if (segundoPlano)
                    return;

                (int? usuarioId, bool sesionVisible) =
                    await ObtenerSesionVisibleAsync();

                if (!usuarioId.HasValue || !sesionVisible)
                {
                    if (conexionReportada)
                    {
                        await EnviarDesconexionAsync(
                            "Sesión cerrada o pantalla de acceso");
                    }

                    ultimoUsuarioReportado = null;
                    conexionReportada = false;
                    return;
                }

                if (ultimoUsuarioReportado.HasValue &&
                    ultimoUsuarioReportado.Value != usuarioId.Value)
                {
                    await EnviarDesconexionAsync(
                        "Cambio de usuario en la instalación");

                    ForzarActualizacionUbicacion();
                    CrearNuevaSesionLocal();
                }

                if (!PuedeIntentarConexion())
                    return;

                string paginaActual = await ObtenerPaginaActualAsync();
                UbicacionSnapshot ubicacion =
                    await ObtenerUbicacionAproximadaAsync();

                var request = new ReportarDispositivoConexionRequest
                {
                    InstalacionId = ObtenerInstalacionId(),
                    SesionId = sesionId,
                    UsuarioId = usuarioId.Value,
                    Plataforma = ObtenerPlataforma(),
                    TipoDispositivo =
                        DeviceInfo.Current.Idiom.ToString(),
                    Fabricante =
                        DeviceInfo.Current.Manufacturer ?? string.Empty,
                    Modelo = DeviceInfo.Current.Model ?? string.Empty,
                    NombreDispositivo =
                        DeviceInfo.Current.Name ?? string.Empty,
                    SistemaOperativo = ObtenerPlataforma(),
                    VersionSistema =
                        DeviceInfo.Current.VersionString ?? string.Empty,
                    VersionApp =
                        AppInfo.Current.VersionString ?? string.Empty,
                    BuildApp =
                        AppInfo.Current.BuildString ?? string.Empty,
                    Idioma = CultureInfo.CurrentUICulture.Name,
                    TipoConexion = ObtenerTipoConexion(),
                    PaginaActual = paginaActual,
                    Latitud = ubicacion.Latitud,
                    Longitud = ubicacion.Longitud,
                    PrecisionMetros = ubicacion.PrecisionMetros,
                    FechaUbicacionUtc = ubicacion.FechaUbicacionUtc,
                    OrigenUbicacion = ubicacion.OrigenUbicacion,
                    EstadoPermisoUbicacion =
                        ubicacion.EstadoPermisoUbicacion,
                    UbicacionSimulada = ubicacion.UbicacionSimulada
                };

                using var timeout = new CancellationTokenSource(
                    TiempoMaximoPeticion);

                using HttpResponseMessage response =
                    await httpClient.PostAsJsonAsync(
                        "conectividad/dispositivos/reportar",
                        request,
                        jsonOptions,
                        timeout.Token);

                if (!response.IsSuccessStatusCode)
                    return;

                ultimoUsuarioReportado = usuarioId.Value;
                conexionReportada = true;
            }
            catch (OperationCanceledException)
            {
                // El siguiente latido volverá a intentarlo.
            }
            catch (HttpRequestException)
            {
                // El proceso es silencioso y no interrumpe el uso de la app.
            }
            catch
            {
                // Un fallo de telemetría nunca debe cerrar ni bloquear la app.
            }
            finally
            {
                bloqueo.Release();
            }
        }

        private async Task<UbicacionSnapshot>
            ObtenerUbicacionAproximadaAsync()
        {
#if ANDROID || WINDOWS
            DateTime ahoraUtc = DateTime.UtcNow;

            if (ahoraUtc < proximaActualizacionUbicacionUtc)
                return ubicacionActual;

            // Incluso cuando falla o se deniega, espera antes de reintentar.
            proximaActualizacionUbicacionUtc =
                ahoraUtc.Add(IntervaloUbicacion);

            try
            {
                PermissionStatus permiso =
                    await Permissions
                        .CheckStatusAsync<
                            Permissions.LocationWhenInUse>();

                bool permisoSolicitadoAnteriormente = Preferences.Get(
                    KeyPermisoUbicacionSolicitado,
                    false);

                bool estadoRequiereSolicitud =
                    permiso == PermissionStatus.Unknown ||
                    permiso == PermissionStatus.Denied;

                bool debeSolicitarPermiso =
                    estadoRequiereSolicitud &&
                    !permisoSolicitadoEnEstaEjecucion &&
                    !permisoSolicitadoAnteriormente;

                if (debeSolicitarPermiso)
                {
                    permisoSolicitadoEnEstaEjecucion = true;
                    Preferences.Set(
                        KeyPermisoUbicacionSolicitado,
                        true);
                    permiso = await SolicitarPermisoUbicacionAsync();
                }

                if (permiso != PermissionStatus.Granted)
                {
                    ubicacionActual = UbicacionSnapshot.SinCoordenadas(
                        MapearEstadoPermiso(permiso));

                    return ubicacionActual;
                }

                Location? location =
                    await Geolocation.Default
                        .GetLastKnownLocationAsync();

                if (!EsUbicacionReciente(location, ahoraUtc))
                {
                    var request = new GeolocationRequest(
                        GeolocationAccuracy.Low,
                        TiempoMaximoUbicacion);

                    using var timeout =
                        new CancellationTokenSource(
                            TiempoMaximoUbicacion);

                    location = await Geolocation.Default.GetLocationAsync(
                        request,
                        timeout.Token);
                }

                if (location == null)
                {
                    ubicacionActual = UbicacionSnapshot.SinCoordenadas(
                        "NO_DISPONIBLE");

                    return ubicacionActual;
                }

                DateTime fechaUbicacionUtc =
                    location.Timestamp == default
                        ? ahoraUtc
                        : location.Timestamp.UtcDateTime;

                if (fechaUbicacionUtc > ahoraUtc.AddMinutes(5))
                    fechaUbicacionUtc = ahoraUtc;

                double precision = location.Accuracy.HasValue
                    ? Math.Max(100d, location.Accuracy.Value)
                    : 1000d;

                // Tres decimales evitan mostrar una posición excesivamente
                // precisa: una milésima de grado equivale aproximadamente a 100 metros.
                ubicacionActual = new UbicacionSnapshot
                {
                    Latitud = Math.Round(
                        location.Latitude,
                        3,
                        MidpointRounding.AwayFromZero),
                    Longitud = Math.Round(
                        location.Longitude,
                        3,
                        MidpointRounding.AwayFromZero),
                    PrecisionMetros = Math.Round(
                        precision,
                        0,
                        MidpointRounding.AwayFromZero),
                    FechaUbicacionUtc = fechaUbicacionUtc,
                    OrigenUbicacion = "DISPOSITIVO",
                    EstadoPermisoUbicacion =
                        "CONCEDIDO_APROXIMADO",
                    UbicacionSimulada = location.IsFromMockProvider
                };

                return ubicacionActual;
            }
            catch (FeatureNotSupportedException)
            {
                ubicacionActual = UbicacionSnapshot.SinCoordenadas(
                    "NO_SOPORTADA");
            }
            catch (FeatureNotEnabledException)
            {
                ubicacionActual = UbicacionSnapshot.SinCoordenadas(
                    "SERVICIO_DESACTIVADO");
            }
            catch (PermissionException)
            {
                ubicacionActual = UbicacionSnapshot.SinCoordenadas(
                    "DENEGADO");
            }
            catch (OperationCanceledException)
            {
                ubicacionActual = UbicacionSnapshot.SinCoordenadas(
                    "TIEMPO_AGOTADO");
            }
            catch
            {
                ubicacionActual = UbicacionSnapshot.SinCoordenadas(
                    "NO_DISPONIBLE");
            }

            return ubicacionActual;
#else
            ubicacionActual = UbicacionSnapshot.SinCoordenadas("NO_APLICA");
            return ubicacionActual;
#endif
        }

#if ANDROID || WINDOWS
        private static Task<PermissionStatus>
            SolicitarPermisoUbicacionAsync()
        {
            return MainThread.InvokeOnMainThreadAsync(
                () => Permissions.RequestAsync<
                    Permissions.LocationWhenInUse>());
        }

        private static bool EsUbicacionReciente(
            Location? location,
            DateTime ahoraUtc)
        {
            if (location == null)
                return false;

            DateTime fechaUtc = location.Timestamp == default
                ? DateTime.MinValue
                : location.Timestamp.UtcDateTime;

            return fechaUtc >=
                ahoraUtc.Subtract(AntiguedadUbicacionCache);
        }

        private static string MapearEstadoPermiso(
            PermissionStatus permiso)
        {
            return permiso switch
            {
                PermissionStatus.Denied => "DENEGADO",
                PermissionStatus.Disabled => "DESACTIVADO",
                PermissionStatus.Restricted => "RESTRINGIDO",
                PermissionStatus.Limited => "LIMITADO",
                PermissionStatus.Granted => "CONCEDIDO",
                _ => "NO_SOLICITADO"
            };
        }
#endif

        private async Task MarcarDesconexionAsync(string motivo)
        {
            await bloqueo.WaitAsync();

            try
            {
                if (!conexionReportada)
                    return;

                await EnviarDesconexionAsync(motivo);
            }
            finally
            {
                conexionReportada = false;
                ultimoUsuarioReportado = null;
                bloqueo.Release();
            }
        }

        private async Task EnviarDesconexionAsync(string motivo)
        {
            if (!PuedeIntentarConexion())
                return;

            try
            {
                var request = new DesconectarDispositivoConexionRequest
                {
                    InstalacionId = ObtenerInstalacionId(),
                    SesionId = sesionId,
                    Motivo = motivo
                };

                using var timeout = new CancellationTokenSource(
                    TiempoMaximoPeticion);

                using HttpResponseMessage response =
                    await httpClient.PostAsJsonAsync(
                        "conectividad/dispositivos/desconectar",
                        request,
                        jsonOptions,
                        timeout.Token);
            }
            catch
            {
                // Al vencer la tolerancia, la API lo marcará desconectado.
            }
        }

        private async Task EjecutarCicloAsync(
            CancellationToken cancellationToken)
        {
            using var timer = new PeriodicTimer(IntervaloLatido);

            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                if (!segundoPlano)
                    await ActualizarEstadoActualAsync();
            }
        }

        private async void Shell_Navigated(
            object? sender,
            ShellNavigatedEventArgs e)
        {
            await ActualizarEstadoActualAsync();
        }

        private async void Connectivity_ConnectivityChanged(
            object? sender,
            ConnectivityChangedEventArgs e)
        {
            if (e.NetworkAccess != NetworkAccess.None)
                await ActualizarEstadoActualAsync();
        }

        private async Task<(int? UsuarioId, bool SesionVisible)>
            ObtenerSesionVisibleAsync()
        {
            string usuarioIdTexto = Preferences.Get(
                SessionKeys.KeyUserId,
                string.Empty);

            if (!int.TryParse(usuarioIdTexto, out int usuarioId) ||
                usuarioId <= 0)
            {
                return (null, false);
            }

            string pagina = await ObtenerPaginaActualAsync();

            if (string.IsNullOrWhiteSpace(pagina))
                return (usuarioId, false);

            bool esPantallaLogin = pagina.Contains(
                "login",
                StringComparison.OrdinalIgnoreCase);

            return (usuarioId, !esPantallaLogin);
        }

        private async Task<string> ObtenerPaginaActualAsync()
        {
            try
            {
                return await MainThread.InvokeOnMainThreadAsync(() =>
                    shellVinculado?
                        .CurrentState?
                        .Location?
                        .OriginalString ?? string.Empty);
            }
            catch
            {
                return string.Empty;
            }
        }

        private static bool PuedeIntentarConexion()
        {
            return Connectivity.Current.NetworkAccess !=
                NetworkAccess.None;
        }

        private static string ObtenerPlataforma()
        {
            string plataforma =
                DeviceInfo.Current.Platform.ToString();

            return plataforma.Equals(
                "WinUI",
                StringComparison.OrdinalIgnoreCase)
                    ? "Windows"
                    : plataforma;
        }

        private static string ObtenerTipoConexion()
        {
            IEnumerable<string> perfiles =
                Connectivity.Current.ConnectionProfiles
                    .Select(x => x.ToString())
                    .Distinct(StringComparer.OrdinalIgnoreCase);

            return string.Join(", ", perfiles);
        }

        private static string ObtenerInstalacionId()
        {
            string valor = Preferences.Get(
                KeyInstalacionId,
                string.Empty);

            if (Guid.TryParse(valor, out Guid guid))
                return guid.ToString("N");

            string nuevo = Guid.NewGuid().ToString("N");
            Preferences.Set(KeyInstalacionId, nuevo);
            return nuevo;
        }

        private void CrearNuevaSesionLocal()
        {
            sesionId = Guid.NewGuid().ToString("N");
            conexionReportada = false;
            ultimoUsuarioReportado = null;
        }

        private void ForzarActualizacionUbicacion()
        {
            proximaActualizacionUbicacionUtc = DateTime.MinValue;
        }

        private sealed class UbicacionSnapshot
        {
            public double? Latitud { get; init; }
            public double? Longitud { get; init; }
            public double? PrecisionMetros { get; init; }
            public DateTime? FechaUbicacionUtc { get; init; }
            public string OrigenUbicacion { get; init; } = string.Empty;
            public string EstadoPermisoUbicacion { get; init; } =
                string.Empty;
            public bool? UbicacionSimulada { get; init; }

            public static UbicacionSnapshot NoReportada()
            {
                return SinCoordenadas("NO_REPORTADO");
            }

            public static UbicacionSnapshot SinCoordenadas(string estado)
            {
                return new UbicacionSnapshot
                {
                    EstadoPermisoUbicacion = estado,
                    OrigenUbicacion = "DISPOSITIVO"
                };
            }
        }
    }
}
