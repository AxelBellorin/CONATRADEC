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
             * En Windows offline se prefiere la copia JPEG original local
             * cuando la URL recibida corresponde a una miniatura.
             *
             * Android conserva su flujo actual.
             */
            if (ModoSesionService.EsOffline &&
                DeviceInfo.Current.Platform == DevicePlatform.WinUI)
            {
                string? originalUrl =
                    IntentarObtenerOriginalDesdeMiniatura(url);

                if (!string.IsNullOrWhiteSpace(originalUrl))
                {
                    string originalPath =
                        ObtenerRutaOriginal(originalUrl);

                    if (ArchivoValido(originalPath))
                    {
                        MarcarUsoFisico(originalPath);
                        return originalPath;
                    }
                }
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

        public static string ObtenerRutaMiniatura(string url) =>
            ObtenerRuta(
                "miniatura",
                url,
                DeviceInfo.Current.Platform == DevicePlatform.WinUI
                    ? ".jpg"
                    : ".webp");

        public static string ObtenerRutaOriginal(string url)
        {
            /*
             * La preparación offline de Windows obtiene una representación
             * JPEG desde el backend. Se conserva la extensión .jpg para que
             * WinUI identifique correctamente el archivo físico.
             *
             * Android conserva la extensión real utilizada actualmente.
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
