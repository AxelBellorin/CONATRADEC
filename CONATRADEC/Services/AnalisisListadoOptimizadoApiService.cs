using CONATRADEC.Models;
using Microsoft.Maui.Storage;
using System.Net.Http.Json;
using System.Text.Json;

namespace CONATRADEC.Services
{
    public sealed class AnalisisListadoOptimizadoApiService
    {
        private static readonly TimeSpan TiempoMaximoListado =
            TimeSpan.FromSeconds(8);

        private readonly HttpClient httpClient;

        private static readonly JsonSerializerOptions JsonOptions =
            new(JsonSerializerDefaults.Web)
            {
                PropertyNameCaseInsensitive = true
            };

        public AnalisisListadoOptimizadoApiService()
            : this(ApiClientService.Client)
        {
        }

        public AnalisisListadoOptimizadoApiService(
            HttpClient httpClient)
        {
            this.httpClient = httpClient ??
                throw new ArgumentNullException(
                    nameof(httpClient));
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
            /*
             * Los análisis calculados localmente forman parte del listado aun
             * sin conexión. No se espera el timeout de la API.
             *
             * SesionActualEsOffline indica cómo se autenticó el usuario, no que
             * deba permanecer desconectado toda la sesión.
             */
            if (!EstadoConexionService.Instance.HayInternet)
            {
                List<AnalisisGuardadoResumen> locales =
                    await ObtenerLocalesFiltradosAsync(
                        usuarioId,
                        buscar,
                        fechaDesde,
                        fechaHasta,
                        incluirSincronizados: false);

                return ApiResult<AnalisisListadoPaginadoResponse>.Ok(
                    CrearListadoOffline(
                        locales,
                        pagina,
                        tamanoPagina),
                    locales.Count == 0
                        ? "Sin conexión. No hay análisis locales pendientes."
                        : "Sin conexión. Se muestran los análisis guardados en este dispositivo.");
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
            {
                query.Add(
                    $"fechaDesde={fechaDesde.Value:yyyy-MM-dd}");
            }

            if (fechaHasta.HasValue)
            {
                query.Add(
                    $"fechaHasta={fechaHasta.Value:yyyy-MM-dd}");
            }

            string route =
                "api/analisis-listado/paginado?" +
                string.Join("&", query);

            using var timeoutSource =
                new CancellationTokenSource(
                    TiempoMaximoListado);

            using var linkedSource =
                CancellationTokenSource
                    .CreateLinkedTokenSource(
                        cancellationToken,
                        timeoutSource.Token);

            try
            {
                using HttpResponseMessage response =
                    await httpClient.GetAsync(
                        route,
                        HttpCompletionOption
                            .ResponseHeadersRead,
                        linkedSource.Token);

                if (!response.IsSuccessStatusCode)
                {
                    string contenido =
                        await response.Content
                            .ReadAsStringAsync(
                                linkedSource.Token);

                    return ApiResult<
                        AnalisisListadoPaginadoResponse>.Fail(
                            ApiErrorMessageParser.Parse(
                                response.StatusCode,
                                contenido,
                                "No fue posible cargar los análisis."),
                            (int)response.StatusCode);
                }

                ApiEnvelope<AnalisisListadoPaginadoResponse>?
                    envelope =
                        await response.Content
                            .ReadFromJsonAsync<
                                ApiEnvelope<
                                    AnalisisListadoPaginadoResponse>>(
                                JsonOptions,
                                linkedSource.Token);

                if (envelope?.Success != true ||
                    envelope.Data == null)
                {
                    return ApiResult<
                        AnalisisListadoPaginadoResponse>.Fail(
                            envelope?.Message ??
                            "El servidor no devolvió el listado esperado.");
                }

                AnalisisListadoPaginadoResponse data =
                    envelope.Data;

                /*
                 * Solo la primera página incorpora las operaciones pendientes
                 * del dispositivo. Se muestran antes que los registros del
                 * servidor y se identifican por su estado en el título.
                 */
                if (Math.Max(1, pagina) == 1)
                {
                    List<AnalisisGuardadoResumen> locales =
                        await ObtenerLocalesFiltradosAsync(
                            usuarioId,
                            buscar,
                            fechaDesde,
                            fechaHasta,
                            incluirSincronizados: false);

                    if (locales.Count > 0)
                    {
                        List<AnalisisGuardadoResumen> combinados =
                            locales
                                .Concat(data.Items)
                                .GroupBy(item =>
                                    item.AnalisisSueloCalculoId)
                                .Select(group =>
                                    group.First())
                                .ToList();

                        data.Items = combinados;
                        data.TotalRegistros +=
                            locales.Count;

                        data.TotalPaginas =
                            Math.Max(
                                1,
                                (int)Math.Ceiling(
                                    data.TotalRegistros /
                                    (double)Math.Max(
                                        1,
                                        data.TamanoPagina)));

                        data.TieneMas =
                            data.Pagina <
                            data.TotalPaginas;
                    }
                }

                return ApiResult<
                    AnalisisListadoPaginadoResponse>.Ok(
                        data,
                        envelope.Message);
            }
            catch (OperationCanceledException)
                when (timeoutSource.IsCancellationRequested &&
                      !cancellationToken.IsCancellationRequested)
            {
                EstadoConexionService.Instance
                    .ReportarServidorNoDisponible();

                return await CrearResultadoLocalPorFalloAsync(
                    usuarioId,
                    buscar,
                    fechaDesde,
                    fechaHasta,
                    pagina,
                    tamanoPagina,
                    "La API no respondió. Se muestran los análisis guardados en este dispositivo.");
            }
            catch (OperationCanceledException)
            {
                return ApiResult<
                    AnalisisListadoPaginadoResponse>.Fail(
                        "La operación fue cancelada.");
            }
            catch (HttpRequestException)
            {
                EstadoConexionService.Instance
                    .ReportarServidorNoDisponible();

                return await CrearResultadoLocalPorFalloAsync(
                    usuarioId,
                    buscar,
                    fechaDesde,
                    fechaHasta,
                    pagina,
                    tamanoPagina,
                    "No fue posible conectar con la API. Se muestran los análisis locales.");
            }
            catch (JsonException)
            {
                return ApiResult<
                    AnalisisListadoPaginadoResponse>.Fail(
                        "El servidor respondió con un formato no válido.");
            }
            catch
            {
                return ApiResult<
                    AnalisisListadoPaginadoResponse>.Fail(
                        "Ocurrió un error inesperado al cargar los análisis.");
            }
        }

        public async Task<ApiResult<List<UsuarioFiltroAnalisis>>>
            ListarUsuariosAsync(
                CancellationToken cancellationToken = default)
        {
            /*
             * SesionActualEsOffline indica cómo se autenticó inicialmente el
             * usuario, no que deba permanecer desconectado toda la sesión.
             * Cuando la API vuelve a estar disponible, el listado debe poder
             * consultar nuevamente el servidor.
             */
            if (!EstadoConexionService.Instance.HayInternet)
            {
                return ApiResult<List<UsuarioFiltroAnalisis>>.Ok(
                    new List<UsuarioFiltroAnalisis>(),
                    "El filtro de usuarios requiere conexión.");
            }

            using var timeoutSource =
                new CancellationTokenSource(
                    TiempoMaximoListado);

            using var linkedSource =
                CancellationTokenSource
                    .CreateLinkedTokenSource(
                        cancellationToken,
                        timeoutSource.Token);

            try
            {
                using HttpResponseMessage response =
                    await httpClient.GetAsync(
                        "api/analisis-listado/usuarios",
                        HttpCompletionOption
                            .ResponseHeadersRead,
                        linkedSource.Token);

                if (!response.IsSuccessStatusCode)
                {
                    string contenido =
                        await response.Content
                            .ReadAsStringAsync(
                                linkedSource.Token);

                    return ApiResult<
                        List<UsuarioFiltroAnalisis>>.Fail(
                            ApiErrorMessageParser.Parse(
                                response.StatusCode,
                                contenido,
                                "No fue posible cargar los usuarios del filtro."),
                            (int)response.StatusCode);
                }

                ApiEnvelope<List<UsuarioFiltroAnalisis>>?
                    envelope =
                        await response.Content
                            .ReadFromJsonAsync<
                                ApiEnvelope<
                                    List<UsuarioFiltroAnalisis>>>(
                                JsonOptions,
                                linkedSource.Token);

                if (envelope?.Success != true ||
                    envelope.Data == null)
                {
                    return ApiResult<
                        List<UsuarioFiltroAnalisis>>.Fail(
                            envelope?.Message ??
                            "El servidor no devolvió los usuarios esperados.");
                }

                return ApiResult<
                    List<UsuarioFiltroAnalisis>>.Ok(
                        envelope.Data,
                        envelope.Message);
            }
            catch (OperationCanceledException)
                when (timeoutSource.IsCancellationRequested &&
                      !cancellationToken.IsCancellationRequested)
            {
                return ApiResult<
                    List<UsuarioFiltroAnalisis>>.Fail(
                        "La carga de usuarios tardó demasiado.");
            }
            catch (OperationCanceledException)
            {
                return ApiResult<
                    List<UsuarioFiltroAnalisis>>.Fail(
                        "La operación fue cancelada.");
            }
            catch
            {
                return ApiResult<
                    List<UsuarioFiltroAnalisis>>.Fail(
                        "No fue posible cargar los usuarios del filtro.");
            }
        }

        private static async Task<ApiResult<
            AnalisisListadoPaginadoResponse>>
            CrearResultadoLocalPorFalloAsync(
                int? usuarioId,
                string? buscar,
                DateTime? fechaDesde,
                DateTime? fechaHasta,
                int pagina,
                int tamanoPagina,
                string mensaje)
        {
            List<AnalisisGuardadoResumen> locales =
                await ObtenerLocalesFiltradosAsync(
                    usuarioId,
                    buscar,
                    fechaDesde,
                    fechaHasta,
                    incluirSincronizados: false);

            return ApiResult<
                AnalisisListadoPaginadoResponse>.Ok(
                    CrearListadoOffline(
                        locales,
                        pagina,
                        tamanoPagina),
                    mensaje);
        }

        private static async Task<List<
            AnalisisGuardadoResumen>>
            ObtenerLocalesFiltradosAsync(
                int? usuarioId,
                string? buscar,
                DateTime? fechaDesde,
                DateTime? fechaHasta,
                bool incluirSincronizados)
        {
            List<AnalisisGuardadoResumen> items =
                incluirSincronizados
                    ? await AnalisisOfflineDatabaseService
                        .Instance
                        .ListarResumenLocalAsync()
                    : await AnalisisOfflineDatabaseService
                        .Instance
                        .ListarResumenPendienteAsync();

            int usuarioActual =
                int.TryParse(
                    Preferences.Get(
                        SessionKeys.KeyUserId,
                        "0"),
                    out int parsed)
                        ? parsed
                        : 0;

            IEnumerable<AnalisisGuardadoResumen> query =
                items;

            if (usuarioId.HasValue &&
                usuarioId.Value > 0 &&
                usuarioId.Value != usuarioActual)
            {
                return new List<
                    AnalisisGuardadoResumen>();
            }

            if (!string.IsNullOrWhiteSpace(buscar))
            {
                string termino =
                    buscar.Trim().ToUpperInvariant();

                query = query.Where(item =>
                    item.TextoBusqueda.Contains(
                        termino,
                        StringComparison.OrdinalIgnoreCase));
            }

            if (fechaDesde.HasValue)
            {
                DateTime desde =
                    fechaDesde.Value.Date;

                query = query.Where(item =>
                    (item.FechaRegistroValor ??
                     item.FechaCalculoValor ??
                     DateTime.MinValue).Date >=
                    desde);
            }

            if (fechaHasta.HasValue)
            {
                DateTime hasta =
                    fechaHasta.Value.Date;

                query = query.Where(item =>
                    (item.FechaRegistroValor ??
                     item.FechaCalculoValor ??
                     DateTime.MinValue).Date <=
                    hasta);
            }

            return query
                .OrderByDescending(item =>
                    item.FechaRegistroValor ??
                    item.FechaCalculoValor)
                .ToList();
        }

        private static AnalisisListadoPaginadoResponse
            CrearListadoOffline(
                List<AnalisisGuardadoResumen> items,
                int pagina,
                int tamanoPagina)
        {
            string rol =
                Preferences.Get(
                    SessionKeys.KeyRolNombre,
                    string.Empty);

            bool esAdministrador =
                !string.IsNullOrWhiteSpace(rol) &&
                rol.Contains(
                    "ADMIN",
                    StringComparison.OrdinalIgnoreCase);

            int page =
                Math.Max(1, pagina);

            int pageSize =
                Math.Clamp(
                    tamanoPagina,
                    4,
                    30);

            int totalPaginas =
                Math.Max(
                    1,
                    (int)Math.Ceiling(
                        items.Count /
                        (double)pageSize));

            List<AnalisisGuardadoResumen> paginaItems =
                items
                    .Skip(
                        (page - 1) *
                        pageSize)
                    .Take(pageSize)
                    .ToList();

            return new AnalisisListadoPaginadoResponse
            {
                Pagina = page,
                TamanoPagina = pageSize,
                TotalRegistros =
                    items.Count,
                TotalPaginas =
                    totalPaginas,
                TieneMas =
                    page < totalPaginas,
                EsAdministrador =
                    esAdministrador,
                Items =
                    paginaItems,
                Usuarios =
                    new List<UsuarioFiltroAnalisis>()
            };
        }

        private sealed class ApiEnvelope<T>
        {
            public bool Success { get; set; }
            public string Message { get; set; } =
                string.Empty;
            public T? Data { get; set; }
        }
    }
}
