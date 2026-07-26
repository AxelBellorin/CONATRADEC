using CONATRADEC.Models;
using Microsoft.Maui.Storage;
using System.Net.Http.Json;
using System.Text.Json;

namespace CONATRADEC.Services
{
    public sealed class AnalisisListadoOptimizadoApiService
    {
        private static readonly TimeSpan TiempoMaximoListado =
            TimeSpan.FromSeconds(25);

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
             * El listado histórico todavía no forma parte del paquete local.
             * En una sesión offline se devuelve inmediatamente una lista vacía
             * para no bloquear durante 25 segundos el botón Nuevo análisis.
             */
            if (SesionOfflineService.SesionActualEsOffline ||
                !EstadoConexionService.Instance.HayInternet)
            {
                return ApiResult<AnalisisListadoPaginadoResponse>.Ok(
                    CrearListadoOfflineVacio(
                        pagina,
                        tamanoPagina),
                    "Sin conexión. Puede crear un nuevo análisis con el motor descargado.");
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

                return ApiResult<
                    AnalisisListadoPaginadoResponse>.Ok(
                        envelope.Data,
                        envelope.Message);
            }
            catch (OperationCanceledException)
                when (timeoutSource.IsCancellationRequested &&
                      !cancellationToken.IsCancellationRequested)
            {
                return ApiResult<
                    AnalisisListadoPaginadoResponse>.Fail(
                        "La consulta tardó demasiado y fue cancelada. " +
                        "Presione Actualizar lista para intentarlo nuevamente.");
            }
            catch (OperationCanceledException)
            {
                return ApiResult<
                    AnalisisListadoPaginadoResponse>.Fail(
                        "La operación fue cancelada.");
            }
            catch (HttpRequestException)
            {
                return ApiResult<
                    AnalisisListadoPaginadoResponse>.Fail(
                        "No fue posible conectarse con el servidor.");
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
            if (SesionOfflineService.SesionActualEsOffline ||
                !EstadoConexionService.Instance.HayInternet)
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

        private static AnalisisListadoPaginadoResponse
            CrearListadoOfflineVacio(
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

            return new AnalisisListadoPaginadoResponse
            {
                Pagina = Math.Max(1, pagina),
                TamanoPagina =
                    Math.Clamp(tamanoPagina, 4, 30),
                TotalRegistros = 0,
                TotalPaginas = 1,
                TieneMas = false,
                EsAdministrador = esAdministrador,
                Items = new List<AnalisisGuardadoResumen>(),
                Usuarios = new List<UsuarioFiltroAnalisis>()
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
