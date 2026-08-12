using CONATRADEC.Models;
using Microsoft.Maui.Devices;
using Microsoft.Maui.Storage;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace CONATRADEC.Services
{
    /// <summary>
    /// Preparación manual del dispositivo.
    ///
    /// No contiene temporizadores, listeners de red ni verificaciones por
    /// navegación. Descargar todo es la única operación que actualiza el
    /// paquete completo.
    /// </summary>
    public sealed class SincronizacionOfflineGlobalService
    {
        private const string EstadoClavePrefijo =
            "offline_global_manual_estado_v3_";

        /*
         * Versión 3 incorpora el alcance de análisis dentro del perfil de
         * preparación y valida las rutas actuales del Álbum jerárquico.
         * Un paquete v2 debe prepararse nuevamente para evitar conservar datos
         * globales cuando el permiso "ver todos" ya no esté habilitado.
         */
        private const int VersionPreparacionActual = 3;

        private const string PreparacionCompletaClavePrefijo =
            "offline_global_preparado_v3_";

        private const string PreparacionFechaClavePrefijo =
            "offline_global_preparado_fecha_v3_";

        private const string PreparacionPerfilClavePrefijo =
            "offline_global_preparado_perfil_v3_";

        private static readonly Lazy<
            SincronizacionOfflineGlobalService> lazy =
                new(() => new SincronizacionOfflineGlobalService());

        private readonly SemaphoreSlim syncLock = new(1, 1);

        private readonly JsonSerializerOptions jsonOptions =
            new(JsonSerializerDefaults.Web)
            {
                PropertyNameCaseInsensitive = true
            };

        private SincronizacionOfflineGlobalEstado estado = new();

        /*
         * Esta bandera existe únicamente en memoria. Si la aplicación se
         * cierra, vuelve a false automáticamente porque el proceso desaparece.
         * De esta forma podemos distinguir una descarga realmente viva de un
         * estado SINCRONIZANDO que quedó persistido por una ejecución anterior.
         */
        private bool descargaActivaEnProceso;
        private string usuarioDescargaActiva = string.Empty;

        public static SincronizacionOfflineGlobalService Instance =>
            lazy.Value;

        public static bool EstaPreparadoParaUsuario(
            string? usuarioId)
        {
            if (string.IsNullOrWhiteSpace(usuarioId) ||
                usuarioId == "0")
            {
                return false;
            }

            return Preferences.Get(
                ConstruirClavePreparacion(
                    usuarioId.Trim()),
                false);
        }

        public static bool CoincidePerfilPreparacion(
            string? usuarioId,
            bool requiereNoticias,
            bool requiereAlbum,
            bool puedeVerTodosAnalisis)
        {
            if (string.IsNullOrWhiteSpace(usuarioId) ||
                usuarioId == "0")
            {
                return false;
            }

            string esperado =
                ConstruirPerfilPermisos(
                    requiereNoticias,
                    requiereAlbum,
                    puedeVerTodosAnalisis);

            string guardado =
                Preferences.Get(
                    ConstruirClavePerfilPreparacion(
                        usuarioId.Trim()),
                    string.Empty);

            return string.Equals(
                guardado,
                esperado,
                StringComparison.Ordinal);
        }

        public static DateTime? ObtenerFechaPreparacionUsuario(
            string? usuarioId)
        {
            if (string.IsNullOrWhiteSpace(usuarioId) ||
                usuarioId == "0")
            {
                return null;
            }

            string value = Preferences.Get(
                ConstruirClaveFechaPreparacion(
                    usuarioId.Trim()),
                string.Empty);

            return DateTime.TryParse(
                value,
                out DateTime result)
                    ? result
                    : null;
        }

        public event EventHandler<
            SincronizacionOfflineGlobalEventArgs>? EstadoCambiado;

        private SincronizacionOfflineGlobalService()
        {
            estado = CargarEstado();

            AnalisisHistorialDescargaService.Instance
                .ProgresoCambiado += OnProgresoAnalisis;
        }

        public async Task<SincronizacionOfflineGlobalEstado>
            ObtenerEstadoAsync()
        {
            estado = CargarEstado();
            estado = await AgregarTamanoActualAsync(estado);
            return estado;
        }

        public async Task<ResultadoSincronizacionOfflineGlobal>
            DescargarOActualizarTodoAsync(
                CancellationToken cancellationToken = default)
        {
            if (!DatosSinConexionPermisos.TienePermiso)
            {
                return ResultadoSincronizacionOfflineGlobal.Fail(
                    "Su usuario no tiene habilitados los datos sin conexión.",
                    conservaCopiaAnterior: estado.PreparacionCompleta);
            }

            if (!ModoSesionService.EsEnLinea)
            {
                return ResultadoSincronizacionOfflineGlobal.Fail(
                    "Descargar todo solamente está disponible durante una sesión en línea.",
                    conservaCopiaAnterior: estado.PreparacionCompleta);
            }

            bool entered = await syncLock.WaitAsync(
                TimeSpan.Zero,
                cancellationToken);

            if (!entered)
            {
                return ResultadoSincronizacionOfflineGlobal.Fail(
                    "Ya existe una descarga completa en curso.",
                    conservaCopiaAnterior: estado.PreparacionCompleta);
            }

            string usuarioActual = ObtenerUsuarioId();
            descargaActivaEnProceso = true;
            usuarioDescargaActiva = usuarioActual;

            SincronizacionOfflineGlobalEstado anterior =
                CargarEstado();

            const int totalPasos = 5;
            int paso = 0;

            ModuloOfflineResumen motor = CrearPendiente(
                "Motor de cálculo");
            ModuloOfflineResumen catalogos = CrearPendiente(
                "Catálogos y terrenos");
            ModuloOfflineResumen analisis = CrearPendiente(
                "Historial de análisis");
            ModuloOfflineResumen noticias =
                PuedeDescargarNoticias()
                    ? CrearPendiente("Noticias")
                    : CrearNoHabilitado("Noticias");
            ModuloOfflineResumen album =
                PuedeDescargarAlbum()
                    ? CrearPendiente("Álbum de fotos")
                    : CrearNoHabilitado("Álbum de fotos");

            try
            {
                /*
                 * Algunos servicios anteriores consultan esta bandera antes de
                 * descargar. Se marca disponible porque el modo online ya fue
                 * seleccionado; la respuesta real de la API sigue siendo la
                 * validación definitiva.
                 */
                EstadoConexionService.Instance
                    .ReportarServidorDisponible();

                string versionTransaccional =
                    "preparacion-v" +
                    VersionPreparacionActual +
                    "-" +
                    DateTime.UtcNow.ToString(
                        "yyyyMMddHHmmssfff") +
                    "-" +
                    Guid.NewGuid().ToString("N");

                using IDisposable scope =
                    DescargaOfflineContext.Iniciar(
                        versionTransaccional);

                estado = CrearEstado(
                    SincronizacionOfflineGlobalEstados.Sincronizando,
                    "Preparando datos sin conexión",
                    "Iniciando descarga manual...",
                    0,
                    0,
                    totalPasos,
                    preparacionCompleta: anterior.PreparacionCompleta,
                    motor,
                    catalogos,
                    analisis,
                    noticias,
                    album,
                    anterior.UltimaSincronizacionCompletaUtc);
                GuardarYNotificar(estado);

                /* Envío único de operaciones pendientes al iniciar la tarea. */
                await AnalisisOfflineSincronizacionService.Instance
                    .SincronizarAhoraAsync(cancellationToken);

                motor = CrearEnCurso(
                    "Motor de cálculo",
                    "Descargando reglas, precios y parámetros...");
                ActualizarPaso(
                    paso,
                    totalPasos,
                    "Descargando motor de cálculo...",
                    motor,
                    catalogos,
                    analisis,
                    noticias,
                    album,
                    anterior);

                ResultadoDescargaMotor motorResult =
                    await MotorCalculoPaqueteService.Instance
                        .DescargarOActualizarAsync(
                            forzar: true,
                            cancellationToken: cancellationToken);

                if (!motorResult.Success)
                    throw new InvalidOperationException(
                        motorResult.Message);

                motor = CrearListo(
                    "Motor de cálculo",
                    motorResult.Message,
                    motorResult.TotalRegistros);
                paso++;

                catalogos = CrearEnCurso(
                    "Catálogos y terrenos",
                    "Descargando catálogos completos...");
                ActualizarPaso(
                    paso,
                    totalPasos,
                    "Descargando catálogos y terrenos...",
                    motor,
                    catalogos,
                    analisis,
                    noticias,
                    album,
                    anterior);

                ResultadoDescargaOffline catalogosResult =
                    await PaqueteCatalogosOfflineService.Instance
                        .DescargarTodoAsync(forzar: true);

                if (!catalogosResult.Success)
                    throw new InvalidOperationException(
                        catalogosResult.Message);

                catalogos = CrearListo(
                    "Catálogos y terrenos",
                    catalogosResult.Message,
                    catalogosResult.TotalRegistros);
                paso++;

                analisis = CrearEnCurso(
                    "Historial de análisis",
                    PuedeDescargarTodosLosAnalisis()
                        ? "Descargando análisis autorizados, detalles y reportes..."
                        : "Descargando sus análisis, detalles y reportes...");
                ActualizarPaso(
                    paso,
                    totalPasos,
                    "Descargando historial de análisis...",
                    motor,
                    catalogos,
                    analisis,
                    noticias,
                    album,
                    anterior);

                AnalisisHistorialDescargaResultado analisisResult =
                    await AnalisisHistorialDescargaService.Instance
                        .DescargarTodoAsync(cancellationToken);

                if (!analisisResult.Success)
                    throw new InvalidOperationException(
                        analisisResult.Message);

                analisis = CrearListo(
                    "Historial de análisis",
                    analisisResult.Message,
                    analisisResult.TotalAnalisis);
                paso++;

                if (PuedeDescargarNoticias())
                {
                    noticias = CrearEnCurso(
                        "Noticias",
                        "Descargando publicaciones e imágenes...");
                    ActualizarPaso(
                        paso,
                        totalPasos,
                        "Descargando noticias...",
                        motor,
                        catalogos,
                        analisis,
                        noticias,
                        album,
                        anterior);

                    NoticiasOfflineSyncResult noticiasResult =
                        await NoticiasOfflineSyncService.Instance
                            .SincronizarSiNecesarioAsync(
                                forzarDescargaCompleta: true,
                                cancellationToken: cancellationToken);

                    if (!noticiasResult.Success)
                        throw new InvalidOperationException(
                            noticiasResult.Message);

                    noticias = CrearListo(
                        "Noticias",
                        noticiasResult.Message,
                        noticiasResult.TotalPublicaciones);
                }
                paso++;

                if (PuedeDescargarAlbum())
                {
                    album = CrearEnCurso(
                        "Álbum de fotos",
                        "Descargando álbum y fotografías...");
                    ActualizarPaso(
                        paso,
                        totalPasos,
                        "Descargando álbum de fotos...",
                        motor,
                        catalogos,
                        analisis,
                        noticias,
                        album,
                        anterior);

                    AlbumOfflineSyncResult albumResult =
                        await AlbumOfflineSyncService.Instance
                            .SincronizarSiNecesarioAsync(
                                forzarDescargaCompleta: true,
                                cancellationToken: cancellationToken);

                    if (!albumResult.Success)
                        throw new InvalidOperationException(
                            albumResult.Message);

                    album = CrearListo(
                        "Álbum de fotos",
                        albumResult.Message,
                        albumResult.TotalRecords,
                        albumResult.TotalPhotos);
                }
                paso++;

                /*
                 * No se marca el dispositivo como preparado solamente porque
                 * los servicios devolvieron Success. Se comprueba que SQLite
                 * contenga las rutas exactas que consumen las pantallas.
                 */
                await ValidarRutasObligatoriasAsync(
                    cancellationToken);

                estado = CrearEstado(
                    SincronizacionOfflineGlobalEstados.Listo,
                    "Dispositivo preparado",
                    "Todos los datos necesarios fueron descargados manualmente.",
                    100,
                    totalPasos,
                    totalPasos,
                    preparacionCompleta: true,
                    motor,
                    catalogos,
                    analisis,
                    noticias,
                    album,
                    DateTime.UtcNow);

                estado = await AgregarTamanoActualAsync(estado);
                GuardarYNotificar(estado);

                string usuarioPreparado =
                    ObtenerUsuarioId();

                Preferences.Set(
                    ConstruirClavePreparacion(
                        usuarioPreparado),
                    true);

                Preferences.Set(
                    ConstruirClaveFechaPreparacion(
                        usuarioPreparado),
                    DateTime.UtcNow.ToString("O"));

                Preferences.Set(
                    ConstruirClavePerfilPreparacion(
                        usuarioPreparado),
                    ConstruirPerfilPermisos(
                        PuedeDescargarNoticias(),
                        PuedeDescargarAlbum(),
                        PuedeDescargarTodosLosAnalisis()));

                /*
                 * Se guarda la versión exacta que acaba de descargarse. Si la
                 * comprobación ligera falla, la descarga continúa siendo válida
                 * y se volverá a consultar al entrar nuevamente a la página.
                 */
                try
                {
                    await SincronizacionOfflineManifiestoService.Instance
                        .RegistrarDescargaActualAsync(
                            cancellationToken);
                }
                catch
                {
                    /* El manifiesto nunca invalida una descarga ya completada. */
                }

                return ResultadoSincronizacionOfflineGlobal.Ok(
                    "El dispositivo quedó preparado para trabajar sin conexión.");
            }
            catch (OperationCanceledException)
            {
                estado = await CrearEstadoErrorAsync(
                    anterior,
                    "La descarga fue cancelada. Se conserva la copia anterior.",
                    motor,
                    catalogos,
                    analisis,
                    noticias,
                    album,
                    totalPasos);
                throw;
            }
            catch (Exception ex)
            {
                estado = await CrearEstadoErrorAsync(
                    anterior,
                    ex.Message,
                    motor,
                    catalogos,
                    analisis,
                    noticias,
                    album,
                    totalPasos);

                return ResultadoSincronizacionOfflineGlobal.Fail(
                    estado.Detalle,
                    anterior.PreparacionCompleta);
            }
            finally
            {
                descargaActivaEnProceso = false;
                usuarioDescargaActiva = string.Empty;
                syncLock.Release();
            }
        }

        /// <summary>
        /// Se conserva para compatibilidad con código anterior. No ejecuta
        /// ninguna verificación ni solicitud.
        /// </summary>
        public void VerificarActualizacionesEnSegundoPlano()
        {
        }

        public Task MarcarActualizacionDisponibleAsync(
            string mensaje)
        {
            SincronizacionOfflineGlobalEstado actual = CargarEstado();

            if (!actual.PreparacionCompleta)
                return Task.CompletedTask;

            estado = CopiarEstado(
                actual,
                estado:
                    SincronizacionOfflineGlobalEstados
                        .ActualizacionDisponible,
                mensaje: "Hay cambios para descargar",
                detalle: string.IsNullOrWhiteSpace(mensaje)
                    ? "Use Actualizar todo cuando lo considere necesario."
                    : mensaje,
                ultimaVerificacionUtc: DateTime.UtcNow);

            GuardarYNotificar(estado);
            return Task.CompletedTask;
        }

        private void OnProgresoAnalisis(
            object? sender,
            AnalisisHistorialDescargaProgreso e)
        {
            if (!estado.SincronizacionEnCurso)
                return;

            int baseProgress = 40;
            int tramo = 20;
            int progress = baseProgress +
                (int)Math.Round(
                    tramo * e.Porcentaje / 100d);

            estado = CopiarEstado(
                estado,
                detalle: e.Mensaje,
                progreso: progress,
                analisis: new ModuloOfflineResumen
                {
                    Nombre = "Historial de análisis",
                    Estado = ModuloOfflineEstados.Sincronizando,
                    Mensaje = e.Mensaje,
                    Registros = e.Procesados
                });

            GuardarYNotificar(estado);
        }

        private void ActualizarPaso(
            int paso,
            int totalPasos,
            string detalle,
            ModuloOfflineResumen motor,
            ModuloOfflineResumen catalogos,
            ModuloOfflineResumen analisis,
            ModuloOfflineResumen noticias,
            ModuloOfflineResumen album,
            SincronizacionOfflineGlobalEstado anterior)
        {
            estado = CrearEstado(
                SincronizacionOfflineGlobalEstados.Sincronizando,
                "Preparando datos sin conexión",
                detalle,
                CalcularProgreso(paso, totalPasos),
                paso,
                totalPasos,
                anterior.PreparacionCompleta,
                motor,
                catalogos,
                analisis,
                noticias,
                album,
                anterior.UltimaSincronizacionCompletaUtc);

            GuardarYNotificar(estado);
        }

        private async Task<SincronizacionOfflineGlobalEstado>
            CrearEstadoErrorAsync(
                SincronizacionOfflineGlobalEstado anterior,
                string error,
                ModuloOfflineResumen motor,
                ModuloOfflineResumen catalogos,
                ModuloOfflineResumen analisis,
                ModuloOfflineResumen noticias,
                ModuloOfflineResumen album,
                int totalPasos)
        {
            SincronizacionOfflineGlobalEstado result = CrearEstado(
                SincronizacionOfflineGlobalEstados.Error,
                anterior.PreparacionCompleta
                    ? "Se conserva la copia anterior"
                    : "Descarga incompleta",
                string.IsNullOrWhiteSpace(error)
                    ? "No fue posible completar la descarga."
                    : error,
                estado.ProgresoPorcentaje,
                estado.PasoActual,
                totalPasos,
                anterior.PreparacionCompleta,
                motor,
                catalogos,
                analisis,
                noticias,
                album,
                anterior.UltimaSincronizacionCompletaUtc);

            result = await AgregarTamanoActualAsync(result);
            GuardarYNotificar(result);
            return result;
        }

        private async Task<SincronizacionOfflineGlobalEstado>
            AgregarTamanoActualAsync(
                SincronizacionOfflineGlobalEstado source)
        {
            long total =
                ImagenLocalCacheService.ObtenerTamanoFisicoBytes() +
                MotorCalculoPaqueteService.Instance
                    .ObtenerTamanoPaqueteBytes() +
                AnalisisHistorialLocalService.Instance
                    .ObtenerTamanoBytes() +
                AnalisisOfflineDatabaseService.Instance
                    .ObtenerTamanoBytes();

            try
            {
                string path =
                    ContenidoLocalDatabaseService.Instance.DatabasePath;

                if (File.Exists(path))
                    total += new FileInfo(path).Length;
            }
            catch
            {
            }

            await Task.CompletedTask;

            return CopiarEstado(
                source,
                tamanoTotalBytes: total);
        }

        private static async Task ValidarRutasObligatoriasAsync(
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string usuarioId = ObtenerUsuarioId();
            string version =
                DescargaOfflineContext.VersionTransaccional;

            if (usuarioId == "0" ||
                string.IsNullOrWhiteSpace(version))
            {
                throw new InvalidOperationException(
                    "No fue posible identificar el paquete de descarga.");
            }

            if (PuedeDescargarNoticias())
            {
                await ExigirRutaAsync(
                    usuarioId,
                    "noticias",
                    "/api/publicacion/categorias",
                    version,
                    cancellationToken);

                foreach (int pageSize in new[] { 12, 6 })
                {
                    await ExigirRutaAsync(
                        usuarioId,
                        "noticias",
                        "/api/publicacion/feed" +
                        "?pagina=1" +
                        $"&tamanoPagina={pageSize}" +
                        "&soloDestacadas=false" +
                        "&soloEventos=false",
                        version,
                        cancellationToken);
                }
            }

            if (PuedeDescargarAlbum())
            {
                int pageSize =
                    DeviceInfo.Platform ==
                        DevicePlatform.WinUI
                        ? 12
                        : 6;

                /*
                 * AlbumOfflineSyncService utiliza la jerarquía nueva. La
                 * validación anterior todavía buscaba rutas legacy
                 * /api/album-botanico y por eso una descarga correcta terminaba
                 * marcada falsamente como incompleta.
                 */
                await ExigirRutaAsync(
                    usuarioId,
                    "album",
                    "/api/album-jerarquia/inicio" +
                    $"?tamanoPagina={pageSize}",
                    version,
                    cancellationToken);

                await ExigirRutaAsync(
                    usuarioId,
                    "album",
                    "/api/album-jerarquia/galeria-paginada" +
                    "?incluirInactivos=false" +
                    "&pagina=1" +
                    $"&tamanoPagina={pageSize}",
                    version,
                    cancellationToken);
            }
        }

        private static async Task ExigirRutaAsync(
            string usuarioId,
            string modulo,
            string ruta,
            string version,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string cacheKey = CalcularHash(
                $"{usuarioId}|{modulo}|{ruta}");

            ContenidoRespuestaCacheEntity? respuesta =
                await ContenidoLocalDatabaseService.Instance
                    .ObtenerRespuestaAsync(cacheKey);

            if (respuesta == null ||
                !string.Equals(
                    respuesta.Version,
                    version,
                    StringComparison.Ordinal) ||
                string.IsNullOrWhiteSpace(
                    respuesta.Json))
            {
                throw new InvalidOperationException(
                    $"La descarga de {modulo} no contiene todas las rutas obligatorias. Se conserva la copia anterior.");
            }
        }

        private static string CalcularHash(
            string value)
        {
            byte[] hash = SHA256.HashData(
                Encoding.UTF8.GetBytes(value));

            return Convert.ToHexString(hash)
                .ToLowerInvariant();
        }

        private static bool PuedeDescargarNoticias() =>
            PermissionService.Instance.HasRead(
                InterfazCodigos.Noticias);

        private static bool PuedeDescargarAlbum() =>
            PermissionService.Instance.HasRead(
                InterfazCodigos.AlbumFotos);

        private static bool PuedeDescargarTodosLosAnalisis() =>
            PermissionService.Instance.HasRead(
                InterfazCodigos.AnalisisSueloTodos);

        private static ModuloOfflineResumen CrearPendiente(
            string nombre) =>
            new()
            {
                Nombre = nombre,
                Estado = ModuloOfflineEstados.Pendiente,
                Mensaje = "Pendiente."
            };

        private static ModuloOfflineResumen CrearEnCurso(
            string nombre,
            string mensaje) =>
            new()
            {
                Nombre = nombre,
                Estado = ModuloOfflineEstados.Sincronizando,
                Mensaje = mensaje
            };

        private static ModuloOfflineResumen CrearListo(
            string nombre,
            string mensaje,
            int registros,
            int imagenes = 0) =>
            new()
            {
                Nombre = nombre,
                Estado = ModuloOfflineEstados.Listo,
                Mensaje = mensaje,
                Registros = registros,
                Imagenes = imagenes
            };

        private static ModuloOfflineResumen CrearNoHabilitado(
            string nombre) =>
            new()
            {
                Nombre = nombre,
                Estado = ModuloOfflineEstados.NoHabilitado,
                Mensaje = "No habilitado para este usuario."
            };

        private static int CalcularProgreso(
            int paso,
            int totalPasos) =>
            totalPasos <= 0
                ? 0
                : Math.Clamp(
                    (int)Math.Round(
                        paso * 100d / totalPasos),
                    0,
                    100);

        private static SincronizacionOfflineGlobalEstado CrearEstado(
            string estado,
            string mensaje,
            string detalle,
            int progreso,
            int paso,
            int totalPasos,
            bool preparacionCompleta,
            ModuloOfflineResumen motor,
            ModuloOfflineResumen catalogos,
            ModuloOfflineResumen analisis,
            ModuloOfflineResumen noticias,
            ModuloOfflineResumen album,
            DateTime? ultimaCompleta) =>
            new()
            {
                Estado = estado,
                Mensaje = mensaje,
                Detalle = detalle,
                ProgresoPorcentaje = progreso,
                PasoActual = paso,
                TotalPasos = totalPasos,
                PreparacionCompleta = preparacionCompleta,
                UltimaSincronizacionCompletaUtc = ultimaCompleta,
                UltimaVerificacionUtc = DateTime.UtcNow,
                MotorCalculo = motor,
                Catalogos = catalogos,
                Analisis = analisis,
                Noticias = noticias,
                Album = album
            };

        private static SincronizacionOfflineGlobalEstado CopiarEstado(
            SincronizacionOfflineGlobalEstado origen,
            string? estado = null,
            string? mensaje = null,
            string? detalle = null,
            int? progreso = null,
            int? pasoActual = null,
            int? totalPasos = null,
            bool? preparacionCompleta = null,
            DateTime? ultimaSincronizacionCompletaUtc = null,
            DateTime? ultimaVerificacionUtc = null,
            long? tamanoTotalBytes = null,
            ModuloOfflineResumen? motorCalculo = null,
            ModuloOfflineResumen? catalogos = null,
            ModuloOfflineResumen? analisis = null,
            ModuloOfflineResumen? noticias = null,
            ModuloOfflineResumen? album = null) =>
            new()
            {
                Estado = estado ?? origen.Estado,
                Mensaje = mensaje ?? origen.Mensaje,
                Detalle = detalle ?? origen.Detalle,
                ProgresoPorcentaje =
                    progreso ?? origen.ProgresoPorcentaje,
                PasoActual = pasoActual ?? origen.PasoActual,
                TotalPasos = totalPasos ?? origen.TotalPasos,
                PreparacionCompleta =
                    preparacionCompleta ?? origen.PreparacionCompleta,
                UltimaSincronizacionCompletaUtc =
                    ultimaSincronizacionCompletaUtc ??
                    origen.UltimaSincronizacionCompletaUtc,
                UltimaVerificacionUtc =
                    ultimaVerificacionUtc ??
                    origen.UltimaVerificacionUtc,
                TamanoTotalBytes =
                    tamanoTotalBytes ?? origen.TamanoTotalBytes,
                MotorCalculo = motorCalculo ?? origen.MotorCalculo,
                Catalogos = catalogos ?? origen.Catalogos,
                Analisis = analisis ?? origen.Analisis,
                Noticias = noticias ?? origen.Noticias,
                Album = album ?? origen.Album
            };

        private void GuardarYNotificar(
            SincronizacionOfflineGlobalEstado value)
        {
            estado = value;
            string usuarioId = ObtenerUsuarioId();

            if (usuarioId != "0")
            {
                Preferences.Set(
                    ConstruirClaveEstado(usuarioId),
                    JsonSerializer.Serialize(
                        value,
                        jsonOptions));
            }

            EstadoCambiado?.Invoke(
                this,
                new SincronizacionOfflineGlobalEventArgs(value));
        }

        private SincronizacionOfflineGlobalEstado CargarEstado()
        {
            string usuarioId = ObtenerUsuarioId();
            if (usuarioId == "0")
                return new SincronizacionOfflineGlobalEstado();

            string json = Preferences.Get(
                ConstruirClaveEstado(usuarioId),
                string.Empty);

            if (string.IsNullOrWhiteSpace(json))
                return new SincronizacionOfflineGlobalEstado();

            try
            {
                SincronizacionOfflineGlobalEstado cargado =
                    JsonSerializer.Deserialize<
                        SincronizacionOfflineGlobalEstado>(
                        json,
                        jsonOptions)
                    ?? new SincronizacionOfflineGlobalEstado();

                if (!cargado.SincronizacionEnCurso ||
                    EsDescargaActivaEnProceso(usuarioId))
                {
                    return cargado;
                }

                /*
                 * SINCRONIZANDO solo es válido mientras existe una tarea real
                 * en memoria para este mismo usuario. Después de cerrar la app
                 * esa tarea desaparece, aunque Preferences conserve el último
                 * porcentaje. En ese caso se convierte automáticamente a error
                 * recuperable y se habilita nuevamente Descargar todo.
                 */
                SincronizacionOfflineGlobalEstado recuperado =
                    CopiarEstado(
                        cargado,
                        estado: SincronizacionOfflineGlobalEstados.Error,
                        mensaje: cargado.PreparacionCompleta
                            ? "Actualización anterior interrumpida"
                            : "Descarga anterior interrumpida",
                        detalle: cargado.PreparacionCompleta
                            ? "La actualización anterior no terminó. La copia offline completa anterior continúa disponible. Puede usar Actualizar todo para intentarlo nuevamente."
                            : "La descarga anterior no terminó. Use Descargar todo para iniciar una preparación nueva.",
                        progreso: 0,
                        pasoActual: 0,
                        ultimaVerificacionUtc: DateTime.UtcNow,
                        motorCalculo: RecuperarModuloInterrumpido(
                            cargado.MotorCalculo),
                        catalogos: RecuperarModuloInterrumpido(
                            cargado.Catalogos),
                        analisis: RecuperarModuloInterrumpido(
                            cargado.Analisis),
                        noticias: RecuperarModuloInterrumpido(
                            cargado.Noticias),
                        album: RecuperarModuloInterrumpido(
                            cargado.Album));

                Preferences.Set(
                    ConstruirClaveEstado(usuarioId),
                    JsonSerializer.Serialize(
                        recuperado,
                        jsonOptions));

                return recuperado;
            }
            catch
            {
                return new SincronizacionOfflineGlobalEstado();
            }
        }

        private bool EsDescargaActivaEnProceso(
            string usuarioId) =>
            descargaActivaEnProceso &&
            string.Equals(
                usuarioDescargaActiva,
                usuarioId,
                StringComparison.Ordinal);

        private static ModuloOfflineResumen RecuperarModuloInterrumpido(
            ModuloOfflineResumen modulo)
        {
            if (modulo.Estado != ModuloOfflineEstados.Sincronizando)
                return modulo;

            return new ModuloOfflineResumen
            {
                Nombre = modulo.Nombre,
                Estado = ModuloOfflineEstados.Error,
                Mensaje = "La descarga fue interrumpida. Inicie nuevamente la preparación.",
                Registros = modulo.Registros,
                Imagenes = modulo.Imagenes
            };
        }

        private static string ObtenerUsuarioId()
        {
            string value = Preferences.Get(
                SessionKeys.KeyUserId,
                "0");

            return string.IsNullOrWhiteSpace(value)
                ? "0"
                : value.Trim();
        }

        private static string ConstruirClaveEstado(
            string usuarioId) =>
            EstadoClavePrefijo + usuarioId;

        private static string ConstruirClavePreparacion(
            string usuarioId) =>
            PreparacionCompletaClavePrefijo +
            usuarioId;

        private static string ConstruirClaveFechaPreparacion(
            string usuarioId) =>
            PreparacionFechaClavePrefijo +
            usuarioId;

        private static string ConstruirClavePerfilPreparacion(
            string usuarioId) =>
            PreparacionPerfilClavePrefijo +
            usuarioId;

        private static string ConstruirPerfilPermisos(
            bool noticias,
            bool album,
            bool analisisTodos) =>
            $"N:{(noticias ? 1 : 0)}|" +
            $"A:{(album ? 1 : 0)}|" +
            $"T:{(analisisTodos ? 1 : 0)}";
    }
}
