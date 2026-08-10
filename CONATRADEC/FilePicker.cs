using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices;
using Microsoft.Maui.Storage;

namespace CONATRADEC
{
    /// <summary>
    /// Selector adaptativo de archivos para CONATRADEC.
    ///
    /// Conserva la forma de uso existente en todo el proyecto:
    /// FilePicker.Default.PickAsync(...) y
    /// FilePicker.Default.PickMultipleAsync(...).
    ///
    /// En Android, cuando se solicitan exclusivamente imágenes, utiliza el
    /// selector fotográfico nativo implementado en MainActivity.
    ///
    /// En Windows, iOS, MacCatalyst y para cualquier tipo de archivo que no sea
    /// imagen, utiliza el FilePicker original de .NET MAUI.
    /// </summary>
    public static class FilePicker
    {
        public static FilePickerProxy Default { get; } = new();

        public static Task<FileResult?> PickAsync(
            PickOptions? options = null) =>
            Default.PickAsync(options);

        public static Task<IEnumerable<FileResult>> PickMultipleAsync(
            PickOptions? options = null) =>
            Default.PickMultipleAsync(options);

        /// <summary>
        /// Proxy que mantiene intactas las llamadas actuales del proyecto y
        /// decide qué selector abrir según plataforma y tipo de archivo.
        /// </summary>
        public sealed class FilePickerProxy
        {
            internal FilePickerProxy()
            {
            }

            public async Task<FileResult?> PickAsync(
                PickOptions? options = null)
            {
#if ANDROID
                if (DebeUsarGaleriaAndroid(options))
                {
                    MainActivity? activity =
                        Platform.CurrentActivity as MainActivity;

                    if (activity != null)
                    {
                        try
                        {
                            IReadOnlyList<FileResult> seleccion =
                                await activity
                                    .SeleccionarFotosDesdeGaleriaAsync(
                                        seleccionMultiple: false);

                            return seleccion.FirstOrDefault();
                        }
                        catch (FeatureNotSupportedException)
                        {
                            // Si el dispositivo no dispone de un selector
                            // multimedia compatible, se usa el FilePicker
                            // estándar como respaldo.
                        }
                    }
                }
#endif

                return await Microsoft.Maui.Storage.FilePicker.Default
                    .PickAsync(options);
            }

            public async Task<IEnumerable<FileResult>> PickMultipleAsync(
                PickOptions? options = null)
            {
#if ANDROID
                if (DebeUsarGaleriaAndroid(options))
                {
                    MainActivity? activity =
                        Platform.CurrentActivity as MainActivity;

                    if (activity != null)
                    {
                        try
                        {
                            return await activity
                                .SeleccionarFotosDesdeGaleriaAsync(
                                    seleccionMultiple: true);
                        }
                        catch (FeatureNotSupportedException)
                        {
                            // Fallback seguro para dispositivos donde no
                            // exista un selector multimedia compatible.
                        }
                    }
                }
#endif

                return await Microsoft.Maui.Storage.FilePicker.Default
                    .PickMultipleAsync(options);
            }

            /// <summary>
            /// Solo sustituye el selector en Android cuando el filtro solicitado
            /// corresponde exclusivamente a imágenes.
            /// </summary>
            private static bool DebeUsarGaleriaAndroid(
                PickOptions? options)
            {
                if (DeviceInfo.Platform != DevicePlatform.Android)
                    return false;

                FilePickerFileType? tipos = options?.FileTypes;

                if (tipos == null)
                    return false;

                if (ReferenceEquals(
                        tipos,
                        FilePickerFileType.Images) ||
                    ReferenceEquals(
                        tipos,
                        FilePickerFileType.Jpeg) ||
                    ReferenceEquals(
                        tipos,
                        FilePickerFileType.Png))
                {
                    return true;
                }

                try
                {
                    string[] valores = tipos.Value?
                        .Where(
                            valor =>
                                !string.IsNullOrWhiteSpace(valor))
                        .ToArray()
                        ?? [];

                    return valores.Length > 0 &&
                           valores.All(EsTipoImagen);
                }
                catch
                {
                    // Si el filtro personalizado no puede inspeccionarse,
                    // no se modifica el comportamiento original de MAUI.
                    return false;
                }
            }

            private static bool EsTipoImagen(string tipo)
            {
                string valor = tipo.Trim();

                return valor.StartsWith(
                           "image/",
                           StringComparison.OrdinalIgnoreCase) ||
                       valor.Equals(
                           ".jpg",
                           StringComparison.OrdinalIgnoreCase) ||
                       valor.Equals(
                           ".jpeg",
                           StringComparison.OrdinalIgnoreCase) ||
                       valor.Equals(
                           ".png",
                           StringComparison.OrdinalIgnoreCase) ||
                       valor.Equals(
                           ".webp",
                           StringComparison.OrdinalIgnoreCase) ||
                       valor.Equals(
                           ".gif",
                           StringComparison.OrdinalIgnoreCase) ||
                       valor.Equals(
                           ".heic",
                           StringComparison.OrdinalIgnoreCase) ||
                       valor.Equals(
                           ".heif",
                           StringComparison.OrdinalIgnoreCase);
            }
        }
    }
}
