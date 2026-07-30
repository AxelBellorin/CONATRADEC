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
    /// La comprobación de versiones utiliza el cliente centralizado de la
    /// aplicación para enviar la sesión activa. El backend devuelve una URL
    /// temporal protegida que incluye el permiso necesario para descargar.
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

        /*
         * Cliente sin los manejadores internos de la aplicación.
         *
         * Se utiliza exclusivamente para transferir el instalador porque
         * Android DownloadManager y Windows BackgroundDownloader reciben una
         * URL temporal que ya contiene su autorización.
         */
        private readonly HttpClient httpClient;

        /*
         * Cliente centralizado de CONATRADEC.
         *
         * Agrega automáticamente X-Usuario-Id, X-Version-Sesion,
         * X-Dispositivo, X-Plataforma y los demás datos de contexto.
         */
        private readonly HttpClient apiClient;

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

            apiClient =
                ApiClientService.Client;
        }

        public bool PlataformaCompatible =>
            ObtenerPlataforma() is not null;

        /// <summary>
        /// Comprueba la versión mediante el endpoint autenticado. Este endpoint
        /// genera la URL temporal que luego utiliza el administrador de
        /// descargas del sistema operativo.
        /// </summary>
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

            return await ConsultarActualizacionProtegidaAsync(
                plataforma,
                versionCodigo,
                canal,
                cancellationToken);
        }

        /// <summary>
        /// Inicia o recupera una transferencia administrada por el sistema.
        ///
        /// Antes de comenzar se solicita un permiso temporal nuevo. Así una
        /// actualización encontrada anteriormente no falla con 401 porque su
        /// autorización haya vencido mientras la aplicación permanecía abierta.
        /// </summary>
        public async Task<string> DescargarEnSegundoPlanoAsync(
            ActualizacionDisponible actualizacion,
            IProgress<ProgresoDescargaActualizacion>? progreso = null,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(actualizacion);

            ActualizacionDisponible autorizada =
                await RenovarPermisoDescargaAsync(
                    actualizacion,
                    cancellationToken);

            Uri url =
                ResolverUrlDescarga(autorizada);

#if ANDROID
            return await DescargarAndroidAsync(
                autorizada,
                url,
                progreso,
                cancellationToken);
#elif WINDOWS
            if (TieneIdentidadPaqueteWindows())
            {
                return await DescargarWindowsAsync(
                    autorizada,
                    url,
                    progreso,
                    cancellationToken);
            }

            return await DescargarHttpAsync(
                autorizada,
                url,
                progreso,
                cancellationToken,
                enSegundoPlano: false);
#else
            return await DescargarHttpAsync(
                autorizada,
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

        /// <summary>
        /// Consulta el endpoint protegido que valida la sesión y el permiso de
        /// lectura de ActualizacionAplicacionPage.
        /// </summary>
        private async Task<ActualizacionDisponible?>
            ConsultarActualizacionProtegidaAsync(
                string plataforma,
                long versionCodigo,
                string canal,
                CancellationToken cancellationToken)
        {
            string ruta =
                "api/actualizaciones/aplicacion/comprobar" +
                $"?plataforma={Uri.EscapeDataString(plataforma)}" +
                $"&versionCodigo={versionCodigo}" +
                $"&canal={Uri.EscapeDataString(canal)}";

            using HttpResponseMessage response =
                await apiClient.GetAsync(
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
        /// Obtiene una URL protegida nueva inmediatamente antes de iniciar una
        /// descarga. El permiso emitido por el backend tiene vigencia limitada.
        /// </summary>
        private async Task<ActualizacionDisponible>
            RenovarPermisoDescargaAsync(
                ActualizacionDisponible actualizacion,
                CancellationToken cancellationToken)
        {
            string plataforma =
                string.IsNullOrWhiteSpace(
                    actualizacion.Plataforma)
                    ? ObtenerPlataforma() ??
                      throw new InvalidOperationException(
                          "La plataforma actual no admite actualizaciones.")
                    : actualizacion.Plataforma;

            string canal =
                string.IsNullOrWhiteSpace(
                    actualizacion.Canal)
                    ? Preferences.Get(
                        PreferenciaCanal,
                        CanalPredeterminado)
                    : actualizacion.Canal;

            ActualizacionDisponible? renovada =
                await ConsultarActualizacionProtegidaAsync(
                    plataforma,
                    ObtenerVersionCodigoInstalada(),
                    canal,
                    cancellationToken);

            if (renovada is null)
            {
                throw new InvalidOperationException(
                    "La actualización ya no está disponible o la aplicación ya se encuentra actualizada.");
            }

            if (string.IsNullOrWhiteSpace(
                    renovada.UrlDescarga))
            {
                throw new InvalidOperationException(
                    "La API no proporcionó el permiso temporal para descargar la actualización.");
            }

            return renovada;
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

            Directory.CreateDirectory(
                carpeta);

            string nombreArchivo =
                ObtenerNombreSeguro(
                    actualizacion.NombreArchivo,
                    actualizacion.Plataforma);

            string rutaFinal =
                Path.Combine(
                    carpeta,
                    nombreArchivo);

            /*
             * Si el archivo completo ya existe, primero se valida. Esto evita
             * volver a descargar una actualización que ya terminó.
             */
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
                    EliminarSeguro(
                        rutaFinal);
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
                EliminarSeguro(
                    rutaFinal);

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

                solicitud.SetAllowedOverMetered(
                    true);

                solicitud.SetAllowedOverRoaming(
                    false);

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
                EliminarDescargaAndroidSeguro(
                    administrador,
                    idDescarga);

                LimpiarPreferenciasAndroid();
                EliminarSeguro(
                    rutaFinal);

                throw;
            }
            catch
            {
                EliminarDescargaAndroidSeguro(
                    administrador,
                    idDescarga);

                LimpiarPreferenciasAndroid();
                EliminarSeguro(
                    rutaFinal);

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

        private static void EliminarDescargaAndroidSeguro(
            AndroidDownloadManager administrador,
            long idDescarga)
        {
            if (idDescarga <= 0)
                return;

            try
            {
                administrador.Remove(
                    idDescarga);
            }
            catch
            {
                // Android puede haber eliminado previamente la transferencia.
            }
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

            DownloadOperation? operacion =
                await BuscarDescargaWindowsAsync(
                    actualizacion);

            bool esNueva =
                operacion is null;

            IStorageFile archivo;

            if (operacion is null)
            {
                archivo =
                    await carpeta.CreateFileAsync(
                        nombreArchivo,
                        CreationCollisionOption.ReplaceExisting);

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

                try
                {
                    await archivo.DeleteAsync();
                }
                catch
                {
                }

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
        /// Conserva completa la URL emitida por el backend. No se reconstruye a
        /// partir del ID porque eso eliminaría el parámetro "permiso" y causaría
        /// nuevamente el error HTTP 401.
        /// </summary>
        private Uri ResolverUrlDescarga(
            ActualizacionDisponible actualizacion)
        {
            if (httpClient.BaseAddress is null)
            {
                throw new InvalidOperationException(
                    "No se encontró la dirección base de la API.");
            }

            string urlDescarga =
                actualizacion.UrlDescarga?.Trim() ??
                string.Empty;

            if (string.IsNullOrWhiteSpace(
                    urlDescarga))
            {
                throw new InvalidOperationException(
                    "La API no proporcionó una dirección autorizada para descargar la actualización.");
            }

            if (Uri.TryCreate(
                    urlDescarga,
                    UriKind.Absolute,
                    out Uri? absoluta))
            {
                return absoluta;
            }

            return new Uri(
                httpClient.BaseAddress,
                urlDescarga.TrimStart('/'));
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
