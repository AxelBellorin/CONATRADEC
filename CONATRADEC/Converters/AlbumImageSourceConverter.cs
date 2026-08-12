using Microsoft.Maui.Controls;
using System;
using System.Globalization;
using System.IO;

namespace CONATRADEC.Converters
{
    /// <summary>
    /// Convierte el origen visual de las fotografías del Álbum Botánico.
    ///
    /// En Windows los archivos descargados a AppDataDirectory se abren
    /// mediante StreamImageSource. Esto evita que WinUI intente resolver una
    /// ruta absoluta local como si fuera un recurso incluido en el instalable.
    ///
    /// Las URL HTTP/HTTPS y el comportamiento normal de Android permanecen
    /// intactos.
    /// </summary>
    public sealed class AlbumImageSourceConverter : IValueConverter
    {
        public object? Convert(
            object? value,
            Type targetType,
            object? parameter,
            CultureInfo culture)
        {
            if (value is ImageSource imageSource)
                return imageSource;

            string source = value?.ToString()?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(source))
                return null;

#if WINDOWS
            /*
             * Ruta física absoluta de AppDataDirectory.
             * Se entrega un stream nuevo cada vez que MAUI lo solicite.
             */
            if (Path.IsPathRooted(source) &&
                File.Exists(source))
            {
                return ImageSource.FromStream(
                    () => AbrirArchivoSeguro(source));
            }
#endif

            if (Uri.TryCreate(
                    source,
                    UriKind.Absolute,
                    out Uri? uri))
            {
#if WINDOWS
                if (uri.IsFile &&
                    File.Exists(uri.LocalPath))
                {
                    string rutaLocal = uri.LocalPath;

                    return ImageSource.FromStream(
                        () => AbrirArchivoSeguro(rutaLocal));
                }
#endif

                if (uri.Scheme.Equals(
                        Uri.UriSchemeHttp,
                        StringComparison.OrdinalIgnoreCase) ||
                    uri.Scheme.Equals(
                        Uri.UriSchemeHttps,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return ImageSource.FromUri(uri);
                }
            }

            /*
             * Recursos empaquetados y rutas locales de Android continúan
             * utilizando el comportamiento estándar de MAUI.
             */
            return ImageSource.FromFile(source);
        }

        public object ConvertBack(
            object? value,
            Type targetType,
            object? parameter,
            CultureInfo culture) =>
            throw new NotSupportedException();

#if WINDOWS
        private static Stream AbrirArchivoSeguro(string ruta)
        {
            try
            {
                return new FileStream(
                    ruta,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.ReadWrite | FileShare.Delete);
            }
            catch
            {
                /*
                 * Una fotografía que desaparezca entre el binding y la lectura
                 * no debe cerrar ni bloquear la pantalla.
                 */
                return new MemoryStream(
                    Array.Empty<byte>(),
                    writable: false);
            }
        }
#endif
    }
}
