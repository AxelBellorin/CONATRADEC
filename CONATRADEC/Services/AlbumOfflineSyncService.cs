using CONATRADEC.Models;
using Microsoft.Maui.Devices;
using Microsoft.Maui.Storage;

namespace CONATRADEC.Services
{
    /// <summary>
    /// Descarga el Álbum Botánico completo con su nueva jerarquía:
    /// categorías, subcategorías, fichas, detalles y fotografías.
    /// Todas las rutas quedan almacenadas por ContenidoSincronizacionHandler
    /// con una única versión transaccional para su consulta sin conexión.
    /// </summary>
    public sealed class AlbumOfflineSyncService
    {
        private static readonly Lazy<AlbumOfflineSyncService> lazy =
            new(() => new AlbumOfflineSyncService());

        private readonly SemaphoreSlim syncLock = new(1, 1);
        private readonly AlbumJerarquiaApiService jerarquiaApi = new();
        private readonly AlbumBotanicoApiService albumApiService = new();

        private const string ClaveVersionCompletaPrefijo =
            "contenido_offline_album_version_completa_";

        public static AlbumOfflineSyncService Instance => lazy.Value;

        private AlbumOfflineSyncService()
        {
        }

        public void MarcarPendiente()
        {
            string usuarioId = ObtenerUsuarioId();

            if (usuarioId != "0")
                Preferences.Remove(ConstruirClaveVersionCompleta(usuarioId));

            ContenidoSincronizacionRuntime.Invalidar("album");
        }

        public async Task<AlbumOfflineSyncResult> SincronizarSiNecesarioAsync(
            bool forzarDescargaCompleta,
            CancellationToken cancellationToken = default)
        {
            ContenidoSincronizacionRuntime.Invalidar("album");

            int pageSize = ObtenerTamanoPagina();
            ApiResult<AlbumInicioJerarquiaResponse> verification =
                await jerarquiaApi.GetInicioAsync(
                    pageSize,
                    cancellationToken);

            if (!verification.Success || verification.Data == null)
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
                EstaVersionCompleta(userId, currentVersion))
            {
                DateTime? lastSync = state?.UltimaSincronizacionExitosaUtc;
                bool conectado = EstadoConexionService.Instance.HayInternet;

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
                          ContenidoEstadoService.ConstruirDetalleFecha(lastSync)
                        : "Datos sincronizados anteriormente · " +
                          ContenidoEstadoService.ConstruirDetalleFecha(lastSync),
                    currentVersion,
                    lastSync);

                return AlbumOfflineSyncResult.Ok(
                    verification.Data.Galeria.TotalRegistros,
                    0,
                    "El álbum local ya contiene la versión vigente.");
            }

            return await SincronizarAsync(cancellationToken);
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
                    "Comprobando categorías, subcategorías y fichas disponibles.");

                ContenidoSincronizacionRuntime.Invalidar("album");

                int pageSize = ObtenerTamanoPagina();
                ApiResult<AlbumInicioJerarquiaResponse> initialResult =
                    await jerarquiaApi.GetInicioAsync(
                        pageSize,
                        cancellationToken);

                if (!initialResult.Success || initialResult.Data == null)
                {
                    return FailWithState(
                        initialResult.Message,
                        "No fue posible descargar el inicio del álbum.");
                }

                List<CategoriaAlbumBotanicoResponse> categories =
                    initialResult.Data.Categorias
                        .Where(item => item.Activo)
                        .ToList();

                List<SubcategoriaAlbumBotanicoResponse> subcategories =
                    initialResult.Data.Subcategorias
                        .Where(item => item.Activo)
                        .ToList();

                var records = new Dictionary<
                    int,
                    AlbumGaleriaJerarquiaItemResponse>();

                AddRecords(records, initialResult.Data.Galeria.Items);

                AlbumPagesResult allPages = await DownloadPagesAsync(
                    categoryId: null,
                    subcategoryId: null,
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
                foreach (CategoriaAlbumBotanicoResponse category in categories)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    currentCategory++;

                    ContenidoEstadoService.Instance.Actualizar(
                        "album",
                        TipoEstadoSincronizacionContenido.Verificando,
                        "Descargando categorías...",
                        $"{currentCategory} de {categories.Count}: " +
                        category.NombreCategoria);

                    AlbumPagesResult categoryPages = await DownloadPagesAsync(
                        category.CategoriaAlbumBotanicoId,
                        subcategoryId: null,
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

                int currentSubcategory = 0;
                foreach (SubcategoriaAlbumBotanicoResponse subcategory in
                         subcategories)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    currentSubcategory++;

                    ContenidoEstadoService.Instance.Actualizar(
                        "album",
                        TipoEstadoSincronizacionContenido.Verificando,
                        "Descargando subcategorías...",
                        $"{currentSubcategory} de {subcategories.Count}: " +
                        subcategory.NombreSubcategoria);

                    AlbumPagesResult subcategoryPages =
                        await DownloadPagesAsync(
                            subcategory.CategoriaAlbumBotanicoId,
                            subcategory.SubcategoriaAlbumBotanicoId,
                            pageSize,
                            records,
                            cancellationToken);

                    if (!subcategoryPages.Success)
                    {
                        return FailWithState(
                            subcategoryPages.Message,
                            $"No fue posible descargar la subcategoría " +
                            $"'{subcategory.NombreSubcategoria}'.");
                    }
                }

                List<AlbumGaleriaJerarquiaItemResponse> activeRecords =
                    records.Values
                        .Where(item =>
                            item.Activo &&
                            item.CategoriaActiva &&
                            item.SubcategoriaActiva)
                        .OrderBy(item => item.AlbumBotanicoCafeId)
                        .ToList();

                int currentRecord = 0;
                int totalPhotos = 0;

                foreach (AlbumGaleriaJerarquiaItemResponse item in activeRecords)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    currentRecord++;

                    ContenidoEstadoService.Instance.Actualizar(
                        "album",
                        TipoEstadoSincronizacionContenido.Verificando,
                        "Descargando detalles y fotografías...",
                        $"{currentRecord} de {activeRecords.Count}: " +
                        item.Titulo);

                    ApiResult<AlbumDetalleResponse> detailResult =
                        await albumApiService.GetDetalleAsync(
                            item.AlbumBotanicoCafeId,
                            incluirInactivos: false,
                            cancellationToken: cancellationToken);

                    if (!detailResult.Success || detailResult.Data == null)
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

                await ContenidoLocalDatabaseService.Instance
                    .EliminarRespuestasVersionAnteriorAsync(
                        userId,
                        "album",
                        version);

                await ImagenLocalCacheService.LimpiarVersionAnteriorAsync(
                    userId,
                    "album",
                    version);

                await ImagenLocalCacheService.AplicarLimiteAsync();
                MarcarVersionCompleta(userId, version);

                DateTime now = DateTime.UtcNow;
                EstadoConexionService.Instance.ReportarServidorDisponible();

                ContenidoEstadoService.Instance.Actualizar(
                    "album",
                    TipoEstadoSincronizacionContenido.Servidor,
                    "Conectado · datos del servidor",
                    "Origen: servidor · datos guardados en el dispositivo actualizados · " +
                    $"{categories.Count} categorías, " +
                    $"{subcategories.Count} subcategorías, " +
                    $"{activeRecords.Count} fichas y " +
                    $"{totalPhotos} fotografías sincronizadas · " +
                    ContenidoEstadoService.ConstruirDetalleFecha(now),
                    version,
                    now);

                return AlbumOfflineSyncResult.Ok(
                    activeRecords.Count,
                    totalPhotos,
                    "El álbum jerárquico completo fue sincronizado correctamente.");
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

        private async Task<AlbumPagesResult> DownloadPagesAsync(
            int? categoryId,
            int? subcategoryId,
            int pageSize,
            IDictionary<int, AlbumGaleriaJerarquiaItemResponse> records,
            CancellationToken cancellationToken)
        {
            const int maxPages = 500;
            int page = 1;

            while (page <= maxPages)
            {
                cancellationToken.ThrowIfCancellationRequested();

                ApiResult<AlbumGaleriaJerarquiaPaginaResponse> result =
                    await jerarquiaApi.GetPaginaAsync(
                        categoryId,
                        subcategoryId,
                        buscar: null,
                        incluirInactivos: false,
                        pagina: page,
                        tamanoPagina: pageSize,
                        cancellationToken: cancellationToken);

                if (!result.Success || result.Data == null)
                    return AlbumPagesResult.Fail(result.Message);

                AddRecords(records, result.Data.Items);

                if (!result.Data.TieneMas)
                    return AlbumPagesResult.Ok();

                int nextPage = result.Data.PaginaActual + 1;
                page = nextPage <= page ? page + 1 : nextPage;
            }

            return AlbumPagesResult.Fail(
                "El servidor devolvió demasiadas páginas para el álbum.");
        }

        private static void AddRecords(
            IDictionary<int, AlbumGaleriaJerarquiaItemResponse> destination,
            IEnumerable<AlbumGaleriaJerarquiaItemResponse>? items)
        {
            if (items == null)
                return;

            foreach (AlbumGaleriaJerarquiaItemResponse item in items)
            {
                if (item.AlbumBotanicoCafeId > 0)
                    destination[item.AlbumBotanicoCafeId] = item;
            }
        }

        private static int ObtenerTamanoPagina() =>
            DeviceInfo.Platform == DevicePlatform.WinUI ? 12 : 6;

        private static string ObtenerUsuarioId()
        {
            string userId = Preferences.Get(
                SessionKeys.KeyUserId,
                string.Empty);

            return string.IsNullOrWhiteSpace(userId) ? "0" : userId;
        }

        private static bool EstaVersionCompleta(
            string userId,
            string version) =>
            string.Equals(
                Preferences.Get(
                    ConstruirClaveVersionCompleta(userId),
                    string.Empty),
                version,
                StringComparison.Ordinal);

        private static void MarcarVersionCompleta(
            string userId,
            string version) =>
            Preferences.Set(
                ConstruirClaveVersionCompleta(userId),
                version);

        private static string ConstruirClaveVersionCompleta(string userId) =>
            ClaveVersionCompletaPrefijo + userId;

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

            public static AlbumPagesResult Ok() => new() { Success = true };

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
