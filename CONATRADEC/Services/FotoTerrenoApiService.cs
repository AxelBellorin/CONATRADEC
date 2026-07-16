using CONATRADEC.Models;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace CONATRADEC.Services
{
    public class FotoTerrenoApiService
    {
        private readonly HttpClient httpClient;
        private readonly UrlApiService urlApiService;

        public FotoTerrenoApiService()
            : this(ApiClientService.Client)
        {
        }

        public FotoTerrenoApiService(HttpClient httpClient)
        {
            this.httpClient = httpClient
                ?? throw new ArgumentNullException(nameof(httpClient));

            urlApiService = new UrlApiService();
        }

        // =========================================================
        // MÉTODOS CON RESULTADO DETALLADO
        // =========================================================

        public async Task<ApiResult<List<FotoTerrenoResponse>>> GetFotosPorTerrenoResultAsync(
            int terrenoId,
            CancellationToken cancellationToken = default)
        {
            if (terrenoId <= 0)
            {
                return ApiResult<List<FotoTerrenoResponse>>.Fail(
                    "No se recibió un terreno válido para cargar sus fotografías.");
            }

            try
            {
                using var response = await httpClient.GetAsync(
                    $"api/fotoTerreno/por-terreno/{terrenoId}",
                    cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    return ApiResult<List<FotoTerrenoResponse>>.Fail(
                        ApiServiceHelper.GetHttpMessage(
                            response.StatusCode,
                            "cargar las fotografías del terreno"),
                        (int)response.StatusCode);
                }

                var fotos = await response.Content.ReadFromJsonAsync<List<FotoTerrenoResponse>>(
                    cancellationToken: cancellationToken);

                return ApiResult<List<FotoTerrenoResponse>>.Ok(
                    fotos ?? new List<FotoTerrenoResponse>());
            }
            catch (TaskCanceledException)
                when (!cancellationToken.IsCancellationRequested)
            {
                return ApiResult<List<FotoTerrenoResponse>>.Fail(
                    "La carga de fotografías tardó demasiado. Verifique su conexión e intente nuevamente.");
            }
            catch (OperationCanceledException)
            {
                return ApiResult<List<FotoTerrenoResponse>>.Fail(
                    "La operación fue cancelada.");
            }
            catch (HttpRequestException)
            {
                return ApiResult<List<FotoTerrenoResponse>>.Fail(
                    "No fue posible conectarse con el servidor para cargar las fotografías.");
            }
            catch (JsonException)
            {
                return ApiResult<List<FotoTerrenoResponse>>.Fail(
                    "El servidor respondió, pero los datos de las fotografías no tienen el formato esperado.");
            }
            catch (Exception)
            {
                return ApiResult<List<FotoTerrenoResponse>>.Fail(
                    "Ocurrió un error inesperado al cargar las fotografías.");
            }
        }

        public async Task<ApiResult<bool>> SubirFotosResultAsync(
            int terrenoId,
            IEnumerable<FotoTerrenoItem> fotos,
            CancellationToken cancellationToken = default)
        {
            if (terrenoId <= 0)
            {
                return ApiResult<bool>.Fail(
                    "No se recibió un terreno válido para subir las fotografías.");
            }

            ArgumentNullException.ThrowIfNull(fotos);

            var fotosNuevas = fotos
                .Where(f => f.EsNueva &&
                            !string.IsNullOrWhiteSpace(f.LocalPath) &&
                            File.Exists(f.LocalPath))
                .ToList();

            if (!fotosNuevas.Any())
            {
                return ApiResult<bool>.Ok(
                    true,
                    "No hay fotografías pendientes de subir.");
            }

            try
            {
                using var form = new MultipartFormDataContent();

                form.Add(
                    new StringContent(terrenoId.ToString()),
                    "terrenoId");

                foreach (var foto in fotosNuevas)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    string nombreArchivo =
                        !string.IsNullOrWhiteSpace(foto.NombreArchivo)
                            ? foto.NombreArchivo
                            : Path.GetFileName(foto.LocalPath);

                    var fileStream = File.OpenRead(foto.LocalPath!);
                    var streamContent = new StreamContent(fileStream);

                    streamContent.Headers.ContentType =
                        new MediaTypeHeaderValue(
                            ObtenerContentType(nombreArchivo));

                    form.Add(
                        streamContent,
                        "fotos",
                        nombreArchivo);
                }

                using var response = await httpClient.PostAsync(
                    "api/fotoTerreno/subir",
                    form,
                    cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    return ApiResult<bool>.Fail(
                        ApiServiceHelper.GetHttpMessage(
                            response.StatusCode,
                            "subir las fotografías del terreno"),
                        (int)response.StatusCode);
                }

                return ApiResult<bool>.Ok(
                    true,
                    "Fotografías subidas correctamente.");
            }
            catch (TaskCanceledException)
                when (!cancellationToken.IsCancellationRequested)
            {
                return ApiResult<bool>.Fail(
                    "La subida de fotografías tardó demasiado. Verifique su conexión e intente nuevamente.");
            }
            catch (OperationCanceledException)
            {
                return ApiResult<bool>.Fail(
                    "La operación fue cancelada.");
            }
            catch (HttpRequestException)
            {
                return ApiResult<bool>.Fail(
                    "No fue posible conectarse con el servidor para subir las fotografías.");
            }
            catch (IOException)
            {
                return ApiResult<bool>.Fail(
                    "No fue posible leer una de las fotografías seleccionadas.");
            }
            catch (Exception)
            {
                return ApiResult<bool>.Fail(
                    "Ocurrió un error inesperado al subir las fotografías.");
            }
        }

        public async Task<ApiResult<bool>> EditarFotoResultAsync(
            int fotoTerrenoId,
            string localPath,
            CancellationToken cancellationToken = default)
        {
            if (fotoTerrenoId <= 0)
            {
                return ApiResult<bool>.Fail(
                    "No se recibió una fotografía válida para editar.");
            }

            if (string.IsNullOrWhiteSpace(localPath) ||
                !File.Exists(localPath))
            {
                return ApiResult<bool>.Fail(
                    "No se encontró la fotografía seleccionada.");
            }

            try
            {
                using var form = new MultipartFormDataContent();

                string nombreArchivo = Path.GetFileName(localPath);

                var fileStream = File.OpenRead(localPath);
                var streamContent = new StreamContent(fileStream);

                streamContent.Headers.ContentType =
                    new MediaTypeHeaderValue(
                        ObtenerContentType(nombreArchivo));

                form.Add(streamContent, "foto", nombreArchivo);

                using var response = await httpClient.PutAsync(
                    $"api/fotoTerreno/editar/{fotoTerrenoId}",
                    form,
                    cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    return ApiResult<bool>.Fail(
                        ApiServiceHelper.GetHttpMessage(
                            response.StatusCode,
                            "editar la fotografía"),
                        (int)response.StatusCode);
                }

                return ApiResult<bool>.Ok(
                    true,
                    "Fotografía actualizada correctamente.");
            }
            catch (TaskCanceledException)
                when (!cancellationToken.IsCancellationRequested)
            {
                return ApiResult<bool>.Fail(
                    "La actualización de la fotografía tardó demasiado.");
            }
            catch (OperationCanceledException)
            {
                return ApiResult<bool>.Fail(
                    "La operación fue cancelada.");
            }
            catch (HttpRequestException)
            {
                return ApiResult<bool>.Fail(
                    "No fue posible conectarse con el servidor para editar la fotografía.");
            }
            catch (IOException)
            {
                return ApiResult<bool>.Fail(
                    "No fue posible leer la fotografía seleccionada.");
            }
            catch (Exception)
            {
                return ApiResult<bool>.Fail(
                    "Ocurrió un error inesperado al editar la fotografía.");
            }
        }

        public Task<ApiResult<bool>> EliminarFotoResultAsync(
            int fotoTerrenoId,
            CancellationToken cancellationToken = default)
        {
            if (fotoTerrenoId <= 0)
            {
                return Task.FromResult(
                    ApiResult<bool>.Fail(
                        "No se recibió una fotografía válida para eliminar."));
            }

            return ApiServiceHelper.SendAsync<FotoTerrenoItem>(
                httpClient,
                HttpMethod.Delete,
                $"api/fotoTerreno/eliminar/{fotoTerrenoId}",
                null,
                "eliminar la fotografía",
                "Fotografía eliminada correctamente.",
                cancellationToken);
        }

        // =========================================================
        // MÉTODOS COMPATIBLES CON EL CÓDIGO EXISTENTE
        // =========================================================

        public async Task<List<FotoTerrenoResponse>> GetFotosPorTerrenoAsync(
            int terrenoId)
        {
            var result = await GetFotosPorTerrenoResultAsync(terrenoId);
            return result.Data ?? new List<FotoTerrenoResponse>();
        }

        public async Task<bool> SubirFotosAsync(
            int terrenoId,
            IEnumerable<FotoTerrenoItem> fotos)
        {
            var result = await SubirFotosResultAsync(terrenoId, fotos);
            return result.Success && result.Data == true;
        }

        public async Task<bool> EditarFotoAsync(
            int fotoTerrenoId,
            string localPath)
        {
            var result = await EditarFotoResultAsync(
                fotoTerrenoId,
                localPath);

            return result.Success && result.Data == true;
        }

        public async Task<bool> EliminarFotoAsync(int fotoTerrenoId)
        {
            var result = await EliminarFotoResultAsync(fotoTerrenoId);
            return result.Success && result.Data == true;
        }

        public string ConstruirUrlCompleta(string? urlFotoTerreno)
        {
            if (string.IsNullOrWhiteSpace(urlFotoTerreno))
                return string.Empty;

            if (urlFotoTerreno.StartsWith(
                    "http",
                    StringComparison.OrdinalIgnoreCase))
            {
                return urlFotoTerreno;
            }

            return $"{urlApiService.BaseUrlApi.TrimEnd('/')}/{urlFotoTerreno.TrimStart('/')}";
        }

        private static string ObtenerContentType(string nombreArchivo)
        {
            string extension =
                Path.GetExtension(nombreArchivo).ToLowerInvariant();

            return extension switch
            {
                ".jpg" => "image/jpeg",
                ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".webp" => "image/webp",
                ".bmp" => "image/bmp",
                _ => "application/octet-stream"
            };
        }
    }
}
