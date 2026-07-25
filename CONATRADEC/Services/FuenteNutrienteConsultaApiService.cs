using CONATRADEC.Models;
using System.Net.Http.Json;
using System.Text.Json;

namespace CONATRADEC.Services
{
    /// <summary>
    /// Servicio exclusivo para la pantalla administrativa.
    ///
    /// No reemplaza FuenteNutrienteApiService, que continúa siendo
    /// utilizado por el formulario y por los cálculos del sistema.
    /// </summary>
    public sealed class FuenteNutrienteConsultaApiService
    {
        private readonly HttpClient httpClient;

        private readonly JsonSerializerOptions jsonOptions =
            new(JsonSerializerDefaults.Web)
            {
                PropertyNameCaseInsensitive =
                    true
            };

        public FuenteNutrienteConsultaApiService()
            : this(ApiClientService.Client)
        {
        }

        public FuenteNutrienteConsultaApiService(
            HttpClient httpClient)
        {
            this.httpClient =
                httpClient
                ?? throw new ArgumentNullException(
                    nameof(httpClient));
        }

        public async Task<ApiResult<FuenteNutrientePaginaResponse>>
            BuscarAsync(
                string? buscar,
                string? categoria,
                int pagina,
                int tamanoPagina,
                CancellationToken cancellationToken = default)
        {
            pagina =
                Math.Max(
                    1,
                    pagina);

            tamanoPagina =
                Math.Clamp(
                    tamanoPagina,
                    5,
                    100);

            string ruta =
                "api/fuente-nutriente/buscar" +
                $"?pagina={pagina}" +
                $"&tamanoPagina={tamanoPagina}" +
                $"&categoria={Uri.EscapeDataString(categoria ?? string.Empty)}";

            if (!string.IsNullOrWhiteSpace(
                    buscar))
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
                    return ApiResult<FuenteNutrientePaginaResponse>
                        .Fail(
                            await ApiServiceHelper
                                .ReadResponseMessageAsync(
                                    response,
                                    "No fue posible cargar las fuentes de nutrientes.",
                                    cancellationToken),
                            (int)response.StatusCode);
                }

                FuenteNutrientePaginaResponse? data =
                    await response.Content
                        .ReadFromJsonAsync<
                            FuenteNutrientePaginaResponse>(
                            jsonOptions,
                            cancellationToken);

                return ApiResult<FuenteNutrientePaginaResponse>
                    .Ok(
                        data ??
                        new FuenteNutrientePaginaResponse());
            }
            catch (TaskCanceledException)
                when (!cancellationToken.IsCancellationRequested)
            {
                return ApiResult<FuenteNutrientePaginaResponse>
                    .Fail(
                        "La carga de fuentes tardó demasiado. Intente nuevamente.");
            }
            catch (OperationCanceledException)
            {
                return ApiResult<FuenteNutrientePaginaResponse>
                    .Fail(
                        "La operación fue cancelada.");
            }
            catch (HttpRequestException)
            {
                return ApiResult<FuenteNutrientePaginaResponse>
                    .Fail(
                        "No fue posible comunicarse con el servidor para cargar las fuentes.");
            }
            catch (JsonException)
            {
                return ApiResult<FuenteNutrientePaginaResponse>
                    .Fail(
                        "El servidor respondió, pero el listado de fuentes no tiene el formato esperado.");
            }
            catch
            {
                return ApiResult<FuenteNutrientePaginaResponse>
                    .Fail(
                        "Ocurrió un error inesperado al cargar las fuentes.");
            }
        }

        public async Task<ApiResult<List<FuenteNutrienteResponse>>>
            ObtenerComposicionAsync(
                string? buscar,
                string? categoria,
                CancellationToken cancellationToken = default)
        {
            string ruta =
                "api/fuente-nutriente/composicion" +
                $"?categoria={Uri.EscapeDataString(categoria ?? string.Empty)}";

            if (!string.IsNullOrWhiteSpace(
                    buscar))
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
                    return ApiResult<List<FuenteNutrienteResponse>>
                        .Fail(
                            await ApiServiceHelper
                                .ReadResponseMessageAsync(
                                    response,
                                    "No fue posible cargar la composición de las fuentes.",
                                    cancellationToken),
                            (int)response.StatusCode);
                }

                List<FuenteNutrienteResponse>? data =
                    await response.Content
                        .ReadFromJsonAsync<
                            List<FuenteNutrienteResponse>>(
                            jsonOptions,
                            cancellationToken);

                return ApiResult<List<FuenteNutrienteResponse>>
                    .Ok(
                        data ??
                        new List<FuenteNutrienteResponse>());
            }
            catch (TaskCanceledException)
                when (!cancellationToken.IsCancellationRequested)
            {
                return ApiResult<List<FuenteNutrienteResponse>>
                    .Fail(
                        "La matriz de composición tardó demasiado en cargar.");
            }
            catch (OperationCanceledException)
            {
                return ApiResult<List<FuenteNutrienteResponse>>
                    .Fail(
                        "La operación fue cancelada.");
            }
            catch (HttpRequestException)
            {
                return ApiResult<List<FuenteNutrienteResponse>>
                    .Fail(
                        "No fue posible comunicarse con el servidor para cargar la composición.");
            }
            catch (JsonException)
            {
                return ApiResult<List<FuenteNutrienteResponse>>
                    .Fail(
                        "El servidor respondió, pero la composición no tiene el formato esperado.");
            }
            catch
            {
                return ApiResult<List<FuenteNutrienteResponse>>
                    .Fail(
                        "Ocurrió un error inesperado al cargar la composición.");
            }
        }
    }
}
