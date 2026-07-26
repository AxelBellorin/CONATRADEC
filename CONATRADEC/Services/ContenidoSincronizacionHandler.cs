using CONATRADEC.Models;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace CONATRADEC.Services
{
    /// <summary>
    /// Intercepta únicamente consultas públicas de Noticias y Álbum.
    ///
    /// - Verifica como máximo una vez por minuto la versión del servidor.
    /// - Usa SQLite cuando la versión local continúa vigente.
    /// - Conserva el respaldo anterior si una actualización falla.
    /// - Limpia versiones e imágenes obsoletas solamente después de guardar
    ///   correctamente la respuesta principal del módulo.
    /// </summary>
    public sealed class ContenidoSincronizacionHandler :
        DelegatingHandler
    {
        private static readonly TimeSpan StateMemoryDuration =
            TimeSpan.FromMinutes(1);

        private static readonly ConcurrentDictionary<
            string,
            EstadoMemoria> StateMemory = new();

        private static readonly ConcurrentDictionary<
            string,
            SemaphoreSlim> StateLocks = new();

        private static readonly SemaphoreSlim ImageDownloads =
            new(initialCount: 3, maxCount: 3);

        private static readonly JsonSerializerOptions JsonOptions =
            new(JsonSerializerDefaults.Web)
            {
                PropertyNameCaseInsensitive = true
            };

        private readonly ContenidoLocalDatabaseService database =
            ContenidoLocalDatabaseService.Instance;

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (request.Method != HttpMethod.Get)
            {
                HttpResponseMessage mutationResponse =
                    await base.SendAsync(
                        request,
                        cancellationToken);

                string? mutationModule =
                    DetermineMutationModule(
                        request);

                if (mutationResponse.IsSuccessStatusCode &&
                    !string.IsNullOrWhiteSpace(
                        mutationModule))
                {
                    if (mutationModule == "noticias")
                    {
                        NoticiasOfflineSyncService.Instance
                            .MarcarPendiente();
                    }
                    else if (mutationModule == "album")
                    {
                        AlbumOfflineSyncService.Instance
                            .MarcarPendiente();
                    }

                    await SincronizacionOfflineGlobalService
                        .Instance
                        .MarcarActualizacionDisponibleAsync(
                            "Hay cambios pendientes de descargar.");
                }

                return mutationResponse;
            }

            string? module =
                DetermineModule(
                    request);

            if (string.IsNullOrWhiteSpace(module))
            {
                return await base.SendAsync(
                    request,
                    cancellationToken);
            }

            /*
             * Cuando el rol no tiene habilitado el trabajo sin conexión,
             * Noticias y Álbum se consultan siempre directamente al servidor.
             */
            if (!DatosSinConexionPermisos.TienePermiso)
            {
                return await base.SendAsync(
                    request,
                    cancellationToken);
            }

            string userId = GetHeader(
                request,
                "X-Usuario-Id");

            if (string.IsNullOrWhiteSpace(userId))
                userId = "0";

            string route = GetPathAndQuery(request);

            string cacheKey = CalculateHash(
                $"{userId}|{module}|{route}");

            ContenidoRespuestaCacheEntity? local =
                await database.ObtenerRespuestaAsync(cacheKey);

            ContenidoModuloEstadoEntity? persistedState =
                await database.ObtenerEstadoAsync(
                    BuildStateKey(userId, module));

            ContenidoEstadoService.Instance.Actualizar(
                module,
                TipoEstadoSincronizacionContenido.Verificando,
                "Verificando cambios...",
                ContenidoEstadoService.ConstruirDetalleFecha(
                    persistedState?
                        .UltimaSincronizacionExitosaUtc),
                persistedState?.Version ?? string.Empty,
                persistedState?
                    .UltimaSincronizacionExitosaUtc);

            ServerState? state = await GetServerStateAsync(
                request,
                module,
                userId,
                cancellationToken);

            if (state?.AccessDenied == true)
            {
                /*
                 * Cuando el servidor revoca el permiso no se entrega una copia
                 * local. Se deja que el endpoint original responda 401/403.
                 */
                return await base.SendAsync(
                    request,
                    cancellationToken);
            }

            bool connected = EstadoConexionService.Instance.HayInternet;

            if (state != null &&
                local != null &&
                string.Equals(
                    local.Version,
                    state.Version,
                    StringComparison.Ordinal))
            {
                await database.MarcarUsoRespuestaAsync(
                    cacheKey,
                    DateTime.UtcNow);

                /*
                 * Si una imagen fue retirada por el límite de almacenamiento,
                 * vuelve a descargarse desde el JSON local cuando hay conexión.
                 * Los archivos que todavía existen se validan sin descargarse.
                 */
                if (connected)
                {
                    await PreloadImagesAsync(
                        request,
                        userId,
                        module,
                        state.Version,
                        route,
                        local.Json,
                        cancellationToken);

                    await ImagenLocalCacheService
                        .AplicarLimiteAsync();
                }

                await MarkLocalUseAsync(
                    userId,
                    module,
                    connected
                        ? "sqlite-version-vigente"
                        : "sqlite-sin-conexion");

                UpdateLocalStatus(
                    module,
                    state.Version,
                    persistedState?
                        .UltimaSincronizacionExitosaUtc,
                    connected);

                return CreateCachedResponse(
                    request,
                    local,
                    connected
                        ? "sqlite-version-vigente"
                        : "sqlite-sin-conexion");
            }

            if (!connected && local != null)
            {
                await database.MarcarUsoRespuestaAsync(
                    cacheKey,
                    DateTime.UtcNow);

                await MarkLocalUseAsync(
                    userId,
                    module,
                    "sqlite-sin-conexion");

                UpdateLocalStatus(
                    module,
                    local.Version,
                    persistedState?
                        .UltimaSincronizacionExitosaUtc,
                    connected: false);

                return CreateCachedResponse(
                    request,
                    local,
                    "sqlite-sin-conexion");
            }

            try
            {
                HttpResponseMessage response =
                    await base.SendAsync(
                        request,
                        cancellationToken);

                /*
                 * La solicitud llegó a la API. El estado de conexión ya no
                 * depende de lo que reporte Windows.
                 */
                EstadoConexionService.Instance
                    .ReportarServidorDisponible();

                if (response.IsSuccessStatusCode &&
                    response.Content != null)
                {
                    string json = await response.Content
                        .ReadAsStringAsync(cancellationToken);

                    string contentType =
                        response.Content.Headers.ContentType?.MediaType ??
                        "application/json";

                    string version = state?.Version ??
                        local?.Version ??
                        CalculateHash(json);

                    DateTime now = DateTime.UtcNow;

                    if (!string.IsNullOrWhiteSpace(json))
                    {
                        await database.GuardarRespuestaAsync(
                            new ContenidoRespuestaCacheEntity
                            {
                                CacheKey = cacheKey,
                                UsuarioId = userId,
                                Modulo = module,
                                Ruta = route,
                                Version = version,
                                StatusCode = (int)response.StatusCode,
                                ContentType = contentType,
                                Json = json,
                                GuardadoUtc = now,
                                UltimoUsoUtc = now
                            });

                        await PreloadImagesAsync(
                            request,
                            userId,
                            module,
                            version,
                            route,
                            json,
                            cancellationToken);
                    }

                    await ConfirmAppliedStateAsync(
                        userId,
                        module,
                        version,
                        state?.FechaServidorUtc ?? now,
                        now);

                    /*
                     * La limpieza se realiza únicamente después de recibir la
                     * respuesta principal. Así, una falla de señal durante la
                     * actualización no elimina el último feed o inicio válido.
                     */
                    /*
                     * Noticias puede cerrar la versión al recibir su feed
                     * principal. En Álbum la limpieza se realiza al terminar la
                     * sincronización completa de páginas, categorías, detalles
                     * y fotografías. Así no se elimina el respaldo anterior a
                     * mitad de una descarga.
                     */
                    if (IsPrimaryRoute(module, route) &&
                        !string.Equals(
                            module,
                            "album",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        await database
                            .EliminarRespuestasVersionAnteriorAsync(
                                userId,
                                module,
                                version);

                        await ImagenLocalCacheService
                            .LimpiarVersionAnteriorAsync(
                                userId,
                                module,
                                version);

                        await ImagenLocalCacheService
                            .AplicarLimiteAsync();
                    }

                    ContenidoEstadoService.Instance.Actualizar(
                        module,
                        TipoEstadoSincronizacionContenido.Servidor,
                        "Conectado · datos del servidor",
                        "Origen: servidor · datos guardados en el dispositivo actualizados · " +
                        ContenidoEstadoService.ConstruirDetalleFecha(now),
                        version,
                        now);

                    ReplaceContent(
                        response,
                        json,
                        contentType);
                }
                else if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    /*
                     * Un detalle eliminado o desactivado no debe seguir
                     * disponible desde una respuesta local antigua.
                     */
                    await database.EliminarRespuestaAsync(cacheKey);

                    ContenidoEstadoService.Instance.Actualizar(
                        module,
                        TipoEstadoSincronizacionContenido.Error,
                        "El contenido ya no está disponible",
                        "El servidor informó que el registro fue retirado.",
                        state?.Version ?? string.Empty,
                        persistedState?
                            .UltimaSincronizacionExitosaUtc);
                }
                else if (CanUseFallback(response.StatusCode) &&
                         local != null)
                {
                    response.Dispose();

                    await MarkLocalUseAsync(
                        userId,
                        module,
                        "sqlite-respaldo-conexion");

                    UpdateLocalStatus(
                        module,
                        local.Version,
                        persistedState?
                            .UltimaSincronizacionExitosaUtc,
                        connected: false);

                    return CreateCachedResponse(
                        request,
                        local,
                        "sqlite-respaldo-conexion");
                }
                else if (!response.IsSuccessStatusCode)
                {
                    ContenidoEstadoService.Instance.Actualizar(
                        module,
                        TipoEstadoSincronizacionContenido.Error,
                        "No se pudo sincronizar",
                        $"El servidor respondió con el código {(int)response.StatusCode}.",
                        persistedState?.Version ?? string.Empty,
                        persistedState?
                            .UltimaSincronizacionExitosaUtc);
                }

                return response;
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                /*
                 * Cambiar de página o cancelar una búsqueda no significa que
                 * se haya perdido la conexión.
                 */
                throw;
            }
            catch (Exception ex)
                when (CanUseFallback(ex) && local != null)
            {
                EstadoConexionService.Instance
                    .ReportarServidorNoDisponible();

                await MarkLocalUseAsync(
                    userId,
                    module,
                    "sqlite-respaldo-excepcion",
                    ex.Message);

                UpdateLocalStatus(
                    module,
                    local.Version,
                    persistedState?
                        .UltimaSincronizacionExitosaUtc,
                    connected: false);

                return CreateCachedResponse(
                    request,
                    local,
                    "sqlite-respaldo-excepcion");
            }
            catch (Exception ex)
            {
                if (CanUseFallback(ex))
                {
                    EstadoConexionService.Instance
                        .ReportarServidorNoDisponible();
                }

                bool falloConexion = CanUseFallback(ex);
                string versionLocal =
                    persistedState?.Version ?? string.Empty;
                bool hayCopiaLocal =
                    !string.IsNullOrWhiteSpace(versionLocal);

                ContenidoEstadoService.Instance.Actualizar(
                    module,
                    falloConexion && !hayCopiaLocal
                        ? TipoEstadoSincronizacionContenido.SinDatos
                        : TipoEstadoSincronizacionContenido.Error,
                    falloConexion && !hayCopiaLocal
                        ? "Sin conexión · sin copia local"
                        : "No se pudo cargar el contenido",
                    falloConexion && !hayCopiaLocal
                        ? "Origen: ninguno · conecte el dispositivo para sincronizar."
                        : ex.Message,
                    versionLocal,
                    persistedState?
                        .UltimaSincronizacionExitosaUtc);

                throw;
            }
        }

        private async Task<ServerState?> GetServerStateAsync(
            HttpRequestMessage sourceRequest,
            string module,
            string userId,
            CancellationToken cancellationToken)
        {
            string stateKey = BuildStateKey(userId, module);
            long invalidation =
                ContenidoSincronizacionRuntime
                    .ObtenerVersionInvalidacion(module);

            if (StateMemory.TryGetValue(
                    stateKey,
                    out EstadoMemoria? memory) &&
                memory.InvalidationVersion == invalidation &&
                DateTime.UtcNow - memory.CheckedUtc <
                StateMemoryDuration)
            {
                return memory.State;
            }

            SemaphoreSlim gate = StateLocks.GetOrAdd(
                stateKey,
                _ => new SemaphoreSlim(1, 1));

            await gate.WaitAsync(cancellationToken);

            try
            {
                if (StateMemory.TryGetValue(
                        stateKey,
                        out memory) &&
                    memory.InvalidationVersion == invalidation &&
                    DateTime.UtcNow - memory.CheckedUtc <
                    StateMemoryDuration)
                {
                    return memory.State;
                }

                /*
                 * Siempre se intenta la API. Connectivity.Current no se utiliza
                 * como bloqueo porque en Windows puede devolver un estado
                 * incorrecto aun cuando el servidor sea accesible.
                 */

                Uri stateUri = BuildAbsoluteUri(
                    sourceRequest,
                    "/api/contenido-sincronizacion/estado" +
                    $"?modulo={Uri.EscapeDataString(module)}");

                using var stateRequest =
                    new HttpRequestMessage(
                        HttpMethod.Get,
                        stateUri);

                CopyHeaders(
                    sourceRequest,
                    stateRequest);

                using var timeout =
                    CancellationTokenSource
                        .CreateLinkedTokenSource(
                            cancellationToken);

                timeout.CancelAfter(
                    TimeSpan.FromSeconds(12));

                using HttpResponseMessage response =
                    await base.SendAsync(
                        stateRequest,
                        timeout.Token);

                /*
                 * Cualquier respuesta HTTP confirma que existe comunicación
                 * real con la API, aunque el código sea 401, 403, 404 o 500.
                 */
                EstadoConexionService.Instance
                    .ReportarServidorDisponible();

                if (response.StatusCode is
                    HttpStatusCode.Unauthorized or
                    HttpStatusCode.Forbidden)
                {
                    return new ServerState
                    {
                        Modulo = module,
                        AccessDenied = true
                    };
                }

                if (!response.IsSuccessStatusCode)
                {
                    return await GetStoredServerStateAsync(
                        stateKey);
                }

                ApiEnvelope<ServerState>? envelope =
                    await response.Content.ReadFromJsonAsync<
                        ApiEnvelope<ServerState>>(
                        JsonOptions,
                        cancellationToken);

                if (envelope?.Success != true ||
                    envelope.Data == null ||
                    string.IsNullOrWhiteSpace(
                        envelope.Data.Version))
                {
                    return await GetStoredServerStateAsync(
                        stateKey);
                }

                DateTime now = DateTime.UtcNow;

                ContenidoModuloEstadoEntity persisted =
                    await database.ObtenerEstadoAsync(stateKey) ??
                    new ContenidoModuloEstadoEntity
                    {
                        Clave = stateKey,
                        UsuarioId = userId,
                        Modulo = module
                    };

                persisted.VersionServidor =
                    envelope.Data.Version;
                persisted.FechaServidorUtc =
                    envelope.Data.FechaServidorUtc;
                persisted.VerificadoUtc = now;
                persisted.UltimoError = string.Empty;

                await database.GuardarEstadoAsync(persisted);

                StateMemory[stateKey] = new EstadoMemoria
                {
                    State = envelope.Data,
                    CheckedUtc = now,
                    InvalidationVersion = invalidation
                };

                return envelope.Data;
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
                when (ex is HttpRequestException ||
                      ex is TaskCanceledException ||
                      ex is IOException)
            {
                EstadoConexionService.Instance
                    .ReportarServidorNoDisponible();

                return await GetStoredServerStateAsync(
                    stateKey);
            }
            catch
            {
                /*
                 * Un error de formato no demuestra que no exista internet.
                 * Se conserva el estado local sin etiquetar la conexión como
                 * caída.
                 */
                return await GetStoredServerStateAsync(
                    stateKey);
            }
            finally
            {
                gate.Release();
            }
        }

        private async Task<ServerState?> GetStoredServerStateAsync(
            string stateKey)
        {
            ContenidoModuloEstadoEntity? stored =
                await database.ObtenerEstadoAsync(stateKey);

            if (stored == null)
                return null;

            string version =
                !string.IsNullOrWhiteSpace(stored.VersionServidor)
                    ? stored.VersionServidor
                    : stored.Version;

            if (string.IsNullOrWhiteSpace(version))
                return null;

            return new ServerState
            {
                Modulo = stored.Modulo,
                Version = version,
                FechaServidorUtc = stored.FechaServidorUtc
            };
        }

        private async Task ConfirmAppliedStateAsync(
            string userId,
            string module,
            string version,
            DateTime serverDateUtc,
            DateTime synchronizedUtc)
        {
            string key = BuildStateKey(userId, module);

            ContenidoModuloEstadoEntity state =
                await database.ObtenerEstadoAsync(key) ??
                new ContenidoModuloEstadoEntity
                {
                    Clave = key,
                    UsuarioId = userId,
                    Modulo = module
                };

            state.Version = version;
            state.VersionServidor = version;
            state.FechaServidorUtc = serverDateUtc;
            state.VerificadoUtc = DateTime.UtcNow;
            state.UltimaSincronizacionExitosaUtc =
                synchronizedUtc;
            state.OrigenUltimaCarga = "servidor";
            state.UltimoError = string.Empty;

            await database.GuardarEstadoAsync(state);
        }

        private async Task MarkLocalUseAsync(
            string userId,
            string module,
            string origin,
            string error = "")
        {
            string key = BuildStateKey(userId, module);

            ContenidoModuloEstadoEntity? state =
                await database.ObtenerEstadoAsync(key);

            if (state == null)
                return;

            state.UltimoUsoLocalUtc = DateTime.UtcNow;
            state.OrigenUltimaCarga = origin;
            state.UltimoError = error ?? string.Empty;

            await database.GuardarEstadoAsync(state);
        }

        private static void UpdateLocalStatus(
            string module,
            string version,
            DateTime? lastSyncUtc,
            bool connected)
        {
            string detalleFecha =
                ContenidoEstadoService.ConstruirDetalleFecha(
                    lastSyncUtc);

            ContenidoEstadoService.Instance.Actualizar(
                module,
                connected
                    ? TipoEstadoSincronizacionContenido.Local
                    : TipoEstadoSincronizacionContenido.SinConexionLocal,
                connected
                    ? "Conectado · usando datos sincronizados"
                    : "Sin conexión · usando datos sincronizados",
                connected
                    ? "Datos sincronizados anteriormente · versión validada con el servidor · " +
                      detalleFecha
                    : "Datos sincronizados anteriormente · " + detalleFecha,
                version,
                lastSyncUtc);
        }

        private async Task PreloadImagesAsync(
            HttpRequestMessage request,
            string userId,
            string module,
            string version,
            string route,
            string json,
            CancellationToken cancellationToken)
        {
            try
            {
                List<ImageDownload> downloads =
                    ExtractImageDownloads(
                        request,
                        module,
                        route,
                        json)
                    .DistinctBy(x =>
                        $"{x.Url}|{x.Original}")
                    .Take(40)
                    .ToList();

                if (downloads.Count == 0)
                    return;

                Task[] tasks = downloads
                    .Select(x => DownloadImageAsync(
                        userId,
                        module,
                        version,
                        x,
                        cancellationToken))
                    .ToArray();

                await Task.WhenAll(tasks);
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch
            {
                // El JSON continúa siendo válido aunque una imagen no pueda
                // guardarse en este intento.
            }
        }

        private IEnumerable<ImageDownload> ExtractImageDownloads(
            HttpRequestMessage request,
            string module,
            string route,
            string json)
        {
            using JsonDocument document =
                JsonDocument.Parse(json);

            Uri authority = GetAuthority(request);

            if (string.Equals(
                    module,
                    "noticias",
                    StringComparison.Ordinal))
            {
                bool detail = route.Contains(
                    "/detalle/",
                    StringComparison.OrdinalIgnoreCase);

                int width = detail ? 1200 : 720;
                int height = detail ? 900 : 480;
                int quality = detail ? 76 : 68;

                foreach (string path in FindStringValues(
                             document.RootElement,
                             "rutaImagenPortada"))
                {
                    if (string.IsNullOrWhiteSpace(path))
                        continue;

                    yield return new ImageDownload
                    {
                        Url = BuildThumbnailUrl(
                            authority,
                            path,
                            width,
                            height,
                            quality),
                        Original = false
                    };
                }

                yield break;
            }

            foreach (string path in FindStringValues(
                         document.RootElement,
                         "rutaImagenPortada"))
            {
                if (string.IsNullOrWhiteSpace(path))
                    continue;

                yield return new ImageDownload
                {
                    Url = BuildThumbnailUrl(
                        authority,
                        path,
                        420,
                        260,
                        65),
                    Original = false
                };
            }

            foreach (string path in FindStringValues(
                         document.RootElement,
                         "fotoPortada"))
            {
                if (string.IsNullOrWhiteSpace(path))
                    continue;

                yield return new ImageDownload
                {
                    Url = BuildThumbnailUrl(
                        authority,
                        path,
                        720,
                        480,
                        68),
                    Original = false
                };
            }

            bool albumDetail = route.Contains(
                "/detalle/",
                StringComparison.OrdinalIgnoreCase);

            foreach (string path in FindStringValues(
                         document.RootElement,
                         "rutaFoto"))
            {
                string original =
                    BuildContentUrl(authority, path);

                if (string.IsNullOrWhiteSpace(original))
                    continue;

                yield return new ImageDownload
                {
                    Url = BuildThumbnailUrl(
                        authority,
                        path,
                        720,
                        480,
                        68),
                    Original = false
                };

                /*
                 * Las originales se descargan cuando el usuario abre el
                 * detalle, no durante la galería inicial. Así se controla el
                 * consumo de datos y almacenamiento.
                 */
                if (albumDetail)
                {
                    yield return new ImageDownload
                    {
                        Url = original,
                        Original = true
                    };
                }
            }
        }

        private async Task DownloadImageAsync(
            string userId,
            string module,
            string version,
            ImageDownload image,
            CancellationToken cancellationToken)
        {
            string destination = image.Original
                ? ImagenLocalCacheService
                    .ObtenerRutaOriginal(image.Url)
                : ImagenLocalCacheService
                    .ObtenerRutaMiniatura(image.Url);

            if (File.Exists(destination) &&
                new FileInfo(destination).Length > 0)
            {
                await ImagenLocalCacheService.RegistrarAsync(
                    userId,
                    module,
                    image.Url,
                    destination,
                    version,
                    image.Original);

                return;
            }

            await ImageDownloads.WaitAsync(
                cancellationToken);

            try
            {
                if (File.Exists(destination) &&
                    new FileInfo(destination).Length > 0)
                {
                    await ImagenLocalCacheService.RegistrarAsync(
                        userId,
                        module,
                        image.Url,
                        destination,
                        version,
                        image.Original);

                    return;
                }

                using var timeout =
                    CancellationTokenSource
                        .CreateLinkedTokenSource(
                            cancellationToken);

                timeout.CancelAfter(
                    TimeSpan.FromSeconds(20));

                using var imageRequest =
                    new HttpRequestMessage(
                        HttpMethod.Get,
                        image.Url);

                using HttpResponseMessage response =
                    await base.SendAsync(
                        imageRequest,
                        timeout.Token);

                if (!response.IsSuccessStatusCode)
                    return;

                await using Stream stream =
                    await response.Content.ReadAsStreamAsync(
                        timeout.Token);

                await ImagenLocalCacheService.GuardarAsync(
                    stream,
                    destination,
                    timeout.Token);

                await ImagenLocalCacheService.RegistrarAsync(
                    userId,
                    module,
                    image.Url,
                    destination,
                    version,
                    image.Original);
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch
            {
                // Se reintentará en una futura sincronización.
            }
            finally
            {
                ImageDownloads.Release();
            }
        }

        private static HttpResponseMessage CreateCachedResponse(
            HttpRequestMessage request,
            ContenidoRespuestaCacheEntity local,
            string origin)
        {
            var response = new HttpResponseMessage(
                (HttpStatusCode)local.StatusCode)
            {
                RequestMessage = request,
                Content = new StringContent(
                    local.Json,
                    Encoding.UTF8,
                    string.IsNullOrWhiteSpace(
                        local.ContentType)
                        ? "application/json"
                        : local.ContentType)
            };

            response.Headers.TryAddWithoutValidation(
                "X-Contenido-Origen",
                origin);

            return response;
        }

        private static void ReplaceContent(
            HttpResponseMessage response,
            string json,
            string contentType)
        {
            response.Content.Dispose();

            response.Content = new StringContent(
                json,
                Encoding.UTF8,
                string.IsNullOrWhiteSpace(contentType)
                    ? "application/json"
                    : contentType);
        }

        private static string? DetermineMutationModule(
            HttpRequestMessage request)
        {
            string path =
                GetPath(request)
                    .ToLowerInvariant();

            if (path.StartsWith(
                    "/api/publicacion/",
                    StringComparison.Ordinal))
            {
                return "noticias";
            }

            if (path.StartsWith(
                    "/api/album-botanico/",
                    StringComparison.Ordinal) ||
                path.StartsWith(
                    "/api/categoria-album-botanico/",
                    StringComparison.Ordinal))
            {
                return "album";
            }

            return null;
        }

        private static string? DetermineModule(
            HttpRequestMessage request)
        {
            string pathAndQuery = GetPathAndQuery(request)
                .ToLowerInvariant();

            /*
             * Las vistas administrativas que incluyen registros inactivos
             * continúan consultando directamente al servidor.
             */
            if (pathAndQuery.Contains(
                    "incluirinactivos=true",
                    StringComparison.Ordinal))
            {
                return null;
            }

            string path = GetPath(request)
                .ToLowerInvariant();

            if (path == "/api/publicacion/categorias" ||
                path == "/api/publicacion/feed" ||
                path.StartsWith(
                    "/api/publicacion/detalle/",
                    StringComparison.Ordinal))
            {
                return "noticias";
            }

            if (path == "/api/album-botanico/inicio" ||
                path == "/api/album-botanico/galeria" ||
                path == "/api/album-botanico/galeria-paginada" ||
                path.StartsWith(
                    "/api/album-botanico/detalle/",
                    StringComparison.Ordinal) ||
                path == "/api/categoria-album-botanico/listar")
            {
                return "album";
            }

            return null;
        }

        private static bool IsPrimaryRoute(
            string module,
            string route)
        {
            if (string.Equals(
                    module,
                    "noticias",
                    StringComparison.Ordinal))
            {
                return route.StartsWith(
                           "/api/publicacion/feed",
                           StringComparison.OrdinalIgnoreCase) &&
                       RouteHasFirstPage(route) &&
                       !route.Contains(
                           "categoriaId=",
                           StringComparison.OrdinalIgnoreCase) &&
                       !route.Contains(
                           "buscar=",
                           StringComparison.OrdinalIgnoreCase) &&
                       route.Contains(
                           "soloDestacadas=false",
                           StringComparison.OrdinalIgnoreCase) &&
                       route.Contains(
                           "soloEventos=false",
                           StringComparison.OrdinalIgnoreCase);
            }

            return route.StartsWith(
                "/api/album-botanico/inicio",
                StringComparison.OrdinalIgnoreCase);
        }

        private static bool RouteHasFirstPage(string route)
        {
            if (!Uri.TryCreate(
                    "https://local.invalid" + route,
                    UriKind.Absolute,
                    out Uri? uri))
            {
                return true;
            }

            string query = uri.Query;

            if (string.IsNullOrWhiteSpace(query))
                return true;

            foreach (string part in query
                         .TrimStart('?')
                         .Split('&',
                             StringSplitOptions.RemoveEmptyEntries))
            {
                string[] values = part.Split('=', 2);

                if (values.Length == 2 &&
                    string.Equals(
                        values[0],
                        "pagina",
                        StringComparison.OrdinalIgnoreCase))
                {
                    return !int.TryParse(values[1], out int page) ||
                           page <= 1;
                }
            }

            return true;
        }


        private static string GetPathAndQuery(
            HttpRequestMessage request)
        {
            Uri? uri = request.RequestUri;

            if (uri == null)
                return string.Empty;

            if (uri.IsAbsoluteUri)
                return uri.PathAndQuery;

            string original = uri.OriginalString;

            return original.StartsWith('/')
                ? original
                : "/" + original;
        }

        private static string GetPath(
            HttpRequestMessage request)
        {
            string pathAndQuery = GetPathAndQuery(request);
            int queryIndex = pathAndQuery.IndexOf('?');

            return queryIndex >= 0
                ? pathAndQuery[..queryIndex]
                : pathAndQuery;
        }

        private static bool CanUseFallback(
            HttpStatusCode statusCode) =>
            statusCode == HttpStatusCode.RequestTimeout ||
            (int)statusCode >= 500;

        private static bool CanUseFallback(Exception exception) =>
            exception is HttpRequestException ||
            exception is TaskCanceledException ||
            exception is IOException;

        private static string GetHeader(
            HttpRequestMessage request,
            string name)
        {
            return request.Headers.TryGetValues(
                name,
                out IEnumerable<string>? values)
                ? values.FirstOrDefault() ?? string.Empty
                : string.Empty;
        }

        private static void CopyHeaders(
            HttpRequestMessage source,
            HttpRequestMessage target)
        {
            foreach (KeyValuePair<
                         string,
                         IEnumerable<string>> header
                     in source.Headers)
            {
                target.Headers.TryAddWithoutValidation(
                    header.Key,
                    header.Value);
            }

            target.Headers.Accept.Clear();
            target.Headers.Accept.Add(
                new MediaTypeWithQualityHeaderValue(
                    "application/json"));
        }

        private static Uri BuildAbsoluteUri(
            HttpRequestMessage source,
            string pathAndQuery)
        {
            Uri authority = GetAuthority(source);

            return new Uri(
                authority,
                pathAndQuery);
        }

        private static Uri GetAuthority(
            HttpRequestMessage request)
        {
            if (request.RequestUri?.IsAbsoluteUri == true)
            {
                return new Uri(
                    request.RequestUri.GetLeftPart(
                        UriPartial.Authority));
            }

            return new Uri(
                new UrlApiService()
                    .BaseUrlApi
                    .TrimEnd('/') + "/");
        }

        private static string BuildContentUrl(
            Uri authority,
            string? pathOrUrl)
        {
            if (string.IsNullOrWhiteSpace(pathOrUrl))
                return string.Empty;

            if (Uri.TryCreate(
                    pathOrUrl,
                    UriKind.Absolute,
                    out Uri? absolute))
            {
                return absolute.ToString();
            }

            return new Uri(
                authority,
                pathOrUrl.TrimStart('/'))
                .ToString();
        }

        private static string BuildThumbnailUrl(
            Uri authority,
            string pathOrUrl,
            int width,
            int height,
            int quality)
        {
            string path = pathOrUrl;

            if (Uri.TryCreate(
                    pathOrUrl,
                    UriKind.Absolute,
                    out Uri? absolute))
            {
                path = absolute.AbsolutePath;
            }

            if (!path.StartsWith('/'))
                path = "/" + path;

            return new Uri(
                authority,
                "/imagenes/miniatura" +
                $"?ruta={Uri.EscapeDataString(path)}" +
                $"&ancho={width}" +
                $"&alto={height}" +
                $"&calidad={quality}")
                .ToString();
        }

        private static IEnumerable<string> FindStringValues(
            JsonElement element,
            string propertyName)
        {
            if (element.ValueKind == JsonValueKind.Object)
            {
                foreach (JsonProperty property
                         in element.EnumerateObject())
                {
                    if (string.Equals(
                            property.Name,
                            propertyName,
                            StringComparison.OrdinalIgnoreCase) &&
                        property.Value.ValueKind ==
                        JsonValueKind.String)
                    {
                        string? value =
                            property.Value.GetString();

                        if (!string.IsNullOrWhiteSpace(value))
                            yield return value;
                    }

                    foreach (string nested
                             in FindStringValues(
                                 property.Value,
                                 propertyName))
                    {
                        yield return nested;
                    }
                }
            }
            else if (element.ValueKind ==
                     JsonValueKind.Array)
            {
                foreach (JsonElement item
                         in element.EnumerateArray())
                {
                    foreach (string nested
                             in FindStringValues(
                                 item,
                                 propertyName))
                    {
                        yield return nested;
                    }
                }
            }
        }

        private static string CalculateHash(string value)
        {
            byte[] hash = SHA256.HashData(
                Encoding.UTF8.GetBytes(value));

            return Convert
                .ToHexString(hash)
                .ToLowerInvariant();
        }

        private static string BuildStateKey(
            string userId,
            string module) =>
            $"{userId}|{module}";

        private sealed class ApiEnvelope<T>
        {
            public bool Success { get; set; }
            public string Message { get; set; } =
                string.Empty;
            public T? Data { get; set; }
        }

        private sealed class ServerState
        {
            public string Modulo { get; set; } =
                string.Empty;
            public string Version { get; set; } =
                string.Empty;
            public DateTime FechaServidorUtc { get; set; }
            public bool AccessDenied { get; set; }
        }

        private sealed class EstadoMemoria
        {
            public ServerState State { get; set; } =
                new();
            public DateTime CheckedUtc { get; set; }
            public long InvalidationVersion { get; set; }
        }

        private sealed class ImageDownload
        {
            public string Url { get; set; } =
                string.Empty;
            public bool Original { get; set; }
        }
    }
}
