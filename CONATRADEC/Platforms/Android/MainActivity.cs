using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Provider;
using Android.Views;
using Android.Webkit;
using Android.Widget;
using AndroidX.Core.View;
using CONATRADEC.Services;
using Microsoft.Maui;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Storage;
using Plugin.Fingerprint;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

using AndroidBackCallback =
    AndroidX.Activity.OnBackPressedCallback;
using AndroidColor = Android.Graphics.Color;
using AndroidRect = Android.Graphics.Rect;
using AndroidUri = Android.Net.Uri;

namespace CONATRADEC
{
    [Activity(
        Theme = "@style/Maui.SplashTheme",
        MainLauncher = true,
        LaunchMode = LaunchMode.SingleTop,
        ConfigurationChanges =
            ConfigChanges.ScreenSize |
            ConfigChanges.Orientation |
            ConfigChanges.UiMode |
            ConfigChanges.ScreenLayout |
            ConfigChanges.SmallestScreenSize |
            ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        private const int SolicitudGaleriaIndividual = 7301;
        private const int SolicitudGaleriaMultiple = 7302;

        private AndroidBackCallback?
            bloquearRetrocesoCallback;

        private TaskCompletionSource<IReadOnlyList<FileResult>>?
            seleccionGaleriaPendiente;

        protected override void OnCreate(
            Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            bloquearRetrocesoCallback =
                new BloquearRetrocesoCallback();

            OnBackPressedDispatcher.AddCallback(
                bloquearRetrocesoCallback);

            Window?.SetStatusBarColor(
                AndroidColor.ParseColor("#3B655B"));

            if (Build.VERSION.SdkInt >=
                    BuildVersionCodes.M &&
                Window != null)
            {
                var insets =
                    WindowCompat.GetInsetsController(
                        Window,
                        Window.DecorView);

                if (insets is not null)
                {
                    insets.AppearanceLightStatusBars =
                        false;
                }
            }

            CrossFingerprint
                .SetCurrentActivityResolver(
                    () => this);
        }

        protected override void OnResume()
        {
            base.OnResume();

            CrossFingerprint
                .SetCurrentActivityResolver(
                    () => this);
        }

        protected override void OnDestroy()
        {
            bloquearRetrocesoCallback?.Remove();
            bloquearRetrocesoCallback?.Dispose();
            bloquearRetrocesoCallback = null;

            if (seleccionGaleriaPendiente != null)
            {
                seleccionGaleriaPendiente.TrySetResult(
                    Array.Empty<FileResult>());

                seleccionGaleriaPendiente = null;
            }

            base.OnDestroy();
        }

        /// <summary>
        /// Abre el selector fotográfico de Android.
        ///
        /// Android 13 o superior utiliza el Photo Picker del sistema.
        /// En versiones anteriores utiliza la galería/selector de imágenes
        /// asociado a MediaStore.
        ///
        /// Las imágenes seleccionadas se copian al caché privado de la app para
        /// devolver FileResult con una ruta física estable, conservando la lógica
        /// que actualmente utilizan los ViewModels de CONATRADEC.
        /// </summary>
        public Task<IReadOnlyList<FileResult>>
            SeleccionarFotosDesdeGaleriaAsync(
                bool seleccionMultiple)
        {
            if (seleccionGaleriaPendiente != null)
            {
                throw new InvalidOperationException(
                    "Ya existe una selección de fotografías en curso.");
            }

            var completion =
                new TaskCompletionSource<IReadOnlyList<FileResult>>(
                    TaskCreationOptions.RunContinuationsAsynchronously);

            seleccionGaleriaPendiente = completion;

            try
            {
                Intent intent =
                    CrearIntentGaleria(seleccionMultiple);

                int codigoSolicitud =
                    seleccionMultiple
                        ? SolicitudGaleriaMultiple
                        : SolicitudGaleriaIndividual;

                StartActivityForResult(
                    intent,
                    codigoSolicitud);
            }
            catch (ActivityNotFoundException ex)
            {
                seleccionGaleriaPendiente = null;

                completion.TrySetException(
                    new FeatureNotSupportedException(
                        "El dispositivo no dispone de un selector de fotografías compatible.",
                        ex));
            }
            catch (Exception ex)
            {
                seleccionGaleriaPendiente = null;
                completion.TrySetException(ex);
            }

            return completion.Task;
        }

        /// <summary>
        /// Android 13+ dispone de Photo Picker, que ofrece una interfaz visual
        /// dedicada a fotos y no requiere acceso general a la biblioteca.
        ///
        /// En Android anteriores se usa ACTION_PICK sobre MediaStore para abrir
        /// la experiencia de galería disponible en el dispositivo.
        /// </summary>
        private static Intent CrearIntentGaleria(
            bool seleccionMultiple)
        {
            if (Build.VERSION.SdkInt >=
                BuildVersionCodes.Tiramisu)
            {
                var intent =
                    new Intent(MediaStore.ActionPickImages);

                intent.SetType("image/*");
                intent.AddFlags(
                    ActivityFlags.GrantReadUriPermission);

                if (seleccionMultiple)
                {
                    int limite =
                        Math.Max(
                            2,
                            MediaStore.PickImagesMaxLimit);

                    intent.PutExtra(
                        MediaStore.ExtraPickImagesMax,
                        limite);
                }

                return intent;
            }

            AndroidUri? galeriaUri =
                MediaStore.Images.Media.ExternalContentUri;

            var intentAnterior =
                new Intent(
                    Intent.ActionPick,
                    galeriaUri);

            intentAnterior.SetType("image/*");
            intentAnterior.AddFlags(
                ActivityFlags.GrantReadUriPermission);

            if (seleccionMultiple)
            {
                intentAnterior.PutExtra(
                    Intent.ExtraAllowMultiple,
                    true);
            }

            return intentAnterior;
        }

        protected override async void OnActivityResult(
            int requestCode,
            Result resultCode,
            Intent? data)
        {
            base.OnActivityResult(
                requestCode,
                resultCode,
                data);

            if (requestCode != SolicitudGaleriaIndividual &&
                requestCode != SolicitudGaleriaMultiple)
            {
                return;
            }

            TaskCompletionSource<IReadOnlyList<FileResult>>?
                completion = seleccionGaleriaPendiente;

            seleccionGaleriaPendiente = null;

            if (completion == null)
                return;

            if (resultCode != Result.Ok ||
                data == null)
            {
                completion.TrySetResult(
                    Array.Empty<FileResult>());

                return;
            }

            try
            {
                IReadOnlyList<AndroidUri> uris =
                    ObtenerUrisSeleccionadas(data);

                if (uris.Count == 0)
                {
                    completion.TrySetResult(
                        Array.Empty<FileResult>());

                    return;
                }

                var archivos =
                    new List<FileResult>(uris.Count);

                foreach (AndroidUri uri in uris)
                {
                    FileResult archivo =
                        await CopiarSeleccionACacheAsync(uri);

                    archivos.Add(archivo);
                }

                completion.TrySetResult(archivos);
            }
            catch (Exception ex)
            {
                completion.TrySetException(ex);
            }
        }

        private static IReadOnlyList<AndroidUri>
            ObtenerUrisSeleccionadas(Intent data)
        {
            var resultado =
                new List<AndroidUri>();

            var existentes =
                new HashSet<string>(
                    StringComparer.Ordinal);

            if (data.ClipData != null)
            {
                for (int i = 0;
                     i < data.ClipData.ItemCount;
                     i++)
                {
                    AndroidUri? uri =
                        data.ClipData
                            .GetItemAt(i)?
                            .Uri;

                    AgregarUriSiEsValida(
                        resultado,
                        existentes,
                        uri);
                }
            }

            AgregarUriSiEsValida(
                resultado,
                existentes,
                data.Data);

            return resultado;
        }

        private static void AgregarUriSiEsValida(
            ICollection<AndroidUri> resultado,
            ISet<string> existentes,
            AndroidUri? uri)
        {
            if (uri == null)
                return;

            string clave =
                uri.ToString() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(clave) ||
                !existentes.Add(clave))
            {
                return;
            }

            resultado.Add(uri);
        }

        /// <summary>
        /// Convierte el content:// entregado por Android en un archivo temporal
        /// privado. De esta manera el resto del proyecto puede continuar usando
        /// FileResult.FullPath y FileResult.OpenReadAsync sin cambios.
        /// </summary>
        private async Task<FileResult>
            CopiarSeleccionACacheAsync(
                AndroidUri uri)
        {
            string tipoContenido =
                ContentResolver.GetType(uri)
                ?? "image/jpeg";

            string nombreArchivo =
                ObtenerNombreArchivo(
                    uri,
                    tipoContenido);

            string carpeta =
                Path.Combine(
                    FileSystem.CacheDirectory,
                    "galeria_seleccion",
                    Guid.NewGuid().ToString("N"));

            Directory.CreateDirectory(carpeta);

            string rutaDestino =
                Path.Combine(
                    carpeta,
                    nombreArchivo);

            await using Stream entrada =
                ContentResolver.OpenInputStream(uri)
                ?? throw new IOException(
                    "Android no pudo abrir la fotografía seleccionada.");

            await using var salida =
                new FileStream(
                    rutaDestino,
                    FileMode.Create,
                    FileAccess.Write,
                    FileShare.None,
                    bufferSize: 81920,
                    useAsync: true);

            await entrada.CopyToAsync(salida);

            return new FileResult(
                rutaDestino,
                tipoContenido);
        }

        private string ObtenerNombreArchivo(
            AndroidUri uri,
            string tipoContenido)
        {
            string? nombre = null;

            try
            {
                string[] columnas =
                [
                    IOpenableColumns.DisplayName
                ];

                using Android.Database.ICursor? cursor =
                    ContentResolver.Query(
                        uri,
                        columnas,
                        null,
                        null,
                        null);

                if (cursor != null &&
                    cursor.MoveToFirst())
                {
                    int indice =
                        cursor.GetColumnIndex(
                            IOpenableColumns.DisplayName);

                    if (indice >= 0)
                    {
                        nombre =
                            cursor.GetString(indice);
                    }
                }
            }
            catch
            {
                // Algunos proveedores multimedia no permiten consultar el
                // nombre. En ese caso se genera uno seguro más abajo.
            }

            if (string.IsNullOrWhiteSpace(nombre))
            {
                nombre =
                    uri.LastPathSegment;
            }

            if (string.IsNullOrWhiteSpace(nombre))
            {
                nombre = "fotografia";
            }

            nombre =
                LimpiarNombreArchivo(nombre);

            if (string.IsNullOrWhiteSpace(
                    Path.GetExtension(nombre)))
            {
                string? extension =
                    MimeTypeMap.Singleton?
                        .GetExtensionFromMimeType(
                            tipoContenido);

                if (!string.IsNullOrWhiteSpace(extension))
                {
                    nombre += "." + extension;
                }
                else
                {
                    nombre += ".jpg";
                }
            }

            return nombre;
        }

        private static string LimpiarNombreArchivo(
            string nombre)
        {
            char[] invalidos =
                Path.GetInvalidFileNameChars();

            foreach (char invalido in invalidos)
            {
                nombre =
                    nombre.Replace(
                        invalido,
                        '_');
            }

            nombre =
                nombre.Replace('/', '_')
                      .Replace('\\', '_')
                      .Trim();

            return string.IsNullOrWhiteSpace(nombre)
                ? "fotografia.jpg"
                : nombre;
        }

        /// <summary>
        /// Registra toques reales y conserva el comportamiento global que oculta
        /// el teclado al tocar fuera de un Entry o Editor.
        /// </summary>
        public override bool DispatchTouchEvent(
            MotionEvent? motionEvent)
        {
            if (motionEvent?.Action ==
                MotionEventActions.Down)
            {
                SesionInactividadService.Instance
                    .RegistrarActividad();
            }

            if (motionEvent?.Action ==
                    MotionEventActions.Down &&
                CurrentFocus is EditText focusedInput)
            {
                var inputBounds =
                    new AndroidRect();

                focusedInput.GetGlobalVisibleRect(
                    inputBounds);

                bool touchedOutsideInput =
                    !inputBounds.Contains(
                        (int)motionEvent.RawX,
                        (int)motionEvent.RawY);

                if (touchedOutsideInput)
                {
                    KeyboardService.HideImmediately();
                }
            }

            return base.DispatchTouchEvent(
                motionEvent);
        }

        public override bool DispatchKeyEvent(
            KeyEvent? keyEvent)
        {
            if (keyEvent?.Action ==
                KeyEventActions.Down)
            {
                SesionInactividadService.Instance
                    .RegistrarActividad();
            }

            return base.DispatchKeyEvent(
                keyEvent);
        }

        private sealed class BloquearRetrocesoCallback
            : AndroidBackCallback
        {
            public BloquearRetrocesoCallback()
                : base(true)
            {
            }

            public override void HandleOnBackPressed()
            {
                /*
                 * Intencionalmente vacío.
                 * La navegación se realiza mediante los botones de la app.
                 */
            }
        }
    }
}
