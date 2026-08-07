using CONATRADEC.Models;
using System.Net.Http.Json;
using System.Text.Json;

namespace CONATRADEC.Services
{
    public sealed class MotivoDevolucionTecnicoApiService
    {
        private const string RutaBase =
            "api/configuracion/motivos-devolucion-tecnico";

        private static readonly SemaphoreSlim CacheLock = new(1, 1);
        private static List<MotivoDevolucionTecnicoItem>? cacheActivos;
        private static DateTime cacheUtc;
        private static readonly TimeSpan DuracionCache = TimeSpan.FromMinutes(15);

        private readonly HttpClient client = ApiClientService.Client;

        public async Task<ApiResult<List<MotivoDevolucionTecnicoItem>>>
            ListarActivosAsync(
                bool forzar = false,
                CancellationToken cancellationToken = default)
        {
            if (!forzar && CacheVigente())
            {
                return ApiResult<List<MotivoDevolucionTecnicoItem>>.Ok(
                    CrearCopiaCache());
            }

            await CacheLock.WaitAsync(cancellationToken);
            try
            {
                if (!forzar && CacheVigente())
                {
                    return ApiResult<List<MotivoDevolucionTecnicoItem>>.Ok(
                        CrearCopiaCache());
                }

                ApiResult<List<MotivoDevolucionTecnicoItem>> resultado =
                    await ObtenerListaAsync(
                        $"{RutaBase}/activos",
                        cancellationToken);

                if (resultado.Success && resultado.Data != null)
                {
                    cacheActivos = resultado.Data
                        .Where(item => item.Activo)
                        .OrderBy(item => item.Orden)
                        .ThenBy(item => item.Nombre)
                        .ToList();
                    cacheUtc = DateTime.UtcNow;

                    return ApiResult<List<MotivoDevolucionTecnicoItem>>.Ok(
                        CrearCopiaCache());
                }

                return resultado;
            }
            finally
            {
                CacheLock.Release();
            }
        }

        public Task<ApiResult<List<MotivoDevolucionTecnicoItem>>>
            ListarAdministracionAsync(
                bool incluirInactivos,
                string? buscar,
                CancellationToken cancellationToken = default)
        {
            string ruta =
                $"{RutaBase}?incluirInactivos={incluirInactivos.ToString().ToLowerInvariant()}";

            if (!string.IsNullOrWhiteSpace(buscar))
                ruta += $"&buscar={Uri.EscapeDataString(buscar.Trim())}";

            return ObtenerListaAsync(ruta, cancellationToken);
        }

        public Task<ApiResult<MotivoDevolucionTecnicoItem>> CrearAsync(
            MotivoDevolucionTecnicoRequest request,
            CancellationToken cancellationToken = default) =>
            EnviarAsync(HttpMethod.Post, RutaBase, request, cancellationToken);

        public Task<ApiResult<MotivoDevolucionTecnicoItem>> ActualizarAsync(
            int id,
            MotivoDevolucionTecnicoRequest request,
            CancellationToken cancellationToken = default) =>
            EnviarAsync(HttpMethod.Put, $"{RutaBase}/{id}", request, cancellationToken);

        public Task<ApiResult<bool>> EliminarAsync(
            int id,
            CancellationToken cancellationToken = default) =>
            CambiarEstadoAsync($"{RutaBase}/{id}/eliminar", cancellationToken);

        public Task<ApiResult<bool>> RecuperarAsync(
            int id,
            CancellationToken cancellationToken = default) =>
            CambiarEstadoAsync($"{RutaBase}/{id}/recuperar", cancellationToken);

        public static void LimpiarCache()
        {
            cacheActivos = null;
            cacheUtc = default;
        }

        private async Task<ApiResult<List<MotivoDevolucionTecnicoItem>>>
            ObtenerListaAsync(
                string ruta,
                CancellationToken cancellationToken)
        {
            try
            {
                using HttpResponseMessage response =
                    await client.GetAsync(ruta, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    return ApiResult<List<MotivoDevolucionTecnicoItem>>.Fail(
                        await ApiServiceHelper.ReadResponseMessageAsync(
                            response,
                            "No fue posible cargar los motivos de devolución.",
                            cancellationToken),
                        (int)response.StatusCode);
                }

                List<MotivoDevolucionTecnicoItem>? data =
                    await response.Content.ReadFromJsonAsync<
                        List<MotivoDevolucionTecnicoItem>>(
                            cancellationToken: cancellationToken);

                return ApiResult<List<MotivoDevolucionTecnicoItem>>.Ok(
                    data ?? []);
            }
            catch (OperationCanceledException)
            {
                return ApiResult<List<MotivoDevolucionTecnicoItem>>.Fail(
                    "La operación fue cancelada.");
            }
            catch (HttpRequestException)
            {
                return ApiResult<List<MotivoDevolucionTecnicoItem>>.Fail(
                    "No fue posible comunicarse con el servidor.");
            }
            catch (JsonException)
            {
                return ApiResult<List<MotivoDevolucionTecnicoItem>>.Fail(
                    "El servidor respondió con un formato no esperado.");
            }
        }

        private async Task<ApiResult<MotivoDevolucionTecnicoItem>> EnviarAsync(
            HttpMethod method,
            string ruta,
            MotivoDevolucionTecnicoRequest request,
            CancellationToken cancellationToken)
        {
            try
            {
                using var mensaje = new HttpRequestMessage(method, ruta)
                {
                    Content = JsonContent.Create(request)
                };
                using HttpResponseMessage response =
                    await client.SendAsync(mensaje, cancellationToken);

                ApiEnvelope<MotivoDevolucionTecnicoItem>? envelope =
                    await LeerEnvelopeAsync<MotivoDevolucionTecnicoItem>(
                        response,
                        cancellationToken);

                if (!response.IsSuccessStatusCode || envelope?.Data == null)
                {
                    return ApiResult<MotivoDevolucionTecnicoItem>.Fail(
                        envelope?.Message ??
                        "No fue posible guardar el motivo de devolución.",
                        (int)response.StatusCode);
                }

                LimpiarCache();
                return ApiResult<MotivoDevolucionTecnicoItem>.Ok(
                    envelope.Data,
                    envelope.Message);
            }
            catch (OperationCanceledException)
            {
                return ApiResult<MotivoDevolucionTecnicoItem>.Fail(
                    "La operación fue cancelada.");
            }
            catch (HttpRequestException)
            {
                return ApiResult<MotivoDevolucionTecnicoItem>.Fail(
                    "No fue posible comunicarse con el servidor.");
            }
            catch (JsonException)
            {
                return ApiResult<MotivoDevolucionTecnicoItem>.Fail(
                    "El servidor respondió con un formato no esperado.");
            }
        }

        private async Task<ApiResult<bool>> CambiarEstadoAsync(
            string ruta,
            CancellationToken cancellationToken)
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Put, ruta)
                {
                    Content = JsonContent.Create(new { })
                };
                using HttpResponseMessage response =
                    await client.SendAsync(request, cancellationToken);
                ApiEnvelope<object>? envelope =
                    await LeerEnvelopeAsync<object>(response, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    return ApiResult<bool>.Fail(
                        envelope?.Message ??
                        "No fue posible cambiar el estado del motivo.",
                        (int)response.StatusCode);
                }

                LimpiarCache();
                return ApiResult<bool>.Ok(true, envelope?.Message ?? string.Empty);
            }
            catch (OperationCanceledException)
            {
                return ApiResult<bool>.Fail("La operación fue cancelada.");
            }
            catch (HttpRequestException)
            {
                return ApiResult<bool>.Fail(
                    "No fue posible comunicarse con el servidor.");
            }
        }

        private static async Task<ApiEnvelope<T>?> LeerEnvelopeAsync<T>(
            HttpResponseMessage response,
            CancellationToken cancellationToken)
            where T : class
        {
            try
            {
                return await response.Content.ReadFromJsonAsync<ApiEnvelope<T>>(
                    cancellationToken: cancellationToken);
            }
            catch
            {
                return null;
            }
        }

        private static bool CacheVigente() =>
            cacheActivos != null && DateTime.UtcNow - cacheUtc < DuracionCache;

        private static List<MotivoDevolucionTecnicoItem> CrearCopiaCache() =>
            cacheActivos?.ToList() ?? [];

        private sealed class ApiEnvelope<T>
            where T : class
        {
            public bool Success { get; set; }
            public string Message { get; set; } = string.Empty;
            public T? Data { get; set; }
        }
    }
}
