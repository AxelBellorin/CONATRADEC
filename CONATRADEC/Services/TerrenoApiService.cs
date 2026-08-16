using CONATRADEC.Models;
using System.Collections.ObjectModel;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace CONATRADEC.Services
{
    public class TerrenoApiService
    {
        private readonly HttpClient httpClient;

        private readonly JsonSerializerOptions jsonOptions =
            new(JsonSerializerDefaults.Web)
            {
                PropertyNameCaseInsensitive = true
            };

        public TerrenoApiService()
            : this(ApiClientService.Client)
        {
        }

        public TerrenoApiService(HttpClient httpClient)
        {
            this.httpClient = httpClient
                ?? throw new ArgumentNullException(nameof(httpClient));
        }

        // =========================================================
        // MÉTODOS CON RESULTADO DETALLADO
        // =========================================================

        /// <summary>
        /// Endpoint histórico conservado porque todavía puede tener consumidores
        /// fuera de la administración paginada de Terrenos.
        /// </summary>
        public async Task<ApiResult<ObservableCollection<TerrenoResponse>>>
            GetTerrenosResultAsync(
                CancellationToken cancellationToken = default)
        {
            try
            {
                using var response = await httpClient.GetAsync(
                    "api/terreno/listar",
                    cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    return ApiResult<ObservableCollection<TerrenoResponse>>.Fail(
                        ObtenerMensajeHttp(
                            response.StatusCode,
                            "cargar los terrenos"),
                        (int)response.StatusCode);
                }

                ObservableCollection<TerrenoResponse>? terrenos =
                    await response.Content
                        .ReadFromJsonAsync<ObservableCollection<TerrenoResponse>>(
                            jsonOptions,
                            cancellationToken);

                var terrenosActivos = terrenos == null
                    ? new ObservableCollection<TerrenoResponse>()
                    : new ObservableCollection<TerrenoResponse>(
                        terrenos.Where(t => t.Activo != false));

                return ApiResult<ObservableCollection<TerrenoResponse>>.Ok(
                    terrenosActivos);
            }
            catch (TaskCanceledException)
                when (!cancellationToken.IsCancellationRequested)
            {
                return ApiResult<ObservableCollection<TerrenoResponse>>.Fail(
                    "La solicitud tardó demasiado. Verifique su conexión e intente nuevamente.");
            }
            catch (OperationCanceledException)
            {
                return ApiResult<ObservableCollection<TerrenoResponse>>.Fail(
                    "La operación fue cancelada.");
            }
            catch (HttpRequestException)
            {
                return ApiResult<ObservableCollection<TerrenoResponse>>.Fail(
                    "No fue posible conectarse con el servidor. Verifique su conexión.");
            }
            catch (JsonException)
            {
                return ApiResult<ObservableCollection<TerrenoResponse>>.Fail(
                    "El servidor respondió, pero los datos de terrenos no tienen el formato esperado.");
            }
            catch
            {
                return ApiResult<ObservableCollection<TerrenoResponse>>.Fail(
                    "Ocurrió un error inesperado al cargar los terrenos.");
            }
        }

        public async Task<ApiResult<bool>> CreateTerrenoResultAsync(
            TerrenoRequest terreno,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(terreno);

            try
            {
                using var response = await httpClient.PostAsJsonAsync(
                    "api/terreno/crear",
                    terreno,
                    cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    string contenido = await response.Content
                        .ReadAsStringAsync(cancellationToken);

                    string mensaje = ApiErrorMessageParser.Parse(
                        response.StatusCode,
                        contenido,
                        ObtenerMensajeHttp(
                            response.StatusCode,
                            "crear el terreno"));

                    return ApiResult<bool>.Fail(
                        mensaje,
                        (int)response.StatusCode);
                }

                TerrenoVisitaService.MarcarListadoParaRecargar();

                return ApiResult<bool>.Ok(
                    true,
                    "Terreno guardado correctamente.");
            }
            catch (TaskCanceledException)
                when (!cancellationToken.IsCancellationRequested)
            {
                return ApiResult<bool>.Fail(
                    "La solicitud tardó demasiado. Verifique su conexión e intente nuevamente.");
            }
            catch (OperationCanceledException)
            {
                return ApiResult<bool>.Fail("La operación fue cancelada.");
            }
            catch (HttpRequestException)
            {
                return ApiResult<bool>.Fail(
                    "No fue posible conectarse con el servidor. Verifique su conexión.");
            }
            catch
            {
                return ApiResult<bool>.Fail(
                    "Ocurrió un error inesperado al crear el terreno.");
            }
        }

        public async Task<ApiResult<TerrenoResponse>>
            CreateTerrenoRetornandoResultAsync(
                TerrenoRequest terreno,
                CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(terreno);

            try
            {
                using var response = await httpClient.PostAsJsonAsync(
                    "api/terreno/crear",
                    terreno,
                    cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    string contenidoError = await response.Content
                        .ReadAsStringAsync(cancellationToken);

                    string mensaje = ApiErrorMessageParser.Parse(
                        response.StatusCode,
                        contenidoError,
                        ObtenerMensajeHttp(
                            response.StatusCode,
                            "crear el terreno"));

                    return ApiResult<TerrenoResponse>.Fail(
                        mensaje,
                        (int)response.StatusCode);
                }

                string contenido = await response.Content
                    .ReadAsStringAsync(cancellationToken);

                TerrenoResponse? terrenoCreado =
                    IntentarLeerTerrenoCreado(contenido);

                if (terrenoCreado?.TerrenoId is not > 0)
                {
                    /*
                     * El backend actual devuelve el DTO creado. No se ejecuta
                     * un GET de todos los terrenos como mecanismo de respaldo,
                     * porque eso volvería a cargar más de 1,800 registros para
                     * identificar una sola creación.
                     */
                    return ApiResult<TerrenoResponse>.Fail(
                        "El terreno fue procesado, pero el servidor no devolvió el registro creado.");
                }

                TerrenoVisitaService.MarcarListadoParaRecargar();

                return ApiResult<TerrenoResponse>.Ok(
                    terrenoCreado,
                    "Terreno guardado correctamente.");
            }
            catch (TaskCanceledException)
                when (!cancellationToken.IsCancellationRequested)
            {
                return ApiResult<TerrenoResponse>.Fail(
                    "La solicitud tardó demasiado. Verifique su conexión e intente nuevamente.");
            }
            catch (OperationCanceledException)
            {
                return ApiResult<TerrenoResponse>.Fail(
                    "La operación fue cancelada.");
            }
            catch (HttpRequestException)
            {
                return ApiResult<TerrenoResponse>.Fail(
                    "No fue posible conectarse con el servidor. Verifique su conexión.");
            }
            catch
            {
                return ApiResult<TerrenoResponse>.Fail(
                    "Ocurrió un error inesperado al crear el terreno.");
            }
        }

        public async Task<ApiResult<bool>> UpdateTerrenoResultAsync(
            TerrenoRequest terreno,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(terreno);

            if (!terreno.TerrenoId.HasValue || terreno.TerrenoId.Value <= 0)
            {
                return ApiResult<bool>.Fail(
                    "No se recibió un identificador de terreno válido.");
            }

            try
            {
                using var response = await httpClient.PutAsJsonAsync(
                    $"api/terreno/editar/{terreno.TerrenoId}",
                    terreno,
                    cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    string contenido = await response.Content
                        .ReadAsStringAsync(cancellationToken);

                    string mensaje = ApiErrorMessageParser.Parse(
                        response.StatusCode,
                        contenido,
                        ObtenerMensajeHttp(
                            response.StatusCode,
                            "actualizar el terreno"));

                    return ApiResult<bool>.Fail(
                        mensaje,
                        (int)response.StatusCode);
                }

                // Un cambio puede mover el registro a otra página según los
                // filtros/orden global. Al volver al listado se consulta solo
                // la página activa, nunca el universo completo.
                TerrenoVisitaService.MarcarListadoParaRecargar();

                return ApiResult<bool>.Ok(
                    true,
                    "Terreno actualizado correctamente.");
            }
            catch (TaskCanceledException)
                when (!cancellationToken.IsCancellationRequested)
            {
                return ApiResult<bool>.Fail(
                    "La solicitud tardó demasiado. Verifique su conexión e intente nuevamente.");
            }
            catch (OperationCanceledException)
            {
                return ApiResult<bool>.Fail("La operación fue cancelada.");
            }
            catch (HttpRequestException)
            {
                return ApiResult<bool>.Fail(
                    "No fue posible conectarse con el servidor. Verifique su conexión.");
            }
            catch
            {
                return ApiResult<bool>.Fail(
                    "Ocurrió un error inesperado al actualizar el terreno.");
            }
        }

        public async Task<ApiResult<bool>> DeleteTerrenoResultAsync(
            TerrenoRequest terreno,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(terreno);

            if (!terreno.TerrenoId.HasValue || terreno.TerrenoId.Value <= 0)
            {
                return ApiResult<bool>.Fail(
                    "No se recibió un identificador de terreno válido.");
            }

            try
            {
                using var response = await httpClient.DeleteAsync(
                    $"api/terreno/eliminar/{terreno.TerrenoId}",
                    cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    return ApiResult<bool>.Fail(
                        ObtenerMensajeHttp(
                            response.StatusCode,
                            "eliminar el terreno"),
                        (int)response.StatusCode);
                }

                return ApiResult<bool>.Ok(
                    true,
                    "Terreno eliminado correctamente.");
            }
            catch (TaskCanceledException)
                when (!cancellationToken.IsCancellationRequested)
            {
                return ApiResult<bool>.Fail(
                    "La solicitud tardó demasiado. Verifique su conexión e intente nuevamente.");
            }
            catch (OperationCanceledException)
            {
                return ApiResult<bool>.Fail("La operación fue cancelada.");
            }
            catch (HttpRequestException)
            {
                return ApiResult<bool>.Fail(
                    "No fue posible conectarse con el servidor. Verifique su conexión.");
            }
            catch
            {
                return ApiResult<bool>.Fail(
                    "Ocurrió un error inesperado al eliminar el terreno.");
            }
        }

        // =========================================================
        // MÉTODOS ANTERIORES
        // Se conservan para no romper formularios ni otros consumidores.
        // =========================================================

        public async Task<ObservableCollection<TerrenoResponse>> GetTerrenosAsync()
        {
            ApiResult<ObservableCollection<TerrenoResponse>> resultado =
                await GetTerrenosResultAsync();

            return resultado.Success && resultado.Data != null
                ? resultado.Data
                : new ObservableCollection<TerrenoResponse>();
        }

        public async Task<bool> CreateTerrenoAsync(TerrenoRequest terreno)
        {
            ApiResult<bool> resultado = await CreateTerrenoResultAsync(terreno);
            return resultado.Success;
        }

        public async Task<TerrenoResponse?> CreateTerrenoRetornandoAsync(
            TerrenoRequest terreno)
        {
            ApiResult<TerrenoResponse> resultado =
                await CreateTerrenoRetornandoResultAsync(terreno);

            return resultado.Success ? resultado.Data : null;
        }

        public async Task<bool> UpdateTerrenoAsync(TerrenoRequest terreno)
        {
            ApiResult<bool> resultado = await UpdateTerrenoResultAsync(terreno);
            return resultado.Success;
        }

        public async Task<bool> DeleteTerrenoAsync(TerrenoRequest terreno)
        {
            ApiResult<bool> resultado = await DeleteTerrenoResultAsync(terreno);
            return resultado.Success;
        }

        // =========================================================
        // MÉTODOS AUXILIARES
        // =========================================================

        private static string ObtenerMensajeHttp(
            HttpStatusCode statusCode,
            string operacion)
        {
            return statusCode switch
            {
                HttpStatusCode.BadRequest =>
                    $"Los datos enviados no son válidos para {operacion}.",
                HttpStatusCode.Unauthorized =>
                    "La sesión no es válida o ha expirado.",
                HttpStatusCode.Forbidden =>
                    $"No tiene permisos para {operacion}.",
                HttpStatusCode.NotFound =>
                    "No se encontró el terreno solicitado.",
                HttpStatusCode.Conflict =>
                    $"No fue posible {operacion} porque existe un conflicto con los datos.",
                HttpStatusCode.InternalServerError =>
                    "El servidor presentó un error interno. Intente nuevamente.",
                HttpStatusCode.BadGateway or
                HttpStatusCode.ServiceUnavailable or
                HttpStatusCode.GatewayTimeout =>
                    "El servidor no está disponible temporalmente. Intente nuevamente.",
                _ =>
                    $"No fue posible {operacion}. Código del servidor: {(int)statusCode}."
            };
        }

        private TerrenoResponse? IntentarLeerTerrenoCreado(string contenido)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(contenido))
                    return null;

                using var document = JsonDocument.Parse(contenido);
                JsonElement root = document.RootElement;

                if (root.ValueKind != JsonValueKind.Object)
                    return null;

                TerrenoResponse? directo =
                    JsonSerializer.Deserialize<TerrenoResponse>(
                        root.GetRawText(),
                        jsonOptions);

                if (directo?.TerrenoId is > 0)
                    return directo;

                string[] posiblesNodos =
                {
                    "terreno",
                    "data",
                    "registro",
                    "resultado",
                    "item"
                };

                foreach (string nodo in posiblesNodos)
                {
                    if (TryGetPropertyIgnoreCase(
                            root,
                            nodo,
                            out JsonElement elemento) &&
                        elemento.ValueKind == JsonValueKind.Object)
                    {
                        TerrenoResponse? desdeNodo =
                            JsonSerializer.Deserialize<TerrenoResponse>(
                                elemento.GetRawText(),
                                jsonOptions);

                        if (desdeNodo?.TerrenoId is > 0)
                            return desdeNodo;
                    }
                }

                if (TryGetIntIgnoreCase(root, "terrenoId", out int terrenoId) ||
                    TryGetIntIgnoreCase(root, "id", out terrenoId))
                {
                    return new TerrenoResponse
                    {
                        TerrenoId = terrenoId
                    };
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        private static bool TryGetPropertyIgnoreCase(
            JsonElement element,
            string propertyName,
            out JsonElement value)
        {
            foreach (JsonProperty property in element.EnumerateObject())
            {
                if (string.Equals(
                        property.Name,
                        propertyName,
                        StringComparison.OrdinalIgnoreCase))
                {
                    value = property.Value;
                    return true;
                }
            }

            value = default;
            return false;
        }

        private static bool TryGetIntIgnoreCase(
            JsonElement element,
            string propertyName,
            out int value)
        {
            value = 0;

            if (!TryGetPropertyIgnoreCase(
                    element,
                    propertyName,
                    out JsonElement property))
            {
                return false;
            }

            if (property.ValueKind == JsonValueKind.Number &&
                property.TryGetInt32(out value))
            {
                return true;
            }

            if (property.ValueKind == JsonValueKind.String &&
                int.TryParse(property.GetString(), out value))
            {
                return true;
            }

            return false;
        }
    }
}
