using CONATRADEC.Models;
using CONATRADEC.Services;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using System.Diagnostics;

namespace CONATRADEC.ViewModels
{
    /// <summary>
    /// Extiende el formulario de terreno para manejar de forma segura las
    /// fotografías nuevas mientras permanecen pendientes.
    ///
    /// Las fotografías seleccionadas se conservan únicamente en el caché
    /// temporal local. No se llama al servidor al seleccionar, quitar o
    /// cancelar. El ViewModel original continúa subiéndolas solamente después
    /// de guardar correctamente el terreno.
    ///
    /// La lógica existente de creación, edición, guardado, GPS, mapa y carga
    /// de fotografías guardadas permanece en TerrenoFormViewModel.
    /// </summary>
    public sealed class TerrenoFormFotosSegurasViewModel :
        TerrenoFormViewModel
    {
        private const int EsperaLiberacionImagenMs = 900;
        private const int IntentosEliminarTemporal = 5;
        private const int EsperaEntreIntentosMs = 350;

        private readonly SemaphoreSlim fotosOperacionLock =
            new(1, 1);

        private readonly Command<FotoTerrenoItem>
            quitarFotoCommandOriginal;

        private readonly Command
            cancelarCommandOriginal;

        private bool procesandoFotos;

        /// <summary>
        /// Oculta el comando original solamente para la selección de
        /// fotografías nuevas.
        /// </summary>
        public new Command SeleccionarFotosCommand { get; }

        /// <summary>
        /// Para fotografías nuevas realiza una liberación segura.
        /// Para fotografías ya guardadas conserva el comando original,
        /// incluyendo confirmación y eliminación mediante la API.
        /// </summary>
        public new Command<FotoTerrenoItem> QuitarFotoCommand { get; }

        /// <summary>
        /// Cancela el formulario asegurando que ninguna fotografía pendiente
        /// sea enviada al servidor y que todos sus temporales sean liberados.
        /// </summary>
        public new Command CancelCommand { get; }

        public TerrenoFormFotosSegurasViewModel()
        {
            /*
             * Se conserva una referencia al comando original antes de
             * ocultarlo. Así las fotos que ya existen en el servidor siguen
             * utilizando exactamente la misma lógica actual.
             */
            quitarFotoCommandOriginal =
                base.QuitarFotoCommand;

            cancelarCommandOriginal =
                base.CancelCommand;

            SeleccionarFotosCommand =
                new Command(
                    async () =>
                        await SeleccionarFotosSegurasAsync(),
                    PuedeProcesarFotos);

            QuitarFotoCommand =
                new Command<FotoTerrenoItem>(
                    async foto =>
                        await QuitarFotoSeguraAsync(foto),
                    foto =>
                        foto != null &&
                        PuedeProcesarFotos());

            CancelCommand =
                new Command(
                    async () =>
                        await CancelarFormularioSeguroAsync(),
                    PuedeCancelarFormulario);

            /*
             * Los comandos ocultos deben refrescarse también cuando el
             * ViewModel original cambia IsBusy.
             */
            PropertyChanged += (_, e) =>
            {
                if (string.Equals(
                        e.PropertyName,
                        nameof(IsBusy),
                        StringComparison.Ordinal) ||
                    string.Equals(
                        e.PropertyName,
                        nameof(AllowEdit),
                        StringComparison.Ordinal))
                {
                    SeleccionarFotosCommand.ChangeCanExecute();
                    QuitarFotoCommand.ChangeCanExecute();
                    CancelCommand.ChangeCanExecute();
                }
            };
        }

        private bool PuedeProcesarFotos() =>
            !procesandoFotos &&
            !IsBusy &&
            AllowEdit;

        private bool PuedeCancelarFormulario() =>
            !procesandoFotos &&
            !IsBusy;

        private void CambiarEstadoProcesamientoFotos(
            bool valor)
        {
            if (procesandoFotos == valor)
                return;

            procesandoFotos = valor;

            SeleccionarFotosCommand
                .ChangeCanExecute();

            QuitarFotoCommand
                .ChangeCanExecute();

            CancelCommand
                .ChangeCanExecute();
        }

        /// <summary>
        /// Si no hay fotografías nuevas, conserva exactamente el flujo
        /// original de cancelación del formulario.
        ///
        /// Si existen fotografías pendientes, solicita confirmación, las
        /// libera y navega al listado sin ejecutar ningún endpoint de fotos.
        /// </summary>
        private async Task CancelarFormularioSeguroAsync()
        {
            if (!PuedeCancelarFormulario())
                return;

            List<FotoTerrenoItem> fotosPendientes =
                FotosTerreno
                    .Where(foto => foto.EsNueva)
                    .ToList();

            /*
             * Sin fotos pendientes se delega al comando original para conservar
             * la detección existente de cambios en los demás campos.
             */
            if (fotosPendientes.Count == 0)
            {
                if (cancelarCommandOriginal.CanExecute(null))
                {
                    cancelarCommandOriginal.Execute(null);
                }

                return;
            }

            bool confirmar =
                await AppNotificationService
                    .ConfirmDiscardChangesAsync();

            if (!confirmar)
                return;

            if (!await fotosOperacionLock.WaitAsync(0))
                return;

            CambiarEstadoProcesamientoFotos(true);

            try
            {
                /*
                 * No se llama a SubirFotosPendientesAsync ni a ningún servicio.
                 * Solamente se cancelan operaciones locales y se liberan los
                 * recursos pendientes.
                 */
                CancelarOperaciones();

                await LiberarFotosPendientesAsync(
                    fotosPendientes);

                await GoToAsyncParameters(
                    AppRoutes.Terrenos);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    "No fue posible cancelar el formulario de terreno: " +
                    ex);

                await AppNotificationService.ShowErrorAsync(
                    "No fue posible cancelar el formulario.");
            }
            finally
            {
                CambiarEstadoProcesamientoFotos(false);
                fotosOperacionLock.Release();
            }
        }

        /// <summary>
        /// Retira las fotografías nuevas de la interfaz, desconecta su
        /// ImageSource y elimina sus archivos temporales de forma diferida.
        /// </summary>
        private async Task LiberarFotosPendientesAsync(
            IReadOnlyCollection<FotoTerrenoItem> fotosPendientes)
        {
            if (fotosPendientes.Count == 0)
                return;

            List<string> rutasTemporales =
                fotosPendientes
                    .Select(foto => foto.LocalPath)
                    .Where(ruta =>
                        !string.IsNullOrWhiteSpace(ruta))
                    .Select(ruta => ruta!)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

            /*
             * Source=null se notifica mediante FotoTerrenoItem para que WinUI
             * y Android dejen de decodificar los archivos antes de retirarlos.
             */
            foreach (FotoTerrenoItem foto in fotosPendientes)
            {
                foto.Imagen = null;
            }

            await Task.Yield();

            await MainThread.InvokeOnMainThreadAsync(
                () =>
                {
                    foreach (FotoTerrenoItem foto in fotosPendientes)
                    {
                        FotosTerreno.Remove(foto);
                    }
                });

            foreach (string ruta in rutasTemporales)
            {
                _ = EliminarArchivoTemporalDiferidoAsync(
                    ruta,
                    EsperaLiberacionImagenMs);
            }
        }

        private async Task SeleccionarFotosSegurasAsync()
        {
            if (!PuedeProcesarFotos())
                return;

            /*
             * WaitAsync(0) evita que dos clics rápidos abran dos selectores
             * o que una selección comience mientras se libera otra imagen.
             */
            if (!await fotosOperacionLock.WaitAsync(0))
                return;

            CambiarEstadoProcesamientoFotos(true);

            try
            {
                var opciones = new PickOptions
                {
                    PickerTitle =
                        "Seleccione fotos del terreno",
                    FileTypes =
                        FilePickerFileType.Images
                };

                IEnumerable<FileResult>? archivos =
                    await FilePicker.PickMultipleAsync(
                        opciones);

                if (archivos == null)
                    return;

                foreach (FileResult archivo in archivos)
                {
                    await AgregarFotoTemporalAsync(
                        archivo);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    "No fue posible seleccionar las fotografías: " +
                    ex);

                await AppNotificationService.ShowErrorAsync(
                    "No fue posible seleccionar las fotografías.");
            }
            finally
            {
                CambiarEstadoProcesamientoFotos(false);
                fotosOperacionLock.Release();
            }
        }

        private async Task AgregarFotoTemporalAsync(
            FileResult archivo)
        {
            string? rutaTemporal = null;

            try
            {
                string extension =
                    Path.GetExtension(
                        archivo.FileName);

                if (string.IsNullOrWhiteSpace(extension))
                    extension = ".jpg";

                rutaTemporal =
                    Path.Combine(
                        FileSystem.CacheDirectory,
                        $"{Guid.NewGuid():N}{extension}");

                /*
                 * El archivo se copia de forma asincrónica. No se utiliza
                 * File.Copy ni se procesa la imagen de manera sincrónica
                 * sobre el hilo visual.
                 */
                await using Stream origen =
                    await archivo.OpenReadAsync();

                await using var destino =
                    new FileStream(
                        rutaTemporal,
                        FileMode.CreateNew,
                        FileAccess.Write,
                        FileShare.Read,
                        bufferSize: 81920,
                        options:
                            FileOptions.Asynchronous |
                            FileOptions.SequentialScan);

                await origen.CopyToAsync(
                    destino);

                await destino.FlushAsync();

                var foto =
                    new FotoTerrenoItem
                    {
                        FotoTerrenoId = null,
                        TerrenoId =
                            Terreno?.TerrenoId,
                        UrlFotoTerreno = null,
                        LocalPath =
                            rutaTemporal,
                        NombreArchivo =
                            archivo.FileName,
                        EsNueva = true,
                        Imagen =
                            ImageSource.FromFile(
                                rutaTemporal)
                    };

                /*
                 * ObservableCollection debe actualizarse en el hilo principal.
                 * MainThread evita problemas si una plataforma reanuda el
                 * FilePicker en un hilo diferente.
                 */
                await MainThread.InvokeOnMainThreadAsync(
                    () =>
                    {
                        FotosTerreno.Add(
                            foto);
                    });
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    $"No fue posible preparar la foto {archivo.FileName}: {ex}");

                if (!string.IsNullOrWhiteSpace(rutaTemporal))
                {
                    _ = EliminarArchivoTemporalDiferidoAsync(
                        rutaTemporal,
                        esperaInicialMs: 0);
                }

                await AppNotificationService.ShowErrorAsync(
                    $"No fue posible agregar la fotografía {archivo.FileName}.");
            }
        }

        private async Task QuitarFotoSeguraAsync(
            FotoTerrenoItem? foto)
        {
            if (foto == null ||
                !AllowEdit ||
                IsBusy)
            {
                return;
            }

            /*
             * Las fotografías existentes continúan usando el flujo original:
             * confirmación, llamada a la API y mensaje de resultado.
             */
            if (!foto.EsNueva &&
                foto.FotoTerrenoId is > 0)
            {
                if (quitarFotoCommandOriginal
                    .CanExecute(foto))
                {
                    quitarFotoCommandOriginal
                        .Execute(foto);
                }

                return;
            }

            if (!await fotosOperacionLock.WaitAsync(0))
                return;

            CambiarEstadoProcesamientoFotos(true);

            string? rutaTemporal =
                foto.LocalPath;

            try
            {
                /*
                 * Primero se desconecta el ImageSource. FotoTerrenoItem
                 * notifica el cambio para que WinUI deje de decodificar
                 * el archivo antes de eliminar la tarjeta.
                 */
                foto.Imagen = null;

                /*
                 * Se entrega un ciclo al hilo visual para que el control Image
                 * procese Source=null antes de modificar la colección.
                 */
                await Task.Yield();

                await MainThread.InvokeOnMainThreadAsync(
                    () =>
                    {
                        FotosTerreno.Remove(
                            foto);
                    });

                /*
                 * Nunca se usa File.Delete directamente en el hilo visual.
                 * Windows puede conservar temporalmente el archivo mientras
                 * termina de liberar BitmapImage/RandomAccessStream.
                 */
                _ = EliminarArchivoTemporalDiferidoAsync(
                    rutaTemporal,
                    EsperaLiberacionImagenMs);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    "No fue posible quitar la fotografía temporal: " +
                    ex);

                await AppNotificationService.ShowErrorAsync(
                    "No fue posible quitar la fotografía.");
            }
            finally
            {
                CambiarEstadoProcesamientoFotos(false);
                fotosOperacionLock.Release();
            }
        }

        private static async Task
            EliminarArchivoTemporalDiferidoAsync(
                string? ruta,
                int esperaInicialMs)
        {
            if (string.IsNullOrWhiteSpace(ruta))
                return;

            try
            {
                if (esperaInicialMs > 0)
                {
                    await Task.Delay(
                        esperaInicialMs)
                        .ConfigureAwait(false);
                }

                for (int intento = 1;
                     intento <= IntentosEliminarTemporal;
                     intento++)
                {
                    try
                    {
                        if (!File.Exists(ruta))
                            return;

                        /*
                         * File.Delete se ejecuta en el grupo de hilos y nunca
                         * puede detener la interfaz de Windows o Android.
                         */
                        await Task.Run(
                            () => File.Delete(ruta))
                            .ConfigureAwait(false);

                        return;
                    }
                    catch (IOException ex)
                    {
                        Debug.WriteLine(
                            $"La fotografía temporal sigue en uso. " +
                            $"Intento {intento}: {ex.Message}");
                    }
                    catch (UnauthorizedAccessException ex)
                    {
                        Debug.WriteLine(
                            $"La fotografía temporal todavía no puede " +
                            $"eliminarse. Intento {intento}: {ex.Message}");
                    }

                    if (intento <
                        IntentosEliminarTemporal)
                    {
                        await Task.Delay(
                            EsperaEntreIntentosMs)
                            .ConfigureAwait(false);
                    }
                }
            }
            catch (Exception ex)
            {
                /*
                 * La limpieza del caché nunca debe cerrar ni bloquear
                 * el formulario.
                 */
                Debug.WriteLine(
                    "No fue posible limpiar la fotografía temporal: " +
                    ex);
            }
        }
    }
}
