using CONATRADEC.Models;
using Microsoft.Maui.Storage;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace CONATRADEC.Services
{
    /// <summary>
    /// Caché transparente de Noticias y Álbum.
    ///
    /// En línea:
    /// - siempre consulta primero la API;
    /// - entrega su respuesta sin esperar comprobaciones de versión;
    /// - guarda JSON e imágenes silenciosamente.
    ///
    /// Sin conexión:
    /// - usa exclusivamente la respuesta exacta guardada en SQLite;
    /// - nunca consulta estado, versión o disponibilidad del servidor.
    /// </summary>
    public sealed class ContenidoSincronizacionHandler :
        DelegatingHandler
    {
        private static readonly SemaphoreSlim ImageDownloads =
            new(initialCount: 3, maxCount: 3);

        private readonly ContenidoLocalDatabaseService database =
            ContenidoLocalDatabaseService.Instance;

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            string? module = DeterminarModulo(request);

            if (request.Method != HttpMethod.Get ||
                string.IsNullOrWhiteSpace(module))
            {
                return await base.SendAsync(
                    request,
                    cancellationToken);
            }

            if (!DatosSinConexionPermisos.TienePermiso)
            {
                return await base.SendAsync(
                    request,
                    cancellationToken);
            }

            string usuarioId = Preferences.Get(
                SessionKeys.KeyUserId,
                "0");

            string route = ObtenerPathYQuery(request);
            string cacheKey = CalcularHash(
                $"{usuarioId}|{module}|{route}");

            if (ModoSesionService.EsOffline)
            {
                ContenidoRespuestaCacheEntity? local =
                    await database.ObtenerRespuestaAsync(cacheKey);

                if (local == null)
                {
                    string? respuestaDinamica =
                        await ContenidoConsultaLocalDinamicaService
                            .IntentarCrearRespuestaAsync(
                                request,
                                usuarioId,
                                module,
                                cancellationToken);

                    if (!string.IsNullOrWhiteSpace(
                            respuestaDinamica))
                    {
                        return CrearRespuestaLocalJson(
                            request,
                            respuestaDinamica);
                    }

                    return CrearErrorLocal(
                        request,
                        module == "noticias"
                            ? "Esta sección de noticias no fue descargada."
                            : "Esta sección del álbum no fue descargada.");
                }

                await database.MarcarUsoRespuestaAsync(
                    cacheKey,
                    DateTime.UtcNow);

                return CrearRespuestaCache(
                    request,
                    local);
            }

            HttpResponseMessage response =
                await base.SendAsync(
                    request,
                    cancellationToken);

            if (!response.IsSuccessStatusCode ||
                response.Content == null)
            {
                return response;
            }

            string json = await response.Content
                .ReadAsStringAsync(cancellationToken);

            string contentType =
                response.Content.Headers.ContentType?.MediaType ??
                "application/json";

            /*
             * Durante Descargar todo todas las rutas del paquete comparten una
             * sola versión. Fuera de esa operación, una consulta online normal
             * puede conservar su hash individual como copia silenciosa.
             */
            string version =
                DescargaOfflineContext.Activa &&
                !string.IsNullOrWhiteSpace(
                    DescargaOfflineContext.VersionTransaccional)
                    ? DescargaOfflineContext.VersionTransaccional
                    : CalcularHash(json);

            DateTime now = DateTime.UtcNow;

            await database.GuardarRespuestaAsync(
                new ContenidoRespuestaCacheEntity
                {
                    CacheKey = cacheKey,
                    UsuarioId = usuarioId,
                    Modulo = module,
                    Ruta = route,
                    Version = version,
                    StatusCode = (int)response.StatusCode,
                    ContentType = contentType,
                    Json = json,
                    GuardadoUtc = now,
                    UltimoUsoUtc = now
                });

            await database.GuardarEstadoAsync(
                new ContenidoModuloEstadoEntity
                {
                    Clave = $"{usuarioId}|{module}",
                    UsuarioId = usuarioId,
                    Modulo = module,
                    Version = version,
                    VersionServidor = version,
                    FechaServidorUtc = now,
                    VerificadoUtc = now,
                    UltimaSincronizacionExitosaUtc = now,
                    UltimoUsoLocalUtc = now,
                    OrigenUltimaCarga = "servidor",
                    UltimoError = string.Empty
                });

            ReemplazarContenido(
                response,
                json,
                contentType);

            Task imagesTask = PreCargarImagenesAsync(
                request,
                usuarioId,
                module,
                version,
                route,
                json,
                cancellationToken);

            if (DescargaOfflineContext.Activa)
            {
                await imagesTask;
                await ImagenLocalCacheService.AplicarLimiteAsync();
            }
            else
            {
                _ = ContinuarEnSegundoPlanoAsync(imagesTask);
            }

            return response;
        }

        private static async Task ContinuarEnSegundoPlanoAsync(
            Task task)
        {
            try
            {
                await task;
            }
            catch
            {
            }
        }

        private async Task PreCargarImagenesAsync(
            HttpRequestMessage request,
            string usuarioId,
            string module,
            string version,
            string route,
            string json,
            CancellationToken cancellationToken)
        {
            try
            {
                List<DescargaImagen> downloads =
                    ExtraerImagenes(
                        request,
                        module,
                        route,
                        json)
                    .Where(item =>
                        !string.IsNullOrWhiteSpace(item.Url))
                    .DistinctBy(item =>
                        $"{item.Url}|{item.Original}")
                    .Take(
                        DescargaOfflineContext.Activa
                            ? 200
                            : 20)
                    .ToList();

                await Task.WhenAll(
                    downloads.Select(item =>
                        DescargarImagenAsync(
                            usuarioId,
                            module,
                            version,
                            item,
                            cancellationToken)));
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch
            {
                /* El JSON sigue siendo válido aunque una imagen falle. */
            }
        }

        private IEnumerable<DescargaImagen> ExtraerImagenes(
            HttpRequestMessage request,
            string module,
            string route,
            string json)
        {
            using JsonDocument document = JsonDocument.Parse(json);
            Uri authority = ObtenerAutoridad(request);

            if (module == "noticias")
            {
                bool detalle = route.Contains(
                    "/detalle/",
                    StringComparison.OrdinalIgnoreCase);

                foreach (string path in EncontrarTextos(
                             document.RootElement,
                             "rutaImagenPortada"))
                {
                    yield return new DescargaImagen
                    {
                        Url = ConstruirMiniatura(
                            authority,
                            path,
                            detalle ? 1200 : 720,
                            detalle ? 900 : 480,
                            detalle ? 76 : 68),
                        Original = false
                    };
                }

                yield break;
            }

            foreach (string path in EncontrarTextos(
                         document.RootElement,
                         "rutaImagenPortada"))
            {
                yield return new DescargaImagen
                {
                    Url = ConstruirMiniatura(
                        authority,
                        path,
                        420,
                        260,
                        65),
                    Original = false
                };
            }

            foreach (string path in EncontrarTextos(
                         document.RootElement,
                         "fotoPortada"))
            {
                yield return new DescargaImagen
                {
                    Url = ConstruirMiniatura(
                        authority,
                        path,
                        720,
                        480,
                        68),
                    Original = false
                };
            }

            bool detalleAlbum = route.Contains(
                "/detalle/",
                StringComparison.OrdinalIgnoreCase);

            foreach (string path in EncontrarTextos(
                         document.RootElement,
                         "rutaFoto"))
            {
                yield return new DescargaImagen
                {
                    Url = ConstruirMiniatura(
                        authority,
                        path,
                        720,
                        480,
                        68),
                    Original = false
                };

                if (detalleAlbum || DescargaOfflineContext.Activa)
                {
                    yield return new DescargaImagen
                    {
                        Url = ConstruirContenidoUrl(
                            authority,
                            path),
                        Original = true
                    };
                }
            }
        }

        private async Task DescargarImagenAsync(
            string usuarioId,
            string module,
            string version,
            DescargaImagen image,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(image.Url))
                return;

            string destination = image.Original
                ? ImagenLocalCacheService.ObtenerRutaOriginal(
                    image.Url)
                : ImagenLocalCacheService.ObtenerRutaMiniatura(
                    image.Url);

            if (File.Exists(destination) &&
                new FileInfo(destination).Length > 0)
            {
                await ImagenLocalCacheService.RegistrarAsync(
                    usuarioId,
                    module,
                    image.Url,
                    destination,
                    version,
                    image.Original);
                return;
            }

            await ImageDownloads.WaitAsync(cancellationToken);

            try
            {
                using var timeout =
                    CancellationTokenSource.CreateLinkedTokenSource(
                        cancellationToken);

                timeout.CancelAfter(TimeSpan.FromSeconds(20));

                using var imageRequest = new HttpRequestMessage(
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
                    usuarioId,
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
            }
            finally
            {
                ImageDownloads.Release();
            }
        }

        private static IEnumerable<string> EncontrarTextos(
            JsonElement element,
            string propertyName)
        {
            if (element.ValueKind == JsonValueKind.Object)
            {
                foreach (JsonProperty property in element.EnumerateObject())
                {
                    if (string.Equals(
                            property.Name,
                            propertyName,
                            StringComparison.OrdinalIgnoreCase) &&
                        property.Value.ValueKind == JsonValueKind.String)
                    {
                        string? value = property.Value.GetString();
                        if (!string.IsNullOrWhiteSpace(value))
                            yield return value;
                    }

                    foreach (string nested in EncontrarTextos(
                                 property.Value,
                                 propertyName))
                    {
                        yield return nested;
                    }
                }
            }
            else if (element.ValueKind == JsonValueKind.Array)
            {
                foreach (JsonElement item in element.EnumerateArray())
                {
                    foreach (string nested in EncontrarTextos(
                                 item,
                                 propertyName))
                    {
                        yield return nested;
                    }
                }
            }
        }

        private static string ConstruirMiniatura(
            Uri authority,
            string path,
            int width,
            int height,
            int quality)
        {
            string absolutePath = ObtenerRuta(path);

            return new Uri(
                authority,
                "/imagenes/miniatura" +
                $"?ruta={Uri.EscapeDataString(absolutePath)}" +
                $"&ancho={Math.Clamp(width, 120, 1200)}" +
                $"&alto={Math.Clamp(height, 120, 1200)}" +
                $"&calidad={Math.Clamp(quality, 45, 85)}")
                .ToString();
        }

        private static string ConstruirContenidoUrl(
            Uri authority,
            string path)
        {
            if (Uri.TryCreate(path, UriKind.Absolute, out Uri? absolute))
                return absolute.ToString();

            return new Uri(authority, ObtenerRuta(path)).ToString();
        }

        private static string ObtenerRuta(string path)
        {
            if (Uri.TryCreate(path, UriKind.Absolute, out Uri? absolute))
                path = absolute.AbsolutePath;

            return path.StartsWith('/')
                ? path
                : "/" + path;
        }

        private static Uri ObtenerAutoridad(
            HttpRequestMessage request)
        {
            Uri uri = request.RequestUri ??
                throw new InvalidOperationException(
                    "La solicitud no contiene una URL válida.");

            if (!uri.IsAbsoluteUri)
            {
                string baseUrl =
                    new UrlApiService()
                        .BaseUrlApi
                        .TrimEnd('/') + "/";

                Uri baseUri = new(
                    baseUrl,
                    UriKind.Absolute);

                uri = new Uri(
                    baseUri,
                    uri);
            }

            return new Uri(uri.GetLeftPart(UriPartial.Authority));
        }

        private static HttpResponseMessage CrearRespuestaCache(
            HttpRequestMessage request,
            ContenidoRespuestaCacheEntity local)
        {
            var response = new HttpResponseMessage(
                (HttpStatusCode)local.StatusCode)
            {
                RequestMessage = request,
                Content = new StringContent(
                    local.Json,
                    Encoding.UTF8,
                    string.IsNullOrWhiteSpace(local.ContentType)
                        ? "application/json"
                        : local.ContentType)
            };

            response.Headers.TryAddWithoutValidation(
                "X-Contenido-Origen",
                "sqlite-sesion-offline");

            return response;
        }

        private static HttpResponseMessage CrearRespuestaLocalJson(
            HttpRequestMessage request,
            string json)
        {
            var response = new HttpResponseMessage(
                HttpStatusCode.OK)
            {
                RequestMessage = request,
                Content = new StringContent(
                    json,
                    Encoding.UTF8,
                    "application/json")
            };

            response.Headers.TryAddWithoutValidation(
                "X-Contenido-Origen",
                "sqlite-consulta-local");

            return response;
        }

        private static HttpResponseMessage CrearErrorLocal(
            HttpRequestMessage request,
            string message) =>
            new(HttpStatusCode.ServiceUnavailable)
            {
                RequestMessage = request,
                Content = new StringContent(
                    JsonSerializer.Serialize(new
                    {
                        success = false,
                        message
                    }),
                    Encoding.UTF8,
                    "application/json")
            };

        private static void ReemplazarContenido(
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

        private static string? DeterminarModulo(
            HttpRequestMessage request)
        {
            string pathAndQuery = ObtenerPathYQuery(request)
                .ToLowerInvariant();

            if (pathAndQuery.Contains("incluirinactivos=true"))
                return null;

            string path = ObtenerPath(request)
                .ToLowerInvariant();

            if (path == "/api/publicacion/categorias" ||
                path == "/api/publicacion/feed" ||
                path.StartsWith("/api/publicacion/detalle/"))
            {
                return "noticias";
            }

            if (path == "/api/album-botanico/inicio" ||
                path == "/api/album-botanico/galeria" ||
                path == "/api/album-botanico/galeria-paginada" ||
                path.StartsWith("/api/album-botanico/detalle/") ||
                path == "/api/categoria-album-botanico/listar")
            {
                return "album";
            }

            return null;
        }

        private static string ObtenerPath(
            HttpRequestMessage request)
        {
            Uri? uri = request.RequestUri;
            if (uri == null)
                return string.Empty;

            if (uri.IsAbsoluteUri)
                return uri.AbsolutePath;

            string raw = uri.OriginalString;
            int query = raw.IndexOf('?');
            if (query >= 0)
                raw = raw[..query];

            return "/" + raw.TrimStart('/');
        }

        private static string ObtenerPathYQuery(
            HttpRequestMessage request)
        {
            Uri? uri = request.RequestUri;
            if (uri == null)
                return string.Empty;

            return uri.IsAbsoluteUri
                ? uri.PathAndQuery
                : "/" + uri.OriginalString.TrimStart('/');
        }

        private static string CalcularHash(string value)
        {
            byte[] hash = SHA256.HashData(
                Encoding.UTF8.GetBytes(value));

            return Convert.ToHexString(hash)
                .ToLowerInvariant();
        }

        private sealed class DescargaImagen
        {
            public string Url { get; init; } = string.Empty;
            public bool Original { get; init; }
        }
    }
}
