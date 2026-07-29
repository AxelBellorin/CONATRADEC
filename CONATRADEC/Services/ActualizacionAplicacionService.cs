using CONATRADEC.Models;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices;
using Microsoft.Maui.Storage;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;

#if ANDROID
using Android.Content;
using Android.Database;
using AndroidDownloadManager = Android.App.DownloadManager;
using AndroidDownloadStatus = Android.App.DownloadStatus;
using AndroidDownloadVisibility = Android.App.DownloadVisibility;
using AndroidEnvironment = Android.OS.Environment;
#endif

#if WINDOWS
using System.Runtime.InteropServices;
using Windows.Networking.BackgroundTransfer;
using WindowsPackage = Windows.ApplicationModel.Package;
using Windows.Storage;
#endif

namespace CONATRADEC.Services
{
    /// <summary>
    /// Consulta, descarga y valida las versiones publicadas.
    ///
    /// Android utiliza DownloadManager para que la transferencia continúe al
    /// minimizar la aplicación, cambiar de pantalla o perder temporalmente la
    /// conexión.
    ///
    /// Windows utiliza BackgroundDownloader cuando la aplicación está
    /// empaquetada como MSIX. En una ejecución Debug sin identidad de paquete se
    /// utiliza HttpClient como respaldo.
    /// </summary>
    public sealed class ActualizacionAplicacionService
    {
        private const string PreferenciaCanal =
            "Actualizaciones.Canal";

#if ANDROID
        private const string AndroidIdDescarga =
            "Actualizaciones.Android.IdDescarga";

        private const string AndroidIdActualizacion =
            "Actualizaciones.Android.IdActualizacion";

        private const string AndroidRutaDescarga =
            "Actualizaciones.Android.RutaDescarga";
#endif

#if WINDOWS
        private const string WindowsGuidDescarga =
            "Actualizaciones.Windows.GuidDescarga";

        private const string WindowsIdActualizacion =
            "Actualizaciones.Windows.IdActualizacion";
#endif

        private static string CanalPredeterminado
        {
            get
            {
#if DEBUG
                return "PRUEBAS";
#else
                return "PRODUCCION";
#endif
            }
        }

        private static readonly Lazy<ActualizacionAplicacionService>
            instancia =
                new(() => new ActualizacionAplicacionService());

        private readonly HttpClient httpClient;

        private readonly JsonSerializerOptions jsonOptions =
            new(JsonSerializerDefaults.Web);

        public static ActualizacionAplicacionService Instance =>
            instancia.Value;

        private ActualizacionAplicacionService()
        {
            string baseUrl =
                new UrlApiService().BaseUrlApi;

            if (!Uri.TryCreate(
                    baseUrl,
                    UriKind.Absolute,
                    out Uri? uri))
            {
                throw new InvalidOperationException(
                    "La URL configurada para la API no es válida.");
            }

            httpClient =
                new HttpClient
                {
                    BaseAddress = uri,
                    Timeout = Timeout.InfiniteTimeSpan
                };
        }

        public bool PlataformaCompatible =>
            ObtenerPlataforma() is not null;

        public async Task<ActualizacionDisponible?>
            ComprobarActualizacionAsync(
                CancellationToken cancellationToken = default)
        {
            string? plataforma =
                ObtenerPlataforma();

            if (plataforma is null)
                return null;

            long versionCodigo =
                ObtenerVersionCodigoInstalada();

            string canal =
                Preferences.Get(
                    PreferenciaCanal,
                    CanalPredeterminado);

            string ruta =
                "api/actualizaciones/comprobar" +
                $"?plataforma={Uri.EscapeDataString(plataforma)}" +
                $"&versionCodigo={versionCodigo}" +
                $"&canal={Uri.EscapeDataString(canal)}";

            using HttpResponseMessage response =
                await httpClient.GetAsync(
                    ruta,
                    cancellationToken);

            string contenido =
                await response.Content.ReadAsStringAsync(
                    cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException(
                    ExtraerMensaje(contenido),
                    null,
                    response.StatusCode);
            }

            RespuestaComprobacionActualizacion? resultado =
                JsonSerializer.Deserialize<
                    RespuestaComprobacionActualizacion>(
                        contenido,
                        jsonOptions);

            return resultado?.ActualizacionDisponible == true
                ? resultado.Data
                : null;
        }

        /// <summary>
        /// Inicia o recupera una transferencia administrada por el sistema.
        /// </summary>
        public async Task<string> DescargarEnSegundoPlanoAsync(
            ActualizacionDisponible actualizacion,
            IProgress<ProgresoDescargaActualizacion>? progreso = null,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(actualizacion);

            Uri url =
                ResolverUrlDescarga(actualizacion);

#if ANDROID
            return await DescargarAndroidAsync(
                actualizacion,
                url,
                progreso,
                cancellationToken);
#elif WINDOWS
            if (TieneIdentidadPaqueteWindows())
            {
                return await DescargarWindowsAsync(
                    actualizacion,
                    url,
                    progreso,
                    cancellationToken);
            }

            return await DescargarHttpAsync(
                actualizacion,
                url,
                progreso,
                cancellationToken,
                enSegundoPlano: false);
#else
            return await DescargarHttpAsync(
                actualizacion,
                url,
                progreso,
                cancellationToken,
                enSegundoPlano: false);
#endif
        }

        /*
         * Mantiene compatibilidad con cualquier código anterior que todavía
         * utilice progreso numérico.
         */
        public async Task<string> DescargarAsync(
            ActualizacionDisponible actualizacion,
            IProgress<double>? progreso = null,
            CancellationToken cancellationToken = default)
        {
            IProgress<ProgresoDescargaActualizacion>? adaptador =
                progreso is null
                    ? null
                    : new Progress<ProgresoDescargaActualizacion>(
                        valor =>
                            progreso.Report(
                                valor.Porcentaje));

            return await DescargarEnSegundoPlanoAsync(
                actualizacion,
                adaptador,
                cancellationToken);
        }

#if ANDROID
        private async Task<string> DescargarAndroidAsync(
            ActualizacionDisponible actualizacion,
            Uri url,
            IProgress<ProgresoDescargaActualizacion>? progreso,
            CancellationToken cancellationToken)
        {
            Context contexto =
                Microsoft.Maui.ApplicationModel
                    .Platform.AppContext;

            var administrador =
                contexto.GetSystemService(
                    Context.DownloadService)
                as AndroidDownloadManager
                ?? throw new InvalidOperationException(
                    "Android no permitió acceder al administrador de descargas.");

            Java.IO.File? carpetaBase =
                contexto.GetExternalFilesDir(
                    AndroidEnvironment.DirectoryDownloads);

            if (carpetaBase is null)
            {
                throw new IOException(
                    "Android no encontró una carpeta disponible para guardar la actualización.");
            }

            string carpeta =
                Path.Combine(
                    carpetaBase.AbsolutePath,
                    "actualizaciones");

            Directory.CreateDirectory(carpeta);

            string nombreArchivo =
                ObtenerNombreSeguro(
                    actualizacion.NombreArchivo,
                    actualizacion.Plataforma);

            string rutaFinal =
                Path.Combine(
                    carpeta,
                    nombreArchivo);

            if (File.Exists(rutaFinal))
            {
                try
                {
                    await ValidarArchivoAsync(
                        rutaFinal,
                        actualizacion,
                        progreso,
                        enSegundoPlano: true,
                        cancellationToken);

                    return rutaFinal;
                }
                catch
                {
                    EliminarSeguro(rutaFinal);
                }
            }

            long idDescarga =
                ObtenerDescargaAndroidGuardada(
                    actualizacion,
                    rutaFinal);

            if (idDescarga <= 0 ||
                !ExisteDescargaAndroid(
                    administrador,
                    idDescarga))
            {
                EliminarSeguro(rutaFinal);

                var solicitud =
                    new AndroidDownloadManager.Request(
                        Android.Net.Uri.Parse(
                            url.AbsoluteUri));

                solicitud.SetTitle(
                    $"ConatraCafé Soil {actualizacion.VersionNombre}");

                solicitud.SetDescription(
                    "Descargando actualización de la aplicación.");

                solicitud.SetMimeType(
                    actualizacion.TipoContenido);

                solicitud.SetAllowedOverMetered(true);
                solicitud.SetAllowedOverRoaming(false);

                solicitud.SetNotificationVisibility(
                    AndroidDownloadVisibility
                        .VisibleNotifyCompleted);

                solicitud.SetDestinationUri(
                    Android.Net.Uri.FromFile(
                        new Java.IO.File(
                            rutaFinal)));

                idDescarga =
                    administrador.Enqueue(
                        solicitud);

                Preferences.Set(
                    AndroidIdDescarga,
                    idDescarga);

                Preferences.Set(
                    AndroidIdActualizacion,
                    actualizacion
                        .ActualizacionAplicacionId);

                Preferences.Set(
                    AndroidRutaDescarga,
                    rutaFinal);
            }

            var cronometro =
                Stopwatch.StartNew();

            long bytesAnterior = 0;
            double segundosAnterior = 0;

            try
            {
                while (true)
                {
                    cancellationToken
                        .ThrowIfCancellationRequested();

                    EstadoAndroid estado =
                        ConsultarDescargaAndroid(
                            administrador,
                            idDescarga);

                    double segundosActuales =
                        cronometro.Elapsed.TotalSeconds;

                    double diferenciaSegundos =
                        Math.Max(
                            segundosActuales -
                            segundosAnterior,
                            0.001);

                    long diferenciaBytes =
                        Math.Max(
                            estado.BytesDescargados -
                            bytesAnterior,
                            0);

                    double velocidad =
                        diferenciaBytes /
                        diferenciaSegundos;

                    long total =
                        estado.TotalBytes > 0
                            ? estado.TotalBytes
                            : actualizacion.TamanoBytes;

                    TimeSpan? restante =
                        velocidad > 0 &&
                        total > estado.BytesDescargados
                            ? TimeSpan.FromSeconds(
                                (total -
                                 estado.BytesDescargados) /
                                velocidad)
                            : null;

                    progreso?.Report(
                        new ProgresoDescargaActualizacion
                        {
                            BytesDescargados =
                                estado.BytesDescargados,
                            TotalBytes =
                                total,
                            BytesPorSegundo =
                                velocidad,
                            TiempoRestante =
                                restante,
                            Estado =
                                estado.Estado,
                            EnSegundoPlano =
                                true
                        });

                    bytesAnterior =
                        estado.BytesDescargados;

                    segundosAnterior =
                        segundosActuales;

                    if (estado.Completada)
                        break;

                    if (estado.Fallida)
                    {
                        throw new IOException(
                            $"Android no pudo descargar la actualización. Código: {estado.Razon}.");
                    }

                    await Task.Delay(
                        600,
                        cancellationToken);
                }

                await ValidarArchivoAsync(
                    rutaFinal,
                    actualizacion,
                    progreso,
                    enSegundoPlano: true,
                    cancellationToken);

                LimpiarPreferenciasAndroid();

                return rutaFinal;
            }
            catch (OperationCanceledException)
            {
                try
                {
                    administrador.Remove(
                        idDescarga);
                }
                catch
                {
                }

                LimpiarPreferenciasAndroid();
                EliminarSeguro(rutaFinal);

                throw;
            }
            catch
            {
                LimpiarPreferenciasAndroid();
                EliminarSeguro(rutaFinal);

                throw;
            }
        }

        private static long ObtenerDescargaAndroidGuardada(
            ActualizacionDisponible actualizacion,
            string rutaEsperada)
        {
            int idActualizacion =
                Preferences.Get(
                    AndroidIdActualizacion,
                    0);

            long idDescarga =
                Preferences.Get(
                    AndroidIdDescarga,
                    0L);

            string ruta =
                Preferences.Get(
                    AndroidRutaDescarga,
                    string.Empty);

            return idActualizacion ==
                       actualizacion.ActualizacionAplicacionId &&
                   string.Equals(
                       ruta,
                       rutaEsperada,
                       StringComparison.OrdinalIgnoreCase)
                ? idDescarga
                : 0;
        }

        private static bool ExisteDescargaAndroid(
            AndroidDownloadManager administrador,
            long idDescarga)
        {
            using var consulta =
                new AndroidDownloadManager.Query();

            consulta.SetFilterById(
                idDescarga);

            using ICursor? cursor =
                administrador.InvokeQuery(
                    consulta);

            return cursor is not null &&
                   cursor.MoveToFirst();
        }

        private static EstadoAndroid ConsultarDescargaAndroid(
            AndroidDownloadManager administrador,
            long idDescarga)
        {
            using var consulta =
                new AndroidDownloadManager.Query();

            consulta.SetFilterById(
                idDescarga);

            using ICursor? cursor =
                administrador.InvokeQuery(
                    consulta);

            if (cursor is null ||
                !cursor.MoveToFirst())
            {
                throw new IOException(
                    "Android perdió la referencia de la descarga.");
            }

            int indiceEstado =
                cursor.GetColumnIndexOrThrow(
                    AndroidDownloadManager.ColumnStatus);

            int indiceDescargados =
                cursor.GetColumnIndexOrThrow(
                    AndroidDownloadManager
                        .ColumnBytesDownloadedSoFar);

            int indiceTotal =
                cursor.GetColumnIndexOrThrow(
                    AndroidDownloadManager
                        .ColumnTotalSizeBytes);

            int indiceRazon =
                cursor.GetColumnIndexOrThrow(
                    AndroidDownloadManager.ColumnReason);

            var estado =
                (AndroidDownloadStatus)
                cursor.GetInt(
                    indiceEstado);

            long descargados =
                Math.Max(
                    cursor.GetLong(
                        indiceDescargados),
                    0);

            long total =
                cursor.GetLong(
                    indiceTotal);

            int razon =
                cursor.GetInt(
                    indiceRazon);

            return estado switch
            {
                AndroidDownloadStatus.Pending =>
                    new EstadoAndroid(
                        descargados,
                        total,
                        "Esperando para iniciar...",
                        false,
                        false,
                        razon),

                AndroidDownloadStatus.Running =>
                    new EstadoAndroid(
                        descargados,
                        total,
                        "Descargando en segundo plano...",
                        false,
                        false,
                        razon),

                AndroidDownloadStatus.Paused =>
                    new EstadoAndroid(
                        descargados,
                        total,
                        "Descarga pausada por Android. Se reanudará automáticamente.",
                        false,
                        false,
                        razon),

                AndroidDownloadStatus.Successful =>
                    new EstadoAndroid(
                        descargados,
                        total,
                        "Descarga completada.",
                        true,
                        false,
                        razon),

                AndroidDownloadStatus.Failed =>
                    new EstadoAndroid(
                        descargados,
                        total,
                        "La descarga falló.",
                        false,
                        true,
                        razon),

                _ =>
                    new EstadoAndroid(
                        descargados,
                        total,
                        "Consultando descarga...",
                        false,
                        false,
                        razon)
            };
        }

        private static void LimpiarPreferenciasAndroid()
        {
            Preferences.Remove(
                AndroidIdDescarga);

            Preferences.Remove(
                AndroidIdActualizacion);

            Preferences.Remove(
                AndroidRutaDescarga);
        }

        private sealed record EstadoAndroid(
            long BytesDescargados,
            long TotalBytes,
            string Estado,
            bool Completada,
            bool Fallida,
            int Razon);
#endif

#if WINDOWS
        private async Task<string> DescargarWindowsAsync(
            ActualizacionDisponible actualizacion,
            Uri url,
            IProgress<ProgresoDescargaActualizacion>? progreso,
            CancellationToken cancellationToken)
        {
            StorageFolder carpeta =
                await ApplicationData.Current.LocalFolder
                    .CreateFolderAsync(
                        "actualizaciones",
                        CreationCollisionOption.OpenIfExists);

            string nombreArchivo =
                ObtenerNombreSeguro(
                    actualizacion.NombreArchivo,
                    actualizacion.Plataforma);

            IStorageFile archivo =
                await carpeta.CreateFileAsync(
                    nombreArchivo,
                    CreationCollisionOption.OpenIfExists);

            DownloadOperation? operacion =
                await BuscarDescargaWindowsAsync(
                    actualizacion);

            bool esNueva =
                operacion is null;

            if (operacion is null)
            {
                var descargador =
                    new BackgroundDownloader
                    {
                        CostPolicy =
                            BackgroundTransferCostPolicy.Always
                    };

                operacion =
                    descargador.CreateDownload(
                        url,
                        archivo);

                Preferences.Set(
                    WindowsGuidDescarga,
                    operacion.Guid.ToString());

                Preferences.Set(
                    WindowsIdActualizacion,
                    actualizacion
                        .ActualizacionAplicacionId);
            }
            else
            {
                archivo =
                    operacion.ResultFile;
            }

            var cronometro =
                Stopwatch.StartNew();

            ulong bytesAnterior = 0;
            double segundosAnterior = 0;

            var progresoWindows =
                new Progress<DownloadOperation>(
                    descarga =>
                    {
                        BackgroundDownloadProgress estado =
                            descarga.Progress;

                        double segundosActuales =
                            cronometro.Elapsed.TotalSeconds;

                        double diferenciaSegundos =
                            Math.Max(
                                segundosActuales -
                                segundosAnterior,
                                0.001);

                        ulong diferenciaBytes =
                            estado.BytesReceived >=
                            bytesAnterior
                                ? estado.BytesReceived -
                                  bytesAnterior
                                : 0;

                        double velocidad =
                            diferenciaBytes /
                            diferenciaSegundos;

                        long total =
                            estado.TotalBytesToReceive > 0
                                ? (long)estado
                                    .TotalBytesToReceive
                                : actualizacion.TamanoBytes;

                        long descargados =
                            (long)estado.BytesReceived;

                        TimeSpan? restante =
                            velocidad > 0 &&
                            total > descargados
                                ? TimeSpan.FromSeconds(
                                    (total - descargados) /
                                    velocidad)
                                : null;

                        progreso?.Report(
                            new ProgresoDescargaActualizacion
                            {
                                BytesDescargados =
                                    descargados,
                                TotalBytes =
                                    total,
                                BytesPorSegundo =
                                    velocidad,
                                TiempoRestante =
                                    restante,
                                Estado =
                                    "Descargando en segundo plano...",
                                EnSegundoPlano =
                                    true
                            });

                        bytesAnterior =
                            estado.BytesReceived;

                        segundosAnterior =
                            segundosActuales;
                    });

            try
            {
                DownloadOperation resultado =
                    esNueva
                        ? await operacion
                            .StartAsync()
                            .AsTask(
                                cancellationToken,
                                progresoWindows)
                        : await operacion
                            .AttachAsync()
                            .AsTask(
                                cancellationToken,
                                progresoWindows);

                string ruta =
                    resultado.ResultFile.Path;

                await ValidarArchivoAsync(
                    ruta,
                    actualizacion,
                    progreso,
                    enSegundoPlano: true,
                    cancellationToken);

                LimpiarPreferenciasWindows();

                return ruta;
            }
            catch (OperationCanceledException)
            {
                LimpiarPreferenciasWindows();

                try
                {
                    await archivo.DeleteAsync();
                }
                catch
                {
                }

                throw;
            }
            catch
            {
                LimpiarPreferenciasWindows();

                throw;
            }
        }

        private static async Task<DownloadOperation?>
            BuscarDescargaWindowsAsync(
                ActualizacionDisponible actualizacion)
        {
            int idActualizacion =
                Preferences.Get(
                    WindowsIdActualizacion,
                    0);

            string guidTexto =
                Preferences.Get(
                    WindowsGuidDescarga,
                    string.Empty);

            if (idActualizacion !=
                    actualizacion
                        .ActualizacionAplicacionId ||
                !Guid.TryParse(
                    guidTexto,
                    out Guid guid))
            {
                return null;
            }

            IReadOnlyList<DownloadOperation> descargas =
                await BackgroundDownloader
                    .GetCurrentDownloadsAsync();

            return descargas.FirstOrDefault(
                item => item.Guid == guid);
        }

        private static bool TieneIdentidadPaqueteWindows()
        {
            try
            {
                _ = WindowsPackage.Current.Id.Name;
                return true;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
            catch (COMException)
            {
                return false;
            }
        }

        private static void LimpiarPreferenciasWindows()
        {
            Preferences.Remove(
                WindowsGuidDescarga);

            Preferences.Remove(
                WindowsIdActualizacion);
        }
#endif

        private async Task<string> DescargarHttpAsync(
            ActualizacionDisponible actualizacion,
            Uri url,
            IProgress<ProgresoDescargaActualizacion>? progreso,
            CancellationToken cancellationToken,
            bool enSegundoPlano)
        {
            using HttpRequestMessage request =
                new(
                    HttpMethod.Get,
                    url);

            using HttpResponseMessage response =
                await httpClient.SendAsync(
                    request,
                    HttpCompletionOption.ResponseHeadersRead,
                    cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                string error =
                    await response.Content.ReadAsStringAsync(
                        cancellationToken);

                throw new HttpRequestException(
                    ExtraerMensaje(error),
                    null,
                    response.StatusCode);
            }

            long total =
                response.Content.Headers.ContentLength ??
                actualizacion.TamanoBytes;

            string nombreSeguro =
                ObtenerNombreSeguro(
                    actualizacion.NombreArchivo,
                    actualizacion.Plataforma);

            string carpeta =
                Path.Combine(
                    FileSystem.CacheDirectory,
                    "actualizaciones");

            Directory.CreateDirectory(
                carpeta);

            string rutaTemporal =
                Path.Combine(
                    carpeta,
                    $"{Guid.NewGuid():N}.tmp");

            string rutaFinal =
                Path.Combine(
                    carpeta,
                    nombreSeguro);

            var cronometro =
                Stopwatch.StartNew();

            long bytesAnterior = 0;
            double segundosAnterior = 0;

            try
            {
                await using Stream origen =
                    await response.Content
                        .ReadAsStreamAsync(
                            cancellationToken);

                await using FileStream destino =
                    new(
                        rutaTemporal,
                        FileMode.Create,
                        FileAccess.Write,
                        FileShare.None,
                        bufferSize: 1024 * 1024,
                        useAsync: true);

                byte[] buffer =
                    new byte[1024 * 1024];

                long descargados = 0;

                while (true)
                {
                    int leidos =
                        await origen.ReadAsync(
                            buffer.AsMemory(
                                0,
                                buffer.Length),
                            cancellationToken);

                    if (leidos == 0)
                        break;

                    await destino.WriteAsync(
                        buffer.AsMemory(
                            0,
                            leidos),
                        cancellationToken);

                    descargados += leidos;

                    double segundosActuales =
                        cronometro.Elapsed.TotalSeconds;

                    double diferenciaSegundos =
                        Math.Max(
                            segundosActuales -
                            segundosAnterior,
                            0.001);

                    long diferenciaBytes =
                        Math.Max(
                            descargados -
                            bytesAnterior,
                            0);

                    double velocidad =
                        diferenciaBytes /
                        diferenciaSegundos;

                    TimeSpan? restante =
                        velocidad > 0 &&
                        total > descargados
                            ? TimeSpan.FromSeconds(
                                (total - descargados) /
                                velocidad)
                            : null;

                    progreso?.Report(
                        new ProgresoDescargaActualizacion
                        {
                            BytesDescargados =
                                descargados,
                            TotalBytes =
                                total,
                            BytesPorSegundo =
                                velocidad,
                            TiempoRestante =
                                restante,
                            Estado =
                                "Descargando actualización...",
                            EnSegundoPlano =
                                enSegundoPlano
                        });

                    bytesAnterior =
                        descargados;

                    segundosAnterior =
                        segundosActuales;
                }

                await destino.FlushAsync(
                    cancellationToken);

                File.Move(
                    rutaTemporal,
                    rutaFinal,
                    overwrite: true);

                await ValidarArchivoAsync(
                    rutaFinal,
                    actualizacion,
                    progreso,
                    enSegundoPlano,
                    cancellationToken);

                return rutaFinal;
            }
            catch
            {
                EliminarSeguro(
                    rutaTemporal);

                throw;
            }
        }

        private static async Task ValidarArchivoAsync(
            string ruta,
            ActualizacionDisponible actualizacion,
            IProgress<ProgresoDescargaActualizacion>? progreso,
            bool enSegundoPlano,
            CancellationToken cancellationToken)
        {
            if (!File.Exists(ruta))
            {
                throw new FileNotFoundException(
                    "La descarga terminó, pero el archivo no fue encontrado.",
                    ruta);
            }

            long tamano =
                new FileInfo(ruta).Length;

            if (actualizacion.TamanoBytes > 0 &&
                tamano != actualizacion.TamanoBytes)
            {
                throw new InvalidDataException(
                    "La descarga no coincide con el tamaño publicado.");
            }

            progreso?.Report(
                new ProgresoDescargaActualizacion
                {
                    BytesDescargados =
                        tamano,
                    TotalBytes =
                        actualizacion.TamanoBytes > 0
                            ? actualizacion.TamanoBytes
                            : tamano,
                    Estado =
                        "Validando seguridad SHA-256...",
                    EnSegundoPlano =
                        enSegundoPlano
                });

            string hash =
                await CalcularSha256Async(
                    ruta,
                    cancellationToken);

            if (!string.Equals(
                    hash,
                    actualizacion.HashSha256,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException(
                    "El archivo descargado no superó la validación de seguridad SHA-256.");
            }

            progreso?.Report(
                new ProgresoDescargaActualizacion
                {
                    BytesDescargados =
                        tamano,
                    TotalBytes =
                        actualizacion.TamanoBytes > 0
                            ? actualizacion.TamanoBytes
                            : tamano,
                    Estado =
                        "Descarga completada y verificada.",
                    EnSegundoPlano =
                        enSegundoPlano
                });
        }

        public static long ObtenerVersionCodigoInstalada()
        {
            string valor =
                Microsoft.Maui.ApplicationModel.AppInfo.Current.BuildString ??
                string.Empty;

            return long.TryParse(
                    valor,
                    out long build)
                ? build
                : 0;
        }

        private static string? ObtenerPlataforma()
        {
            if (DeviceInfo.Current.Platform ==
                DevicePlatform.Android)
            {
                return "ANDROID";
            }

            if (DeviceInfo.Current.Platform ==
                DevicePlatform.WinUI)
            {
                return "WINDOWS";
            }

            return null;
        }

        /// <summary>
        /// Usa el ID y la BaseAddress conocida por la aplicación. Así la
        /// descarga no depende de una URL absoluta construida por un proxy.
        /// </summary>
        private Uri ResolverUrlDescarga(
            ActualizacionDisponible actualizacion)
        {
            if (httpClient.BaseAddress is null)
            {
                throw new InvalidOperationException(
                    "No se encontró la dirección base de la API.");
            }

            if (actualizacion.ActualizacionAplicacionId > 0)
            {
                return new Uri(
                    httpClient.BaseAddress,
                    "api/actualizaciones/descargar/" +
                    actualizacion
                        .ActualizacionAplicacionId);
            }

            if (Uri.TryCreate(
                    actualizacion.UrlDescarga,
                    UriKind.Absolute,
                    out Uri? absoluta))
            {
                return absoluta;
            }

            return new Uri(
                httpClient.BaseAddress,
                actualizacion
                    .UrlDescarga
                    .TrimStart('/'));
        }

        private static string ObtenerNombreSeguro(
            string? nombre,
            string plataforma)
        {
            string extensionPredeterminada =
                plataforma.Equals(
                    "ANDROID",
                    StringComparison.OrdinalIgnoreCase)
                    ? ".apk"
                    : ".msix";

            string valor =
                Path.GetFileName(
                    nombre ??
                    string.Empty);

            if (string.IsNullOrWhiteSpace(valor))
            {
                valor =
                    "ConatraCafeSoil" +
                    extensionPredeterminada;
            }

            foreach (
                char invalido
                in Path.GetInvalidFileNameChars())
            {
                valor =
                    valor.Replace(
                        invalido,
                        '_');
            }

            return valor;
        }

        private static async Task<string> CalcularSha256Async(
            string ruta,
            CancellationToken cancellationToken)
        {
            await using FileStream stream =
                new(
                    ruta,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read,
                    bufferSize: 1024 * 1024,
                    useAsync: true);

            byte[] hash =
                await SHA256.HashDataAsync(
                    stream,
                    cancellationToken);

            return Convert.ToHexString(hash);
        }

        private static string ExtraerMensaje(
            string contenido)
        {
            if (string.IsNullOrWhiteSpace(
                    contenido))
            {
                return
                    "La API no devolvió detalles del error.";
            }

            try
            {
                using JsonDocument documento =
                    JsonDocument.Parse(
                        contenido);

                JsonElement raiz =
                    documento.RootElement;

                foreach (
                    string propiedad
                    in new[]
                    {
                        "message",
                        "mensaje",
                        "title",
                        "error"
                    })
                {
                    if (raiz.TryGetProperty(
                            propiedad,
                            out JsonElement valor) &&
                        valor.ValueKind ==
                            JsonValueKind.String)
                    {
                        return valor.GetString() ??
                               contenido;
                    }
                }
            }
            catch (JsonException)
            {
                // La API también puede devolver texto plano.
            }

            return contenido
                .Trim()
                .Trim('"');
        }

        private static void EliminarSeguro(
            string ruta)
        {
            try
            {
                if (File.Exists(ruta))
                    File.Delete(ruta);
            }
            catch
            {
                // El sistema podrá limpiar el archivo posteriormente.
            }
        }
    }
}
