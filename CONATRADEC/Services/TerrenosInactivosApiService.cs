using CONATRADEC.Models;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CONATRADEC.Services
{
    /// <summary>
    /// Consulta paginada exclusiva de Terrenos eliminados.
    /// La reactivación continúa utilizando CatalogosEliminadosApiService para
    /// conservar una sola regla de negocio en el servidor.
    /// </summary>
    public sealed class TerrenosInactivosApiService
    {
        private readonly HttpClient httpClient;

        private static readonly JsonSerializerOptions JsonOptions =
            new(JsonSerializerDefaults.Web)
            {
                PropertyNameCaseInsensitive = true
            };

        public TerrenosInactivosApiService()
            : this(ApiClientService.Client)
        {
        }

        public TerrenosInactivosApiService(HttpClient httpClient)
        {
            this.httpClient = httpClient ??
                throw new ArgumentNullException(nameof(httpClient));
        }

        public async Task<ApiResult<TerrenoInactivoPaginaResponse>>
            BuscarAsync(
                string? buscar,
                int pagina,
                int tamanoPagina,
                CancellationToken cancellationToken = default)
        {
            pagina = Math.Max(1, pagina);
            tamanoPagina = Math.Clamp(tamanoPagina, 5, 100);

            string ruta =
                "api/administracion/terrenos/inactivos" +
                $"?pagina={pagina}" +
                $"&tamanoPagina={tamanoPagina}";

            if (!string.IsNullOrWhiteSpace(buscar))
            {
                ruta +=
                    $"&buscar={Uri.EscapeDataString(buscar.Trim())}";
            }

            try
            {
                using HttpResponseMessage response =
                    await httpClient.GetAsync(
                        ruta,
                        cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    return ApiResult<TerrenoInactivoPaginaResponse>.Fail(
                        await ApiServiceHelper.ReadResponseMessageAsync(
                            response,
                            "No fue posible cargar los terrenos eliminados.",
                            cancellationToken),
                        (int)response.StatusCode);
                }

                TerrenoInactivoPaginaEnvelope? envelope =
                    await response.Content
                        .ReadFromJsonAsync<TerrenoInactivoPaginaEnvelope>(
                            JsonOptions,
                            cancellationToken);

                if (envelope?.Data == null)
                {
                    return ApiResult<TerrenoInactivoPaginaResponse>.Fail(
                        "El servidor respondió, pero no devolvió la página de terrenos eliminados.");
                }

                return ApiResult<TerrenoInactivoPaginaResponse>.Ok(
                    envelope.Data,
                    envelope.Message ?? string.Empty);
            }
            catch (TaskCanceledException)
                when (!cancellationToken.IsCancellationRequested)
            {
                return ApiResult<TerrenoInactivoPaginaResponse>.Fail(
                    "La carga tardó demasiado. Verifique su conexión.");
            }
            catch (OperationCanceledException)
            {
                return ApiResult<TerrenoInactivoPaginaResponse>.Fail(
                    "La operación fue cancelada.");
            }
            catch (HttpRequestException)
            {
                return ApiResult<TerrenoInactivoPaginaResponse>.Fail(
                    "No fue posible comunicarse con el servidor.");
            }
            catch (JsonException)
            {
                return ApiResult<TerrenoInactivoPaginaResponse>.Fail(
                    "El servidor respondió, pero la página de terrenos eliminados no tiene el formato esperado.");
            }
            catch
            {
                return ApiResult<TerrenoInactivoPaginaResponse>.Fail(
                    "Ocurrió un error inesperado al cargar los terrenos eliminados.");
            }
        }
    }

    public sealed class TerrenoInactivoPaginaResponse
    {
        [JsonPropertyName("items")]
        public List<CatalogoEliminadoItem> Items { get; set; } = new();

        [JsonPropertyName("paginaActual")]
        public int PaginaActual { get; set; } = 1;

        [JsonPropertyName("tamanoPagina")]
        public int TamanoPagina { get; set; }

        [JsonPropertyName("totalRegistros")]
        public int TotalRegistros { get; set; }

        [JsonPropertyName("totalPaginas")]
        public int TotalPaginas { get; set; }
    }

    internal sealed class TerrenoInactivoPaginaEnvelope
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }

        [JsonPropertyName("data")]
        public TerrenoInactivoPaginaResponse? Data { get; set; }
    }
}
