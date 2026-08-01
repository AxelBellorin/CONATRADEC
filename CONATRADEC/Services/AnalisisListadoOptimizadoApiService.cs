using CONATRADEC.Models;
using Microsoft.Maui.Storage;
using System.Text.Json;

namespace CONATRADEC.Services
{
    /// <summary>
    /// En línea consulta páginas en la API. Sin conexión crea una fotografía
    /// local filtrada al cargar la primera página y la reutiliza al pedir las
    /// siguientes, evitando releer y deserializar todas las bases SQLite en
    /// cada operación de "cargar más".
    /// </summary>
    public sealed class AnalisisListadoOptimizadoApiService
    {
        private static readonly TimeSpan TiempoMaximoListado =
            TimeSpan.FromSeconds(20);

        private readonly HttpClient httpClient;
        private readonly SemaphoreSlim cacheLocalLock = new(1, 1);
        private List<AnalisisGuardadoResumen>? cacheLocal;
        private string claveCacheLocal = string.Empty;

        private static readonly JsonSerializerOptions JsonOptions =
            new(JsonSerializerDefaults.Web)
            {
                PropertyNameCaseInsensitive = true
            };

        public AnalisisListadoOptimizadoApiService()
            : this(ApiClientService.Client)
        {
        }

        public AnalisisListadoOptimizadoApiService(HttpClient httpClient)
        {
            this.httpClient = httpClient ??
                throw new ArgumentNullException(nameof(httpClient));
        }

        public async Task<ApiResult<AnalisisListadoPaginadoResponse>>
            ListarAsync(
                bool soloPropios,
                int? usuarioId,
                string? buscar,
                DateTime? fechaDesde,
                DateTime? fechaHasta,
                int pagina,
                int tamanoPagina,
                CancellationToken cancellationToken = default)
        {
            if (ModoSesionService.EsOffline)
            {
                List<AnalisisGuardadoResumen> locales =
                    await ObtenerLocalesCacheadosAsync(
                        soloPropios,
                        usuarioId,
                        buscar,
                        fechaDesde,
                        fechaHasta,
                        forzarRecarga: Math.Max(1, pagina) == 1,
                        cancellationToken);

                return ApiResult<AnalisisListadoPaginadoResponse>.Ok(
                    CrearListadoLocal(locales, pagina, tamanoPagina),
                    locales.Count == 0
                        ? "No existen análisis descargados para los filtros seleccionados."
                        : "Mostrando análisis almacenados en este dispositivo.");
            }

            var query = new List<string>
            {
                $"soloPropios={soloPropios.ToString().ToLowerInvariant()}",
                $"pagina={Math.Max(1, pagina)}",
                $"tamanoPagina={Math.Clamp(tamanoPagina, 4, 30)}"
            };

            if (usuarioId.HasValue && usuarioId.Value > 0)
                query.Add($"usuarioId={usuarioId.Value}");

            if (!string.IsNullOrWhiteSpace(buscar))
            {
                query.Add(
                    "buscar=" +
                    Uri.EscapeDataString(buscar.Trim()));
            }

            if (fechaDesde.HasValue)
                query.Add($"fechaDesde={fechaDesde.Value:yyyy-MM-dd}");

            if (fechaHasta.HasValue)
                query.Add($"fechaHasta={fechaHasta.Value:yyyy-MM-dd}");

            string route =
                "api/analisis-listado/paginado?" +
                string.Join("&", query);

            using var timeoutSource =
                new CancellationTokenSource(TiempoMaximoListado);

            using var linkedSource =
                CancellationTokenSource.CreateLinkedTokenSource(
                    cancellationToken,
                    timeoutSource.Token);

            try
            {
                using HttpResponseMessage response =
                    await httpClient.GetAsync(
                        route,
                        HttpCompletionOption.ResponseHeadersRead,
                        linkedSource.Token);

                string contenido = await response.Content
                    .ReadAsStringAsync(linkedSource.Token);

                if (!response.IsSuccessStatusCode)
                {
                    return ApiResult<AnalisisListadoPaginadoResponse>.Fail(
                        ApiErrorMessageParser.Parse(
                            response.StatusCode,
                            contenido,
                            "No fue posible cargar los análisis."),
                        (int)response.StatusCode);
                }

                ApiEnvelope<AnalisisListadoPaginadoResponse>? envelope =
                    JsonSerializer.Deserialize<
                        ApiEnvelope<AnalisisListadoPaginadoResponse>>(
                        contenido,
                        JsonOptions);

                if (envelope?.Success != true ||
                    envelope.Data == null)
                {
                    return ApiResult<AnalisisListadoPaginadoResponse>.Fail(
                        envelope?.Message ??
                        "El servidor no devolvió el listado esperado.");
                }

                AnalisisListadoPaginadoResponse data = envelope.Data;

                GuardarResumenesSilenciosamente(data.Items ?? new());

                if (Math.Max(1, pagina) == 1)
                {
                    List<AnalisisGuardadoResumen> pendientes =
                        await AnalisisOfflineDatabaseService.Instance
                            .ListarResumenPendienteAsync();

                    if (pendientes.Count > 0)
                    {
                        data.Items = pendientes
                            .Concat(data.Items)
                            .GroupBy(item =>
                                item.AnalisisSueloCalculoId)
                            .Select(group => group.First())
                            .ToList();

                        data.TotalRegistros += pendientes.Count;
                        data.TotalPaginas = Math.Max(
                            1,
                            (int)Math.Ceiling(
                                data.TotalRegistros /
                                (double)Math.Max(1, data.TamanoPagina)));
                        data.TieneMas =
                            data.Pagina < data.TotalPaginas;
                    }
                }

                return ApiResult<AnalisisListadoPaginadoResponse>.Ok(
                    data,
                    envelope.Message);
            }
            catch (OperationCanceledException)
                when (timeoutSource.IsCancellationRequested &&
                      !cancellationToken.IsCancellationRequested)
            {
                return ApiResult<AnalisisListadoPaginadoResponse>.Fail(
                    "La API tardó demasiado en responder. La sesión permanece en línea.");
            }
            catch (OperationCanceledException)
            {
                return ApiResult<AnalisisListadoPaginadoResponse>.Fail(
                    "La operación fue cancelada.");
            }
            catch (HttpRequestException)
            {
                return ApiResult<AnalisisListadoPaginadoResponse>.Fail(
                    "No fue posible conectarse con la API. La sesión permanece en línea.");
            }
            catch (JsonException)
            {
                return ApiResult<AnalisisListadoPaginadoResponse>.Fail(
                    "El servidor respondió con un formato no válido.");
            }
            catch
            {
                return ApiResult<AnalisisListadoPaginadoResponse>.Fail(
                    "Ocurrió un error inesperado al cargar los análisis.");
            }
        }

        public async Task<ApiResult<List<UsuarioFiltroAnalisis>>>
            ListarUsuariosAsync(
                CancellationToken cancellationToken = default)
        {
            if (ModoSesionService.EsOffline)
            {
                string json =
                    await AnalisisHistorialLocalService.Instance
                        .ObtenerUsuariosFiltroJsonAsync();

                if (string.IsNullOrWhiteSpace(json))
                {
                    return ApiResult<List<UsuarioFiltroAnalisis>>.Ok(
                        new List<UsuarioFiltroAnalisis>(),
                        "No hay filtros de usuario descargados.");
                }

                try
                {
                    ApiEnvelope<List<UsuarioFiltroAnalisis>>? envelope =
                        JsonSerializer.Deserialize<
                            ApiEnvelope<List<UsuarioFiltroAnalisis>>>(
                            json,
                            JsonOptions);

                    return ApiResult<List<UsuarioFiltroAnalisis>>.Ok(
                        envelope?.Data ??
                        new List<UsuarioFiltroAnalisis>(),
                        "Filtros cargados desde el dispositivo.");
                }
                catch
                {
                    return ApiResult<List<UsuarioFiltroAnalisis>>.Ok(
                        new List<UsuarioFiltroAnalisis>(),
                        "La copia local de filtros no es válida.");
                }
            }

            using var timeoutSource =
                new CancellationTokenSource(TiempoMaximoListado);

            using var linkedSource =
                CancellationTokenSource.CreateLinkedTokenSource(
                    cancellationToken,
                    timeoutSource.Token);

            try
            {
                using HttpResponseMessage response =
                    await httpClient.GetAsync(
                        "api/analisis-listado/usuarios",
                        HttpCompletionOption.ResponseHeadersRead,
                        linkedSource.Token);

                string contenido = await response.Content
                    .ReadAsStringAsync(linkedSource.Token);

                if (!response.IsSuccessStatusCode)
                {
                    return ApiResult<List<UsuarioFiltroAnalisis>>.Fail(
                        ApiErrorMessageParser.Parse(
                            response.StatusCode,
                            contenido,
                            "No fue posible cargar los usuarios del filtro."),
                        (int)response.StatusCode);
                }

                ApiEnvelope<List<UsuarioFiltroAnalisis>>? envelope =
                    JsonSerializer.Deserialize<
                        ApiEnvelope<List<UsuarioFiltroAnalisis>>>(
                        contenido,
                        JsonOptions);

                if (envelope?.Success != true ||
                    envelope.Data == null)
                {
                    return ApiResult<List<UsuarioFiltroAnalisis>>.Fail(
                        envelope?.Message ??
                        "El servidor no devolvió los usuarios esperados.");
                }

                _ = Task.Run(async () =>
                {
                    try
                    {
                        await AnalisisHistorialLocalService.Instance
                            .GuardarUsuariosFiltroConsultadosAsync(
                                contenido);
                    }
                    catch
                    {
                    }
                });

                return ApiResult<List<UsuarioFiltroAnalisis>>.Ok(
                    envelope.Data,
                    envelope.Message);
            }
            catch (OperationCanceledException)
                when (timeoutSource.IsCancellationRequested &&
                      !cancellationToken.IsCancellationRequested)
            {
                return ApiResult<List<UsuarioFiltroAnalisis>>.Fail(
                    "La carga de usuarios tardó demasiado.");
            }
            catch (OperationCanceledException)
            {
                return ApiResult<List<UsuarioFiltroAnalisis>>.Fail(
                    "La operación fue cancelada.");
            }
            catch
            {
                return ApiResult<List<UsuarioFiltroAnalisis>>.Fail(
                    "No fue posible cargar los usuarios del filtro.");
            }
        }

        private async Task<List<AnalisisGuardadoResumen>>
            ObtenerLocalesCacheadosAsync(
                bool soloPropios,
                int? usuarioId,
                string? buscar,
                DateTime? fechaDesde,
                DateTime? fechaHasta,
                bool forzarRecarga,
                CancellationToken cancellationToken)
        {
            string clave = CrearClaveCacheLocal(
                soloPropios,
                usuarioId,
                buscar,
                fechaDesde,
                fechaHasta);

            await cacheLocalLock.WaitAsync(cancellationToken);

            try
            {
                if (forzarRecarga ||
                    cacheLocal == null ||
                    !string.Equals(
                        claveCacheLocal,
                        clave,
                        StringComparison.Ordinal))
                {
                    cacheLocal = await ObtenerTodoLocalFiltradoAsync(
                        soloPropios,
                        usuarioId,
                        buscar,
                        fechaDesde,
                        fechaHasta);

                    claveCacheLocal = clave;
                }

                return cacheLocal;
            }
            finally
            {
                cacheLocalLock.Release();
            }
        }

        private static string CrearClaveCacheLocal(
            bool soloPropios,
            int? usuarioId,
            string? buscar,
            DateTime? fechaDesde,
            DateTime? fechaHasta)
        {
            string sesionUsuario = Preferences.Get(
                SessionKeys.KeyUserId,
                "0");

            return string.Join(
                "|",
                sesionUsuario,
                soloPropios,
                usuarioId?.ToString() ?? string.Empty,
                buscar?.Trim().ToUpperInvariant() ?? string.Empty,
                fechaDesde?.Date.ToString("yyyyMMdd") ?? string.Empty,
                fechaHasta?.Date.ToString("yyyyMMdd") ?? string.Empty);
        }

        private static void GuardarResumenesSilenciosamente(
            IEnumerable<AnalisisGuardadoResumen> items)
        {
            List<AnalisisGuardadoResumen> copia = items.ToList();
            if (copia.Count == 0)
                return;

            _ = Task.Run(async () =>
            {
                foreach (AnalisisGuardadoResumen item in copia)
                {
                    try
                    {
                        await AnalisisHistorialLocalService.Instance
                            .GuardarResumenConsultadoAsync(item);
                    }
                    catch
                    {
                    }
                }
            });
        }

        private static async Task<List<AnalisisGuardadoResumen>>
            ObtenerTodoLocalFiltradoAsync(
                bool soloPropios,
                int? usuarioId,
                string? buscar,
                DateTime? fechaDesde,
                DateTime? fechaHasta)
        {
            /*
             * Las tres lecturas no dependen una de otra. Ejecutarlas en
             * paralelo reduce la espera inicial de la primera página offline.
             */
            Task<List<AnalisisGuardadoResumen>> historialTask =
                AnalisisHistorialLocalService.Instance.ListarAsync();

            Task<List<AnalisisGuardadoResumen>> pendientesTask =
                AnalisisOfflineDatabaseService.Instance
                    .ListarResumenPendienteAsync();

            Task<List<AnalisisGuardadoResumen>> operacionesTask =
                AnalisisOfflineDatabaseService.Instance
                    .ListarResumenLocalAsync();

            await Task.WhenAll(
                historialTask,
                pendientesTask,
                operacionesTask);

            List<AnalisisGuardadoResumen> historial =
                await historialTask;
            List<AnalisisGuardadoResumen> pendientes =
                await pendientesTask;
            List<AnalisisGuardadoResumen> operacionesLocales =
                await operacionesTask;

            await AnalisisReporteLocalEnrichmentService
                .EnriquecerResumenesAsync(
                    pendientes.Concat(operacionesLocales));

            HashSet<int> idsPendientes = pendientes
                .Select(item => item.AnalisisSueloCalculoId)
                .ToHashSet();

            IEnumerable<AnalisisGuardadoResumen> sincronizadosLocales =
                operacionesLocales.Where(item =>
                    !idsPendientes.Contains(
                        item.AnalisisSueloCalculoId));

            IEnumerable<AnalisisGuardadoResumen> query =
                pendientes
                    .Concat(historial)
                    .Concat(sincronizadosLocales)
                    .GroupBy(ClaveLogica)
                    .Select(group => group.First());

            int usuarioActual = int.TryParse(
                Preferences.Get(
                    SessionKeys.KeyUserId,
                    "0"),
                out int parsed)
                    ? parsed
                    : 0;

            if (soloPropios)
            {
                query = query.Where(item =>
                    !item.UsuarioId.HasValue ||
                    item.UsuarioId.Value == usuarioActual);
            }

            if (usuarioId.HasValue && usuarioId.Value > 0)
            {
                query = query.Where(item =>
                    item.UsuarioId == usuarioId.Value);
            }

            if (!string.IsNullOrWhiteSpace(buscar))
            {
                string termino = buscar.Trim();

                query = query.Where(item =>
                    item.TextoBusqueda.Contains(
                        termino,
                        StringComparison.OrdinalIgnoreCase));
            }

            if (fechaDesde.HasValue)
            {
                DateTime desde = fechaDesde.Value.Date;
                query = query.Where(item =>
                    ObtenerFecha(item).Date >= desde);
            }

            if (fechaHasta.HasValue)
            {
                DateTime hasta = fechaHasta.Value.Date;
                query = query.Where(item =>
                    ObtenerFecha(item).Date <= hasta);
            }

            return query
                .OrderByDescending(ObtenerFecha)
                .ToList();
        }

        private static string ClaveLogica(
            AnalisisGuardadoResumen item)
        {
            string identificador =
                item.IdentificadorAnalisisSuelo ??
                string.Empty;

            int separador = identificador.IndexOf(
                " · ",
                StringComparison.Ordinal);

            if (separador >= 0)
                identificador = identificador[..separador];

            identificador = identificador
                .Trim()
                .ToUpperInvariant();

            return string.IsNullOrWhiteSpace(identificador)
                ? "ID:" + item.AnalisisSueloCalculoId
                : "COD:" + identificador;
        }

        private static DateTime ObtenerFecha(
            AnalisisGuardadoResumen item) =>
            item.FechaRegistroValor ??
            item.FechaCalculoValor ??
            item.FechaAnalisisValor ??
            DateTime.MinValue;

        private static AnalisisListadoPaginadoResponse CrearListadoLocal(
            List<AnalisisGuardadoResumen> items,
            int pagina,
            int tamanoPagina)
        {
            int page = Math.Max(1, pagina);
            int pageSize = Math.Clamp(tamanoPagina, 4, 30);
            int totalPages = Math.Max(
                1,
                (int)Math.Ceiling(
                    items.Count / (double)pageSize));

            string rol = Preferences.Get(
                SessionKeys.KeyRolNombre,
                string.Empty);

            return new AnalisisListadoPaginadoResponse
            {
                Pagina = page,
                TamanoPagina = pageSize,
                TotalRegistros = items.Count,
                TotalPaginas = totalPages,
                TieneMas = page < totalPages,
                EsAdministrador = rol.Contains(
                    "ADMIN",
                    StringComparison.OrdinalIgnoreCase),
                Items = items
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList(),
                Usuarios = new List<UsuarioFiltroAnalisis>()
            };
        }

        private sealed class ApiEnvelope<T>
        {
            public bool Success { get; set; }
            public string? Message { get; set; }
            public T? Data { get; set; }
        }
    }
}
