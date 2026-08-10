using CONATRADEC.Models;
using System.Net;
using System.Text.Json;

namespace CONATRADEC.Services
{
    /// <summary>
    /// Descarga manualmente todos los análisis a los que el usuario tiene
    /// acceso, incluyendo detalle y datos completos de reportes.
    ///
    /// El alcance se determina por permisos:
    /// - MainPage/Leer: permite trabajar con análisis propios.
    /// - AnalisisSueloTodosPage/Leer: permite incluir análisis de otros usuarios.
    ///
    /// La API vuelve a validar el alcance, por lo que un cliente no puede
    /// ampliar sus datos modificando soloPropios manualmente.
    /// </summary>
    public sealed class AnalisisHistorialDescargaService
    {
        private const int TamanoPagina = 30;
        private const int TamanoLoteDetalles = 4;
        private const int MaximoIntentosHttp = 3;

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
                bool puedeVerTodos =
                    PermissionService.Instance.HasRead(
                        InterfazCodigos.AnalisisSueloTodos);

                /*
                 * El catálogo de usuarios solo tiene sentido cuando existe el
                 * permiso de alcance global. Para un técnico normal no se hace
                 * esta solicitud ni se guarda información de otros usuarios.
                 */
                string usuariosJson = puedeVerTodos
                    ? await DescargarUsuariosAsync(cancellationToken)
                    : string.Empty;

                var items = new List<AnalisisGuardadoResumen>();
                int pagina = 1;

                while (true)
                {
                    ApiEnvelope<AnalisisListadoPaginadoResponse> envelope =
                        await DescargarPaginaAsync(
                            pagina,
                            TamanoPagina,
                            soloPropios: !puedeVerTodos,
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
                        : puedeVerTodos
                            ? $"Preparando {total} análisis autorizados para este usuario..."
                            : $"Preparando {total} análisis propios...");

                /*
                 * Se descargan pocos análisis en paralelo para reducir el
                 * tiempo total sin generar cientos de solicitudes simultáneas.
                 * Cada lote se escribe en SQLite de forma secuencial.
                 */
                foreach (AnalisisGuardadoResumen[] lote in
                         items.Chunk(TamanoLoteDetalles))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    Task<AnalisisDescargado>[] tareas = lote
                        .Select(item =>
                            DescargarAnalisisAsync(
                                item,
                                cancellationToken))
                        .ToArray();

                    AnalisisDescargado[] descargados =
                        await Task.WhenAll(tareas);

                    foreach (AnalisisDescargado descargado in descargados)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        await AnalisisHistorialLocalService.Instance
                            .GuardarTemporalAsync(
                                paqueteId,
                                descargado.Resumen,
                                descargado.DetalleJson,
                                descargado.ReporteJson);

                        detalles++;
                        reportes++;
                        procesados++;

                        Notificar(
                            procesados,
                            total,
                            $"Descargando análisis {procesados} de {total}...");
                    }
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
                        : puedeVerTodos
                            ? $"Se descargaron {total} análisis autorizados con sus detalles y reportes."
                            : $"Se descargaron {total} análisis propios con sus detalles y reportes.");
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

        private static async Task<AnalisisDescargado>
            DescargarAnalisisAsync(
                AnalisisGuardadoResumen resumen,
                CancellationToken cancellationToken)
        {
            try
            {
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

                return new AnalisisDescargado
                {
                    Resumen = resumen,
                    DetalleJson = detalleJson,
                    ReporteJson = reporteJson
                };
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                string identificador =
                    string.IsNullOrWhiteSpace(
                        resumen.IdentificadorAnalisisSuelo)
                        ? $"ID {resumen.AnalisisSueloCalculoId}"
                        : $"{resumen.IdentificadorAnalisisSuelo} " +
                          $"(ID {resumen.AnalisisSueloCalculoId})";

                throw new InvalidOperationException(
                    $"Falló la descarga del análisis {identificador}. " +
                    ex.Message,
                    ex);
            }
        }

        private static async Task<ApiEnvelope<
            AnalisisListadoPaginadoResponse>>
            DescargarPaginaAsync(
                int pagina,
                int tamanoPagina,
                bool soloPropios,
                CancellationToken cancellationToken)
        {
            string route =
                "api/analisis-listado/paginado" +
                $"?soloPropios={soloPropios.ToString().ToLowerInvariant()}" +
                $"&pagina={pagina}" +
                $"&tamanoPagina={tamanoPagina}";

            string json = await DescargarJsonAsync(
                route,
                cancellationToken);

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
            try
            {
                return await DescargarJsonAsync(
                    "api/analisis-listado/usuarios",
                    cancellationToken);
            }
            catch (InvalidOperationException)
            {
                /*
                 * El filtro de usuarios es auxiliar. Si cambia el permiso entre
                 * el login y la descarga, el servidor seguirá protegiendo el
                 * listado principal y simplemente se omite este catálogo.
                 */
                return string.Empty;
            }
        }

        private static async Task<string> DescargarJsonAsync(
            string route,
            CancellationToken cancellationToken)
        {
            string ultimoMensaje =
                $"No fue posible descargar {route}.";

            for (int intento = 1;
                 intento <= MaximoIntentosHttp;
                 intento++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    using HttpResponseMessage response =
                        await ApiClientService.Client.GetAsync(
                            route,
                            HttpCompletionOption.ResponseHeadersRead,
                            cancellationToken);

                    string json = await response.Content
                        .ReadAsStringAsync(cancellationToken);

                    if (response.IsSuccessStatusCode)
                        return json;

                    ultimoMensaje = ApiErrorMessageParser.Parse(
                        response.StatusCode,
                        json,
                        $"No fue posible descargar {route}.");

                    if (!EsErrorTransitorio(response.StatusCode) ||
                        intento >= MaximoIntentosHttp)
                    {
                        throw new InvalidOperationException(
                            ultimoMensaje);
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (HttpRequestException ex)
                {
                    ultimoMensaje =
                        $"No fue posible conectar con el servidor al descargar {route}. " +
                        ex.Message;

                    if (intento >= MaximoIntentosHttp)
                    {
                        throw new InvalidOperationException(
                            ultimoMensaje,
                            ex);
                    }
                }

                await Task.Delay(
                    TimeSpan.FromMilliseconds(350 * intento),
                    cancellationToken);
            }

            throw new InvalidOperationException(ultimoMensaje);
        }

        private static bool EsErrorTransitorio(
            HttpStatusCode statusCode) =>
            statusCode is
                HttpStatusCode.RequestTimeout or
                HttpStatusCode.TooManyRequests or
                HttpStatusCode.InternalServerError or
                HttpStatusCode.BadGateway or
                HttpStatusCode.ServiceUnavailable or
                HttpStatusCode.GatewayTimeout;

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

        private sealed class AnalisisDescargado
        {
            public AnalisisGuardadoResumen Resumen { get; init; } = null!;
            public string DetalleJson { get; init; } = string.Empty;
            public string ReporteJson { get; init; } = string.Empty;
        }

        private sealed class ApiEnvelope<T>
        {
            public bool Success { get; set; }
            public string? Message { get; set; }
            public T? Data { get; set; }
        }
    }
}
