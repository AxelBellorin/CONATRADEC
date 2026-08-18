using CONATRADEC.Models;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices;
using Microsoft.Maui.Storage;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
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
    /// La API v2 entrega una URL limpia y una credencial temporal separada.
    /// La credencial se envía únicamente mediante X-Permiso-Descarga y nunca se
    /// almacena dentro de la URL.
    ///
    /// Android conserva DownloadManager. Windows empaquetado conserva
    /// BackgroundDownloader; la distribución Windows actual es desempaquetada,
    /// por lo que utiliza una transferencia HTTP reanudable mediante Range y un
    /// archivo .part persistente.
    /// </summary>
    public sealed class ActualizacionAplicacionService
    {
        private const string PreferenciaCanal =
            "Actualizaciones.Canal";

        private const string HeaderPermisoDescarga =
            "X-Permiso-Descarga";

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
         * Cliente sin los manejadores internos de la aplicación. Se usa para
         * descargar el instalador con la credencial temporal v2.
         */
        private readonly HttpClient httpClient;

        /*
         * Cliente centralizado de CONATRADEC para comprobar versiones mediante
         * JWT y contexto de sesión.
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
        /// Comprueba una versión fresca en el endpoint v2. Este endpoint exige
        /// una sesión JWT válida, pero no depende del permiso administrativo de
        /// la tarjeta Actualizaciones: las actualizaciones obligatorias deben
        /// alcanzar a cualquier usuario autenticado.
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
        /// Inicia o recupera una transferencia. Antes de comenzar siempre pide
        /// una autorización temporal nueva, por lo que un estado persistido no
        /// conserva ni reutiliza credenciales vencidas.
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
         * Compatibilidad con cualquier consumidor anterior que todavía use
         * progreso numérico.
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
        /// Elimina una transferencia HTTP parcial únicamente cuando el usuario
        /// la cancela explícitamente. Un cierre abrupto de Windows deja el .part
        /// para que pueda reanudarse en la siguiente ejecución.
        /// </summary>
        public void EliminarDescargaParcial(
            ActualizacionDisponible actualizacion)
        {
#if WINDOWS
            if (TieneIdentidadPaqueteWindows())
                return;
#endif

            string rutaParcial =
                ObtenerRutaParcialHttp(actualizacion);

            EliminarSeguro(rutaParcial);
        }

        private async Task<ActualizacionDisponible?>
            ConsultarActualizacionProtegidaAsync(
                string plataforma,
                long versionCodigo,
                string canal,
                CancellationToken cancellationToken)
        {
            string ruta =
                "api/actualizaciones/aplicacion/v2/comprobar" +
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
        /// Renueva metadatos y autorización inmediatamente antes de descargar.
        /// Si el servidor ya no publica esa versión, se informa explícitamente
        /// al estado global para retirar cualquier bloqueo obligatorio antiguo.
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
                throw new ActualizacionYaNoDisponibleException(
                    "La actualización ya no está publicada o la aplicación ya se encuentra actualizada.");
            }

            if (renovada.ActualizacionAplicacionId !=
                actualizacion.ActualizacionAplicacionId)
            {
                throw new ActualizacionYaNoDisponibleException(
                    "Se publicó una versión más reciente. Vuelva a buscar actualizaciones antes de descargar.");
            }

            if (string.IsNullOrWhiteSpace(
                    renovada.UrlDescarga) ||
                string.IsNullOrWhiteSpace(
                    renovada.PermisoDescarga))
            {
                throw new InvalidOperationException(
                    "La API no proporcionó una autorización temporal completa para descargar la actualización.");
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

                solicitud.AddRequestHeader(
                    HeaderPermisoDescarga,
                    actualizacion.PermisoDescarga);

                solicitud.SetDestinationUri(
                    Android.Net.Uri.FromFile(
                        new Java.IO.File(
                            rutaFinal)));

                idDescarga =
                    administrador.Enqueue(solicitud);

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
                            TotalBytes = total,
                            BytesPorSegundo = velocidad,
                            TiempoRestante = restante,
                            Estado = estado.Estado,
                            EnSegundoPlano = true
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
                EliminarSeguro(rutaFinal);

                throw;
            }
            catch
            {
                EliminarDescargaAndroidSeguro(
                    administrador,
                    idDescarga);

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

            consulta.SetFilterById(idDescarga);

            using ICursor? cursor =
                administrador.InvokeQuery(consulta);

            return cursor is not null &&
                   cursor.MoveToFirst();
        }

        private static EstadoAndroid ConsultarDescargaAndroid(
            AndroidDownloadManager administrador,
            long idDescarga)
        {
            using var consulta =
                new AndroidDownloadManager.Query();

            consulta.SetFilterById(idDescarga);

            using ICursor? cursor =
                administrador.InvokeQuery(consulta);

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
                cursor.GetInt(indiceEstado);

            long descargados =
                Math.Max(
                    cursor.GetLong(indiceDescargados),
                    0);

            long total =
                cursor.GetLong(indiceTotal);

            int razon =
                cursor.GetInt(indiceRazon);

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
                administrador.Remove(idDescarga);
            }
            catch
            {
                // Android puede haber eliminado previamente la transferencia.
            }
        }

        private static void LimpiarPreferenciasAndroid()
        {
            Preferences.Remove(AndroidIdDescarga);
            Preferences.Remove(AndroidIdActualizacion);
            Preferences.Remove(AndroidRutaDescarga);
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
                await BuscarDescargaWindowsAsync(actualizacion);

            bool esNueva = operacion is null;
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

                descargador.SetRequestHeader(
                    HeaderPermisoDescarga,
                    actualizacion.PermisoDescarga);

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
                archivo = operacion.ResultFile;
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
                            estado.BytesReceived >= bytesAnterior
                                ? estado.BytesReceived - bytesAnterior
                                : 0;

                        double velocidad =
                            diferenciaBytes /
                            diferenciaSegundos;

                        long total =
                            estado.TotalBytesToReceive > 0
                                ? (long)estado.TotalBytesToReceive
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
                                BytesDescargados = descargados,
                                TotalBytes = total,
                                BytesPorSegundo = velocidad,
                                TiempoRestante = restante,
                                Estado =
                                    "Descargando en segundo plano...",
                                EnSegundoPlano = true
                            });

                        bytesAnterior = estado.BytesReceived;
                        segundosAnterior = segundosActuales;
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
                    actualizacion.ActualizacionAplicacionId ||
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
            Preferences.Remove(WindowsGuidDescarga);
            Preferences.Remove(WindowsIdActualizacion);
        }
#endif

        /// <summary>
        /// Descarga HTTP reanudable. En Windows desempaquetado el archivo .part
        /// se guarda en AppDataDirectory, por lo que un cierre de la aplicación
        /// no obliga a empezar desde cero. El servidor ya admite Range.
        /// </summary>
        private async Task<string> DescargarHttpAsync(
            ActualizacionDisponible actualizacion,
            Uri url,
            IProgress<ProgresoDescargaActualizacion>? progreso,
            CancellationToken cancellationToken,
            bool enSegundoPlano)
        {
            string rutaFinal =
                ObtenerRutaFinalHttp(actualizacion);

            string rutaParcial =
                ObtenerRutaParcialHttp(actualizacion);

            Directory.CreateDirectory(
                Path.GetDirectoryName(rutaFinal)!);

            if (File.Exists(rutaFinal))
            {
                try
                {
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
                    EliminarSeguro(rutaFinal);
                }
            }

            long descargadosPrevios =
                File.Exists(rutaParcial)
                    ? new FileInfo(rutaParcial).Length
                    : 0;

            if (actualizacion.TamanoBytes > 0 &&
                descargadosPrevios >= actualizacion.TamanoBytes)
            {
                if (descargadosPrevios == actualizacion.TamanoBytes)
                {
                    try
                    {
                        await ValidarArchivoAsync(
                            rutaParcial,
                            actualizacion,
                            progreso,
                            enSegundoPlano,
                            cancellationToken);

                        File.Move(
                            rutaParcial,
                            rutaFinal,
                            overwrite: true);

                        return rutaFinal;
                    }
                    catch
                    {
                        EliminarSeguro(rutaParcial);
                    }
                }
                else
                {
                    EliminarSeguro(rutaParcial);
                }

                descargadosPrevios = 0;
            }

            HttpResponseMessage? response = null;

            try
            {
                response = await EnviarSolicitudDescargaAsync(
                    actualizacion,
                    url,
                    descargadosPrevios,
                    cancellationToken);

                if (response.StatusCode ==
                    HttpStatusCode.RequestedRangeNotSatisfiable)
                {
                    response.Dispose();
                    response = null;
                    EliminarSeguro(rutaParcial);
                    descargadosPrevios = 0;

                    response = await EnviarSolicitudDescargaAsync(
                        actualizacion,
                        url,
                        0,
                        cancellationToken);
                }

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

                bool reanudada =
                    descargadosPrevios > 0 &&
                    response.StatusCode ==
                        HttpStatusCode.PartialContent;

                if (!reanudada)
                    descargadosPrevios = 0;

                long total =
                    response.Content.Headers.ContentRange?.Length ??
                    (response.Content.Headers.ContentLength.HasValue
                        ? descargadosPrevios +
                          response.Content.Headers.ContentLength.Value
                        : actualizacion.TamanoBytes);

                if (actualizacion.TamanoBytes > 0)
                    total = actualizacion.TamanoBytes;

                var cronometro = Stopwatch.StartNew();
                long bytesAnterior = descargadosPrevios;
                double segundosAnterior = 0;

                await using Stream origen =
                    await response.Content.ReadAsStreamAsync(
                        cancellationToken);

                await using FileStream destino =
                    new(
                        rutaParcial,
                        reanudada
                            ? FileMode.Append
                            : FileMode.Create,
                        FileAccess.Write,
                        FileShare.None,
                        bufferSize: 1024 * 1024,
                        useAsync: true);

                byte[] buffer = new byte[1024 * 1024];
                long descargados = descargadosPrevios;

                progreso?.Report(
                    new ProgresoDescargaActualizacion
                    {
                        BytesDescargados = descargados,
                        TotalBytes = total,
                        Estado = reanudada
                            ? "Reanudando descarga de Windows..."
                            : "Descargando actualización...",
                        EnSegundoPlano = enSegundoPlano
                    });

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
                        buffer.AsMemory(0, leidos),
                        cancellationToken);

                    descargados += leidos;

                    double segundosActuales =
                        cronometro.Elapsed.TotalSeconds;

                    double diferenciaSegundos =
                        Math.Max(
                            segundosActuales - segundosAnterior,
                            0.001);

                    long diferenciaBytes =
                        Math.Max(
                            descargados - bytesAnterior,
                            0);

                    double velocidad =
                        diferenciaBytes /
                        diferenciaSegundos;

                    TimeSpan? restante =
                        velocidad > 0 && total > descargados
                            ? TimeSpan.FromSeconds(
                                (total - descargados) /
                                velocidad)
                            : null;

                    progreso?.Report(
                        new ProgresoDescargaActualizacion
                        {
                            BytesDescargados = descargados,
                            TotalBytes = total,
                            BytesPorSegundo = velocidad,
                            TiempoRestante = restante,
                            Estado =
                                "Descargando actualización...",
                            EnSegundoPlano = enSegundoPlano
                        });

                    bytesAnterior = descargados;
                    segundosAnterior = segundosActuales;
                }

                await destino.FlushAsync(cancellationToken);

                /*
                 * Se valida el .part antes de convertirlo en archivo instalable.
                 * Así un hash/tamaño inválido nunca queda guardado como EXE/APK
                 * final.
                 */
                try
                {
                    await ValidarArchivoAsync(
                        rutaParcial,
                        actualizacion,
                        progreso,
                        enSegundoPlano,
                        cancellationToken);
                }
                catch (InvalidDataException)
                {
                    EliminarSeguro(rutaParcial);
                    throw;
                }

                File.Move(
                    rutaParcial,
                    rutaFinal,
                    overwrite: true);

                return rutaFinal;
            }
            finally
            {
                response?.Dispose();
            }
        }

        private async Task<HttpResponseMessage>
            EnviarSolicitudDescargaAsync(
                ActualizacionDisponible actualizacion,
                Uri url,
                long desde,
                CancellationToken cancellationToken)
        {
            using var request =
                new HttpRequestMessage(
                    HttpMethod.Get,
                    url);

            request.Headers.TryAddWithoutValidation(
                HeaderPermisoDescarga,
                actualizacion.PermisoDescarga);

            if (desde > 0)
            {
                request.Headers.Range =
                    new RangeHeaderValue(
                        desde,
                        null);
            }

            return await httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);
        }

        private static string ObtenerRutaFinalHttp(
            ActualizacionDisponible actualizacion)
        {
            string carpeta =
                ObtenerCarpetaDescargaHttp();

            string nombreSeguro =
                ObtenerNombreSeguro(
                    actualizacion.NombreArchivo,
                    actualizacion.Plataforma);

            /*
             * El identificador evita reutilizar accidentalmente un .part de
             * otra versión que tenga el mismo nombre de instalador (por
             * ejemplo Setup.exe).
             */
            string nombreVersionado =
                $"{actualizacion.ActualizacionAplicacionId}_{nombreSeguro}";

            return Path.Combine(
                carpeta,
                nombreVersionado);
        }

        private static string ObtenerRutaParcialHttp(
            ActualizacionDisponible actualizacion) =>
            ObtenerRutaFinalHttp(actualizacion) +
            ".part";

        private static string ObtenerCarpetaDescargaHttp()
        {
#if WINDOWS
            return Path.Combine(
                FileSystem.AppDataDirectory,
                "actualizaciones");
#else
            return Path.Combine(
                FileSystem.CacheDirectory,
                "actualizaciones");
#endif
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
                    BytesDescargados = tamano,
                    TotalBytes =
                        actualizacion.TamanoBytes > 0
                            ? actualizacion.TamanoBytes
                            : tamano,
                    Estado =
                        "Validando seguridad SHA-256...",
                    EnSegundoPlano = enSegundoPlano
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
                    BytesDescargados = tamano,
                    TotalBytes =
                        actualizacion.TamanoBytes > 0
                            ? actualizacion.TamanoBytes
                            : tamano,
                    Estado =
                        "Descarga completada y verificada.",
                    EnSegundoPlano = enSegundoPlano
                });
        }

        public static long ObtenerVersionCodigoInstalada()
        {
            string valor =
                AppInfo.Current.BuildString ??
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

            if (string.IsNullOrWhiteSpace(urlDescarga))
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
                    : ".exe";

            string valor =
                Path.GetFileName(
                    nombre ?? string.Empty);

            if (string.IsNullOrWhiteSpace(valor))
            {
                valor =
                    "ConatraCafeSoil" +
                    extensionPredeterminada;
            }

            foreach (char invalido
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
            if (string.IsNullOrWhiteSpace(contenido))
            {
                return
                    "La API no devolvió detalles del error.";
            }

            try
            {
                using JsonDocument documento =
                    JsonDocument.Parse(contenido);

                JsonElement raiz =
                    documento.RootElement;

                foreach (string propiedad
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
