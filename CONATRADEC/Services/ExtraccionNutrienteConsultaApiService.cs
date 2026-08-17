using CONATRADEC.Models;
using System.Net.Http.Json;
using System.Text.Json;

namespace CONATRADEC.Services
{
    /// <summary>
    /// Consultas administrativas de Extracción de nutrientes.
    /// Utiliza la API protegida para paginación y detalle fresco por ID.
    /// </summary>
    public sealed class ExtraccionNutrienteConsultaApiService
    {
        private const string RutaBase =
            "api/administracion/extraccion-nutrientes";

        private readonly HttpClient httpClient;

        private readonly JsonSerializerOptions jsonOptions =
            new(JsonSerializerDefaults.Web)
            {
                PropertyNameCaseInsensitive = true
            };

        public ExtraccionNutrienteConsultaApiService()
            : this(ApiClientService.Client)
        {
        }

        public ExtraccionNutrienteConsultaApiService(
            HttpClient httpClient)
        {
            this.httpClient = httpClient
                ?? throw new ArgumentNullException(
                    nameof(httpClient));
        }

        public async Task<ApiResult<ExtraccionNutrientePaginaResponse>> BuscarAsync(
            string? buscar,
            int pagina,
            int tamanoPagina,
            CancellationToken cancellationToken = default)
        {
            pagina = Math.Max(1, pagina);
            tamanoPagina = Math.Clamp(tamanoPagina, 5, 100);

            string ruta =
                RutaBase +
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
                    return ApiResult<ExtraccionNutrientePaginaResponse>.Fail(
                        await ApiServiceHelper.ReadResponseMessageAsync(
                            response,
                            "No fue posible cargar los parámetros de extracción.",
                            cancellationToken),
                        (int)response.StatusCode);
                }

                ExtraccionNutrientePaginaResponse? data =
                    await response.Content.ReadFromJsonAsync<
                        ExtraccionNutrientePaginaResponse>(
                        jsonOptions,
                        cancellationToken);

                return ApiResult<ExtraccionNutrientePaginaResponse>.Ok(
                    data ?? new ExtraccionNutrientePaginaResponse());
            }
            catch (TaskCanceledException)
                when (!cancellationToken.IsCancellationRequested)
            {
                return ApiResult<ExtraccionNutrientePaginaResponse>.Fail(
                    "La carga de parámetros tardó demasiado. Intente nuevamente.");
            }
            catch (OperationCanceledException)
            {
                return ApiResult<ExtraccionNutrientePaginaResponse>.Fail(
                    "La operación fue cancelada.");
            }
            catch (HttpRequestException)
            {
                return ApiResult<ExtraccionNutrientePaginaResponse>.Fail(
                    "No fue posible comunicarse con el servidor.");
            }
            catch (JsonException)
            {
                return ApiResult<ExtraccionNutrientePaginaResponse>.Fail(
                    "El servidor respondió con un formato inesperado.");
            }
            catch
            {
                return ApiResult<ExtraccionNutrientePaginaResponse>.Fail(
                    "Ocurrió un error inesperado al cargar los parámetros.");
            }
        }

        public async Task<ApiResult<ExtraccionNutrienteResponse>> GetByIdAsync(
            int id,
            CancellationToken cancellationToken = default)
        {
            if (id <= 0)
            {
                return ApiResult<ExtraccionNutrienteResponse>.Fail(
                    "El identificador del parámetro de extracción no es válido.");
            }

            try
            {
                using HttpResponseMessage response =
                    await httpClient.GetAsync(
                        $"{RutaBase}/{id}",
                        cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    return ApiResult<ExtraccionNutrienteResponse>.Fail(
                        await ApiServiceHelper.ReadResponseMessageAsync(
                            response,
                            "No fue posible cargar el parámetro de extracción.",
                            cancellationToken),
                        (int)response.StatusCode);
                }

                ExtraccionNutrienteResponse? data =
                    await response.Content.ReadFromJsonAsync<
                        ExtraccionNutrienteResponse>(
                        jsonOptions,
                        cancellationToken);

                return data?.ParametroExtraccionNutrienteCafeId is > 0
                    ? ApiResult<ExtraccionNutrienteResponse>.Ok(data)
                    : ApiResult<ExtraccionNutrienteResponse>.Fail(
                        "El servidor no devolvió un parámetro de extracción válido.");
            }
            catch (TaskCanceledException)
                when (!cancellationToken.IsCancellationRequested)
            {
                return ApiResult<ExtraccionNutrienteResponse>.Fail(
                    "La consulta tardó demasiado. Intente nuevamente.");
            }
            catch (OperationCanceledException)
            {
                return ApiResult<ExtraccionNutrienteResponse>.Fail(
                    "La operación fue cancelada.");
            }
            catch (HttpRequestException)
            {
                return ApiResult<ExtraccionNutrienteResponse>.Fail(
                    "No fue posible comunicarse con el servidor.");
            }
            catch (JsonException)
            {
                return ApiResult<ExtraccionNutrienteResponse>.Fail(
                    "El servidor respondió con un formato inesperado.");
            }
            catch
            {
                return ApiResult<ExtraccionNutrienteResponse>.Fail(
                    "Ocurrió un error inesperado al cargar el parámetro.");
            }
        }
    }
}
