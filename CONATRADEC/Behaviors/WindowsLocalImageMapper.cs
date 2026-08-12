using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Handlers;
using System.IO;

namespace CONATRADEC.Behaviors
{
    /// <summary>
    /// Corrige exclusivamente en Windows la carga de imágenes almacenadas en
    /// carpetas locales escribibles de la aplicación.
    ///
    /// Una ruta absoluta recibida mediante Binding puede convertirse en un
    /// FileImageSource. En WinUI, cuando la aplicación está instalada, ese
    /// origen puede resolverse como si perteneciera al contenido del paquete.
    ///
    /// Si el archivo existe físicamente, se sustituye únicamente ese origen
    /// por un StreamImageSource. Las imágenes empaquetadas, las URL remotas y
    /// el comportamiento de Android permanecen sin cambios.
    /// </summary>
    public static class WindowsLocalImageMapper
    {
#if WINDOWS
        private static bool registrado;
#endif

        public static void Register()
        {
#if WINDOWS
            if (registrado)
                return;

            registrado = true;

            /*
             * Se usa la clave real Source para que también se ejecute cuando
             * un Binding cambie la imagen después de haberse creado el handler.
             */
            ImageHandler.Mapper.AppendToMapping(
                nameof(Microsoft.Maui.IImage.Source),
                (_, image) =>
                {
                    if (image.Source is not FileImageSource fileSource)
                        return;

                    string? ruta = fileSource.File;

                    if (string.IsNullOrWhiteSpace(ruta) ||
                        !Path.IsPathRooted(ruta) ||
                        !File.Exists(ruta))
                    {
                        return;
                    }

                    if (image is not Image control)
                        return;

                    /*
                     * Cambiar Source provoca nuevamente el mapping. La segunda
                     * pasada recibe StreamImageSource y sale inmediatamente,
                     * por lo que no existe un ciclo de conversión.
                     *
                     * La fábrica devuelve un stream NUEVO en cada invocación,
                     * tal como requiere ImageSource.FromStream.
                     */
                    control.Source = ImageSource.FromStream(
                        () => AbrirArchivoSeguro(ruta));
                });
#endif
        }

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
                 * Si el archivo desapareció entre el Binding y la lectura,
                 * la imagen simplemente queda vacía sin afectar la pantalla.
                 */
                return new MemoryStream(
                    Array.Empty<byte>(),
                    writable: false);
            }
        }
#endif
    }
}