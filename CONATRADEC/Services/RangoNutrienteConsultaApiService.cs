using CONATRADEC.Models;
using System.Net.Http.Json;
using System.Text.Json;

namespace CONATRADEC.Services
{
    /// <summary>
    /// Servicio paginado para las pantallas administrativas de rangos.
    /// El CRUD existente permanece en RangoNutrienteApiService.
    /// </summary>
    public sealed class RangoNutrienteConsultaApiService
    {
        private readonly HttpClient httpClient;

        private readonly JsonSerializerOptions jsonOptions =
            new(JsonSerializerDefaults.Web)
            {
                PropertyNameCaseInsensitive = true
            };

        public RangoNutrienteConsultaApiService()
            : this(ApiClientService.Client)
        {
        }

        public RangoNutrienteConsultaApiService(HttpClient httpClient)
        {
            this.httpClient =
                httpClient
                ?? throw new ArgumentNullException(nameof(httpClient));
        }

        public async Task<ApiResult<RangoNutrienteCategoriaPaginaResponse>>
            BuscarCultivosAsync(
                string? buscar,
                int pagina,
                int tamanoPagina,
                CancellationToken cancellationToken = default)
        {
            string ruta =
                "api/configuracion/rangos-nutrientes/cultivos" +
                $"?pagina={Math.Max(1, pagina)}" +
                $"&tamanoPagina={Math.Clamp(tamanoPagina, 5, 100)}";

            if (!string.IsNullOrWhiteSpace(buscar))
            {
                ruta +=
                    $"&buscar={Uri.EscapeDataString(buscar.Trim())}";
            }

            return await GetAsync<RangoNutrienteCategoriaPaginaResponse>(
                ruta,
                "No fue posible cargar los tipos de cultivo.",
                cancellationToken);
        }

        public async Task<ApiResult<RangoNutrientePaginaResponse>>
            BuscarRangosAsync(
                int tipoCultivoId,
                string? buscar,
                int pagina,
                int tamanoPagina,
                CancellationToken cancellationToken = default)
        {
            if (tipoCultivoId <= 0)
            {
                return ApiResult<RangoNutrientePaginaResponse>.Fail(
                    "El tipo de cultivo indicado no es válido.");
            }

            string ruta =
                "api/configuracion/rangos-nutrientes/buscar" +
                $"?tipoCultivoId={tipoCultivoId}" +
                $"&pagina={Math.Max(1, pagina)}" +
                $"&tamanoPagina={Math.Clamp(tamanoPagina, 5, 100)}";

            if (!string.IsNullOrWhiteSpace(buscar))
            {
                ruta +=
                    $"&buscar={Uri.EscapeDataString(buscar.Trim())}";
            }

            return await GetAsync<RangoNutrientePaginaResponse>(
                ruta,
                "No fue posible cargar los rangos nutricionales.",
                cancellationToken);
        }

        public async Task<ApiResult<List<ElementoQuimicoSelectorItem>>>
            ObtenerElementosDisponiblesAsync(
                int tipoCultivoId,
                int parametroActualId,
                CancellationToken cancellationToken = default)
        {
            if (tipoCultivoId <= 0)
            {
                return ApiResult<List<ElementoQuimicoSelectorItem>>.Fail(
                    "El tipo de cultivo indicado no es válido.");
            }

            string ruta =
                "api/configuracion/rangos-nutrientes/" +
                "elementos-disponibles" +
                $"?tipoCultivoId={tipoCultivoId}" +
                $"&parametroActualId={Math.Max(0, parametroActualId)}";

            return await GetAsync<List<ElementoQuimicoSelectorItem>>(
                ruta,
                "No fue posible cargar los elementos químicos disponibles.",
                cancellationToken);
        }

        private async Task<ApiResult<T>> GetAsync<T>(
            string ruta,
            string mensajeError,
            CancellationToken cancellationToken)
            where T : class, new()
        {
            try
            {
                using HttpResponseMessage response =
                    await httpClient.GetAsync(
                        ruta,
                        cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    return ApiResult<T>.Fail(
                        await ApiServiceHelper.ReadResponseMessageAsync(
                            response,
                            mensajeError,
                            cancellationToken),
                        (int)response.StatusCode);
                }

                T? data =
                    await response.Content.ReadFromJsonAsync<T>(
                        jsonOptions,
                        cancellationToken);

                return ApiResult<T>.Ok(data ?? new T());
            }
            catch (TaskCanceledException)
                when (!cancellationToken.IsCancellationRequested)
            {
                return ApiResult<T>.Fail(
                    "La consulta tardó demasiado. Intente nuevamente.");
            }
            catch (OperationCanceledException)
            {
                return ApiResult<T>.Fail(
                    "La operación fue cancelada.");
            }
            catch (HttpRequestException)
            {
                return ApiResult<T>.Fail(
                    "No fue posible comunicarse con el servidor.");
            }
            catch (JsonException)
            {
                return ApiResult<T>.Fail(
                    "El servidor respondió con un formato inesperado.");
            }
            catch
            {
                return ApiResult<T>.Fail(
                    "Ocurrió un error inesperado al consultar los rangos.");
            }
        }
    }
}
