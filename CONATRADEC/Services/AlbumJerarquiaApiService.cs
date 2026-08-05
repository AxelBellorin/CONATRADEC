using CONATRADEC.Models;
using System.Net.Http.Json;
using System.Text.Json;

namespace CONATRADEC.Services
{
    /// <summary>
    /// Cliente del nivel jerárquico del Álbum Botánico. Todas las consultas
    /// por página o inspección se realizan en lote para evitar N+1.
    /// </summary>
    public sealed class AlbumJerarquiaApiService
    {
        private readonly HttpClient httpClient;
        private readonly UrlApiService urlApiService;

        private static readonly JsonSerializerOptions JsonOptions =
            new(JsonSerializerDefaults.Web)
            {
                PropertyNameCaseInsensitive = true
            };

        public AlbumJerarquiaApiService()
            : this(ApiClientService.Client)
        {
        }

        public AlbumJerarquiaApiService(HttpClient client)
        {
            httpClient = client ??
                throw new ArgumentNullException(nameof(client));
            urlApiService = new UrlApiService();
        }

        public async Task<ApiResult<AlbumInicioJerarquiaResponse>>
            GetInicioAsync(
                int tamanoPagina,
                CancellationToken cancellationToken = default)
        {
            ApiResult<AlbumInicioJerarquiaResponse> result =
                await GetAsync<AlbumInicioJerarquiaResponse>(
                    "api/album-jerarquia/inicio" +
                        $"?tamanoPagina={Math.Clamp(tamanoPagina, 1, 30)}",
                    "cargar el álbum jerárquico",
                    cancellationToken);

            if (result.Success && result.Data != null)
                PrepararInicio(result.Data);

            return result;
        }

        public async Task<ApiResult<AlbumGaleriaJerarquiaPaginaResponse>>
            GetPaginaAsync(
                int? categoriaId,
                int? subcategoriaId,
                string? buscar,
                bool incluirInactivos,
                int pagina,
                int tamanoPagina,
                CancellationToken cancellationToken = default)
        {
            var query = new List<string>
            {
                "incluirInactivos=" +
                    incluirInactivos.ToString().ToLowerInvariant(),
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

            ApiResult<AlbumGaleriaJerarquiaPaginaResponse> result =
                await GetAsync<AlbumGaleriaJerarquiaPaginaResponse>(
                    "api/album-jerarquia/galeria-paginada?" +
                        string.Join("&", query),
                    "cargar la página del álbum",
                    cancellationToken);

            if (result.Success && result.Data != null)
                PrepararPagina(result.Data);

            return result;
        }

        public Task<ApiResult<List<SubcategoriaAlbumBotanicoResponse>>>
            GetSubcategoriasAsync(
                int? categoriaId,
                bool incluirInactivas = false,
                CancellationToken cancellationToken = default)
        {
            var query = new List<string>
            {
                "incluirInactivas=" +
                    incluirInactivas.ToString().ToLowerInvariant()
            };

            if (categoriaId.HasValue && categoriaId.Value > 0)
                query.Add($"categoriaId={categoriaId.Value}");

            return GetAsync<List<SubcategoriaAlbumBotanicoResponse>>(
                "api/album-jerarquia/subcategorias?" +
                    string.Join("&", query),
                "cargar las subcategorías",
                cancellationToken);
        }

        public Task<ApiResult<SubcategoriaAlbumBotanicoResponse>>
            CrearSubcategoriaAsync(
                GuardarSubcategoriaAlbumRequest request,
                CancellationToken cancellationToken = default) =>
            SendAndReadAsync<
                GuardarSubcategoriaAlbumRequest,
                SubcategoriaAlbumBotanicoResponse>(
                HttpMethod.Post,
                "api/album-jerarquia/subcategorias",
                request,
                "crear la subcategoría",
                cancellationToken);

        public Task<ApiResult<bool>> ActualizarSubcategoriaAsync(
            int id,
            GuardarSubcategoriaAlbumRequest request,
            CancellationToken cancellationToken = default) =>
            SendWithoutDataAsync(
                HttpMethod.Put,
                $"api/album-jerarquia/subcategorias/{id}",
                request,
                "actualizar la subcategoría",
                cancellationToken);

        public Task<ApiResult<bool>> CambiarEstadoSubcategoriaAsync(
            int id,
            bool activo,
            CancellationToken cancellationToken = default) =>
            SendWithoutBodyAsync(
                HttpMethod.Patch,
                "api/album-jerarquia/subcategorias/" +
                    $"{id}/estado?activo={activo.ToString().ToLowerInvariant()}",
                activo
                    ? "activar la subcategoría"
                    : "desactivar la subcategoría",
                cancellationToken);

        public Task<ApiResult<List<AlbumRegistroJerarquiaResponse>>>
            GetJerarquiaRegistrosAsync(
                IEnumerable<int>? ids = null,
                int? categoriaId = null,
                int? subcategoriaId = null,
                bool incluirInactivos = false,
                CancellationToken cancellationToken = default)
        {
            var query = new List<string>
            {
                "incluirInactivos=" +
                    incluirInactivos.ToString().ToLowerInvariant()
            };

            List<int> identificadores = ids?
                .Where(id => id > 0)
                .Distinct()
                .Take(200)
                .ToList() ?? [];

            if (identificadores.Count > 0)
            {
                query.Add(
                    "ids=" +
                    Uri.EscapeDataString(
                        string.Join(',', identificadores)));
            }

            if (categoriaId.HasValue && categoriaId.Value > 0)
                query.Add($"categoriaId={categoriaId.Value}");

            if (subcategoriaId.HasValue && subcategoriaId.Value > 0)
                query.Add($"subcategoriaId={subcategoriaId.Value}");

            return GetAsync<List<AlbumRegistroJerarquiaResponse>>(
                "api/album-jerarquia/registros?" +
                    string.Join("&", query),
                "cargar la jerarquía de las fichas",
                cancellationToken);
        }

        public Task<ApiResult<bool>> AsignarSubcategoriaRegistroAsync(
            int registroId,
            int subcategoriaId,
            CancellationToken cancellationToken = default) =>
            SendWithoutDataAsync(
                HttpMethod.Put,
                $"api/album-jerarquia/registros/{registroId}/subcategoria",
                new AsignarSubcategoriaRegistroRequest
                {
                    SubcategoriaAlbumBotanicoId = subcategoriaId
                },
                "asignar la subcategoría",
                cancellationToken);

        public Task<ApiResult<List<JerarquiaDiagnosticoFotoResponse>>>
            GetJerarquiaDiagnosticoAsync(
                int diagnosticoId,
                CancellationToken cancellationToken = default)
        {
            if (diagnosticoId <= 0)
            {
                return Task.FromResult(
                    ApiResult<List<JerarquiaDiagnosticoFotoResponse>>.Fail(
                        "La inspección seleccionada no es válida."));
            }

            return GetAsync<List<JerarquiaDiagnosticoFotoResponse>>(
                $"api/album-jerarquia/diagnosticos/{diagnosticoId}",
                "cargar la clasificación jerárquica",
                cancellationToken);
        }

        public Task<ApiResult<bool>> ResolverJerarquiaAsync(
            int diagnosticoId,
            int fotografiaId,
            ResolverJerarquiaAlbumRequest request,
            CancellationToken cancellationToken = default) =>
            SendWithoutDataAsync(
                HttpMethod.Post,
                "api/album-jerarquia/diagnosticos/" +
                    $"{diagnosticoId}/fotografias/{fotografiaId}/resolver",
                request,
                "guardar la clasificación jerárquica",
                cancellationToken);

        private void PrepararInicio(AlbumInicioJerarquiaResponse inicio)
        {
            foreach (CategoriaAlbumBotanicoResponse categoria in
                inicio.Categorias)
            {
                categoria.ImagenPortadaUrl =
                    ConstruirUrlCompleta(categoria.RutaImagenPortada);
            }

            PrepararPagina(inicio.Galeria);
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

                return ApiResult<T>.Ok(envelope.Data, envelope.Message);
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

        private async Task<ApiResult<TData>>
            SendAndReadAsync<TRequest, TData>(
                HttpMethod method,
                string route,
                TRequest request,
                string action,
                CancellationToken cancellationToken)
        {
            try
            {
                using var message = new HttpRequestMessage(method, route)
                {
                    Content = JsonContent.Create(request)
                };

                using HttpResponseMessage response = await httpClient.SendAsync(
                    message,
                    cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    return ApiResult<TData>.Fail(
                        await LeerMensajeErrorAsync(
                            response,
                            action,
                            cancellationToken),
                        (int)response.StatusCode);
                }

                ApiEnvelope<TData>? envelope = await response.Content
                    .ReadFromJsonAsync<ApiEnvelope<TData>>(
                        JsonOptions,
                        cancellationToken);

                if (envelope == null ||
                    !envelope.Success ||
                    envelope.Data == null)
                {
                    return ApiResult<TData>.Fail(
                        envelope?.Message ??
                        "El servidor no devolvió los datos esperados.");
                }

                return ApiResult<TData>.Ok(
                    envelope.Data,
                    envelope.Message);
            }
            catch (Exception ex)
            {
                return ApiResult<TData>.Fail(
                    ex is HttpRequestException
                        ? "No fue posible conectarse con el servidor."
                        : $"Ocurrió un error inesperado al {action}.");
            }
        }

        private async Task<ApiResult<bool>>
            SendWithoutDataAsync<TRequest>(
                HttpMethod method,
                string route,
                TRequest request,
                string action,
                CancellationToken cancellationToken)
        {
            try
            {
                using var message = new HttpRequestMessage(method, route)
                {
                    Content = JsonContent.Create(request)
                };

                using HttpResponseMessage response = await httpClient.SendAsync(
                    message,
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

                return ApiResult<bool>.Ok(
                    true,
                    await LeerMensajeExitoAsync(
                        response,
                        cancellationToken));
            }
            catch (Exception ex)
            {
                return ApiResult<bool>.Fail(
                    ex is HttpRequestException
                        ? "No fue posible conectarse con el servidor."
                        : $"Ocurrió un error inesperado al {action}.");
            }
        }

        private async Task<ApiResult<bool>> SendWithoutBodyAsync(
            HttpMethod method,
            string route,
            string action,
            CancellationToken cancellationToken)
        {
            try
            {
                using var message = new HttpRequestMessage(method, route);
                using HttpResponseMessage response = await httpClient.SendAsync(
                    message,
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

                return ApiResult<bool>.Ok(
                    true,
                    await LeerMensajeExitoAsync(
                        response,
                        cancellationToken));
            }
            catch (Exception ex)
            {
                return ApiResult<bool>.Fail(
                    ex is HttpRequestException
                        ? "No fue posible conectarse con el servidor."
                        : $"Ocurrió un error inesperado al {action}.");
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
