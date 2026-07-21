using CONATRADEC.Models;
using Microsoft.Maui.Storage;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace CONATRADEC.Services
{
    public sealed class AlbumBotanicoApiService
    {
        private readonly HttpClient httpClient;
        private readonly UrlApiService urlApiService;

        private static readonly JsonSerializerOptions JsonOptions =
            new(JsonSerializerDefaults.Web)
            {
                PropertyNameCaseInsensitive = true
            };

        public AlbumBotanicoApiService()
            : this(ApiClientService.Client)
        {
        }

        public AlbumBotanicoApiService(HttpClient client)
        {
            httpClient = client ??
                throw new ArgumentNullException(nameof(client));
            urlApiService = new UrlApiService();
        }

        public async Task<ApiResult<List<CategoriaAlbumBotanicoResponse>>>
            GetCategoriasAsync(
                bool incluirInactivos,
                CancellationToken cancellationToken = default)
        {
            string route =
                "api/categoria-album-botanico/listar" +
                $"?incluirInactivos={incluirInactivos.ToString().ToLowerInvariant()}";

            ApiResult<List<CategoriaAlbumBotanicoResponse>> result =
                await GetAsync<List<CategoriaAlbumBotanicoResponse>>(
                    route,
                    "cargar las categorías del álbum",
                    cancellationToken);

            if (result.Success && result.Data != null)
            {
                foreach (var categoria in result.Data)
                {
                    categoria.ImagenPortadaUrl =
                        ConstruirUrlCompleta(
                            categoria.RutaImagenPortada);
                }
            }

            return result;
        }

        public async Task<ApiResult<CategoriaAlbumBotanicoResponse>>
            GetCategoriaAsync(
                int id,
                CancellationToken cancellationToken = default)
        {
            if (id <= 0)
            {
                return ApiResult<CategoriaAlbumBotanicoResponse>.Fail(
                    "La categoría seleccionada no es válida.");
            }

            var result =
                await GetAsync<CategoriaAlbumBotanicoResponse>(
                    $"api/categoria-album-botanico/obtener/{id}",
                    "cargar la categoría",
                    cancellationToken);

            if (result.Success && result.Data != null)
            {
                result.Data.ImagenPortadaUrl =
                    ConstruirUrlCompleta(
                        result.Data.RutaImagenPortada);
            }

            return result;
        }

        public Task<ApiResult<CategoriaCreadaData>>
            CrearCategoriaAsync(
                CategoriaAlbumBotanicoRequest request,
                CancellationToken cancellationToken = default) =>
            SendAndReadAsync<
                CategoriaAlbumBotanicoRequest,
                CategoriaCreadaData>(
                HttpMethod.Post,
                "api/categoria-album-botanico/crear",
                request,
                "crear la categoría",
                cancellationToken);

        public Task<ApiResult<bool>> ActualizarCategoriaAsync(
            CategoriaAlbumBotanicoRequest request,
            CancellationToken cancellationToken = default)
        {
            if (request.CategoriaAlbumBotanicoId <= 0)
            {
                return Task.FromResult(
                    ApiResult<bool>.Fail(
                        "La categoría seleccionada no es válida."));
            }

            return SendWithoutDataAsync(
                HttpMethod.Put,
                "api/categoria-album-botanico/actualizar/" +
                    request.CategoriaAlbumBotanicoId,
                request,
                "actualizar la categoría",
                cancellationToken);
        }

        public Task<ApiResult<bool>> CambiarEstadoCategoriaAsync(
            int id,
            bool activo,
            CancellationToken cancellationToken = default) =>
            SendWithoutBodyAsync(
                HttpMethod.Patch,
                "api/categoria-album-botanico/cambiar-estado/" +
                    $"{id}?activo={activo.ToString().ToLowerInvariant()}",
                activo ? "activar la categoría" : "desactivar la categoría",
                cancellationToken);

        public Task<ApiResult<bool>> EliminarCategoriaAsync(
            int id,
            CancellationToken cancellationToken = default) =>
            SendWithoutBodyAsync(
                HttpMethod.Delete,
                $"api/categoria-album-botanico/eliminar/{id}",
                "desactivar la categoría",
                cancellationToken);

        public async Task<ApiResult<PortadaCategoriaData>>
            SubirPortadaCategoriaAsync(
                int categoriaId,
                FileResult archivo,
                CancellationToken cancellationToken = default)
        {
            if (categoriaId <= 0)
            {
                return ApiResult<PortadaCategoriaData>.Fail(
                    "La categoría seleccionada no es válida.");
            }

            return await SendFileAsync<PortadaCategoriaData>(
                $"api/categoria-album-botanico/{categoriaId}/portada",
                archivo,
                null,
                "subir la portada de la categoría",
                cancellationToken);
        }

        public async Task<ApiResult<List<AlbumGaleriaItemResponse>>>
            GetGaleriaAsync(
                int? categoriaId,
                string? buscar,
                bool incluirInactivos,
                CancellationToken cancellationToken = default)
        {
            var query = new List<string>
            {
                "incluirInactivos=" +
                    incluirInactivos.ToString().ToLowerInvariant()
            };

            if (categoriaId.HasValue && categoriaId.Value > 0)
            {
                query.Add($"categoriaId={categoriaId.Value}");
            }

            if (!string.IsNullOrWhiteSpace(buscar))
            {
                query.Add(
                    "buscar=" +
                    Uri.EscapeDataString(buscar.Trim()));
            }

            string route =
                "api/album-botanico/galeria?" +
                string.Join("&", query);

            var result =
                await GetAsync<List<AlbumGaleriaItemResponse>>(
                    route,
                    "cargar la galería",
                    cancellationToken);

            if (result.Success && result.Data != null)
            {
                foreach (var item in result.Data)
                {
                    item.FotoPortadaUrl =
                        ConstruirUrlCompleta(item.FotoPortada);
                }
            }

            return result;
        }

        public async Task<ApiResult<AlbumDetalleResponse>>
            GetDetalleAsync(
                int id,
                bool incluirInactivos,
                CancellationToken cancellationToken = default)
        {
            if (id <= 0)
            {
                return ApiResult<AlbumDetalleResponse>.Fail(
                    "El registro seleccionado no es válido.");
            }

            string route =
                $"api/album-botanico/detalle/{id}" +
                "?incluirInactivos=" +
                incluirInactivos.ToString().ToLowerInvariant();

            var result = await GetAsync<AlbumDetalleResponse>(
                route,
                "cargar el detalle del álbum",
                cancellationToken);

            if (result.Success && result.Data != null)
            {
                foreach (var foto in result.Data.Fotos)
                {
                    foto.FotoUrl =
                        ConstruirUrlCompleta(foto.RutaFoto);
                }
            }

            return result;
        }

        public Task<ApiResult<RegistroAlbumCreadoData>>
            CrearRegistroAsync(
                AlbumRegistroRequest request,
                CancellationToken cancellationToken = default) =>
            SendAndReadAsync<
                AlbumRegistroRequest,
                RegistroAlbumCreadoData>(
                HttpMethod.Post,
                "api/album-botanico/crear",
                request,
                "crear el registro del álbum",
                cancellationToken);

        public Task<ApiResult<bool>> ActualizarRegistroAsync(
            AlbumRegistroRequest request,
            CancellationToken cancellationToken = default)
        {
            if (request.AlbumBotanicoCafeId <= 0)
            {
                return Task.FromResult(
                    ApiResult<bool>.Fail(
                        "El registro seleccionado no es válido."));
            }

            return SendWithoutDataAsync(
                HttpMethod.Put,
                "api/album-botanico/actualizar/" +
                    request.AlbumBotanicoCafeId,
                request,
                "actualizar el registro del álbum",
                cancellationToken);
        }

        public Task<ApiResult<bool>> CambiarEstadoRegistroAsync(
            int id,
            bool activo,
            CancellationToken cancellationToken = default) =>
            SendWithoutBodyAsync(
                HttpMethod.Patch,
                "api/album-botanico/cambiar-estado/" +
                    $"{id}?activo={activo.ToString().ToLowerInvariant()}",
                activo ? "activar el registro" : "desactivar el registro",
                cancellationToken);

        public Task<ApiResult<bool>> EliminarRegistroAsync(
            int id,
            CancellationToken cancellationToken = default) =>
            SendWithoutBodyAsync(
                HttpMethod.Delete,
                $"api/album-botanico/eliminar/{id}",
                "desactivar el registro",
                cancellationToken);

        public async Task<ApiResult<FotoAlbumCreadaData>>
            SubirFotoAsync(
                int registroId,
                FileResult archivo,
                string? descripcion,
                bool esPortada,
                int orden,
                CancellationToken cancellationToken = default)
        {
            if (registroId <= 0)
            {
                return ApiResult<FotoAlbumCreadaData>.Fail(
                    "El registro seleccionado no es válido.");
            }

            var fields = new Dictionary<string, string>
            {
                ["descripcionFoto"] = descripcion?.Trim() ?? string.Empty,
                ["esPortada"] =
                    esPortada.ToString().ToLowerInvariant(),
                ["orden"] = orden.ToString()
            };

            return await SendFileAsync<FotoAlbumCreadaData>(
                $"api/album-botanico/{registroId}/fotos",
                archivo,
                fields,
                "subir la fotografía",
                cancellationToken);
        }

        public Task<ApiResult<bool>> ActualizarFotoAsync(
            AlbumFotoResponse foto,
            CancellationToken cancellationToken = default) =>
            SendWithoutDataAsync(
                HttpMethod.Put,
                $"api/album-botanico/fotos/{foto.AlbumBotanicoCafeFotoId}",
                new ActualizarFotoAlbumRequest
                {
                    DescripcionFoto = foto.DescripcionFoto,
                    Orden = foto.Orden
                },
                "actualizar la fotografía",
                cancellationToken);

        public Task<ApiResult<bool>> EstablecerPortadaAsync(
            int fotoId,
            CancellationToken cancellationToken = default) =>
            SendWithoutBodyAsync(
                HttpMethod.Patch,
                $"api/album-botanico/fotos/{fotoId}/portada",
                "establecer la fotografía como portada",
                cancellationToken);

        public Task<ApiResult<bool>> EliminarFotoAsync(
            int fotoId,
            CancellationToken cancellationToken = default) =>
            SendWithoutBodyAsync(
                HttpMethod.Delete,
                $"api/album-botanico/fotos/{fotoId}",
                "eliminar la fotografía",
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
                using var response = await httpClient.GetAsync(
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
                using var message = new HttpRequestMessage(
                    method,
                    route)
                {
                    Content = JsonContent.Create(request)
                };

                using var response = await httpClient.SendAsync(
                    message,
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
            catch (TaskCanceledException)
                when (!cancellationToken.IsCancellationRequested)
            {
                return ApiResult<TData>.Fail(
                    "La solicitud tardó demasiado. Verifique su conexión.");
            }
            catch (OperationCanceledException)
            {
                return ApiResult<TData>.Fail(
                    "La operación fue cancelada.");
            }
            catch (HttpRequestException)
            {
                return ApiResult<TData>.Fail(
                    "No fue posible conectarse con el servidor.");
            }
            catch (Exception)
            {
                return ApiResult<TData>.Fail(
                    $"Ocurrió un error inesperado al {action}.");
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
                using var message = new HttpRequestMessage(
                    method,
                    route)
                {
                    Content = JsonContent.Create(request)
                };

                using var response = await httpClient.SendAsync(
                    message,
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

                string successMessage =
                    await ReadSuccessMessageAsync(
                        response,
                        cancellationToken);

                return ApiResult<bool>.Ok(
                    true,
                    successMessage);
            }
            catch (TaskCanceledException)
                when (!cancellationToken.IsCancellationRequested)
            {
                return ApiResult<bool>.Fail(
                    "La solicitud tardó demasiado. Verifique su conexión.");
            }
            catch (OperationCanceledException)
            {
                return ApiResult<bool>.Fail(
                    "La operación fue cancelada.");
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

        private async Task<ApiResult<bool>>
            SendWithoutBodyAsync(
                HttpMethod method,
                string route,
                string action,
                CancellationToken cancellationToken)
        {
            try
            {
                using var message =
                    new HttpRequestMessage(method, route);

                using var response = await httpClient.SendAsync(
                    message,
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

                string successMessage =
                    await ReadSuccessMessageAsync(
                        response,
                        cancellationToken);

                return ApiResult<bool>.Ok(
                    true,
                    successMessage);
            }
            catch (TaskCanceledException)
                when (!cancellationToken.IsCancellationRequested)
            {
                return ApiResult<bool>.Fail(
                    "La solicitud tardó demasiado. Verifique su conexión.");
            }
            catch (OperationCanceledException)
            {
                return ApiResult<bool>.Fail(
                    "La operación fue cancelada.");
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

        private async Task<ApiResult<TData>> SendFileAsync<TData>(
            string route,
            FileResult archivo,
            IDictionary<string, string>? fields,
            string action,
            CancellationToken cancellationToken)
        {
            try
            {
                await using Stream stream =
                    await archivo.OpenReadAsync();

                using var form =
                    new MultipartFormDataContent();

                using var fileContent =
                    new StreamContent(stream);

                fileContent.Headers.ContentType =
                    new MediaTypeHeaderValue(
                        ObtenerContentType(archivo.FileName));

                form.Add(
                    fileContent,
                    "archivo",
                    archivo.FileName);

                if (fields != null)
                {
                    foreach (var field in fields)
                    {
                        form.Add(
                            new StringContent(field.Value),
                            field.Key);
                    }
                }

                using var response = await httpClient.PostAsync(
                    route,
                    form,
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
            catch (TaskCanceledException)
                when (!cancellationToken.IsCancellationRequested)
            {
                return ApiResult<TData>.Fail(
                    "La carga tardó demasiado. Verifique su conexión.");
            }
            catch (OperationCanceledException)
            {
                return ApiResult<TData>.Fail(
                    "La operación fue cancelada.");
            }
            catch (IOException)
            {
                return ApiResult<TData>.Fail(
                    "No fue posible leer la imagen seleccionada.");
            }
            catch (HttpRequestException)
            {
                return ApiResult<TData>.Fail(
                    "No fue posible conectarse con el servidor.");
            }
            catch (Exception)
            {
                return ApiResult<TData>.Fail(
                    $"Ocurrió un error inesperado al {action}.");
            }
        }

        private static async Task<string>
            ReadErrorMessageAsync(
                HttpResponseMessage response,
                string action,
                CancellationToken cancellationToken)
        {
            try
            {
                ApiEnvelope<JsonElement>? envelope =
                    await response.Content
                        .ReadFromJsonAsync<
                            ApiEnvelope<JsonElement>>(
                            JsonOptions,
                            cancellationToken);

                if (!string.IsNullOrWhiteSpace(
                        envelope?.Message))
                {
                    return envelope.Message;
                }
            }
            catch
            {
            }

            return ApiServiceHelper.GetHttpMessage(
                response.StatusCode,
                action);
        }

        private static async Task<string>
            ReadSuccessMessageAsync(
                HttpResponseMessage response,
                CancellationToken cancellationToken)
        {
            try
            {
                ApiEnvelope<JsonElement>? envelope =
                    await response.Content
                        .ReadFromJsonAsync<
                            ApiEnvelope<JsonElement>>(
                            JsonOptions,
                            cancellationToken);

                if (!string.IsNullOrWhiteSpace(
                        envelope?.Message))
                {
                    return envelope.Message;
                }
            }
            catch
            {
            }

            return "Operación realizada correctamente.";
        }

        private static string ObtenerContentType(
            string fileName)
        {
            string extension =
                Path.GetExtension(fileName)
                    .ToLowerInvariant();

            return extension switch
            {
                ".jpg" => "image/jpeg",
                ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".webp" => "image/webp",
                _ => "application/octet-stream"
            };
        }
    }
}
