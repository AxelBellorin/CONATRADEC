using CONATRADEC.Models;
using Microsoft.Maui.Devices;
using Microsoft.Maui.Storage;

namespace CONATRADEC.Services
{
    /// <summary>
    /// Descarga el contenido activo completo del Álbum Botánico para que pueda
    /// consultarse sin conexión.
    ///
    /// Se almacenan:
    /// - la carga inicial;
    /// - todas las páginas de la galería;
    /// - todas las páginas de cada categoría;
    /// - el detalle de cada registro;
    /// - miniaturas y fotografías internas.
    ///
    /// Las respuestas continúan pasando por ContenidoSincronizacionHandler,
    /// por lo que se guardan en SQLite con la versión vigente.
    /// </summary>
    public sealed class AlbumOfflineSyncService
    {
        private static readonly Lazy<AlbumOfflineSyncService> lazy =
            new(() => new AlbumOfflineSyncService());

        private readonly SemaphoreSlim syncLock = new(1, 1);
        private readonly AlbumBotanicoCargaApiService cargaApiService = new();
        private readonly AlbumBotanicoApiService albumApiService = new();

        private const string ClaveVersionCompletaPrefijo =
            "contenido_offline_album_version_completa_";

        public static AlbumOfflineSyncService Instance => lazy.Value;

        private AlbumOfflineSyncService()
        {
        }

        public void MarcarPendiente()
        {
            string usuarioId =
                ObtenerUsuarioId();

            if (usuarioId != "0")
            {
                Preferences.Remove(
                    ConstruirClaveVersionCompleta(
                        usuarioId));
            }

            ContenidoSincronizacionRuntime
                .Invalidar("album");
        }

        /// <summary>
        /// Comprueba la versión del servidor con una solicitud ligera.
        /// Solamente ejecuta la descarga completa cuando:
        /// - el usuario la fuerza manualmente;
        /// - la versión cambió;
        /// - la versión actual nunca terminó de descargarse por completo.
        /// </summary>
        public async Task<AlbumOfflineSyncResult>
            SincronizarSiNecesarioAsync(
                bool forzarDescargaCompleta,
                CancellationToken cancellationToken = default)
        {
            ContenidoSincronizacionRuntime.Invalidar("album");

            int pageSize =
                DeviceInfo.Platform == DevicePlatform.WinUI
                    ? 12
                    : 6;

            ApiResult<AlbumInicioResponse> verification =
                await cargaApiService.GetInicioAsync(
                    pageSize,
                    cancellationToken);

            if (!verification.Success ||
                verification.Data == null)
            {
                return FailWithState(
                    verification.Message,
                    "No fue posible comprobar cambios nuevos del álbum.");
            }

            string userId = ObtenerUsuarioId();
            string stateKey = $"{userId}|album";

            ContenidoModuloEstadoEntity? state =
                await ContenidoLocalDatabaseService.Instance
                    .ObtenerEstadoAsync(stateKey);

            string currentVersion =
                !string.IsNullOrWhiteSpace(state?.VersionServidor)
                    ? state!.VersionServidor
                    : state?.Version ?? string.Empty;

            if (string.IsNullOrWhiteSpace(currentVersion))
            {
                return FailWithState(
                    "No se pudo confirmar la versión actual del álbum.",
                    "La copia local existente se conservará.");
            }

            if (!forzarDescargaCompleta &&
                EstaVersionCompleta(
                    userId,
                    currentVersion))
            {
                DateTime? lastSync =
                    state?.UltimaSincronizacionExitosaUtc;

                bool conectado =
                    EstadoConexionService.Instance.HayInternet;

                ContenidoEstadoService.Instance.Actualizar(
                    "album",
                    conectado
                        ? TipoEstadoSincronizacionContenido.Local
                        : TipoEstadoSincronizacionContenido.SinConexionLocal,
                    conectado
                        ? "Conectado · usando datos sincronizados"
                        : "Sin conexión · usando datos sincronizados",
                    conectado
                        ? "Datos sincronizados anteriormente · versión validada con el servidor · " +
                          ContenidoEstadoService.ConstruirDetalleFecha(
                              lastSync)
                        : "Datos sincronizados anteriormente · " +
                          ContenidoEstadoService.ConstruirDetalleFecha(
                              lastSync),
                    currentVersion,
                    lastSync);

                return AlbumOfflineSyncResult.Ok(
                    verification.Data.Galeria.TotalRegistros,
                    0,
                    "El álbum local ya contiene la versión vigente.");
            }

            return await SincronizarAsync(
                cancellationToken);
        }

        public async Task<AlbumOfflineSyncResult> SincronizarAsync(
            CancellationToken cancellationToken = default)
        {
            bool entered = await syncLock.WaitAsync(
                TimeSpan.Zero,
                cancellationToken);

            if (!entered)
            {
                return AlbumOfflineSyncResult.Fail(
                    "Ya existe una sincronización del álbum en curso.");
            }

            try
            {
                ContenidoEstadoService.Instance.Actualizar(
                    "album",
                    TipoEstadoSincronizacionContenido.Verificando,
                    "Sincronizando álbum completo...",
                    "Comprobando categorías y registros disponibles.");

                ContenidoSincronizacionRuntime.Invalidar("album");

                int pageSize =
                    DeviceInfo.Platform == DevicePlatform.WinUI
                        ? 12
                        : 6;

                ApiResult<AlbumInicioResponse> initialResult =
                    await cargaApiService.GetInicioAsync(
                        pageSize,
                        cancellationToken);

                if (!initialResult.Success ||
                    initialResult.Data == null)
                {
                    return FailWithState(
                        initialResult.Message,
                        "No fue posible descargar el inicio del álbum.");
                }

                List<CategoriaAlbumBotanicoResponse> categories =
                    initialResult.Data.Categorias
                        .Where(x => x.Activo)
                        .ToList();

                var records = new Dictionary<
                    int,
                    AlbumGaleriaItemResponse>();

                AddRecords(
                    records,
                    initialResult.Data.Galeria.Items);

                /*
                 * La pantalla utiliza /galeria-paginada al seleccionar Todos,
                 * aunque la primera entrada se cargue con /inicio. Se guardan
                 * ambas rutas para que regresar a Todos funcione offline.
                 */
                AlbumPagesResult allPages =
                    await DownloadPagesAsync(
                        categoryId: null,
                        pageSize,
                        records,
                        cancellationToken);

                if (!allPages.Success)
                {
                    return FailWithState(
                        allPages.Message,
                        "No fue posible descargar la galería completa.");
                }

                int currentCategory = 0;

                foreach (CategoriaAlbumBotanicoResponse category
                         in categories)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    currentCategory++;

                    ContenidoEstadoService.Instance.Actualizar(
                        "album",
                        TipoEstadoSincronizacionContenido.Verificando,
                        "Descargando categorías...",
                        $"{currentCategory} de {categories.Count}: " +
                        category.NombreCategoria);

                    AlbumPagesResult categoryPages =
                        await DownloadPagesAsync(
                            category.CategoriaAlbumBotanicoId,
                            pageSize,
                            records,
                            cancellationToken);

                    if (!categoryPages.Success)
                    {
                        return FailWithState(
                            categoryPages.Message,
                            $"No fue posible descargar la categoría " +
                            $"'{category.NombreCategoria}'.");
                    }
                }

                List<AlbumGaleriaItemResponse> activeRecords =
                    records.Values
                        .Where(x => x.Activo && x.CategoriaActiva)
                        .OrderBy(x => x.AlbumBotanicoCafeId)
                        .ToList();

                int currentRecord = 0;
                int totalPhotos = 0;

                foreach (AlbumGaleriaItemResponse item in activeRecords)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    currentRecord++;

                    ContenidoEstadoService.Instance.Actualizar(
                        "album",
                        TipoEstadoSincronizacionContenido.Verificando,
                        "Descargando detalles y fotografías...",
                        $"{currentRecord} de {activeRecords.Count}: " +
                        item.Titulo);

                    /*
                     * Se usa incluirInactivos=false para que el detalle activo
                     * tenga una ruta pública cacheable. La administración de
                     * registros inactivos continúa requiriendo internet.
                     */
                    ApiResult<AlbumDetalleResponse> detailResult =
                        await albumApiService.GetDetalleAsync(
                            item.AlbumBotanicoCafeId,
                            incluirInactivos: false,
                            cancellationToken: cancellationToken);

                    if (!detailResult.Success ||
                        detailResult.Data == null)
                    {
                        return FailWithState(
                            detailResult.Message,
                            $"No fue posible descargar el detalle de " +
                            $"'{item.Titulo}'.");
                    }

                    totalPhotos += detailResult.Data.Fotos.Count;
                }

                string userId = ObtenerUsuarioId();

                string stateKey = $"{userId}|album";

                ContenidoModuloEstadoEntity? state =
                    await ContenidoLocalDatabaseService.Instance
                        .ObtenerEstadoAsync(stateKey);

                string version = state?.Version ?? string.Empty;

                if (string.IsNullOrWhiteSpace(version))
                {
                    return FailWithState(
                        "No se pudo confirmar la versión local del álbum.",
                        "Los datos descargados se conservarán para el próximo intento.");
                }

                /*
                 * La versión anterior se elimina únicamente después de
                 * completar categorías, páginas, detalles y fotografías.
                 */
                await ContenidoLocalDatabaseService.Instance
                    .EliminarRespuestasVersionAnteriorAsync(
                        userId,
                        "album",
                        version);

                await ImagenLocalCacheService
                    .LimpiarVersionAnteriorAsync(
                        userId,
                        "album",
                        version);

                await ImagenLocalCacheService
                    .AplicarLimiteAsync();

                MarcarVersionCompleta(
                    userId,
                    version);

                DateTime now = DateTime.UtcNow;

                /*
                 * Para llegar hasta aquí todas las solicitudes del álbum
                 * respondieron correctamente. Por tanto, la API está accesible
                 * aunque Windows todavía no haya actualizado su indicador.
                 */
                EstadoConexionService.Instance
                    .ReportarServidorDisponible();

                ContenidoEstadoService.Instance.Actualizar(
                    "album",
                    TipoEstadoSincronizacionContenido.Servidor,
                    "Conectado · datos del servidor",
                    "Origen: servidor · datos guardados en el dispositivo actualizados · " +
                    $"{activeRecords.Count} registros y " +
                    $"{totalPhotos} fotografías sincronizadas · " +
                    ContenidoEstadoService.ConstruirDetalleFecha(now),
                    version,
                    now);

                return AlbumOfflineSyncResult.Ok(
                    activeRecords.Count,
                    totalPhotos,
                    "El álbum completo fue sincronizado correctamente.");
            }
            catch (OperationCanceledException)
            {
                ContenidoEstadoService.Instance.Actualizar(
                    "album",
                    TipoEstadoSincronizacionContenido.Local,
                    "Sincronización cancelada",
                    "Se conserva la última copia local disponible.");

                return AlbumOfflineSyncResult.Fail(
                    "La sincronización fue cancelada.");
            }
            catch (Exception ex)
            {
                return FailWithState(
                    ex.Message,
                    "Se conserva la última copia local disponible.");
            }
            finally
            {
                syncLock.Release();
            }
        }

        private static string ObtenerUsuarioId()
        {
            string userId = Preferences.Get(
                SessionKeys.KeyUserId,
                string.Empty);

            return string.IsNullOrWhiteSpace(userId)
                ? "0"
                : userId;
        }

        private static bool EstaVersionCompleta(
            string userId,
            string version)
        {
            string storedVersion = Preferences.Get(
                ConstruirClaveVersionCompleta(userId),
                string.Empty);

            return string.Equals(
                storedVersion,
                version,
                StringComparison.Ordinal);
        }

        private static void MarcarVersionCompleta(
            string userId,
            string version)
        {
            Preferences.Set(
                ConstruirClaveVersionCompleta(userId),
                version);
        }

        private static string ConstruirClaveVersionCompleta(
            string userId) =>
            ClaveVersionCompletaPrefijo + userId;

        private async Task<AlbumPagesResult> DownloadPagesAsync(
            int? categoryId,
            int pageSize,
            IDictionary<int, AlbumGaleriaItemResponse> records,
            CancellationToken cancellationToken)
        {
            const int maxPages = 500;
            int page = 1;

            while (page <= maxPages)
            {
                cancellationToken.ThrowIfCancellationRequested();

                ApiResult<AlbumGaleriaPaginaResponse> result =
                    await cargaApiService.GetPaginaAsync(
                        categoryId,
                        buscar: null,
                        incluirInactivos: false,
                        pagina: page,
                        tamanoPagina: pageSize,
                        cancellationToken: cancellationToken);

                if (!result.Success || result.Data == null)
                {
                    return AlbumPagesResult.Fail(
                        result.Message);
                }

                AddRecords(
                    records,
                    result.Data.Items);

                if (!result.Data.TieneMas)
                    return AlbumPagesResult.Ok();

                int nextPage = result.Data.PaginaActual + 1;

                if (nextPage <= page)
                    nextPage = page + 1;

                page = nextPage;
            }

            return AlbumPagesResult.Fail(
                "El servidor devolvió demasiadas páginas para el álbum.");
        }

        private static void AddRecords(
            IDictionary<int, AlbumGaleriaItemResponse> destination,
            IEnumerable<AlbumGaleriaItemResponse>? items)
        {
            if (items == null)
                return;

            foreach (AlbumGaleriaItemResponse item in items)
            {
                if (item.AlbumBotanicoCafeId > 0)
                    destination[item.AlbumBotanicoCafeId] = item;
            }
        }

        private static AlbumOfflineSyncResult FailWithState(
            string? originalMessage,
            string fallback)
        {
            string message = string.IsNullOrWhiteSpace(originalMessage)
                ? fallback
                : originalMessage;

            ContenidoEstadoService.Instance.Actualizar(
                "album",
                EstadoConexionService.Instance.HayInternet
                    ? TipoEstadoSincronizacionContenido.Error
                    : TipoEstadoSincronizacionContenido.SinConexionLocal,
                EstadoConexionService.Instance.HayInternet
                    ? "No se completó la sincronización"
                    : "Sin conexión · usando datos sincronizados",
                fallback);

            return AlbumOfflineSyncResult.Fail(message);
        }

        private sealed class AlbumPagesResult
        {
            public bool Success { get; private init; }
            public string Message { get; private init; } = string.Empty;

            public static AlbumPagesResult Ok() =>
                new()
                {
                    Success = true
                };

            public static AlbumPagesResult Fail(string? message) =>
                new()
                {
                    Success = false,
                    Message = message ?? string.Empty
                };
        }
    }

    public sealed class AlbumOfflineSyncResult
    {
        public bool Success { get; private init; }
        public int TotalRecords { get; private init; }
        public int TotalPhotos { get; private init; }
        public string Message { get; private init; } = string.Empty;

        public static AlbumOfflineSyncResult Ok(
            int totalRecords,
            int totalPhotos,
            string message) =>
            new()
            {
                Success = true,
                TotalRecords = totalRecords,
                TotalPhotos = totalPhotos,
                Message = message
            };

        public static AlbumOfflineSyncResult Fail(string message) =>
            new()
            {
                Success = false,
                Message = message
            };
    }
}
