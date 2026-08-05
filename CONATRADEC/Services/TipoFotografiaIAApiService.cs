using CONATRADEC.Models;
using System.Net.Http.Json;
using System.Text.Json;

namespace CONATRADEC.Services
{
    public sealed class TipoFotografiaIAApiService
    {
        private const string RutaBase =
            "api/configuracion/tipos-fotografia-ia";

        private static readonly SemaphoreSlim CacheLock = new(1, 1);
        private static List<TipoFotografiaIAItem>? cacheActivos;
        private static DateTime cacheUtc;
        private static readonly TimeSpan DuracionCache =
            TimeSpan.FromMinutes(15);

        private readonly HttpClient client;

        public TipoFotografiaIAApiService()
            : this(ApiClientService.Client)
        {
        }

        public TipoFotografiaIAApiService(HttpClient client)
        {
            this.client = client ??
                throw new ArgumentNullException(nameof(client));
        }

        public async Task<ApiResult<List<TipoFotografiaIAItem>>>
            ListarActivosAsync(
                bool forzar = false,
                CancellationToken cancellationToken = default)
        {
            if (!forzar && CacheVigente())
            {
                return ApiResult<List<TipoFotografiaIAItem>>.Ok(
                    CrearCopiaCache());
            }

            await CacheLock.WaitAsync(cancellationToken);

            try
            {
                if (!forzar && CacheVigente())
                {
                    return ApiResult<List<TipoFotografiaIAItem>>.Ok(
                        CrearCopiaCache());
                }

                ApiResult<List<TipoFotografiaIAItem>> result =
                    await ObtenerListaAsync(
                        $"{RutaBase}/activos",
                        cancellationToken);

                if (result.Success && result.Data != null)
                {
                    cacheActivos = result.Data
                        .Where(item => item.Activo)
                        .OrderBy(item => item.Orden)
                        .ThenBy(item => item.Nombre)
                        .ToList();
                    cacheUtc = DateTime.UtcNow;

                    return ApiResult<List<TipoFotografiaIAItem>>.Ok(
                        CrearCopiaCache());
                }

                return result;
            }
            finally
            {
                CacheLock.Release();
            }
        }

        public Task<ApiResult<List<TipoFotografiaIAItem>>>
            ListarAdministracionAsync(
                bool incluirInactivos,
                string? buscar,
                CancellationToken cancellationToken = default)
        {
            string ruta =
                $"{RutaBase}?incluirInactivos={incluirInactivos.ToString().ToLowerInvariant()}";

            if (!string.IsNullOrWhiteSpace(buscar))
            {
                ruta +=
                    $"&buscar={Uri.EscapeDataString(buscar.Trim())}";
            }

            return ObtenerListaAsync(ruta, cancellationToken);
        }

        public async Task<ApiResult<TipoFotografiaIAItem>> CrearAsync(
            TipoFotografiaIARequest request,
            CancellationToken cancellationToken = default) =>
            await EnviarAsync(
                HttpMethod.Post,
                RutaBase,
                request,
                cancellationToken);

        public async Task<ApiResult<TipoFotografiaIAItem>> ActualizarAsync(
            int id,
            TipoFotografiaIARequest request,
            CancellationToken cancellationToken = default) =>
            await EnviarAsync(
                HttpMethod.Put,
                $"{RutaBase}/{id}",
                request,
                cancellationToken);

        public async Task<ApiResult<bool>> EliminarAsync(
            int id,
            CancellationToken cancellationToken = default) =>
            await EnviarSinDatosAsync(
                $"{RutaBase}/{id}/eliminar",
                cancellationToken);

        public async Task<ApiResult<bool>> RecuperarAsync(
            int id,
            CancellationToken cancellationToken = default) =>
            await EnviarSinDatosAsync(
                $"{RutaBase}/{id}/recuperar",
                cancellationToken);

        public static void LimpiarCache()
        {
            cacheActivos = null;
            cacheUtc = default;
        }

        private async Task<ApiResult<List<TipoFotografiaIAItem>>>
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
                    return ApiResult<List<TipoFotografiaIAItem>>.Fail(
                        await ApiServiceHelper.ReadResponseMessageAsync(
                            response,
                            "No fue posible cargar los tipos de fotografía.",
                            cancellationToken),
                        (int)response.StatusCode);
                }

                List<TipoFotografiaIAItem>? data =
                    await response.Content.ReadFromJsonAsync<
                        List<TipoFotografiaIAItem>>(
                            cancellationToken: cancellationToken);

                return ApiResult<List<TipoFotografiaIAItem>>.Ok(
                    data ?? []);
            }
            catch (OperationCanceledException)
            {
                return ApiResult<List<TipoFotografiaIAItem>>.Fail(
                    "La operación fue cancelada.");
            }
            catch (HttpRequestException)
            {
                return ApiResult<List<TipoFotografiaIAItem>>.Fail(
                    "No fue posible comunicarse con el servidor.");
            }
            catch (JsonException)
            {
                return ApiResult<List<TipoFotografiaIAItem>>.Fail(
                    "El servidor respondió con un formato no esperado.");
            }
            catch
            {
                return ApiResult<List<TipoFotografiaIAItem>>.Fail(
                    "Ocurrió un error inesperado al cargar los tipos de fotografía.");
            }
        }

        private async Task<ApiResult<TipoFotografiaIAItem>> EnviarAsync(
            HttpMethod method,
            string ruta,
            TipoFotografiaIARequest request,
            CancellationToken cancellationToken)
        {
            try
            {
                using var httpRequest = new HttpRequestMessage(method, ruta)
                {
                    Content = JsonContent.Create(request)
                };

                using HttpResponseMessage response =
                    await client.SendAsync(httpRequest, cancellationToken);

                ApiEnvelope<TipoFotografiaIAItem>? envelope =
                    await LeerEnvelopeAsync<TipoFotografiaIAItem>(
                        response,
                        cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    return ApiResult<TipoFotografiaIAItem>.Fail(
                        envelope?.Message ??
                        "No fue posible guardar el tipo de fotografía.",
                        (int)response.StatusCode);
                }

                if (envelope?.Data == null)
                {
                    return ApiResult<TipoFotografiaIAItem>.Fail(
                        "El servidor no devolvió el tipo de fotografía guardado.");
                }

                LimpiarCache();
                return ApiResult<TipoFotografiaIAItem>.Ok(
                    envelope.Data,
                    envelope.Message);
            }
            catch (Exception ex) when (
                ex is HttpRequestException or
                JsonException or
                OperationCanceledException)
            {
                return ApiResult<TipoFotografiaIAItem>.Fail(
                    ex is OperationCanceledException
                        ? "La operación fue cancelada."
                        : "No fue posible completar la operación contra el servidor.");
            }
        }

        private async Task<ApiResult<bool>> EnviarSinDatosAsync(
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
                        "No fue posible actualizar el estado del tipo de fotografía.",
                        (int)response.StatusCode);
                }

                LimpiarCache();
                return ApiResult<bool>.Ok(
                    true,
                    envelope?.Message ?? string.Empty);
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
            catch (JsonException)
            {
                return ApiResult<bool>.Fail(
                    "El servidor respondió con un formato no esperado.");
            }
        }

        private static async Task<ApiEnvelope<T>?> LeerEnvelopeAsync<T>(
            HttpResponseMessage response,
            CancellationToken cancellationToken)
            where T : class
        {
            try
            {
                return await response.Content.ReadFromJsonAsync<
                    ApiEnvelope<T>>(
                        cancellationToken: cancellationToken);
            }
            catch
            {
                return null;
            }
        }

        private static bool CacheVigente() =>
            cacheActivos != null &&
            DateTime.UtcNow - cacheUtc < DuracionCache;

        private static List<TipoFotografiaIAItem> CrearCopiaCache() =>
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
