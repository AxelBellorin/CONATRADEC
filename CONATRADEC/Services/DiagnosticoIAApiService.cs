using CONATRADEC.Models;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace CONATRADEC.Services
{
    public sealed class DiagnosticoIAApiService
    {
        private static readonly Lazy<DiagnosticoIAApiService> lazy =
            new(() => new DiagnosticoIAApiService());

        private static readonly JsonSerializerOptions JsonOptions =
            new(JsonSerializerDefaults.Web)
            {
                PropertyNameCaseInsensitive = true
            };

        public static DiagnosticoIAApiService Instance =>
            lazy.Value;

        private DiagnosticoIAApiService()
        {
        }

        public async Task<DiagnosticoIAItem> AnalizarAsync(
            IReadOnlyCollection<FotoDiagnosticoSeleccionada> fotos,
            string? codigoTerreno,
            string? observacion,
            CancellationToken cancellationToken = default)
        {
            using var contenido = new MultipartFormDataContent();

            if (!string.IsNullOrWhiteSpace(codigoTerreno))
            {
                contenido.Add(
                    new StringContent(
                        codigoTerreno.Trim(),
                        Encoding.UTF8),
                    "CodigoTerreno");
            }

            if (!string.IsNullOrWhiteSpace(observacion))
            {
                contenido.Add(
                    new StringContent(
                        observacion.Trim(),
                        Encoding.UTF8),
                    "Observacion");
            }

            foreach (FotoDiagnosticoSeleccionada foto in fotos)
            {
                var stream = new FileStream(
                    foto.RutaLocal,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read,
                    bufferSize: 81920,
                    useAsync: true);

                var archivo = new StreamContent(stream);
                archivo.Headers.ContentType =
                    new MediaTypeHeaderValue(
                        string.IsNullOrWhiteSpace(foto.TipoContenido)
                            ? "image/jpeg"
                            : foto.TipoContenido);

                contenido.Add(
                    archivo,
                    "Fotos",
                    foto.NombreArchivo);
            }

            using HttpResponseMessage response =
                await ApiClientService.Client.PostAsync(
                    "api/diagnostico-ia/analizar",
                    contenido,
                    cancellationToken);

            string json = await response.Content
                .ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
                throw CrearExcepcion(response, json);

            DiagnosticoIADetalleRespuesta? resultado =
                JsonSerializer.Deserialize<
                    DiagnosticoIADetalleRespuesta>(
                        json,
                        JsonOptions);

            return resultado?.Data ??
                throw new InvalidOperationException(
                    "La API no devolvió el diagnóstico generado.");
        }

        public Task<DiagnosticoIAPaginaRespuesta>
            ObtenerMisDiagnosticosAsync(
                CancellationToken cancellationToken = default) =>
            ObtenerPaginaAsync(
                "api/diagnostico-ia/mis-diagnosticos?pagina=1&tamanoPagina=30",
                cancellationToken);

        public Task<DiagnosticoIAPaginaRespuesta>
            ObtenerPendientesAsync(
                CancellationToken cancellationToken = default) =>
            ObtenerPaginaAsync(
                "api/diagnostico-ia/pendientes?pagina=1&tamanoPagina=30",
                cancellationToken);

        public async Task<DiagnosticoIAItem>
            SolicitarSegundaRevisionAsync(
                int diagnosticoIAId,
                string retroalimentacionClasificador,
                string? diagnosticoPropuestoClasificador,
                CancellationToken cancellationToken = default)
        {
            var payload = new
            {
                retroalimentacionClasificador,
                diagnosticoPropuestoClasificador
            };

            using HttpResponseMessage response =
                await ApiClientService.Client.PostAsJsonAsync(
                    $"api/diagnostico-ia/{diagnosticoIAId}/segunda-revision",
                    payload,
                    JsonOptions,
                    cancellationToken);

            string json = await response.Content
                .ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
                throw CrearExcepcion(response, json);

            DiagnosticoIADetalleRespuesta? resultado =
                JsonSerializer.Deserialize<
                    DiagnosticoIADetalleRespuesta>(
                        json,
                        JsonOptions);

            return resultado?.Data ??
                throw new InvalidOperationException(
                    "La API no devolvió la segunda revisión generada.");
        }

        public async Task<DiagnosticoIAItem> ClasificarAsync(
            int diagnosticoIAId,
            string decision,
            string? diagnosticoFinal,
            string? observaciones,
            CancellationToken cancellationToken = default)
        {
            var payload = new
            {
                decision,
                diagnosticoFinal,
                observaciones
            };

            using HttpResponseMessage response =
                await ApiClientService.Client.PutAsJsonAsync(
                    $"api/diagnostico-ia/{diagnosticoIAId}/clasificar",
                    payload,
                    JsonOptions,
                    cancellationToken);

            string json = await response.Content
                .ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
                throw CrearExcepcion(response, json);

            DiagnosticoIADetalleRespuesta? resultado =
                JsonSerializer.Deserialize<
                    DiagnosticoIADetalleRespuesta>(
                        json,
                        JsonOptions);

            return resultado?.Data ??
                throw new InvalidOperationException(
                    "La API no devolvió el diagnóstico clasificado.");
        }

        private static async Task<DiagnosticoIAPaginaRespuesta>
            ObtenerPaginaAsync(
                string ruta,
                CancellationToken cancellationToken)
        {
            using HttpResponseMessage response =
                await ApiClientService.Client.GetAsync(
                    ruta,
                    cancellationToken);

            string json = await response.Content
                .ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
                throw CrearExcepcion(response, json);

            return JsonSerializer.Deserialize<
                       DiagnosticoIAPaginaRespuesta>(
                           json,
                           JsonOptions) ??
                new DiagnosticoIAPaginaRespuesta();
        }

        private static Exception CrearExcepcion(
            HttpResponseMessage response,
            string json)
        {
            string mensaje = ApiErrorMessageParser.Parse(
                response.StatusCode,
                json,
                ApiErrorMessageParser.GetDefaultMessage(
                    response.StatusCode,
                    "No fue posible completar la operación de diagnóstico."));

            string detalle = ExtraerDetalleTecnico(json);

            if (!string.IsNullOrWhiteSpace(detalle) &&
                !mensaje.Contains(
                    detalle,
                    StringComparison.OrdinalIgnoreCase))
            {
                mensaje =
                    $"{mensaje}{Environment.NewLine}{Environment.NewLine}" +
                    $"Detalle técnico: {detalle}";
            }

            return new DiagnosticoIAApiException(
                (int)response.StatusCode,
                mensaje);
        }

        private static string ExtraerDetalleTecnico(
            string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return string.Empty;

            try
            {
                using JsonDocument document =
                    JsonDocument.Parse(json);

                JsonElement root =
                    document.RootElement;

                if (root.TryGetProperty(
                        "detail",
                        out JsonElement detail) &&
                    detail.ValueKind ==
                        JsonValueKind.String)
                {
                    return detail.GetString()?.Trim() ??
                        string.Empty;
                }

                if (root.TryGetProperty(
                        "error",
                        out JsonElement error) &&
                    error.ValueKind ==
                        JsonValueKind.Object &&
                    error.TryGetProperty(
                        "message",
                        out JsonElement message) &&
                    message.ValueKind ==
                        JsonValueKind.String)
                {
                    return message.GetString()?.Trim() ??
                        string.Empty;
                }
            }
            catch (JsonException)
            {
            }

            return string.Empty;
        }
    }

    public sealed class DiagnosticoIAApiException : Exception
    {
        public DiagnosticoIAApiException(
            int statusCode,
            string message)
            : base(message)
        {
            StatusCode = statusCode;
        }

        public int StatusCode { get; }
    }
}
