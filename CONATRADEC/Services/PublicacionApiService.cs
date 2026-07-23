using CONATRADEC.Models;
using Microsoft.Maui.Storage;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace CONATRADEC.Services
{
    public sealed class PublicacionApiService
    {
        private readonly HttpClient httpClient;
        private readonly UrlApiService urlApiService;

        private static readonly JsonSerializerOptions JsonOptions =
            new(JsonSerializerDefaults.Web)
            {
                PropertyNameCaseInsensitive = true
            };

        public PublicacionApiService()
            : this(ApiClientService.Client)
        {
        }

        public PublicacionApiService(HttpClient client)
        {
            httpClient = client ??
                throw new ArgumentNullException(nameof(client));

            urlApiService = new UrlApiService();
        }

        public async Task<ApiResult<List<CategoriaPublicacionResponse>>>
            GetCategoriasAsync(
                CancellationToken cancellationToken = default)
        {
            return await GetAsync<List<CategoriaPublicacionResponse>>(
                "api/publicacion/categorias",
                "cargar las categorías",
                cancellationToken);
        }

        public async Task<ApiResult<PublicacionPaginadaResponse>>
            GetFeedAsync(
                int? categoriaId,
                string? buscar,
                bool soloDestacadas,
                bool soloEventos,
                int pagina,
                int tamanoPagina,
                CancellationToken cancellationToken = default)
        {
            var query = new List<string>
            {
                $"pagina={Math.Max(1, pagina)}",
                $"tamanoPagina={Math.Clamp(tamanoPagina, 6, 30)}",
                $"soloDestacadas={soloDestacadas.ToString().ToLowerInvariant()}",
                $"soloEventos={soloEventos.ToString().ToLowerInvariant()}"
            };

            if (categoriaId.HasValue && categoriaId.Value > 0)
                query.Add($"categoriaId={categoriaId.Value}");

            if (!string.IsNullOrWhiteSpace(buscar))
            {
                query.Add(
                    "buscar=" +
                    Uri.EscapeDataString(buscar.Trim()));
            }

            ApiResult<PublicacionPaginadaResponse> result =
                await GetAsync<PublicacionPaginadaResponse>(
                    "api/publicacion/feed?" + string.Join("&", query),
                    "cargar las noticias",
                    cancellationToken);

            PrepararImagenes(result.Data?.Items);
            return result;
        }

        public async Task<ApiResult<PublicacionDetalleResponse>>
            GetDetalleAsync(
                int publicacionId,
                CancellationToken cancellationToken = default)
        {
            if (publicacionId <= 0)
            {
                return ApiResult<PublicacionDetalleResponse>.Fail(
                    "La publicación seleccionada no es válida.");
            }

            ApiResult<PublicacionDetalleResponse> result =
                await GetAsync<PublicacionDetalleResponse>(
                    $"api/publicacion/detalle/{publicacionId}",
                    "cargar la publicación",
                    cancellationToken);

            PrepararImagen(result.Data);
            return result;
        }

        public async Task<ApiResult<PublicacionPaginadaResponse>>
            GetAdministracionAsync(
                int? categoriaId,
                string? estado,
                string? buscar,
                int pagina,
                int tamanoPagina,
                CancellationToken cancellationToken = default)
        {
            var query = new List<string>
            {
                $"pagina={Math.Max(1, pagina)}",
                $"tamanoPagina={Math.Clamp(tamanoPagina, 10, 50)}"
            };

            if (categoriaId.HasValue && categoriaId.Value > 0)
                query.Add($"categoriaId={categoriaId.Value}");

            if (!string.IsNullOrWhiteSpace(estado) &&
                !string.Equals(
                    estado,
                    "TODOS",
                    StringComparison.OrdinalIgnoreCase))
            {
                query.Add(
                    "estado=" +
                    Uri.EscapeDataString(estado.Trim()));
            }

            if (!string.IsNullOrWhiteSpace(buscar))
            {
                query.Add(
                    "buscar=" +
                    Uri.EscapeDataString(buscar.Trim()));
            }

            ApiResult<PublicacionPaginadaResponse> result =
                await GetAsync<PublicacionPaginadaResponse>(
                    "api/publicacion/administrar?" +
                    string.Join("&", query),
                    "cargar las publicaciones",
                    cancellationToken);

            PrepararImagenes(result.Data?.Items);
            return result;
        }

        public async Task<ApiResult<PublicacionDetalleResponse>>
            GetParaAdministrarAsync(
                int publicacionId,
                CancellationToken cancellationToken = default)
        {
            if (publicacionId <= 0)
            {
                return ApiResult<PublicacionDetalleResponse>.Fail(
                    "La publicación seleccionada no es válida.");
            }

            ApiResult<PublicacionDetalleResponse> result =
                await GetAsync<PublicacionDetalleResponse>(
                    $"api/publicacion/administrar/{publicacionId}",
                    "cargar la publicación",
                    cancellationToken);

            PrepararImagen(result.Data);
            return result;
        }

        public Task<ApiResult<PublicacionCreadaResponse>> CrearAsync(
            PublicacionGuardarRequest request,
            CancellationToken cancellationToken = default) =>
            SendAndReadAsync<
                PublicacionGuardarRequest,
                PublicacionCreadaResponse>(
                HttpMethod.Post,
                "api/publicacion/crear",
                request,
                "crear la publicación",
                cancellationToken);

        public Task<ApiResult<bool>> ActualizarAsync(
            PublicacionGuardarRequest request,
            CancellationToken cancellationToken = default)
        {
            if (request.PublicacionId <= 0)
            {
                return Task.FromResult(
                    ApiResult<bool>.Fail(
                        "La publicación seleccionada no es válida."));
            }

            return SendWithoutDataAsync(
                HttpMethod.Put,
                $"api/publicacion/actualizar/{request.PublicacionId}",
                request,
                "actualizar la publicación",
                cancellationToken);
        }

        public Task<ApiResult<bool>> CambiarEstadoAsync(
            int publicacionId,
            string estado,
            CancellationToken cancellationToken = default) =>
            SendWithoutDataAsync(
                HttpMethod.Patch,
                $"api/publicacion/cambiar-estado/{publicacionId}",
                new
                {
                    estadoPublicacion = estado
                },
                "cambiar el estado de la publicación",
                cancellationToken);

        public Task<ApiResult<bool>> CambiarDestacadaAsync(
            int publicacionId,
            bool destacada,
            CancellationToken cancellationToken = default) =>
            SendWithoutDataAsync(
                HttpMethod.Patch,
                $"api/publicacion/cambiar-destacada/{publicacionId}",
                new
                {
                    destacada
                },
                "actualizar el destacado de la publicación",
                cancellationToken);

        public Task<ApiResult<bool>> EliminarAsync(
            int publicacionId,
            CancellationToken cancellationToken = default) =>
            SendWithoutBodyAsync(
                HttpMethod.Delete,
                $"api/publicacion/eliminar/{publicacionId}",
                "eliminar la publicación",
                cancellationToken);

        public async Task<ApiResult<PortadaPublicacionResponse>>
            SubirPortadaAsync(
                int publicacionId,
                FileResult archivo,
                CancellationToken cancellationToken = default)
        {
            if (publicacionId <= 0)
            {
                return ApiResult<PortadaPublicacionResponse>.Fail(
                    "La publicación seleccionada no es válida.");
            }

            if (archivo == null)
            {
                return ApiResult<PortadaPublicacionResponse>.Fail(
                    "Debe seleccionar una imagen.");
            }

            try
            {
                await using Stream stream =
                    await archivo.OpenReadAsync();

                using var content = new MultipartFormDataContent();
                using var fileContent = new StreamContent(stream);

                string contentType = ObtenerContentType(archivo.FileName);
                fileContent.Headers.ContentType =
                    new MediaTypeHeaderValue(contentType);

                content.Add(
                    fileContent,
                    "Archivo",
                    archivo.FileName);

                using HttpResponseMessage response =
                    await httpClient.PostAsync(
                        $"api/publicacion/{publicacionId}/portada",
                        content,
                        cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    return ApiResult<PortadaPublicacionResponse>.Fail(
                        await ReadErrorMessageAsync(
                            response,
                            "subir la portada",
                            cancellationToken),
                        (int)response.StatusCode);
                }

                ApiEnvelope<PortadaPublicacionResponse>? envelope =
                    await response.Content
                        .ReadFromJsonAsync<
                            ApiEnvelope<PortadaPublicacionResponse>>(
                            JsonOptions,
                            cancellationToken);

                if (envelope?.Success != true || envelope.Data == null)
                {
                    return ApiResult<PortadaPublicacionResponse>.Fail(
                        envelope?.Message ??
                        "El servidor no devolvió la portada guardada.");
                }

                envelope.Data.RutaImagenPortada =
                    ConstruirUrlCompleta(
                        envelope.Data.RutaImagenPortada);

                return ApiResult<PortadaPublicacionResponse>.Ok(
                    envelope.Data,
                    envelope.Message);
            }
            catch (TaskCanceledException)
                when (!cancellationToken.IsCancellationRequested)
            {
                return ApiResult<PortadaPublicacionResponse>.Fail(
                    "La solicitud tardó demasiado. Verifique su conexión.");
            }
            catch (OperationCanceledException)
            {
                return ApiResult<PortadaPublicacionResponse>.Fail(
                    "La operación fue cancelada.");
            }
            catch (HttpRequestException)
            {
                return ApiResult<PortadaPublicacionResponse>.Fail(
                    "No fue posible conectarse con el servidor.");
            }
            catch (Exception)
            {
                return ApiResult<PortadaPublicacionResponse>.Fail(
                    "Ocurrió un error inesperado al subir la portada.");
            }
        }

        public Task<ApiResult<bool>> EliminarPortadaAsync(
            int publicacionId,
            CancellationToken cancellationToken = default) =>
            SendWithoutBodyAsync(
                HttpMethod.Delete,
                $"api/publicacion/{publicacionId}/portada",
                "eliminar la portada",
                cancellationToken);

        public string ConstruirUrlCompleta(string? ruta)
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
                using HttpResponseMessage response =
                    await httpClient.GetAsync(
                        route,
                        cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    return ApiResult<T>.Fail(
                        await ReadErrorMessageAsync(
                            response,
                            action,
                            cancellationToken),
                        (int)response.StatusCode);
                }

                ApiEnvelope<T>? envelope =
                    await response.Content
                        .ReadFromJsonAsync<ApiEnvelope<T>>(
                            JsonOptions,
                            cancellationToken);

                if (envelope?.Success != true || envelope.Data == null)
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

        private async Task<ApiResult<TData>> SendAndReadAsync<TBody, TData>(
            HttpMethod method,
            string route,
            TBody body,
            string action,
            CancellationToken cancellationToken)
        {
            try
            {
                using var request = new HttpRequestMessage(method, route)
                {
                    Content = JsonContent.Create(
                        body,
                        options: JsonOptions)
                };

                using HttpResponseMessage response =
                    await httpClient.SendAsync(
                        request,
                        cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    return ApiResult<TData>.Fail(
                        await ReadErrorMessageAsync(
                            response,
                            action,
                            cancellationToken),
                        (int)response.StatusCode);
                }

                ApiEnvelope<TData>? envelope =
                    await response.Content
                        .ReadFromJsonAsync<ApiEnvelope<TData>>(
                            JsonOptions,
                            cancellationToken);

                if (envelope?.Success != true || envelope.Data == null)
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
                return ConvertirExcepcion<TData>(ex, action, cancellationToken);
            }
        }

        private async Task<ApiResult<bool>> SendWithoutDataAsync<TBody>(
            HttpMethod method,
            string route,
            TBody body,
            string action,
            CancellationToken cancellationToken)
        {
            try
            {
                using var request = new HttpRequestMessage(method, route)
                {
                    Content = JsonContent.Create(
                        body,
                        options: JsonOptions)
                };

                using HttpResponseMessage response =
                    await httpClient.SendAsync(
                        request,
                        cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    return ApiResult<bool>.Fail(
                        await ReadErrorMessageAsync(
                            response,
                            action,
                            cancellationToken),
                        (int)response.StatusCode);
                }

                ApiEnvelope<object>? envelope =
                    await LeerEnvelopeOpcionalAsync(
                        response,
                        cancellationToken);

                return ApiResult<bool>.Ok(
                    true,
                    envelope?.Message ?? string.Empty);
            }
            catch (Exception ex)
            {
                return ConvertirExcepcion<bool>(ex, action, cancellationToken);
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
                using var request = new HttpRequestMessage(method, route);
                using HttpResponseMessage response =
                    await httpClient.SendAsync(
                        request,
                        cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    return ApiResult<bool>.Fail(
                        await ReadErrorMessageAsync(
                            response,
                            action,
                            cancellationToken),
                        (int)response.StatusCode);
                }

                ApiEnvelope<object>? envelope =
                    await LeerEnvelopeOpcionalAsync(
                        response,
                        cancellationToken);

                return ApiResult<bool>.Ok(
                    true,
                    envelope?.Message ?? string.Empty);
            }
            catch (Exception ex)
            {
                return ConvertirExcepcion<bool>(ex, action, cancellationToken);
            }
        }

        private static ApiResult<T> ConvertirExcepcion<T>(
            Exception ex,
            string action,
            CancellationToken cancellationToken)
        {
            if (ex is TaskCanceledException &&
                !cancellationToken.IsCancellationRequested)
            {
                return ApiResult<T>.Fail(
                    "La solicitud tardó demasiado. Verifique su conexión.");
            }

            if (ex is OperationCanceledException)
            {
                return ApiResult<T>.Fail(
                    "La operación fue cancelada.");
            }

            if (ex is HttpRequestException)
            {
                return ApiResult<T>.Fail(
                    "No fue posible conectarse con el servidor.");
            }

            if (ex is JsonException)
            {
                return ApiResult<T>.Fail(
                    "El servidor respondió con un formato no válido.");
            }

            return ApiResult<T>.Fail(
                $"Ocurrió un error inesperado al {action}.");
        }

        private static async Task<ApiEnvelope<object>?>
            LeerEnvelopeOpcionalAsync(
                HttpResponseMessage response,
                CancellationToken cancellationToken)
        {
            string content = await response.Content
                .ReadAsStringAsync(cancellationToken);

            if (string.IsNullOrWhiteSpace(content))
                return null;

            try
            {
                return JsonSerializer.Deserialize<ApiEnvelope<object>>(
                    content,
                    JsonOptions);
            }
            catch
            {
                return null;
            }
        }

        private static async Task<string> ReadErrorMessageAsync(
            HttpResponseMessage response,
            string action,
            CancellationToken cancellationToken)
        {
            string content;

            try
            {
                content = await response.Content
                    .ReadAsStringAsync(cancellationToken);
            }
            catch
            {
                content = string.Empty;
            }

            return ApiErrorMessageParser.Parse(
                response.StatusCode,
                content,
                ApiErrorMessageParser.GetDefaultMessage(
                    response.StatusCode,
                    $"No fue posible {action}."));
        }

        private void PrepararImagenes(
            IEnumerable<PublicacionListadoResponse>? publicaciones)
        {
            if (publicaciones == null)
                return;

            foreach (PublicacionListadoResponse publicacion in publicaciones)
                PrepararImagen(publicacion);
        }

        private void PrepararImagen(
            PublicacionListadoResponse? publicacion)
        {
            if (publicacion == null)
                return;

            publicacion.ImagenPortadaUrl =
                ConstruirUrlCompleta(
                    publicacion.RutaImagenPortada);
        }

        private static string ObtenerContentType(string fileName)
        {
            return Path.GetExtension(fileName).ToLowerInvariant() switch
            {
                ".png" => "image/png",
                ".webp" => "image/webp",
                _ => "image/jpeg"
            };
        }

        private sealed class ApiEnvelope<T>
        {
            public bool Success { get; set; }
            public string Message { get; set; } = string.Empty;
            public T? Data { get; set; }
        }
    }
}
