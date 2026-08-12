using CONATRADEC.Models;
using Microsoft.Maui.Devices;
using Microsoft.Maui.Storage;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;

namespace CONATRADEC.Services
{
    /// <summary>
    /// Guarda imágenes en AppDataDirectory y resuelve sus rutas locales.
    /// En una sesión offline nunca devuelve una URL remota si el archivo no
    /// existe, evitando que ImageSource intente abrir sockets por su cuenta.
    /// </summary>
    public static class ImagenLocalCacheService
    {
        private const long LimitePredeterminadoBytes =
            250L * 1024L * 1024L;

        private static readonly string RootDirectory = Path.Combine(
            FileSystem.AppDataDirectory,
            "contenido-local",
            "imagenes");

        /*
         * Cada destino físico se escribe de forma exclusiva.
         *
         * El Álbum puede descubrir la misma fotografía desde diferentes
         * respuestas JSON mientras varias descargas están en paralelo.
         * Este bloqueo evita que dos tareas intenten reemplazar el mismo
         * archivo al mismo tiempo, sin serializar imágenes distintas.
         */
        private static readonly ConcurrentDictionary<string, SemaphoreSlim>
            BloqueosEscritura =
                new(StringComparer.OrdinalIgnoreCase);

        public static string ResolverMiniatura(string? url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return string.Empty;

            /*
             * El Álbum offline de Windows usa exclusivamente copias JPEG
             * preparadas por el endpoint dedicado.
             *
             * Se revisa primero la nueva ubicación aislada y después, por
             * compatibilidad, el archivo utilizado por versiones anteriores.
             * El archivo legacy solamente se acepta si sus bytes corresponden
             * realmente a JPEG; un WebP guardado antiguamente con extensión
             * .jpg queda descartado.
             */
            if (ModoSesionService.EsOffline &&
                DeviceInfo.Current.Platform == DevicePlatform.WinUI &&
                EsUrlAlbum(url))
            {
                string? originalUrl =
                    IntentarObtenerOriginalDesdeMiniatura(url);

                if (!string.IsNullOrWhiteSpace(originalUrl))
                {
                    string originalOffline =
                        ObtenerRutaOriginalOfflineWindows(originalUrl);

                    if (ArchivoJpegValido(originalOffline))
                    {
                        MarcarUsoFisico(originalOffline);
                        return originalOffline;
                    }

                    string originalLegacy =
                        ObtenerRuta(
                            "original",
                            originalUrl,
                            ".jpg");

                    if (ArchivoJpegValido(originalLegacy))
                    {
                        MarcarUsoFisico(originalLegacy);
                        return originalLegacy;
                    }
                }

                string miniaturaOffline =
                    ObtenerRutaMiniaturaOfflineWindows(url);

                if (ArchivoJpegValido(miniaturaOffline))
                {
                    MarcarUsoFisico(miniaturaOffline);
                    return miniaturaOffline;
                }

                string miniaturaLegacy =
                    ObtenerRuta(
                        "miniatura",
                        url,
                        ".jpg");

                if (ArchivoJpegValido(miniaturaLegacy))
                {
                    MarcarUsoFisico(miniaturaLegacy);
                    return miniaturaLegacy;
                }

                return string.Empty;
            }

            string path = ObtenerRutaMiniatura(url);

            if (!ArchivoValido(path))
            {
                return ModoSesionService.EsOffline
                    ? string.Empty
                    : url;
            }

            MarcarUsoFisico(path);
            return path;
        }

        public static string ResolverOriginal(string? url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return string.Empty;

            if (ModoSesionService.EsOffline &&
                DeviceInfo.Current.Platform == DevicePlatform.WinUI &&
                EsUrlAlbum(url))
            {
                string offlinePath =
                    ObtenerRutaOriginalOfflineWindows(url);

                if (ArchivoJpegValido(offlinePath))
                {
                    MarcarUsoFisico(offlinePath);
                    return offlinePath;
                }

                string legacyPath =
                    ObtenerRuta(
                        "original",
                        url,
                        ".jpg");

                if (ArchivoJpegValido(legacyPath))
                {
                    MarcarUsoFisico(legacyPath);
                    return legacyPath;
                }

                return string.Empty;
            }

            string path = ObtenerRutaOriginal(url);

            if (!ArchivoValido(path))
            {
                return ModoSesionService.EsOffline
                    ? string.Empty
                    : url;
            }

            MarcarUsoFisico(path);
            return path;
        }

        public static string ObtenerRutaMiniatura(string url)
        {
            if (DebeUsarRutaJpegOfflineWindows(url))
                return ObtenerRutaMiniaturaOfflineWindows(url);

            return ObtenerRuta(
                "miniatura",
                url,
                DeviceInfo.Current.Platform == DevicePlatform.WinUI
                    ? ".jpg"
                    : ".webp");
        }

        public static string ObtenerRutaOriginal(string url)
        {
            /*
             * Durante "Descargar todo", las fotografías del Álbum en Windows
             * se guardan en una ubicación exclusiva para JPEG. Así una visita
             * online previa no puede dejar un WebP con extensión .jpg que haga
             * pensar a una descarga posterior que ya existe una copia válida.
             */
            if (DebeUsarRutaJpegOfflineWindows(url))
                return ObtenerRutaOriginalOfflineWindows(url);

            /*
             * Se conserva la ubicación histórica para la navegación normal.
             * El modo offline del Álbum valida la firma JPEG antes de aceptar
             * cualquiera de estos archivos legacy.
             */
            if (DeviceInfo.Current.Platform == DevicePlatform.WinUI)
            {
                return ObtenerRuta(
                    "original",
                    url,
                    ".jpg");
            }

            string extension = ".webp";

            if (Uri.TryCreate(
                    url,
                    UriKind.Absolute,
                    out Uri? uri))
            {
                string candidate = Path
                    .GetExtension(uri.AbsolutePath)
                    .ToLowerInvariant();

                if (candidate is
                    ".jpg" or ".jpeg" or ".png" or ".webp")
                {
                    extension = candidate;
                }
            }

            return ObtenerRuta(
                "original",
                url,
                extension);
        }

        public static async Task GuardarAsync(
            Stream source,
            string destination,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(source);

            if (string.IsNullOrWhiteSpace(destination))
            {
                throw new ArgumentException(
                    "La ruta de destino de la imagen es obligatoria.",
                    nameof(destination));
            }

            string destinationFullPath =
                Path.GetFullPath(destination);

            SemaphoreSlim bloqueo =
                BloqueosEscritura.GetOrAdd(
                    destinationFullPath,
                    _ => new SemaphoreSlim(1, 1));

            await bloqueo.WaitAsync(cancellationToken);

            string? temporary = null;

            try
            {
                /*
                 * Otra tarea pudo haber terminado de guardar la misma imagen
                 * mientras esta esperaba el bloqueo.
                 */
                if (ArchivoValido(destinationFullPath))
                    return;

                string? directory =
                    Path.GetDirectoryName(destinationFullPath);

                if (!string.IsNullOrWhiteSpace(directory))
                    Directory.CreateDirectory(directory);

                temporary =
                    destinationFullPath +
                    $".{Guid.NewGuid():N}.tmp";

                /*
                 * IMPORTANTE EN WINDOWS:
                 *
                 * El FileStream debe estar completamente cerrado ANTES de
                 * ejecutar File.Move. Un "await using FileStream output = ..."
                 * declarado directamente dentro del try mantiene abierto el
                 * archivo hasta terminar todo el bloque, por lo que Windows
                 * rechaza el Move indicando que el archivo está siendo usado.
                 */
                await using (
                    FileStream output = new(
                        temporary,
                        FileMode.CreateNew,
                        FileAccess.Write,
                        FileShare.None,
                        bufferSize: 81920,
                        useAsync: true))
                {
                    await source.CopyToAsync(
                        output,
                        cancellationToken);

                    await output.FlushAsync(cancellationToken);
                }

                cancellationToken.ThrowIfCancellationRequested();

                File.Move(
                    temporary,
                    destinationFullPath,
                    overwrite: true);

                temporary = null;
            }
            finally
            {
                if (!string.IsNullOrWhiteSpace(temporary) &&
                    File.Exists(temporary))
                {
                    try
                    {
                        File.Delete(temporary);
                    }
                    catch
                    {
                    }
                }

                bloqueo.Release();

                /*
                 * Se elimina el semáforo únicamente cuando no hay otra tarea
                 * dentro del bloqueo. TryRemove no afecta a tareas que ya
                 * conservaron la referencia local al mismo SemaphoreSlim.
                 */
                if (bloqueo.CurrentCount == 1)
                {
                    BloqueosEscritura.TryRemove(
                        new KeyValuePair<string, SemaphoreSlim>(
                            destinationFullPath,
                            bloqueo));
                }
            }
        }

        public static async Task RegistrarAsync(
            string usuarioId,
            string modulo,
            string urlRemota,
            string rutaLocal,
            string version,
            bool esOriginal)
        {
            long tamano = 0;

            try
            {
                if (File.Exists(rutaLocal))
                    tamano = new FileInfo(rutaLocal).Length;
            }
            catch
            {
            }

            string clave = CalcularHash(
                $"{usuarioId}|{modulo}|{urlRemota}");

            await ContenidoLocalDatabaseService.Instance
                .GuardarImagenAsync(
                    new ContenidoImagenCacheEntity
                    {
                        Clave = clave,
                        UsuarioId = usuarioId,
                        Modulo = modulo,
                        Version = version,
                        UrlRemota = urlRemota,
                        RutaLocal = rutaLocal,
                        EsOriginal = esOriginal,
                        TamanoBytes = tamano,
                        GuardadoUtc = DateTime.UtcNow,
                        UltimoUsoUtc = DateTime.UtcNow
                    });
        }

        public static async Task<int> LimpiarVersionAnteriorAsync(
            string usuarioId,
            string modulo,
            string versionVigente)
        {
            List<ContenidoImagenCacheEntity> obsoletas =
                await ContenidoLocalDatabaseService.Instance
                    .ObtenerImagenesVersionAnteriorAsync(
                        usuarioId,
                        modulo,
                        versionVigente);

            int eliminadas = 0;

            foreach (ContenidoImagenCacheEntity image in obsoletas)
            {
                await ContenidoLocalDatabaseService.Instance
                    .EliminarImagenAsync(image.Clave);

                int referencias =
                    await ContenidoLocalDatabaseService.Instance
                        .ContarReferenciasImagenAsync(
                            image.RutaLocal);

                if (referencias > 0)
                    continue;

                if (EliminarArchivoSeguro(image.RutaLocal))
                    eliminadas++;
            }

            return eliminadas;
        }

        public static async Task AplicarLimiteAsync(
            long limiteBytes = LimitePredeterminadoBytes)
        {
            if (limiteBytes <= 0 ||
                !Directory.Exists(RootDirectory))
            {
                return;
            }

            List<FileInfo> archivos;

            try
            {
                archivos = Directory
                    .EnumerateFiles(
                        RootDirectory,
                        "*",
                        SearchOption.AllDirectories)
                    .Where(path =>
                        !path.EndsWith(
                            ".tmp",
                            StringComparison.OrdinalIgnoreCase))
                    .Select(path => new FileInfo(path))
                    .Where(file => file.Exists)
                    .OrderBy(file => file.LastWriteTimeUtc)
                    .ToList();
            }
            catch
            {
                return;
            }

            long total = archivos.Sum(item => item.Length);
            if (total <= limiteBytes)
                return;

            long objetivo = (long)(limiteBytes * 0.90);

            foreach (FileInfo file in archivos)
            {
                if (total <= objetivo)
                    break;

                int referencias =
                    await ContenidoLocalDatabaseService.Instance
                        .ContarReferenciasImagenAsync(
                            file.FullName);

                if (referencias > 0)
                    continue;

                long length = file.Length;

                if (!EliminarArchivoSeguro(file.FullName))
                    continue;

                total -= length;

                await ContenidoLocalDatabaseService.Instance
                    .EliminarImagenesPorRutaAsync(
                        file.FullName);
            }
        }

        public static long ObtenerTamanoFisicoBytes()
        {
            if (!Directory.Exists(RootDirectory))
                return 0;

            try
            {
                return Directory
                    .EnumerateFiles(
                        RootDirectory,
                        "*",
                        SearchOption.AllDirectories)
                    .Select(path => new FileInfo(path))
                    .Where(file => file.Exists)
                    .Sum(file => file.Length);
            }
            catch
            {
                return 0;
            }
        }

        private static bool DebeUsarRutaJpegOfflineWindows(
            string url) =>
            DescargaOfflineContext.Activa &&
            DeviceInfo.Current.Platform == DevicePlatform.WinUI &&
            EsUrlAlbum(url);

        private static string ObtenerRutaMiniaturaOfflineWindows(
            string url) =>
            ObtenerRuta(
                "miniatura-jpeg-windows",
                url,
                ".jpg");

        private static string ObtenerRutaOriginalOfflineWindows(
            string url) =>
            ObtenerRuta(
                "original-jpeg-windows",
                url,
                ".jpg");

        private static bool EsUrlAlbum(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return false;

            if (Uri.TryCreate(
                    url,
                    UriKind.Absolute,
                    out Uri? uri))
            {
                if (uri.AbsolutePath.StartsWith(
                        "/imagenes/miniatura",
                        StringComparison.OrdinalIgnoreCase))
                {
                    string? original =
                        IntentarObtenerOriginalDesdeMiniatura(url);

                    return !string.IsNullOrWhiteSpace(original) &&
                           EsUrlAlbum(original);
                }

                return EsRutaAlbum(uri.AbsolutePath);
            }

            return EsRutaAlbum(url);
        }

        private static bool EsRutaAlbum(string ruta)
        {
            string normalizada = ruta
                .Replace('\\', '/')
                .Trim();

            if (!normalizada.StartsWith('/'))
                normalizada = "/" + normalizada;

            return normalizada.StartsWith(
                       "/resources/uploads/album-botanico/",
                       StringComparison.OrdinalIgnoreCase) ||
                   normalizada.StartsWith(
                       "/resources/uploads/categorias-album/",
                       StringComparison.OrdinalIgnoreCase) ||
                   normalizada.StartsWith(
                       "/resources/uploads/diagnosticos-ia/",
                       StringComparison.OrdinalIgnoreCase) ||
                   normalizada.StartsWith(
                       "/resources/uploads/diagnostico-ia/",
                       StringComparison.OrdinalIgnoreCase);
        }

        private static bool ArchivoJpegValido(string path)
        {
            try
            {
                if (!File.Exists(path))
                    return false;

                var info = new FileInfo(path);
                if (info.Length < 3)
                    return false;

                using FileStream stream = new(
                    path,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.ReadWrite | FileShare.Delete);

                int primero = stream.ReadByte();
                int segundo = stream.ReadByte();
                int tercero = stream.ReadByte();

                return primero == 0xFF &&
                       segundo == 0xD8 &&
                       tercero == 0xFF;
            }
            catch
            {
                return false;
            }
        }

        private static string ObtenerRuta(
            string tipo,
            string url,
            string extension)
        {
            string filename =
                CalcularHash(url.Trim()) + extension;

            return Path.Combine(
                RootDirectory,
                tipo,
                filename);
        }

        /// <summary>
        /// Convierte una URL de /imagenes/miniatura en la URL del archivo
        /// original utilizando el parámetro "ruta". No realiza peticiones
        /// de red.
        /// </summary>
        private static string? IntentarObtenerOriginalDesdeMiniatura(
            string miniaturaUrl)
        {
            if (!Uri.TryCreate(
                    miniaturaUrl,
                    UriKind.Absolute,
                    out Uri? uri))
            {
                return null;
            }

            if (!uri.AbsolutePath.StartsWith(
                    "/imagenes/miniatura",
                    StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            string query = uri.Query.TrimStart('?');

            foreach (string part in query.Split(
                         '&',
                         StringSplitOptions.RemoveEmptyEntries))
            {
                string[] pair = part.Split('=', 2);

                if (pair.Length != 2 ||
                    !string.Equals(
                        pair[0],
                        "ruta",
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string ruta;

                try
                {
                    ruta = Uri.UnescapeDataString(pair[1]);
                }
                catch
                {
                    return null;
                }

                if (string.IsNullOrWhiteSpace(ruta))
                    return null;

                if (Uri.TryCreate(
                        ruta,
                        UriKind.Absolute,
                        out Uri? absolute))
                {
                    return absolute.ToString();
                }

                string rutaNormalizada =
                    ruta.StartsWith('/')
                        ? ruta
                        : "/" + ruta;

                return new Uri(
                    new Uri(
                        uri.GetLeftPart(UriPartial.Authority)),
                    rutaNormalizada)
                    .ToString();
            }

            return null;
        }

        private static string CalcularHash(string value)
        {
            byte[] hash = SHA256.HashData(
                Encoding.UTF8.GetBytes(value));

            return Convert
                .ToHexString(hash)
                .ToLowerInvariant();
        }

        private static bool ArchivoValido(string path)
        {
            try
            {
                return File.Exists(path) &&
                       new FileInfo(path).Length > 0;
            }
            catch
            {
                return false;
            }
        }

        private static void MarcarUsoFisico(string path)
        {
            try
            {
                DateTime ultimoUso =
                    File.GetLastWriteTimeUtc(path);

                if (DateTime.UtcNow - ultimoUso >
                    TimeSpan.FromHours(12))
                {
                    File.SetLastWriteTimeUtc(
                        path,
                        DateTime.UtcNow);
                }
            }
            catch
            {
            }
        }

        private static bool EliminarArchivoSeguro(string path)
        {
            try
            {
                if (!File.Exists(path))
                    return false;

                File.Delete(path);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
