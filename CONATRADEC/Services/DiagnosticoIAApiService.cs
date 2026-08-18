using CONATRADEC.Models;
using Microsoft.Maui.Storage;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace CONATRADEC.Services
{
    /// <summary>
    /// Cliente único del módulo. Utiliza ApiClientService para conservar JWT,
    /// bitácora y control de sesión. Durante llamadas largas mantiene activa la
    /// sesión local para evitar que Gemini y el cierre por inactividad compitan.
    /// </summary>
    public sealed class DiagnosticoIAApiService
    {
        private static readonly Lazy<DiagnosticoIAApiService> lazy =
            new(() => new DiagnosticoIAApiService());

        private static readonly JsonSerializerOptions JsonOptions =
            new(JsonSerializerDefaults.Web)
            {
                PropertyNameCaseInsensitive = true
            };

        private readonly HttpClient client;

        public static DiagnosticoIAApiService Instance => lazy.Value;

        private DiagnosticoIAApiService()
        {
            client = ApiClientService.Client;
        }

        public Task<DiagnosticoIACatalogos> ObtenerCatalogosAsync(
            CancellationToken cancellationToken = default) =>
            GetAsync<DiagnosticoIACatalogos>(
                "api/diagnostico-ia/catalogos",
                cancellationToken);

        public async Task<DiagnosticoIADetalle> AnalizarAsync(
            IReadOnlyCollection<FotoDiagnosticoSeleccionada> fotos,
            string? codigoTerreno,
            string? observacion,
            IProgress<DiagnosticoIAProcesamientoEstado>? progreso = null,
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
                archivo.Headers.ContentType = new MediaTypeHeaderValue(
                    string.IsNullOrWhiteSpace(foto.TipoContenido)
                        ? "image/jpeg"
                        : foto.TipoContenido);

                contenido.Add(
                    archivo,
                    "Fotos",
                    foto.NombreArchivo);

                contenido.Add(
                    new StringContent(
                        string.IsNullOrWhiteSpace(foto.TipoFotografia)
                            ? "EVIDENCIA"
                            : foto.TipoFotografia.Trim(),
                        Encoding.UTF8),
                    "TiposFotografia");
            }

            SesionInactividadService.Instance.RegistrarActividad();

            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                "api/diagnostico-ia-procesamiento/crear")
            {
                Content = contenido
            };

            using HttpResponseMessage response = await client.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            DiagnosticoIAProcesamientoEstado inicio =
                await LeerRespuestaAsync<DiagnosticoIAProcesamientoEstado>(
                    response,
                    cancellationToken);

            progreso?.Report(inicio);

            return await EsperarProcesamientoAsync(
                inicio.DiagnosticoIAId,
                progreso,
                cancellationToken);
        }

        public async Task<DiagnosticoIADetalle> ReintentarIAAsync(
            int diagnosticoId,
            IProgress<DiagnosticoIAProcesamientoEstado>? progreso = null,
            CancellationToken cancellationToken = default)
        {
            DiagnosticoIAProcesamientoEstado inicio =
                await PostSinCuerpoAsync<DiagnosticoIAProcesamientoEstado>(
                    $"api/diagnostico-ia-procesamiento/{diagnosticoId}/reintentar",
                    cancellationToken);

            progreso?.Report(inicio);

            return await EsperarProcesamientoAsync(
                diagnosticoId,
                progreso,
                cancellationToken);
        }

        public async Task EnviarAlAnalizadorAsync(
            int diagnosticoId,
            CancellationToken cancellationToken = default)
        {
            await PostSinCuerpoAsync<JsonElement>(
                $"api/diagnostico-ia/{diagnosticoId}/decision-tecnico/enviar-analizador",
                cancellationToken);
        }

        public async Task<DiagnosticoIADetalle>
            SolicitarNuevaEvaluacionTecnicoAsync(
                int diagnosticoId,
                string motivo,
                string? diagnosticoPropuesto = null,
                IProgress<DiagnosticoIAProcesamientoEstado>? progreso = null,
                CancellationToken cancellationToken = default)
        {
            DiagnosticoIAProcesamientoEstado inicio =
                await PostAsync<DiagnosticoIAProcesamientoEstado>(
                    $"api/diagnostico-ia-revisiones/v2/{diagnosticoId}/tecnico",
                    new
                    {
                        motivo = motivo?.Trim() ?? string.Empty,
                        diagnosticoPropuesto =
                            diagnosticoPropuesto?.Trim()
                    },
                    cancellationToken);

            progreso?.Report(inicio);

            return await EsperarProcesamientoAsync(
                diagnosticoId,
                progreso,
                cancellationToken,
                operacion: "REVISION");
        }

        public async Task NoContinuarAsync(
            int diagnosticoId,
            string motivo,
            CancellationToken cancellationToken = default)
        {
            await PostAsync<JsonElement>(
                $"api/diagnostico-ia/{diagnosticoId}/decision-tecnico/no-continuar",
                new
                {
                    motivo = motivo?.Trim() ?? string.Empty
                },
                cancellationToken);
        }

        public Task<DiagnosticoIADetalle> AnularAsync(
            int diagnosticoId,
            string motivo,
            CancellationToken cancellationToken = default) =>
            PostAsync<DiagnosticoIADetalle>(
                $"api/diagnostico-ia/{diagnosticoId}/anular",
                new
                {
                    motivo = motivo?.Trim() ?? string.Empty
                },
                cancellationToken);

        public Task<List<DiagnosticoIAListaItem>> ObtenerMisSolicitudesAsync(
            CancellationToken cancellationToken = default) =>
            GetAsync<List<DiagnosticoIAListaItem>>(
                "api/diagnostico-ia/mis-solicitudes",
                cancellationToken);

        public Task<List<DiagnosticoIAListaItem>> ObtenerColaAnalizadorAsync(
            CancellationToken cancellationToken = default) =>
            GetAsync<List<DiagnosticoIAListaItem>>(
                "api/diagnostico-ia/cola-analizador",
                cancellationToken);

        public Task<List<DiagnosticoIAListaItem>> ObtenerColaAprobadorAsync(
            CancellationToken cancellationToken = default) =>
            GetAsync<List<DiagnosticoIAListaItem>>(
                "api/diagnostico-ia/cola-aprobador",
                cancellationToken);

        public Task<DiagnosticoIADetalle> ObtenerDetalleAsync(
            int diagnosticoId,
            CancellationToken cancellationToken = default) =>
            GetAsync<DiagnosticoIADetalle>(
                $"api/diagnostico-ia/{diagnosticoId}",
                cancellationToken);

        public async Task<DiagnosticoIADetalle> SolicitarSegundaRevisionAsync(
            int diagnosticoId,
            string retroalimentacion,
            string? diagnosticoPropuesto,
            IProgress<DiagnosticoIAProcesamientoEstado>? progreso = null,
            CancellationToken cancellationToken = default)
        {
            DiagnosticoIAProcesamientoEstado inicio =
                await PostAsync<DiagnosticoIAProcesamientoEstado>(
                    $"api/diagnostico-ia-revisiones/v2/{diagnosticoId}/analizador",
                    new
                    {
                        retroalimentacionAnalizador = retroalimentacion,
                        diagnosticoPropuestoAnalizador = diagnosticoPropuesto
                    },
                    cancellationToken);

            progreso?.Report(inicio);

            return await EsperarProcesamientoAsync(
                diagnosticoId,
                progreso,
                cancellationToken,
                operacion: "REVISION");
        }

        public Task<DiagnosticoIADetalle> GuardarAnalisisHumanoAsync(
            int diagnosticoId,
            DiagnosticoIAAnalisisHumanoRequest request,
            CancellationToken cancellationToken = default) =>
            PostAsync<DiagnosticoIADetalle>(
                $"api/diagnostico-ia/{diagnosticoId}/analisis-humano/guardar",
                request,
                cancellationToken);

        public Task<DiagnosticoIADetalle> EnviarAnalisisHumanoAsync(
            int diagnosticoId,
            CancellationToken cancellationToken = default) =>
            PostSinCuerpoAsync<DiagnosticoIADetalle>(
                $"api/diagnostico-ia/{diagnosticoId}/analisis-humano/enviar",
                cancellationToken);

        public Task<DiagnosticoIADetalle> RegistrarAprobacionAsync(
            int diagnosticoId,
            DiagnosticoIAAprobacionRequest request,
            CancellationToken cancellationToken = default) =>
            PostAsync<DiagnosticoIADetalle>(
                $"api/diagnostico-ia/{diagnosticoId}/aprobacion",
                request,
                cancellationToken);

        public async Task ResolverClasificacionExistenteAsync(
            int diagnosticoId,
            int imagenId,
            int albumBotanicoCafeId,
            CancellationToken cancellationToken = default)
        {
            await PostAsync<JsonElement>(
                $"api/diagnostico-ia-clasificacion/{diagnosticoId}/imagen/{imagenId}/usar-existente",
                new
                {
                    albumBotanicoCafeId
                },
                cancellationToken);
        }

        public async Task ProponerClasificacionAlbumAsync(
            int diagnosticoId,
            int imagenId,
            int categoriaAlbumBotanicoId,
            string titulo,
            string? nombreCientifico,
            string motivo,
            CancellationToken cancellationToken = default)
        {
            await PostAsync<JsonElement>(
                $"api/diagnostico-ia-clasificacion/{diagnosticoId}/imagen/{imagenId}/proponer-nueva",
                new
                {
                    categoriaAlbumBotanicoId,
                    titulo = titulo.Trim(),
                    nombreCientifico = nombreCientifico?.Trim(),
                    motivo = motivo.Trim()
                },
                cancellationToken);
        }

        public async Task CrearClasificacionAlbumAsync(
            int diagnosticoId,
            int imagenId,
            int categoriaAlbumBotanicoId,
            string titulo,
            string? nombreCientifico,
            string descripcion,
            string? sintomas,
            CancellationToken cancellationToken = default)
        {
            await PostAsync<JsonElement>(
                $"api/diagnostico-ia-clasificacion/{diagnosticoId}/imagen/{imagenId}/crear-clasificacion",
                new
                {
                    categoriaAlbumBotanicoId,
                    titulo = titulo.Trim(),
                    nombreCientifico = nombreCientifico?.Trim(),
                    descripcion = descripcion.Trim(),
                    sintomas = sintomas?.Trim()
                },
                cancellationToken);
        }

        public Task<DiagnosticoIAAlbumCatalogo> ObtenerCatalogoAlbumAsync(
            int? categoriaId = null,
            CancellationToken cancellationToken = default)
        {
            string ruta = "api/diagnostico-ia/album/catalogo";

            if (categoriaId is > 0)
                ruta += $"?categoriaId={categoriaId.Value}";

            return GetAsync<DiagnosticoIAAlbumCatalogo>(
                ruta,
                cancellationToken);
        }

        public Task<DiagnosticoIAPublicacionResultado> PublicarAlbumAsync(
            int diagnosticoId,
            DiagnosticoIAPublicarAlbumRequest request,
            CancellationToken cancellationToken = default) =>
            PostAsync<DiagnosticoIAPublicacionResultado>(
                $"api/diagnostico-ia/{diagnosticoId}/publicar-album",
                request,
                cancellationToken);

        private async Task<DiagnosticoIADetalle> EsperarProcesamientoAsync(
            int diagnosticoId,
            IProgress<DiagnosticoIAProcesamientoEstado>? progreso,
            CancellationToken cancellationToken,
            string operacion = "ANALISIS")
        {
            DateTime limiteUtc = DateTime.UtcNow.AddMinutes(30);

            while (DateTime.UtcNow < limiteUtc)
            {
                cancellationToken.ThrowIfCancellationRequested();

                DiagnosticoIAProcesamientoEstado estado =
                    await GetAsync<DiagnosticoIAProcesamientoEstado>(
                        $"api/diagnostico-ia-procesamiento/{diagnosticoId}/estado?operacion={Uri.EscapeDataString(operacion)}",
                        cancellationToken);

                progreso?.Report(estado);

                if (estado.Finalizado)
                {
                    if (estado.TieneError)
                    {
                        throw new DiagnosticoIAApiException(
                            502,
                            string.IsNullOrWhiteSpace(estado.Mensaje)
                                ? "Gemini no pudo completar el análisis."
                                : estado.Mensaje,
                            diagnosticoId);
                    }

                    return await ObtenerDetalleAsync(
                        diagnosticoId,
                        cancellationToken);
                }

                await Task.Delay(
                    TimeSpan.FromSeconds(4),
                    cancellationToken);
            }

            throw new DiagnosticoIAApiException(
                408,
                "El análisis continúa ejecutándose en el servidor. Puede cerrar este mensaje y usar Actualizar para consultar el resultado más tarde.",
                diagnosticoId);
        }

        private async Task<T> GetAsync<T>(
            string ruta,
            CancellationToken cancellationToken)
        {
            SesionInactividadService.Instance.RegistrarActividad();

            using HttpResponseMessage response = await client.GetAsync(
                ruta,
                cancellationToken);

            return await LeerRespuestaAsync<T>(
                response,
                cancellationToken);
        }

        private async Task<T> PostAsync<T>(
            string ruta,
            object payload,
            CancellationToken cancellationToken)
        {
            SesionInactividadService.Instance.RegistrarActividad();

            using HttpResponseMessage response = await client.PostAsJsonAsync(
                ruta,
                payload,
                JsonOptions,
                cancellationToken);

            return await LeerRespuestaAsync<T>(
                response,
                cancellationToken);
        }

        private async Task<T> PostSinCuerpoAsync<T>(
            string ruta,
            CancellationToken cancellationToken)
        {
            SesionInactividadService.Instance.RegistrarActividad();

            using HttpResponseMessage response = await client.PostAsync(
                ruta,
                null,
                cancellationToken);

            return await LeerRespuestaAsync<T>(
                response,
                cancellationToken);
        }

        private async Task<T> PostProlongadoAsync<T>(
            string ruta,
            object payload,
            CancellationToken cancellationToken)
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                ruta)
            {
                Content = JsonContent.Create(
                    payload,
                    options: JsonOptions)
            };

            return await EnviarProlongadoAsync<T>(
                request,
                cancellationToken);
        }

        private async Task<T> PostProlongadoSinCuerpoAsync<T>(
            string ruta,
            CancellationToken cancellationToken)
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                ruta);

            return await EnviarProlongadoAsync<T>(
                request,
                cancellationToken);
        }

        private async Task<T> EnviarProlongadoAsync<T>(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            SesionInactividadService.Instance.RegistrarActividad();

            Task<HttpResponseMessage> envio = client.SendAsync(
                request,
                HttpCompletionOption.ResponseContentRead,
                cancellationToken);

            while (!envio.IsCompleted)
            {
                Task pausa = Task.Delay(
                    TimeSpan.FromSeconds(20),
                    cancellationToken);

                Task terminada = await Task.WhenAny(envio, pausa);

                if (terminada == envio)
                    break;

                await RegistrarActividadEnServidorAsync(
                    cancellationToken);
            }

            using HttpResponseMessage response = await envio;
            SesionInactividadService.Instance.RegistrarActividad();

            if (string.IsNullOrWhiteSpace(
                    Preferences.Get(
                        SessionKeys.KeyAccessToken,
                        string.Empty)))
            {
                throw new DiagnosticoIAApiException(
                    401,
                    "La sesión terminó mientras se procesaba la solicitud.");
            }

            return await LeerRespuestaAsync<T>(
                response,
                cancellationToken);
        }

        private async Task RegistrarActividadEnServidorAsync(
            CancellationToken cancellationToken)
        {
            SesionInactividadService.Instance.RegistrarActividad();

            try
            {
                using HttpResponseMessage respuesta =
                    await client.GetAsync(
                        "api/sesion/validar",
                        cancellationToken);
            }
            catch (OperationCanceledException)
                when (!cancellationToken.IsCancellationRequested)
            {
                // El latido no debe cancelar la operación principal.
            }
            catch (HttpRequestException)
            {
                // Una falla temporal de red no invalida el análisis en curso.
            }
            catch
            {
                // La respuesta principal conserva el manejo definitivo del error.
            }
        }

        private static async Task<T> LeerRespuestaAsync<T>(
            HttpResponseMessage response,
            CancellationToken cancellationToken)
        {
            string json = await response.Content.ReadAsStringAsync(
                cancellationToken);

            if (!response.IsSuccessStatusCode)
                throw CrearExcepcion(response.StatusCode, json);

            DiagnosticoIAApiEnvelope<T>? envelope;

            try
            {
                envelope = JsonSerializer.Deserialize<
                    DiagnosticoIAApiEnvelope<T>>(
                        json,
                        JsonOptions);
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException(
                    "La API respondió con un formato no válido.",
                    ex);
            }

            if (envelope == null ||
                !envelope.Success ||
                envelope.Data == null)
            {
                throw new InvalidOperationException(
                    envelope?.Message ??
                    "La API no devolvió los datos esperados.");
            }

            return envelope.Data;
        }

        private static DiagnosticoIAApiException CrearExcepcion(
            HttpStatusCode statusCode,
            string json)
        {
            string mensaje = ApiErrorMessageParser.Parse(
                statusCode,
                json,
                ApiErrorMessageParser.GetDefaultMessage(
                    statusCode,
                    "No fue posible completar la operación de diagnóstico."));

            int? diagnosticoIAId = ExtraerDiagnosticoId(json);

            return new DiagnosticoIAApiException(
                (int)statusCode,
                mensaje,
                diagnosticoIAId);
        }

        private static int? ExtraerDiagnosticoId(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return null;

            try
            {
                using JsonDocument document = JsonDocument.Parse(json);
                JsonElement root = document.RootElement;

                if (root.TryGetProperty("diagnosticoIAId", out JsonElement directo) &&
                    directo.TryGetInt32(out int idDirecto))
                {
                    return idDirecto;
                }

                if (root.TryGetProperty("data", out JsonElement data) &&
                    data.ValueKind == JsonValueKind.Object)
                {
                    foreach (string nombre in new[]
                    {
                        "diagnosticoIAId",
                        "DiagnosticoIAId"
                    })
                    {
                        if (data.TryGetProperty(nombre, out JsonElement valor) &&
                            valor.TryGetInt32(out int id))
                        {
                            return id;
                        }
                    }
                }
            }
            catch (JsonException)
            {
            }

            return null;
        }
    }

    public sealed class DiagnosticoIAApiException : Exception
    {
        public DiagnosticoIAApiException(
            int statusCode,
            string message,
            int? diagnosticoIAId = null)
            : base(message)
        {
            StatusCode = statusCode;
            DiagnosticoIAId = diagnosticoIAId;
        }

        public int StatusCode { get; }
        public int? DiagnosticoIAId { get; }

        public bool EsSesionInvalidada =>
            StatusCode == 401;
    }
}
