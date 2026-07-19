using CommunityToolkit.Maui.Storage;
using CONATRADEC.Models;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Storage;
using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace CONATRADEC.Services
{
    public sealed class AnalisisReporteService
    {
        private const string EndpointReporte = "api/reportes/analisis";

        private readonly HttpClient httpClient;

        public AnalisisReporteService()
            : this(ApiClientService.Client)
        {
        }

        public AnalisisReporteService(HttpClient httpClient)
        {
            this.httpClient = httpClient
                ?? throw new ArgumentNullException(nameof(httpClient));
        }

        public async Task<AnalisisReporteArchivoResult> GuardarPdfAsync(
            int analisisSueloCalculoId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                ReportePdfDescarga descarga = await DescargarPdfAsync(
                    analisisSueloCalculoId,
                    cancellationToken);

                return await GuardarAsync(
                    descarga.NombreArchivo,
                    descarga.Contenido,
                    cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return AnalisisReporteArchivoResult.Cancelado();
            }
            catch (Exception ex)
            {
                return AnalisisReporteArchivoResult.Error(
                    $"No fue posible descargar el PDF: {ex.Message}");
            }
        }

        public async Task<AnalisisReporteArchivoResult> GuardarExcelAsync(
            AnalisisReporte reporte,
            CancellationToken cancellationToken = default)
        {
            try
            {
                byte[] contenido = await Task.Run(
                    () => AnalisisReporteExcel.Generar(reporte),
                    cancellationToken);

                return await GuardarAsync(
                    $"{reporte.NombreArchivoBase}.xlsx",
                    contenido,
                    cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return AnalisisReporteArchivoResult.Cancelado();
            }
            catch (Exception ex)
            {
                return AnalisisReporteArchivoResult.Error(
                    $"No fue posible generar el Excel: {ex.Message}");
            }
        }

        public async Task<AnalisisReporteArchivoResult> AbrirPdfParaImprimirAsync(
            int analisisSueloCalculoId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                ReportePdfDescarga descarga = await DescargarPdfAsync(
                    analisisSueloCalculoId,
                    cancellationToken);

                string ruta = Path.Combine(
                    FileSystem.CacheDirectory,
                    descarga.NombreArchivo);

                await File.WriteAllBytesAsync(
                    ruta,
                    descarga.Contenido,
                    cancellationToken);

                bool abierto = await Launcher.Default.OpenAsync(
                    new OpenFileRequest(
                        "Reporte de análisis de suelo",
                        new ReadOnlyFile(ruta)));

                return abierto
                    ? AnalisisReporteArchivoResult.Exito(
                        ruta,
                        "El PDF se abrió en el visor del dispositivo. Desde allí puede imprimirlo o compartirlo.")
                    : AnalisisReporteArchivoResult.Error(
                        "No se encontró una aplicación para abrir archivos PDF.");
            }
            catch (OperationCanceledException)
            {
                return AnalisisReporteArchivoResult.Cancelado();
            }
            catch (Exception ex)
            {
                return AnalisisReporteArchivoResult.Error(
                    $"No fue posible abrir el PDF: {ex.Message}");
            }
        }

        private async Task<ReportePdfDescarga> DescargarPdfAsync(
            int analisisSueloCalculoId,
            CancellationToken cancellationToken)
        {
            if (analisisSueloCalculoId <= 0)
            {
                throw new InvalidOperationException(
                    "El identificador del cálculo no es válido.");
            }

            using HttpResponseMessage response = await httpClient.GetAsync(
                $"{EndpointReporte}/{analisisSueloCalculoId}/pdf",
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                string contenidoError = await response.Content
                    .ReadAsStringAsync(cancellationToken);

                throw new InvalidOperationException(
                    ExtraerMensajeError(
                        contenidoError,
                        $"El servidor respondió con el código HTTP {(int)response.StatusCode}."));
            }

            byte[] contenido = await response.Content.ReadAsByteArrayAsync(
                cancellationToken);

            if (contenido.Length == 0)
                throw new InvalidOperationException("El servidor devolvió un PDF vacío.");

            string? nombreEncabezado =
                response.Content.Headers.ContentDisposition?.FileNameStar ??
                response.Content.Headers.ContentDisposition?.FileName;

            string nombreArchivo = LimpiarNombreArchivo(
                nombreEncabezado,
                analisisSueloCalculoId);

            return new ReportePdfDescarga(nombreArchivo, contenido);
        }

        private static string ExtraerMensajeError(
            string contenido,
            string mensajeAlternativo)
        {
            if (string.IsNullOrWhiteSpace(contenido))
                return mensajeAlternativo;

            try
            {
                using JsonDocument documento = JsonDocument.Parse(contenido);

                if (documento.RootElement.TryGetProperty(
                        "message",
                        out JsonElement mensaje) &&
                    mensaje.ValueKind == JsonValueKind.String)
                {
                    return mensaje.GetString() ?? mensajeAlternativo;
                }
            }
            catch (JsonException)
            {
                // El servidor puede devolver texto plano en errores de infraestructura.
            }

            return mensajeAlternativo;
        }

        private static string LimpiarNombreArchivo(
            string? nombreEncabezado,
            int analisisSueloCalculoId)
        {
            string nombre = string.IsNullOrWhiteSpace(nombreEncabezado)
                ? $"Reporte_Analisis_{analisisSueloCalculoId}.pdf"
                : nombreEncabezado.Trim().Trim('"');

            nombre = Path.GetFileName(nombre);

            foreach (char caracter in Path.GetInvalidFileNameChars())
                nombre = nombre.Replace(caracter, '_');

            if (!nombre.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                nombre += ".pdf";

            return nombre;
        }

        private static async Task<AnalisisReporteArchivoResult> GuardarAsync(
            string nombreArchivo,
            byte[] contenido,
            CancellationToken cancellationToken)
        {
            using MemoryStream stream = new(contenido, writable: false);

            FileSaverResult resultado = await FileSaver.Default.SaveAsync(
                nombreArchivo,
                stream,
                cancellationToken);

            if (resultado.IsSuccessful)
            {
                return AnalisisReporteArchivoResult.Exito(
                    resultado.FilePath,
                    $"El archivo {nombreArchivo} se guardó correctamente.");
            }

            if (EsCancelacionSelector(resultado.Exception))
                return AnalisisReporteArchivoResult.Cancelado();

            return AnalisisReporteArchivoResult.Error(
                resultado.Exception?.Message ??
                "No fue posible guardar el archivo seleccionado.");
        }

        private static bool EsCancelacionSelector(Exception? exception)
        {
            if (exception is OperationCanceledException)
                return true;

            string mensaje = exception?.Message ?? string.Empty;

            return mensaje.Contains(
                       "cancel",
                       StringComparison.OrdinalIgnoreCase) ||
                   mensaje.Contains(
                       "Path doesn't exist",
                       StringComparison.OrdinalIgnoreCase) ||
                   mensaje.Contains(
                       "Path is not selected",
                       StringComparison.OrdinalIgnoreCase);
        }

        private sealed record ReportePdfDescarga(
            string NombreArchivo,
            byte[] Contenido);
    }

    public sealed class AnalisisReporteArchivoResult
    {
        public bool Success { get; private set; }

        public bool FueCancelado { get; private set; }

        public string FilePath { get; private set; } = string.Empty;

        public string Message { get; private set; } = string.Empty;

        public static AnalisisReporteArchivoResult Exito(
            string? filePath,
            string message) =>
            new()
            {
                Success = true,
                FilePath = filePath ?? string.Empty,
                Message = message
            };

        public static AnalisisReporteArchivoResult Error(string message) =>
            new()
            {
                Success = false,
                Message = message
            };

        public static AnalisisReporteArchivoResult Cancelado() =>
            new()
            {
                Success = false,
                FueCancelado = true,
                Message = "La operación fue cancelada."
            };
    }
}
