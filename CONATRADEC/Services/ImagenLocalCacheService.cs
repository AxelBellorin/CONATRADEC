using CONATRADEC.Models;
using Microsoft.Maui.Storage;
using System.Security.Cryptography;
using System.Text;

namespace CONATRADEC.Services
{
    /// <summary>
    /// Guarda imágenes en AppDataDirectory, registra sus referencias y
    /// elimina archivos que ya no pertenecen a una versión vigente.
    /// </summary>
    public static class ImagenLocalCacheService
    {
        private const long LimitePredeterminadoBytes =
            250L * 1024L * 1024L;

        private static readonly string RootDirectory = Path.Combine(
            FileSystem.AppDataDirectory,
            "contenido-local",
            "imagenes");

        public static string ResolverMiniatura(string? url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return string.Empty;

            string path = ObtenerRutaMiniatura(url);

            if (!ArchivoValido(path))
                return url;

            MarcarUsoFisico(path);
            return path;
        }

        public static string ResolverOriginal(string? url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return string.Empty;

            string path = ObtenerRutaOriginal(url);

            if (!ArchivoValido(path))
                return url;

            MarcarUsoFisico(path);
            return path;
        }

        public static string ObtenerRutaMiniatura(string url) =>
            ObtenerRuta(
                "miniatura",
                url,
                ".webp");

        public static string ObtenerRutaOriginal(string url)
        {
            string extension = ".webp";

            if (Uri.TryCreate(
                    url,
                    UriKind.Absolute,
                    out Uri? uri))
            {
                string candidate = Path
                    .GetExtension(uri.AbsolutePath)
                    .ToLowerInvariant();

                if (candidate is ".jpg" or ".jpeg" or ".png" or ".webp")
                    extension = candidate;
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

            string? directory = Path.GetDirectoryName(destination);

            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);

            string temporary =
                destination + $".{Guid.NewGuid():N}.tmp";

            try
            {
                await using FileStream output = new(
                    temporary,
                    FileMode.Create,
                    FileAccess.Write,
                    FileShare.None,
                    bufferSize: 81920,
                    useAsync: true);

                await source.CopyToAsync(
                    output,
                    cancellationToken);

                await output.FlushAsync(cancellationToken);

                File.Move(
                    temporary,
                    destination,
                    overwrite: true);
            }
            finally
            {
                if (File.Exists(temporary))
                {
                    try
                    {
                        File.Delete(temporary);
                    }
                    catch
                    {
                    }
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

            long total = archivos.Sum(x => x.Length);

            if (total <= limiteBytes)
                return;

            long objetivo = (long)(limiteBytes * 0.90);

            foreach (FileInfo file in archivos)
            {
                if (total <= objetivo)
                    break;

                /*
                 * Nunca se elimina una imagen todavía referenciada por la
                 * versión vigente. De esta forma, Descargar todo conserva
                 * realmente el contenido actual aunque supere el límite
                 * orientativo. El límite se utiliza para limpiar archivos
                 * huérfanos y temporales antiguos.
                 */
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
                DateTime ultimoUso = File.GetLastWriteTimeUtc(path);

                if (DateTime.UtcNow - ultimoUso > TimeSpan.FromHours(12))
                    File.SetLastWriteTimeUtc(path, DateTime.UtcNow);
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
