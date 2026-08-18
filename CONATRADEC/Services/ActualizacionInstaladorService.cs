using CONATRADEC.Models;
using Microsoft.Maui.ApplicationModel;

#if ANDROID
using Android.Content;
using Android.OS;
using Android.Provider;
using AndroidFileProvider =
    AndroidX.Core.Content.FileProvider;
#endif

#if WINDOWS
using System.Diagnostics;
#endif

namespace CONATRADEC.Services
{
    /// <summary>
    /// Abre el instalador nativo. La confirmación final siempre la realiza el
    /// sistema operativo; la aplicación nunca instala en silencio.
    /// </summary>
    public static class ActualizacionInstaladorService
    {
        public static async Task<ResultadoInstalacionActualizacion>
            IniciarInstalacionAsync(
                string rutaArchivo)
        {
            if (string.IsNullOrWhiteSpace(
                    rutaArchivo) ||
                !File.Exists(
                    rutaArchivo))
            {
                return new ResultadoInstalacionActualizacion(
                    false,
                    false,
                    "El archivo de actualización no existe.");
            }

#if ANDROID
            Context contexto =
                Microsoft.Maui.ApplicationModel
                    .Platform.AppContext;

            if (Build.VERSION.SdkInt >=
                    BuildVersionCodes.O &&
                contexto.PackageManager?
                    .CanRequestPackageInstalls() != true)
            {
                var configuracion =
                    new Intent(
                        Settings
                            .ActionManageUnknownAppSources,
                        Android.Net.Uri.Parse(
                            $"package:{contexto.PackageName}"));

                configuracion.AddFlags(
                    ActivityFlags.NewTask);

                contexto.StartActivity(
                    configuracion);

                return new ResultadoInstalacionActualizacion(
                    false,
                    true,
                    "Autorice a ConatraCafé Soil para instalar " +
                    "actualizaciones y luego presione Continuar " +
                    "instalación.");
            }

            var archivoJava =
                new Java.IO.File(
                    rutaArchivo);

            Android.Net.Uri uri =
                AndroidFileProvider.GetUriForFile(
                    contexto,
                    $"{contexto.PackageName}.fileprovider",
                    archivoJava);

            var intent =
                new Intent(
                    Intent.ActionView);

            intent.SetDataAndType(
                uri,
                "application/vnd.android.package-archive");

            intent.AddFlags(
                ActivityFlags.NewTask);

            intent.AddFlags(
                ActivityFlags.GrantReadUriPermission);

            intent.ClipData =
                ClipData.NewRawUri(
                    "Actualización ConatraCafé Soil",
                    uri);

            contexto.StartActivity(
                intent);

            return new ResultadoInstalacionActualizacion(
                true,
                false,
                "Android abrió el instalador de la actualización.");

#elif WINDOWS
            string extension =
                Path.GetExtension(rutaArchivo)
                    .ToLowerInvariant();

            /*
             * CONATRADEC para Windows se distribuye actualmente como aplicación
             * desempaquetada mediante instalador EXE (Inno Setup). Launcher es
             * apropiado para archivos asociados, pero para un EXE debemos pedir
             * explícitamente al Shell de Windows que ejecute el instalador.
             */
            if (extension == ".exe")
            {
                Process? proceso = Process.Start(
                    new ProcessStartInfo
                    {
                        FileName = rutaArchivo,
                        WorkingDirectory =
                            Path.GetDirectoryName(rutaArchivo) ??
                            AppContext.BaseDirectory,
                        UseShellExecute = true
                    });

                return proceso is not null
                    ? new ResultadoInstalacionActualizacion(
                        true,
                        false,
                        "Windows abrió el instalador de la actualización.")
                    : new ResultadoInstalacionActualizacion(
                        false,
                        false,
                        "Windows no pudo iniciar el instalador de la actualización.");
            }

            bool abierto =
                await Launcher.Default.OpenAsync(
                    new OpenFileRequest(
                        "Instalar actualización de " +
                        "ConatraCafé Soil",
                        new ReadOnlyFile(
                            rutaArchivo)));

            return abierto
                ? new ResultadoInstalacionActualizacion(
                    true,
                    false,
                    "Windows abrió el paquete de actualización.")
                : new ResultadoInstalacionActualizacion(
                    false,
                    false,
                    "Windows no pudo abrir el paquete de actualización.");

#else
            await Task.CompletedTask;

            return new ResultadoInstalacionActualizacion(
                false,
                false,
                "La actualización interna está habilitada " +
                "únicamente para Android y Windows.");
#endif
        }
    }
}
