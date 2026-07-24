using CONATRADEC.Models;
using System.Net.Http.Json;
using System.Text.Json;

namespace CONATRADEC.Services
{
    /// <summary>
    /// Servicio específico para la carga progresiva del álbum. Se mantiene
    /// separado del servicio original para no alterar los demás endpoints.
    /// </summary>
    public sealed class AlbumBotanicoCargaApiService
    {
        private readonly HttpClient httpClient;
        private readonly UrlApiService urlApiService = new();

        private static readonly JsonSerializerOptions JsonOptions =
            new(JsonSerializerDefaults.Web)
            {
                PropertyNameCaseInsensitive = true
            };

        public AlbumBotanicoCargaApiService()
            : this(ApiClientService.Client)
        {
        }

        public AlbumBotanicoCargaApiService(HttpClient httpClient)
        {
            this.httpClient = httpClient ??
                throw new ArgumentNullException(nameof(httpClient));
        }

        public async Task<ApiResult<AlbumInicioResponse>>
            GetInicioAsync(
                int tamanoPagina,
                CancellationToken cancellationToken = default)
        {
            string route =
                "api/album-botanico/inicio" +
                $"?tamanoPagina={Math.Clamp(tamanoPagina, 1, 30)}";

            ApiResult<AlbumInicioResponse> result =
                await GetAsync<AlbumInicioResponse>(
                    route,
                    "cargar el inicio del álbum",
                    cancellationToken);

            if (result.Success && result.Data != null)
            {
                PrepararCategorias(result.Data.Categorias);
                PrepararRegistros(result.Data.Galeria.Items);
            }

            return result;
        }

        public async Task<ApiResult<AlbumGaleriaPaginaResponse>>
            GetPaginaAsync(
                int? categoriaId,
                string? buscar,
                bool incluirInactivos,
                int pagina,
                int tamanoPagina,
                CancellationToken cancellationToken = default)
        {
            var query = new List<string>
            {
                $"pagina={Math.Max(1, pagina)}",
                $"tamanoPagina={Math.Clamp(tamanoPagina, 1, 30)}",
                "incluirInactivos=" +
                    incluirInactivos.ToString().ToLowerInvariant()
            };

            if (categoriaId.HasValue && categoriaId.Value > 0)
                query.Add($"categoriaId={categoriaId.Value}");

            if (!string.IsNullOrWhiteSpace(buscar))
            {
                query.Add(
                    "buscar=" +
                    Uri.EscapeDataString(buscar.Trim()));
            }

            string route =
                "api/album-botanico/galeria-paginada?" +
                string.Join("&", query);

            ApiResult<AlbumGaleriaPaginaResponse> result =
                await GetAsync<AlbumGaleriaPaginaResponse>(
                    route,
                    "cargar la página del álbum",
                    cancellationToken);

            if (result.Success && result.Data != null)
                PrepararRegistros(result.Data.Items);

            return result;
        }

        private void PrepararCategorias(
            IEnumerable<CategoriaAlbumBotanicoResponse> categorias)
        {
            foreach (CategoriaAlbumBotanicoResponse categoria in categorias)
            {
                categoria.ImagenPortadaUrl =
                    ConstruirUrlCompleta(
                        categoria.RutaImagenPortada);
            }
        }

        private void PrepararRegistros(
            IEnumerable<AlbumGaleriaItemResponse> registros)
        {
            foreach (AlbumGaleriaItemResponse item in registros)
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
                ruta.TrimStart('/');
        }

        private async Task<ApiResult<T>> GetAsync<T>(
            string route,
            string action,
            CancellationToken cancellationToken)
        {
            try
            {
                using HttpResponseMessage response =
                    await httpClient.GetAsync(
                        route,
                        HttpCompletionOption.ResponseHeadersRead,
                        cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    string content = await response.Content
                        .ReadAsStringAsync(cancellationToken);

                    string message = ApiErrorMessageParser.Parse(
                        response.StatusCode,
                        content,
                        $"No fue posible {action}.");

                    return ApiResult<T>.Fail(
                        message,
                        (int)response.StatusCode);
                }

                ApiEnvelope<T>? envelope =
                    await response.Content
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
            catch (OperationCanceledException)
            {
                return ApiResult<T>.Fail(
                    "La operación fue cancelada.");
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
    }
}
