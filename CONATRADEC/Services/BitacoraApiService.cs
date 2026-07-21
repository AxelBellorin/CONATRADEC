using CONATRADEC.Models;
using System.Net;
using System.Net.Http.Json;

namespace CONATRADEC.Services
{
    public sealed class BitacoraApiService
    {
        private readonly HttpClient httpClient;

        public BitacoraApiService() : this(ApiClientService.Client)
        {
        }

        public BitacoraApiService(HttpClient httpClient)
        {
            this.httpClient = httpClient ??
                throw new ArgumentNullException(nameof(httpClient));
        }

        public Task<ApiResult<BitacoraPaginadaResponse>> ListarAsync(
            DateTime fechaDesdeUtc,
            DateTime fechaHastaUtc,
            int? usuarioId,
            string? accion,
            string? modulo,
            bool? exitoso,
            string? buscar,
            int pagina,
            int tamanoPagina,
            CancellationToken cancellationToken = default)
        {
            var parametros = new List<string>
            {
                $"fechaDesdeUtc={Uri.EscapeDataString(fechaDesdeUtc.ToString("O"))}",
                $"fechaHastaUtc={Uri.EscapeDataString(fechaHastaUtc.ToString("O"))}",
                $"pagina={pagina}",
                $"tamanoPagina={tamanoPagina}"
            };

            if (usuarioId.HasValue)
                parametros.Add($"usuarioId={usuarioId.Value}");

            if (!string.IsNullOrWhiteSpace(accion))
                parametros.Add($"accion={Uri.EscapeDataString(accion)}");

            if (!string.IsNullOrWhiteSpace(modulo))
                parametros.Add($"modulo={Uri.EscapeDataString(modulo)}");

            if (exitoso.HasValue)
                parametros.Add($"exitoso={exitoso.Value.ToString().ToLowerInvariant()}");

            if (!string.IsNullOrWhiteSpace(buscar))
                parametros.Add($"buscar={Uri.EscapeDataString(buscar.Trim())}");

            return GetAsync<BitacoraPaginadaResponse>(
                "api/bitacora?" + string.Join("&", parametros),
                cancellationToken);
        }

        public Task<ApiResult<BitacoraDetalleItem>> ObtenerAsync(
            Guid bitacoraId,
            CancellationToken cancellationToken = default) =>
            GetAsync<BitacoraDetalleItem>(
                $"api/bitacora/{bitacoraId}",
                cancellationToken);

        public Task<ApiResult<BitacoraCatalogosResponse>> CatalogosAsync(
            CancellationToken cancellationToken = default) =>
            GetAsync<BitacoraCatalogosResponse>(
                "api/bitacora/catalogos",
                cancellationToken);

        private async Task<ApiResult<T>> GetAsync<T>(
            string endpoint,
            CancellationToken cancellationToken)
        {
            try
            {
                using HttpResponseMessage response = await httpClient.GetAsync(
                    endpoint,
                    cancellationToken);

                if (response.StatusCode == HttpStatusCode.Forbidden)
                {
                    return ApiResult<T>.Fail(
                        "No tiene permiso para consultar la bitácora.",
                        (int)response.StatusCode);
                }

                if (!response.IsSuccessStatusCode)
                {
                    string contenido = await response.Content.ReadAsStringAsync(
                        cancellationToken);

                    return ApiResult<T>.Fail(
                        string.IsNullOrWhiteSpace(contenido)
                            ? "No fue posible consultar la bitácora."
                            : contenido,
                        (int)response.StatusCode);
                }

                T? data = await response.Content.ReadFromJsonAsync<T>(
                    cancellationToken: cancellationToken);

                return data == null
                    ? ApiResult<T>.Fail(
                        "La API no devolvió información de bitácora.")
                    : ApiResult<T>.Ok(data);
            }
            catch (TaskCanceledException)
            {
                return ApiResult<T>.Fail(
                    "La consulta tardó demasiado. Revise su conexión.");
            }
            catch (HttpRequestException)
            {
                return ApiResult<T>.Fail(
                    "No fue posible conectarse con la API.");
            }
            catch (Exception)
            {
                return ApiResult<T>.Fail(
                    "Ocurrió un error inesperado al consultar la bitácora.");
            }
        }
    }
}
