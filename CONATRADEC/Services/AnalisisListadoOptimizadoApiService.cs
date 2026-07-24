using CONATRADEC.Models;
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

                if (!response.IsSuccessStatusCode)
                {
                    string contenido = await response.Content
                        .ReadAsStringAsync(linkedSource.Token);

                    return ApiResult<AnalisisListadoPaginadoResponse>.Fail(
                        ApiErrorMessageParser.Parse(
                            response.StatusCode,
                            contenido,
                            "No fue posible cargar los análisis."),
                        (int)response.StatusCode);
                }

                ApiEnvelope<AnalisisListadoPaginadoResponse>? envelope =
                    await response.Content.ReadFromJsonAsync<
                        ApiEnvelope<AnalisisListadoPaginadoResponse>>(
                            JsonOptions,
                            linkedSource.Token);

                if (envelope?.Success != true || envelope.Data == null)
                {
                    return ApiResult<AnalisisListadoPaginadoResponse>.Fail(
                        envelope?.Message ??
                        "El servidor no devolvió el listado esperado.");
                }

                return ApiResult<AnalisisListadoPaginadoResponse>.Ok(
                    envelope.Data,
                    envelope.Message);
            }
            catch (OperationCanceledException)
                when (timeoutSource.IsCancellationRequested &&
                      !cancellationToken.IsCancellationRequested)
            {
                return ApiResult<AnalisisListadoPaginadoResponse>.Fail(
                    "La consulta tardó demasiado y fue cancelada. " +
                    "Presione Actualizar lista para intentarlo nuevamente.");
            }
            catch (OperationCanceledException)
            {
                return ApiResult<AnalisisListadoPaginadoResponse>.Fail(
                    "La operación fue cancelada.");
            }
            catch (HttpRequestException)
            {
                return ApiResult<AnalisisListadoPaginadoResponse>.Fail(
                    "No fue posible conectarse con el servidor.");
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

                if (!response.IsSuccessStatusCode)
                {
                    string contenido = await response.Content
                        .ReadAsStringAsync(linkedSource.Token);

                    return ApiResult<List<UsuarioFiltroAnalisis>>.Fail(
                        ApiErrorMessageParser.Parse(
                            response.StatusCode,
                            contenido,
                            "No fue posible cargar los usuarios del filtro."),
                        (int)response.StatusCode);
                }

                ApiEnvelope<List<UsuarioFiltroAnalisis>>? envelope =
                    await response.Content.ReadFromJsonAsync<
                        ApiEnvelope<List<UsuarioFiltroAnalisis>>>(
                            JsonOptions,
                            linkedSource.Token);

                if (envelope?.Success != true || envelope.Data == null)
                {
                    return ApiResult<List<UsuarioFiltroAnalisis>>.Fail(
                        envelope?.Message ??
                        "El servidor no devolvió los usuarios esperados.");
                }

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

        private sealed class ApiEnvelope<T>
        {
            public bool Success { get; set; }
            public string Message { get; set; } = string.Empty;
            public T? Data { get; set; }
        }
    }
}
