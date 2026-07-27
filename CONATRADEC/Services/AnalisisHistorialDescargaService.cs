using CONATRADEC.Models;
using System.Net.Http.Json;
using System.Text.Json;

namespace CONATRADEC.Services
{
    /// <summary>
    /// Descarga manualmente todos los análisis a los que el usuario tiene
    /// acceso, incluyendo detalle y datos completos de reportes.
    /// </summary>
    public sealed class AnalisisHistorialDescargaService
    {
        private static readonly Lazy<AnalisisHistorialDescargaService> lazy =
            new(() => new AnalisisHistorialDescargaService());

        private readonly SemaphoreSlim downloadLock = new(1, 1);

        private static readonly JsonSerializerOptions JsonOptions =
            new(JsonSerializerDefaults.Web)
            {
                PropertyNameCaseInsensitive = true
            };

        public static AnalisisHistorialDescargaService Instance =>
            lazy.Value;

        public event EventHandler<AnalisisHistorialDescargaProgreso>?
            ProgresoCambiado;

        private AnalisisHistorialDescargaService()
        {
        }

        public async Task<AnalisisHistorialDescargaResultado>
            DescargarTodoAsync(
                CancellationToken cancellationToken = default)
        {
            if (!ModoSesionService.EsEnLinea)
            {
                return AnalisisHistorialDescargaResultado.Fail(
                    "Los análisis solamente pueden descargarse durante una sesión en línea.");
            }

            bool entered = await downloadLock.WaitAsync(
                TimeSpan.Zero,
                cancellationToken);

            if (!entered)
            {
                return AnalisisHistorialDescargaResultado.Fail(
                    "Ya existe una descarga de análisis en curso.");
            }

            string paqueteId =
                AnalisisHistorialLocalService.Instance
                    .CrearPaqueteTemporal();

            try
            {
                string usuariosJson =
                    await DescargarUsuariosAsync(cancellationToken);

                var items = new List<AnalisisGuardadoResumen>();
                int pagina = 1;
                const int tamanoPagina = 30;

                while (true)
                {
                    ApiEnvelope<AnalisisListadoPaginadoResponse> envelope =
                        await DescargarPaginaAsync(
                            pagina,
                            tamanoPagina,
                            cancellationToken);

                    AnalisisListadoPaginadoResponse data =
                        envelope.Data ??
                        throw new InvalidOperationException(
                            envelope.Message ??
                            "El servidor no devolvió el listado de análisis.");

                    items.AddRange(data.Items ?? new());

                    if (!data.TieneMas ||
                        pagina >= data.TotalPaginas)
                    {
                        break;
                    }

                    pagina++;
                }

                /*
                 * El endpoint puede devolver un registro repetido por cambios
                 * entre páginas. Se conserva el cálculo más reciente por ID.
                 */
                items = items
                    .GroupBy(item =>
                        item.AnalisisSueloCalculoId)
                    .Select(group => group.First())
                    .OrderByDescending(item =>
                        item.FechaRegistroValor ??
                        item.FechaCalculoValor)
                    .ToList();

                int total = items.Count;
                int procesados = 0;
                int detalles = 0;
                int reportes = 0;

                Notificar(
                    0,
                    total,
                    total == 0
                        ? "No existen análisis para descargar."
                        : "Preparando el historial de análisis...");

                foreach (AnalisisGuardadoResumen resumen in items)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    Task<string> detalleTask = DescargarJsonAsync(
                        $"api/guardar-todo/listardetalle/" +
                        resumen.AnalisisSueloCalculoId,
                        cancellationToken);

                    Task<string> reporteTask = DescargarJsonAsync(
                        $"api/reportes/analisis/" +
                        resumen.AnalisisSueloCalculoId +
                        "/datos",
                        cancellationToken);

                    await Task.WhenAll(
                        detalleTask,
                        reporteTask);

                    string detalleJson = await detalleTask;
                    string reporteJson = await reporteTask;

                    ValidarDetalle(detalleJson);
                    ValidarReporte(reporteJson);

                    await AnalisisHistorialLocalService.Instance
                        .GuardarTemporalAsync(
                            paqueteId,
                            resumen,
                            detalleJson,
                            reporteJson);

                    detalles++;
                    reportes++;
                    procesados++;

                    Notificar(
                        procesados,
                        total,
                        $"Descargando análisis {procesados} de {total}...");
                }

                await AnalisisHistorialLocalService.Instance
                    .ActivarPaqueteAsync(
                        paqueteId,
                        usuariosJson);

                long tamano =
                    AnalisisHistorialLocalService.Instance
                        .ObtenerTamanoBytes();

                Notificar(
                    total,
                    total,
                    "Historial de análisis preparado.");

                return AnalisisHistorialDescargaResultado.Ok(
                    total,
                    detalles,
                    reportes,
                    tamano,
                    total == 0
                        ? "No existen análisis accesibles para este usuario."
                        : $"Se descargaron {total} análisis con sus detalles y reportes.");
            }
            catch (OperationCanceledException)
            {
                await AnalisisHistorialLocalService.Instance
                    .CancelarPaqueteAsync(paqueteId);

                throw;
            }
            catch (Exception ex)
            {
                await AnalisisHistorialLocalService.Instance
                    .CancelarPaqueteAsync(paqueteId);

                return AnalisisHistorialDescargaResultado.Fail(
                    "No fue posible completar el historial de análisis. " +
                    "Se conserva la copia anterior. " +
                    ex.Message);
            }
            finally
            {
                downloadLock.Release();
            }
        }

        private static async Task<ApiEnvelope<
            AnalisisListadoPaginadoResponse>>
            DescargarPaginaAsync(
                int pagina,
                int tamanoPagina,
                CancellationToken cancellationToken)
        {
            string route =
                "api/analisis-listado/paginado" +
                $"?soloPropios=false&pagina={pagina}" +
                $"&tamanoPagina={tamanoPagina}";

            using HttpResponseMessage response =
                await ApiClientService.Client.GetAsync(
                    route,
                    HttpCompletionOption.ResponseHeadersRead,
                    cancellationToken);

            string json = await response.Content
                .ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(
                    ApiErrorMessageParser.Parse(
                        response.StatusCode,
                        json,
                        "No fue posible descargar el listado de análisis."));
            }

            return JsonSerializer.Deserialize<ApiEnvelope<
                       AnalisisListadoPaginadoResponse>>(
                       json,
                       JsonOptions)
                   ?? throw new InvalidOperationException(
                       "No fue posible interpretar el listado de análisis.");
        }

        private static async Task<string> DescargarUsuariosAsync(
            CancellationToken cancellationToken)
        {
            using HttpResponseMessage response =
                await ApiClientService.Client.GetAsync(
                    "api/analisis-listado/usuarios",
                    cancellationToken);

            string json = await response.Content
                .ReadAsStringAsync(cancellationToken);

            return response.IsSuccessStatusCode
                ? json
                : string.Empty;
        }

        private static async Task<string> DescargarJsonAsync(
            string route,
            CancellationToken cancellationToken)
        {
            using HttpResponseMessage response =
                await ApiClientService.Client.GetAsync(
                    route,
                    HttpCompletionOption.ResponseContentRead,
                    cancellationToken);

            string json = await response.Content
                .ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(
                    ApiErrorMessageParser.Parse(
                        response.StatusCode,
                        json,
                        $"No fue posible descargar {route}."));
            }

            return json;
        }

        private static void ValidarDetalle(string json)
        {
            try
            {
                AnalisisGuardadoDetalleResponse? detalle =
                    JsonSerializer.Deserialize<
                        AnalisisGuardadoDetalleResponse>(
                        json,
                        JsonOptions);

                if (detalle?.Success != true ||
                    detalle.Data == null)
                {
                    throw new InvalidOperationException(
                        detalle?.Message ??
                        "El detalle descargado está incompleto.");
                }
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException(
                    "El servidor devolvió un detalle no válido.",
                    ex);
            }
        }

        private static void ValidarReporte(string json)
        {
            try
            {
                AnalisisReporte? reporte =
                    JsonSerializer.Deserialize<AnalisisReporte>(
                        json,
                        JsonOptions);

                if (reporte == null)
                {
                    throw new InvalidOperationException(
                        "Los datos del reporte están incompletos.");
                }
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException(
                    "El servidor devolvió datos de reporte no válidos.",
                    ex);
            }
        }

        private void Notificar(
            int procesados,
            int total,
            string mensaje)
        {
            ProgresoCambiado?.Invoke(
                this,
                new AnalisisHistorialDescargaProgreso
                {
                    Procesados = procesados,
                    Total = total,
                    Mensaje = mensaje
                });
        }

        private sealed class ApiEnvelope<T>
        {
            public bool Success { get; set; }
            public string? Message { get; set; }
            public T? Data { get; set; }
        }
    }
}
