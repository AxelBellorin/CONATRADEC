using CONATRADEC.Models;
using Microsoft.Maui.Storage;

namespace CONATRADEC.Services
{
    /// <summary>
    /// Descarga todas las publicaciones activas, categorías, combinaciones de
    /// filtros y detalles necesarios para consultar Noticias sin conexión.
    ///
    /// Las respuestas pasan por ContenidoSincronizacionHandler, que guarda
    /// los datos y las imágenes asociadas en el dispositivo.
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
                CancellationToken cancellationToken =
                    default)
        {
            bool entered =
                await syncLock.WaitAsync(
                    TimeSpan.Zero,
                    cancellationToken);

            if (!entered)
            {
                return NoticiasOfflineSyncResult.Fail(
                    "Ya existe una sincronización de noticias en curso.");
            }

            try
            {
                ContenidoSincronizacionRuntime
                    .Invalidar(Modulo);

                ApiResult<
                    List<CategoriaPublicacionResponse>>
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

                ApiResult<PublicacionPaginadaResponse>
                    primeraPagina =
                        await apiService.GetFeedAsync(
                            categoriaId: null,
                            buscar: null,
                            soloDestacadas: false,
                            soloEventos: false,
                            pagina: 1,
                            tamanoPagina: 30,
                            cancellationToken);

                if (!primeraPagina.Success ||
                    primeraPagina.Data == null)
                {
                    return Fallar(
                        primeraPagina.Message,
                        "No fue posible comprobar las noticias disponibles.");
                }

                string usuarioId =
                    ObtenerUsuarioId();

                ContenidoModuloEstadoEntity? estado =
                    await ContenidoLocalDatabaseService.Instance
                        .ObtenerEstadoAsync(
                            $"{usuarioId}|{Modulo}");

                string version =
                    !string.IsNullOrWhiteSpace(
                        estado?.VersionServidor)
                        ? estado!.VersionServidor
                        : estado?.Version ??
                          string.Empty;

                if (string.IsNullOrWhiteSpace(version))
                {
                    return Fallar(
                        "No se pudo confirmar la versión de noticias.",
                        "Se conservará la copia anterior.");
                }

                if (!forzarDescargaCompleta &&
                    EstaVersionCompleta(
                        usuarioId,
                        version))
                {
                    DateTime? ultima =
                        estado?
                            .UltimaSincronizacionExitosaUtc;

                    ContenidoEstadoService.Instance.Actualizar(
                        Modulo,
                        EstadoConexionService.Instance.HayInternet
                            ? TipoEstadoSincronizacionContenido.Local
                            : TipoEstadoSincronizacionContenido
                                .SinConexionLocal,
                        EstadoConexionService.Instance.HayInternet
                            ? "Conectado · usando datos sincronizados"
                            : "Sin conexión · usando datos sincronizados",
                        "Datos sincronizados anteriormente · " +
                        ContenidoEstadoService
                            .ConstruirDetalleFecha(
                                ultima),
                        version,
                        ultima);

                    return NoticiasOfflineSyncResult.Ok(
                        primeraPagina.Data.TotalRegistros,
                        categoriasResultado.Data.Count,
                        "Las noticias sincronizadas continúan vigentes.");
                }

                ContenidoEstadoService.Instance.Actualizar(
                    Modulo,
                    TipoEstadoSincronizacionContenido.Verificando,
                    "Sincronizando noticias completas...",
                    "Descargando categorías, publicaciones e imágenes.");

                var publicaciones =
                    new Dictionary<
                        int,
                        PublicacionListadoResponse>();

                AgregarPublicaciones(
                    publicaciones,
                    primeraPagina.Data.Items);

                List<int?> categorias =
                    new()
                    {
                        null
                    };

                categorias.AddRange(
                    categoriasResultado.Data
                        .Where(x =>
                            x.CategoriaPublicacionId > 0)
                        .Select(x =>
                            (int?)x.CategoriaPublicacionId));

                (bool Destacadas, bool Eventos)[] filtros =
                {
                    (false, false),
                    (true, false),
                    (false, true),
                    (true, true)
                };

                int totalCombinaciones =
                    categorias.Count *
                    filtros.Length;

                int combinacionActual = 0;

                foreach (int? categoriaId in categorias)
                {
                    foreach (
                        (bool destacadas, bool eventos)
                        in filtros)
                    {
                        cancellationToken
                            .ThrowIfCancellationRequested();

                        combinacionActual++;

                        string categoriaTexto =
                            categoriaId.HasValue
                                ? categoriasResultado.Data
                                    .FirstOrDefault(x =>
                                        x.CategoriaPublicacionId ==
                                        categoriaId.Value)?
                                    .Nombre ??
                                  "Categoría"
                                : "Todas";

                        ContenidoEstadoService.Instance.Actualizar(
                            Modulo,
                            TipoEstadoSincronizacionContenido.Verificando,
                            "Descargando publicaciones...",
                            $"{combinacionActual} de " +
                            $"{totalCombinaciones}: " +
                            categoriaTexto);

                        ResultadoPaginas resultado =
                            await DescargarPaginasAsync(
                                categoriaId,
                                destacadas,
                                eventos,
                                publicaciones,
                                cancellationToken);

                        if (!resultado.Success)
                        {
                            return Fallar(
                                resultado.Message,
                                "No fue posible descargar todas las publicaciones.");
                        }
                    }
                }

                List<PublicacionListadoResponse>
                    publicacionesActivas =
                        publicaciones.Values
                            .OrderBy(x =>
                                x.PublicacionId)
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

                EstadoConexionService.Instance
                    .ReportarServidorDisponible();

                ContenidoEstadoService.Instance.Actualizar(
                    Modulo,
                    TipoEstadoSincronizacionContenido.Servidor,
                    "Conectado · datos del servidor",
                    "Datos guardados en el dispositivo actualizados · " +
                    $"{publicacionesActivas.Count} publicaciones · " +
                    ContenidoEstadoService
                        .ConstruirDetalleFecha(
                            ahora),
                    version,
                    ahora);

                return NoticiasOfflineSyncResult.Ok(
                    publicacionesActivas.Count,
                    categoriasResultado.Data.Count,
                    "Las noticias fueron sincronizadas correctamente.");
            }
            catch (OperationCanceledException)
            {
                return NoticiasOfflineSyncResult.Fail(
                    "La sincronización de noticias fue cancelada.");
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
                            tamanoPagina: 30,
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
                EstadoConexionService.Instance.HayInternet
                    ? TipoEstadoSincronizacionContenido.Error
                    : TipoEstadoSincronizacionContenido
                        .SinConexionLocal,
                EstadoConexionService.Instance.HayInternet
                    ? "No se completó la sincronización"
                    : "Sin conexión · usando datos sincronizados",
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
            string version) =>
                string.Equals(
                    Preferences.Get(
                        ConstruirClaveVersionCompleta(
                            usuarioId),
                        string.Empty),
                    version,
                    StringComparison.Ordinal);

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
                    Message = message ??
                              string.Empty
                };
        }
    }
}
