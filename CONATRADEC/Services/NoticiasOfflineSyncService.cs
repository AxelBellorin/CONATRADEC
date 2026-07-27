using CONATRADEC.Models;
using Microsoft.Maui.Storage;

namespace CONATRADEC.Services
{
    /// <summary>
    /// Descarga Noticias con las rutas exactas utilizadas por Windows y móvil.
    ///
    /// La descarga manual usa la versión transaccional de
    /// DescargaOfflineContext. Todas las páginas, categorías, filtros, detalles
    /// e imágenes permanecen dentro del mismo paquete.
    /// </summary>
    public sealed class NoticiasOfflineSyncService
    {
        private const string Modulo = "noticias";

        private const string ClaveVersionCompletaPrefijo =
            "contenido_offline_noticias_version_completa_";

        private static readonly Lazy<
            NoticiasOfflineSyncService> lazy =
                new(() =>
                    new NoticiasOfflineSyncService());

        private readonly SemaphoreSlim syncLock =
            new(1, 1);

        private readonly PublicacionApiService apiService =
            new();

        public static NoticiasOfflineSyncService Instance =>
            lazy.Value;

        private NoticiasOfflineSyncService()
        {
        }

        public async Task<NoticiasOfflineSyncResult>
            SincronizarSiNecesarioAsync(
                bool forzarDescargaCompleta,
                CancellationToken cancellationToken = default)
        {
            bool entered =
                await syncLock.WaitAsync(
                    TimeSpan.Zero,
                    cancellationToken);

            if (!entered)
            {
                return NoticiasOfflineSyncResult.Fail(
                    "Ya existe una descarga de noticias en curso.");
            }

            try
            {
                ContenidoSincronizacionRuntime
                    .Invalidar(Modulo);

                ApiResult<List<CategoriaPublicacionResponse>>
                    categoriasResultado =
                        await apiService.GetCategoriasAsync(
                            cancellationToken);

                if (!categoriasResultado.Success ||
                    categoriasResultado.Data == null)
                {
                    return Fallar(
                        categoriasResultado.Message,
                        "No fue posible descargar las categorías de noticias.");
                }

                string usuarioId =
                    ObtenerUsuarioId();

                ContenidoModuloEstadoEntity? estadoAnterior =
                    await ContenidoLocalDatabaseService.Instance
                        .ObtenerEstadoAsync(
                            $"{usuarioId}|{Modulo}");

                if (!forzarDescargaCompleta &&
                    estadoAnterior != null &&
                    EstaVersionCompleta(
                        usuarioId,
                        estadoAnterior.Version))
                {
                    return NoticiasOfflineSyncResult.Ok(
                        0,
                        categoriasResultado.Data.Count,
                        "La copia completa de noticias continúa disponible.");
                }

                ContenidoEstadoService.Instance.Actualizar(
                    Modulo,
                    TipoEstadoSincronizacionContenido.Verificando,
                    "Descargando noticias completas...",
                    "Preparando páginas de Windows y móvil.");

                var publicaciones =
                    new Dictionary<
                        int,
                        PublicacionListadoResponse>();

                List<int?> categorias =
                    new()
                    {
                        null
                    };

                categorias.AddRange(
                    categoriasResultado.Data
                        .Where(item =>
                            item.CategoriaPublicacionId > 0)
                        .Select(item =>
                            (int?)item.CategoriaPublicacionId));

                (bool Destacadas, bool Eventos)[] filtros =
                {
                    (false, false),
                    (true, false),
                    (false, true),
                    (true, true)
                };

                /*
                 * NoticiasViewModel solicita 12 elementos en Windows y 6 en
                 * Android/iOS. Se descargan ambos tamaños para que la misma
                 * lógica de caché responda exactamente a la pantalla.
                 */
                int[] tamanosPagina =
                {
                    12,
                    6
                };

                int totalCombinaciones =
                    categorias.Count *
                    filtros.Length *
                    tamanosPagina.Length;

                int combinacionActual = 0;

                foreach (int tamanoPagina
                         in tamanosPagina)
                {
                    foreach (int? categoriaId
                             in categorias)
                    {
                        foreach (
                            (bool destacadas, bool eventos)
                            in filtros)
                        {
                            cancellationToken
                                .ThrowIfCancellationRequested();

                            combinacionActual++;

                            ContenidoEstadoService.Instance.Actualizar(
                                Modulo,
                                TipoEstadoSincronizacionContenido.Verificando,
                                "Descargando publicaciones...",
                                $"{combinacionActual} de " +
                                $"{totalCombinaciones}");

                            ResultadoPaginas resultado =
                                await DescargarPaginasAsync(
                                    categoriaId,
                                    destacadas,
                                    eventos,
                                    tamanoPagina,
                                    publicaciones,
                                    cancellationToken);

                            if (!resultado.Success)
                            {
                                return Fallar(
                                    resultado.Message,
                                    "No fue posible descargar todas las páginas de noticias.");
                            }
                        }
                    }
                }

                List<PublicacionListadoResponse>
                    publicacionesActivas =
                        publicaciones.Values
                            .OrderBy(item =>
                                item.PublicacionId)
                            .ToList();

                int detalleActual = 0;

                foreach (
                    PublicacionListadoResponse publicacion
                    in publicacionesActivas)
                {
                    cancellationToken
                        .ThrowIfCancellationRequested();

                    detalleActual++;

                    ContenidoEstadoService.Instance.Actualizar(
                        Modulo,
                        TipoEstadoSincronizacionContenido.Verificando,
                        "Descargando detalles e imágenes...",
                        $"{detalleActual} de " +
                        $"{publicacionesActivas.Count}: " +
                        publicacion.Titulo);

                    ApiResult<PublicacionDetalleResponse>
                        detalle =
                            await apiService.GetDetalleAsync(
                                publicacion.PublicacionId,
                                cancellationToken);

                    if (!detalle.Success ||
                        detalle.Data == null)
                    {
                        return Fallar(
                            detalle.Message,
                            $"No fue posible descargar " +
                            $"'{publicacion.Titulo}'.");
                    }
                }

                ContenidoModuloEstadoEntity? estado =
                    await ContenidoLocalDatabaseService.Instance
                        .ObtenerEstadoAsync(
                            $"{usuarioId}|{Modulo}");

                string version =
                    DescargaOfflineContext.Activa
                        ? DescargaOfflineContext
                            .VersionTransaccional
                        : estado?.Version ??
                          string.Empty;

                if (string.IsNullOrWhiteSpace(version))
                {
                    return Fallar(
                        "No se pudo identificar la versión transaccional de noticias.",
                        "Se conserva la copia anterior.");
                }

                await ContenidoLocalDatabaseService.Instance
                    .EliminarRespuestasVersionAnteriorAsync(
                        usuarioId,
                        Modulo,
                        version);

                await ImagenLocalCacheService
                    .LimpiarVersionAnteriorAsync(
                        usuarioId,
                        Modulo,
                        version);

                MarcarVersionCompleta(
                    usuarioId,
                    version);

                DateTime ahora =
                    DateTime.UtcNow;

                ContenidoEstadoService.Instance.Actualizar(
                    Modulo,
                    TipoEstadoSincronizacionContenido.Servidor,
                    "Noticias preparadas",
                    $"{publicacionesActivas.Count} publicaciones " +
                    "guardadas para Windows y móvil.",
                    version,
                    ahora);

                return NoticiasOfflineSyncResult.Ok(
                    publicacionesActivas.Count,
                    categoriasResultado.Data.Count,
                    "Las noticias fueron descargadas completamente.");
            }
            catch (OperationCanceledException)
            {
                return NoticiasOfflineSyncResult.Fail(
                    "La descarga de noticias fue cancelada.");
            }
            catch (Exception ex)
            {
                return Fallar(
                    ex.Message,
                    "Se conserva la copia anterior de noticias.");
            }
            finally
            {
                syncLock.Release();
            }
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
                .Invalidar(Modulo);
        }

        private async Task<ResultadoPaginas>
            DescargarPaginasAsync(
                int? categoriaId,
                bool soloDestacadas,
                bool soloEventos,
                int tamanoPagina,
                IDictionary<
                    int,
                    PublicacionListadoResponse>
                    publicaciones,
                CancellationToken cancellationToken)
        {
            const int maxPaginas = 500;
            int pagina = 1;

            while (pagina <= maxPaginas)
            {
                ApiResult<PublicacionPaginadaResponse>
                    resultado =
                        await apiService.GetFeedAsync(
                            categoriaId,
                            buscar: null,
                            soloDestacadas,
                            soloEventos,
                            pagina,
                            tamanoPagina,
                            cancellationToken);

                if (!resultado.Success ||
                    resultado.Data == null)
                {
                    return ResultadoPaginas.Fail(
                        resultado.Message);
                }

                AgregarPublicaciones(
                    publicaciones,
                    resultado.Data.Items);

                if (pagina >=
                    Math.Max(
                        1,
                        resultado.Data.TotalPaginas))
                {
                    return ResultadoPaginas.Ok();
                }

                pagina++;
            }

            return ResultadoPaginas.Fail(
                "El servidor devolvió demasiadas páginas de noticias.");
        }

        private static void AgregarPublicaciones(
            IDictionary<
                int,
                PublicacionListadoResponse>
                destino,
            IEnumerable<
                PublicacionListadoResponse>?
                items)
        {
            if (items == null)
                return;

            foreach (
                PublicacionListadoResponse item
                in items)
            {
                if (item.PublicacionId > 0)
                    destino[item.PublicacionId] = item;
            }
        }

        private static NoticiasOfflineSyncResult
            Fallar(
                string? mensajeOriginal,
                string respaldo)
        {
            string mensaje =
                string.IsNullOrWhiteSpace(
                    mensajeOriginal)
                    ? respaldo
                    : mensajeOriginal;

            ContenidoEstadoService.Instance.Actualizar(
                Modulo,
                TipoEstadoSincronizacionContenido.Error,
                "No se completó la descarga de noticias",
                respaldo);

            return NoticiasOfflineSyncResult.Fail(
                mensaje);
        }

        private static string ObtenerUsuarioId()
        {
            string usuarioId =
                Preferences.Get(
                    SessionKeys.KeyUserId,
                    string.Empty);

            return string.IsNullOrWhiteSpace(
                    usuarioId)
                ? "0"
                : usuarioId;
        }

        private static bool EstaVersionCompleta(
            string usuarioId,
            string version)
        {
            if (string.IsNullOrWhiteSpace(version))
                return false;

            return string.Equals(
                Preferences.Get(
                    ConstruirClaveVersionCompleta(
                        usuarioId),
                    string.Empty),
                version,
                StringComparison.Ordinal);
        }

        private static void MarcarVersionCompleta(
            string usuarioId,
            string version) =>
            Preferences.Set(
                ConstruirClaveVersionCompleta(
                    usuarioId),
                version);

        private static string
            ConstruirClaveVersionCompleta(
                string usuarioId) =>
            ClaveVersionCompletaPrefijo +
            usuarioId;

        private sealed class ResultadoPaginas
        {
            public bool Success { get; init; }
            public string Message { get; init; } =
                string.Empty;

            public static ResultadoPaginas Ok() =>
                new()
                {
                    Success = true
                };

            public static ResultadoPaginas Fail(
                string? message) =>
                new()
                {
                    Success = false,
                    Message =
                        string.IsNullOrWhiteSpace(
                            message)
                            ? "No fue posible descargar las páginas."
                            : message
                };
        }
    }
}
