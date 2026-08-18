using CONATRADEC.Models;
using System.Net.Http.Json;

namespace CONATRADEC.Services
{
    public sealed class BitacoraApiService
    {
        private const string RutaBase = "api/bitacora/v2";
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
            DateTime? corteConsultaUtc = null,
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
                parametros.Add($"accion={Uri.EscapeDataString(accion.Trim())}");

            if (!string.IsNullOrWhiteSpace(modulo))
                parametros.Add($"modulo={Uri.EscapeDataString(modulo.Trim())}");

            if (exitoso.HasValue)
                parametros.Add($"exitoso={exitoso.Value.ToString().ToLowerInvariant()}");

            if (!string.IsNullOrWhiteSpace(buscar))
                parametros.Add($"buscar={Uri.EscapeDataString(buscar.Trim())}");

            if (corteConsultaUtc.HasValue)
            {
                parametros.Add(
                    $"corteConsultaUtc={Uri.EscapeDataString(corteConsultaUtc.Value.ToString("O"))}");
            }

            return GetAsync<BitacoraPaginadaResponse>(
                RutaBase + "?" + string.Join("&", parametros),
                cancellationToken);
        }

        public Task<ApiResult<BitacoraDetalleItem>> ObtenerAsync(
            Guid bitacoraId,
            CancellationToken cancellationToken = default) =>
            GetAsync<BitacoraDetalleItem>(
                $"{RutaBase}/{bitacoraId}",
                cancellationToken);

        public Task<ApiResult<BitacoraCatalogosResponse>> CatalogosAsync(
            CancellationToken cancellationToken = default) =>
            GetAsync<BitacoraCatalogosResponse>(
                $"{RutaBase}/catalogos",
                cancellationToken);

        private async Task<ApiResult<T>> GetAsync<T>(
            string endpoint,
            CancellationToken cancellationToken)
        {
            try
            {
                SesionInactividadService.Instance.RegistrarActividad();

                using HttpResponseMessage response = await httpClient.GetAsync(
                    endpoint,
                    cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    return ApiResult<T>.Fail(
                        await ApiServiceHelper.ReadResponseMessageAsync(
                            response,
                            "No fue posible consultar la bitácora.",
                            cancellationToken),
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
                when (!cancellationToken.IsCancellationRequested)
            {
                return ApiResult<T>.Fail(
                    "La consulta tardó demasiado. Revise su conexión e intente nuevamente.");
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                throw;
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
