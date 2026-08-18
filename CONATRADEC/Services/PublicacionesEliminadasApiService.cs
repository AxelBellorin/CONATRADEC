using CONATRADEC.Models;
using System.Net.Http.Json;
using System.Text.Json;

namespace CONATRADEC.Services
{
    /// <summary>
    /// Cliente exclusivo de la papelera administrativa de publicaciones.
    /// No modifica los endpoints históricos de PublicacionApiService.
    /// </summary>
    public sealed class PublicacionesEliminadasApiService
    {
        private readonly HttpClient httpClient;
        private readonly PublicacionApiService publicacionApiService;

        private static readonly JsonSerializerOptions JsonOptions =
            new(JsonSerializerDefaults.Web)
            {
                PropertyNameCaseInsensitive = true
            };

        public PublicacionesEliminadasApiService()
            : this(ApiClientService.Client)
        {
        }

        public PublicacionesEliminadasApiService(HttpClient client)
        {
            httpClient = client ??
                throw new ArgumentNullException(nameof(client));

            publicacionApiService =
                new PublicacionApiService(client);
        }

        public async Task<ApiResult<PublicacionPaginadaResponse>>
            ListarAsync(
                string? buscar,
                int pagina,
                int tamanoPagina,
                CancellationToken cancellationToken = default)
        {
            var query = new List<string>
            {
                $"pagina={Math.Max(1, pagina)}",
                $"tamanoPagina={Math.Clamp(tamanoPagina, 8, 50)}"
            };

            if (!string.IsNullOrWhiteSpace(buscar))
            {
                query.Add(
                    "buscar=" +
                    Uri.EscapeDataString(buscar.Trim()));
            }

            try
            {
                using HttpResponseMessage response =
                    await httpClient.GetAsync(
                        "api/publicacion-eliminadas?" +
                        string.Join("&", query),
                        cancellationToken);

                string contenido =
                    await response.Content.ReadAsStringAsync(
                        cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    return ApiResult<PublicacionPaginadaResponse>.Fail(
                        ApiErrorMessageParser.Parse(
                            response.StatusCode,
                            contenido,
                            "No fue posible cargar las publicaciones eliminadas."),
                        (int)response.StatusCode);
                }

                ApiEnvelopeLocal<PublicacionPaginadaResponse>? envelope =
                    JsonSerializer.Deserialize<
                        ApiEnvelopeLocal<PublicacionPaginadaResponse>>(
                            contenido,
                            JsonOptions);

                if (envelope?.Success != true ||
                    envelope.Data == null)
                {
                    return ApiResult<PublicacionPaginadaResponse>.Fail(
                        envelope?.Message ??
                        "El servidor no devolvió las publicaciones eliminadas.");
                }

                foreach (PublicacionListadoResponse item
                         in envelope.Data.Items)
                {
                    item.ImagenPortadaUrl =
                        publicacionApiService.ConstruirUrlCompleta(
                            item.RutaImagenPortada);
                }

                return ApiResult<PublicacionPaginadaResponse>.Ok(
                    envelope.Data,
                    envelope.Message);
            }
            catch (TaskCanceledException)
                when (!cancellationToken.IsCancellationRequested)
            {
                return ApiResult<PublicacionPaginadaResponse>.Fail(
                    "La solicitud tardó demasiado. Verifique su conexión.");
            }
            catch (OperationCanceledException)
            {
                return ApiResult<PublicacionPaginadaResponse>.Fail(
                    "La operación fue cancelada.");
            }
            catch (HttpRequestException)
            {
                return ApiResult<PublicacionPaginadaResponse>.Fail(
                    "No fue posible conectarse con el servidor.");
            }
            catch (JsonException)
            {
                return ApiResult<PublicacionPaginadaResponse>.Fail(
                    "El servidor respondió con un formato no válido.");
            }
            catch
            {
                return ApiResult<PublicacionPaginadaResponse>.Fail(
                    "Ocurrió un error inesperado al cargar las publicaciones eliminadas.");
            }
        }

        public async Task<ApiResult<bool>> ReactivarAsync(
            int publicacionId,
            CancellationToken cancellationToken = default)
        {
            if (publicacionId <= 0)
            {
                return ApiResult<bool>.Fail(
                    "La publicación seleccionada no es válida.");
            }

            try
            {
                using var request =
                    new HttpRequestMessage(
                        HttpMethod.Put,
                        $"api/publicacion-eliminadas/{publicacionId}/reactivar");

                using HttpResponseMessage response =
                    await httpClient.SendAsync(
                        request,
                        cancellationToken);

                string contenido =
                    await response.Content.ReadAsStringAsync(
                        cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    return ApiResult<bool>.Fail(
                        ApiErrorMessageParser.Parse(
                            response.StatusCode,
                            contenido,
                            "No fue posible restaurar la publicación."),
                        (int)response.StatusCode);
                }

                ApiEnvelopeLocal<object>? envelope = null;

                if (!string.IsNullOrWhiteSpace(contenido))
                {
                    try
                    {
                        envelope = JsonSerializer.Deserialize<
                            ApiEnvelopeLocal<object>>(
                                contenido,
                                JsonOptions);
                    }
                    catch (JsonException)
                    {
                        // Una respuesta 2xx sin envelope sigue siendo exitosa.
                    }
                }

                return ApiResult<bool>.Ok(
                    true,
                    string.IsNullOrWhiteSpace(envelope?.Message)
                        ? "Publicación restaurada como borrador."
                        : envelope.Message);
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
            catch
            {
                return ApiResult<bool>.Fail(
                    "Ocurrió un error inesperado al restaurar la publicación.");
            }
        }

        private sealed class ApiEnvelopeLocal<T>
        {
            public bool Success { get; set; }
            public string Message { get; set; } = string.Empty;
            public T? Data { get; set; }
        }
    }
}
