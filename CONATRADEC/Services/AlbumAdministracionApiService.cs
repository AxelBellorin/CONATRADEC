using CONATRADEC.Models;
using System.Net.Http.Json;
using System.Text.Json;

namespace CONATRADEC.Services
{
    /// <summary>
    /// Cliente de los endpoints administrativos nuevos del Álbum Botánico.
    /// Se mantiene separado de AlbumJerarquiaApiService porque este último es
    /// utilizado también por la sincronización offline y Diagnóstico IA.
    /// </summary>
    public sealed class AlbumAdministracionApiService
    {
        private readonly HttpClient httpClient;
        private readonly UrlApiService urlApiService;

        private static readonly JsonSerializerOptions JsonOptions =
            new(JsonSerializerDefaults.Web)
            {
                PropertyNameCaseInsensitive = true
            };

        public AlbumAdministracionApiService()
            : this(ApiClientService.Client)
        {
        }

        public AlbumAdministracionApiService(HttpClient client)
        {
            httpClient = client ??
                throw new ArgumentNullException(nameof(client));
            urlApiService = new UrlApiService();
        }

        public async Task<ApiResult<AlbumInicioJerarquiaResponse>>
            GetContextoAsync(
                int? categoriaId,
                int? subcategoriaId,
                string? buscar,
                int pagina,
                int tamanoPagina,
                CancellationToken cancellationToken = default)
        {
            string route = ConstruirRuta(
                "api/album-administracion/contexto",
                categoriaId,
                subcategoriaId,
                buscar,
                pagina,
                tamanoPagina);

            ApiResult<AlbumInicioJerarquiaResponse> result =
                await GetAsync<AlbumInicioJerarquiaResponse>(
                    route,
                    "cargar el contexto administrativo del álbum",
                    cancellationToken);

            if (result.Success && result.Data != null)
                PrepararContexto(result.Data);

            return result;
        }

        public async Task<ApiResult<AlbumGaleriaJerarquiaPaginaResponse>>
            GetPaginaAsync(
                int? categoriaId,
                int? subcategoriaId,
                string? buscar,
                int pagina,
                int tamanoPagina,
                CancellationToken cancellationToken = default)
        {
            string route = ConstruirRuta(
                "api/album-administracion/pagina",
                categoriaId,
                subcategoriaId,
                buscar,
                pagina,
                tamanoPagina);

            ApiResult<AlbumGaleriaJerarquiaPaginaResponse> result =
                await GetAsync<AlbumGaleriaJerarquiaPaginaResponse>(
                    route,
                    "cargar la página del álbum",
                    cancellationToken);

            if (result.Success && result.Data != null)
                PrepararPagina(result.Data);

            return result;
        }

        public async Task<ApiResult<AlbumGaleriaJerarquiaPaginaResponse>>
            GetEliminadosAsync(
                string? buscar,
                int pagina,
                int tamanoPagina,
                CancellationToken cancellationToken = default)
        {
            var query = new List<string>
            {
                $"pagina={Math.Max(1, pagina)}",
                $"tamanoPagina={Math.Clamp(tamanoPagina, 1, 30)}"
            };

            if (!string.IsNullOrWhiteSpace(buscar))
            {
                query.Add(
                    "buscar=" +
                    Uri.EscapeDataString(buscar.Trim()));
            }

            string route =
                "api/album-administracion/eliminados?" +
                string.Join("&", query);

            ApiResult<AlbumGaleriaJerarquiaPaginaResponse> result =
                await GetAsync<AlbumGaleriaJerarquiaPaginaResponse>(
                    route,
                    "cargar las subcategorías eliminadas",
                    cancellationToken);

            if (result.Success && result.Data != null)
                PrepararPagina(result.Data);

            return result;
        }

        public Task<ApiResult<bool>> ReactivarAsync(
            int id,
            CancellationToken cancellationToken = default)
        {
            if (id <= 0)
            {
                return Task.FromResult(
                    ApiResult<bool>.Fail(
                        "La subcategoría seleccionada no es válida."));
            }

            return SendWithoutDataAsync(
                HttpMethod.Put,
                $"api/album-administracion/eliminados/{id}/reactivar",
                "reactivar la subcategoría",
                cancellationToken);
        }

        private static string ConstruirRuta(
            string baseRoute,
            int? categoriaId,
            int? subcategoriaId,
            string? buscar,
            int pagina,
            int tamanoPagina)
        {
            var query = new List<string>
            {
                $"pagina={Math.Max(1, pagina)}",
                $"tamanoPagina={Math.Clamp(tamanoPagina, 1, 30)}"
            };

            if (categoriaId is > 0)
                query.Add($"categoriaId={categoriaId.Value}");

            if (subcategoriaId is > 0)
                query.Add($"subcategoriaId={subcategoriaId.Value}");

            if (!string.IsNullOrWhiteSpace(buscar))
            {
                query.Add(
                    "buscar=" +
                    Uri.EscapeDataString(buscar.Trim()));
            }

            return baseRoute + "?" + string.Join("&", query);
        }

        private void PrepararContexto(AlbumInicioJerarquiaResponse contexto)
        {
            foreach (CategoriaAlbumBotanicoResponse categoria in
                contexto.Categorias)
            {
                categoria.ImagenPortadaUrl =
                    ConstruirUrlCompleta(categoria.RutaImagenPortada);
            }

            PrepararPagina(contexto.Galeria);
        }

        private void PrepararPagina(
            AlbumGaleriaJerarquiaPaginaResponse pagina)
        {
            foreach (AlbumGaleriaJerarquiaItemResponse item in pagina.Items)
            {
                item.FotoPortadaUrl =
                    ConstruirUrlCompleta(item.FotoPortada);
            }
        }

        private string ConstruirUrlCompleta(string? ruta)
        {
            if (string.IsNullOrWhiteSpace(ruta))
                return string.Empty;

            if (ruta.StartsWith(
                    "http",
                    StringComparison.OrdinalIgnoreCase))
            {
                return ruta;
            }

            return
                $"{urlApiService.BaseUrlApi.TrimEnd('/')}/" +
                $"{ruta.TrimStart('/')}";
        }

        private async Task<ApiResult<T>> GetAsync<T>(
            string route,
            string action,
            CancellationToken cancellationToken)
        {
            try
            {
                using HttpResponseMessage response = await httpClient.GetAsync(
                    route,
                    cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    return ApiResult<T>.Fail(
                        await LeerMensajeErrorAsync(
                            response,
                            action,
                            cancellationToken),
                        (int)response.StatusCode);
                }

                ApiEnvelope<T>? envelope = await response.Content
                    .ReadFromJsonAsync<ApiEnvelope<T>>(
                        JsonOptions,
                        cancellationToken);

                if (envelope == null ||
                    !envelope.Success ||
                    envelope.Data == null)
                {
                    return ApiResult<T>.Fail(
                        envelope?.Message ??
                        "El servidor no devolvió los datos esperados.");
                }

                return ApiResult<T>.Ok(
                    envelope.Data,
                    envelope.Message);
            }
            catch (TaskCanceledException)
                when (!cancellationToken.IsCancellationRequested)
            {
                return ApiResult<T>.Fail(
                    "La solicitud tardó demasiado. Verifique su conexión.");
            }
            catch (OperationCanceledException)
            {
                return ApiResult<T>.Fail("La operación fue cancelada.");
            }
            catch (HttpRequestException)
            {
                return ApiResult<T>.Fail(
                    "No fue posible conectarse con el servidor.");
            }
            catch (JsonException)
            {
                return ApiResult<T>.Fail(
                    "El servidor respondió con un formato no válido.");
            }
            catch (Exception)
            {
                return ApiResult<T>.Fail(
                    $"Ocurrió un error inesperado al {action}.");
            }
        }

        private async Task<ApiResult<bool>> SendWithoutDataAsync(
            HttpMethod method,
            string route,
            string action,
            CancellationToken cancellationToken)
        {
            try
            {
                using var request = new HttpRequestMessage(method, route);
                using HttpResponseMessage response = await httpClient.SendAsync(
                    request,
                    cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    return ApiResult<bool>.Fail(
                        await LeerMensajeErrorAsync(
                            response,
                            action,
                            cancellationToken),
                        (int)response.StatusCode);
                }

                string mensaje = await LeerMensajeExitoAsync(
                    response,
                    cancellationToken);

                return ApiResult<bool>.Ok(true, mensaje);
            }
            catch (TaskCanceledException)
                when (!cancellationToken.IsCancellationRequested)
            {
                return ApiResult<bool>.Fail(
                    "La solicitud tardó demasiado. Verifique su conexión.");
            }
            catch (OperationCanceledException)
            {
                return ApiResult<bool>.Fail("La operación fue cancelada.");
            }
            catch (HttpRequestException)
            {
                return ApiResult<bool>.Fail(
                    "No fue posible conectarse con el servidor.");
            }
            catch (Exception)
            {
                return ApiResult<bool>.Fail(
                    $"Ocurrió un error inesperado al {action}.");
            }
        }

        private static async Task<string> LeerMensajeErrorAsync(
            HttpResponseMessage response,
            string action,
            CancellationToken cancellationToken)
        {
            try
            {
                ApiEnvelope<JsonElement>? envelope = await response.Content
                    .ReadFromJsonAsync<ApiEnvelope<JsonElement>>(
                        JsonOptions,
                        cancellationToken);

                if (!string.IsNullOrWhiteSpace(envelope?.Message))
                    return envelope.Message;
            }
            catch
            {
            }

            return $"No fue posible {action}.";
        }

        private static async Task<string> LeerMensajeExitoAsync(
            HttpResponseMessage response,
            CancellationToken cancellationToken)
        {
            try
            {
                ApiEnvelope<JsonElement>? envelope = await response.Content
                    .ReadFromJsonAsync<ApiEnvelope<JsonElement>>(
                        JsonOptions,
                        cancellationToken);

                return envelope?.Message ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
