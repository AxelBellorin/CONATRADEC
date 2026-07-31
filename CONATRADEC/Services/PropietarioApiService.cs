using CONATRADEC.Models;
using System.Collections.ObjectModel;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace CONATRADEC.Services
{
    public sealed class PropietarioApiService
    {
        private readonly HttpClient httpClient;

        private readonly JsonSerializerOptions jsonOptions =
            new(JsonSerializerDefaults.Web)
            {
                PropertyNameCaseInsensitive = true
            };

        public PropietarioApiService()
            : this(ApiClientService.Client)
        {
        }

        public PropietarioApiService(
            HttpClient httpClient)
        {
            this.httpClient = httpClient ??
                throw new ArgumentNullException(
                    nameof(httpClient));
        }

        public async Task<ApiResult<
            ObservableCollection<PropietarioResponse>>>
            GetPropietariosResultAsync(
                string? buscar = null,
                bool incluirInactivos = false,
                bool paraSeleccionTerreno = false,
                CancellationToken cancellationToken = default)
        {
            try
            {
                string baseRuta =
                    paraSeleccionTerreno
                        ? "api/terreno/propietarios-disponibles"
                        : "api/parametrizacion-acceso/propietarios";

                string ruta =
                    baseRuta +
                    $"?buscar={Uri.EscapeDataString(
                        buscar?.Trim() ?? string.Empty)}";

                if (!paraSeleccionTerreno)
                {
                    ruta +=
                        $"&incluirInactivos=" +
                        incluirInactivos
                            .ToString()
                            .ToLowerInvariant();
                }

                using HttpResponseMessage response =
                    await httpClient.GetAsync(
                        ruta,
                        cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    return ApiResult<
                        ObservableCollection<
                            PropietarioResponse>>.Fail(
                        await ObtenerMensajeAsync(
                            response,
                            "No fue posible cargar los propietarios.",
                            cancellationToken),
                        (int)response.StatusCode);
                }

                List<PropietarioResponse>? items =
                    await response.Content
                        .ReadFromJsonAsync<
                            List<PropietarioResponse>>(
                            jsonOptions,
                            cancellationToken);

                return ApiResult<
                    ObservableCollection<
                        PropietarioResponse>>.Ok(
                    new ObservableCollection<
                        PropietarioResponse>(
                        items ?? []));
            }
            catch (TaskCanceledException)
                when (!cancellationToken
                    .IsCancellationRequested)
            {
                return ApiResult<
                    ObservableCollection<
                        PropietarioResponse>>.Fail(
                    "La consulta tardó demasiado.");
            }
            catch (OperationCanceledException)
            {
                return ApiResult<
                    ObservableCollection<
                        PropietarioResponse>>.Fail(
                    "La operación fue cancelada.");
            }
            catch (HttpRequestException)
            {
                return ApiResult<
                    ObservableCollection<
                        PropietarioResponse>>.Fail(
                    "No fue posible conectarse con el servidor.");
            }
            catch (JsonException)
            {
                return ApiResult<
                    ObservableCollection<
                        PropietarioResponse>>.Fail(
                    "El servidor devolvió propietarios con un " +
                    "formato no reconocido.");
            }
        }

        public async Task<ApiResult<int>>
            CrearPropietarioResultAsync(
                PropietarioGuardarRequest request,
                CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            try
            {
                using HttpResponseMessage response =
                    await httpClient.PostAsJsonAsync(
                        "api/parametrizacion-acceso/propietarios",
                        request,
                        cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    return ApiResult<int>.Fail(
                        await ObtenerMensajeAsync(
                            response,
                            "No fue posible crear el propietario.",
                            cancellationToken),
                        (int)response.StatusCode);
                }

                string contenido =
                    await response.Content
                        .ReadAsStringAsync(
                            cancellationToken);

                int propietarioId =
                    ExtraerPropietarioId(contenido);

                return propietarioId > 0
                    ? ApiResult<int>.Ok(
                        propietarioId,
                        "Propietario creado correctamente.")
                    : ApiResult<int>.Fail(
                        "El propietario se procesó, pero no se " +
                        "recibió su identificador.");
            }
            catch (TaskCanceledException)
                when (!cancellationToken
                    .IsCancellationRequested)
            {
                return ApiResult<int>.Fail(
                    "La solicitud tardó demasiado.");
            }
            catch (HttpRequestException)
            {
                return ApiResult<int>.Fail(
                    "No fue posible conectarse con el servidor.");
            }
            catch (Exception)
            {
                return ApiResult<int>.Fail(
                    "Ocurrió un error inesperado al crear el propietario.");
            }
        }

        public async Task<ApiResult<bool>>
            ActualizarPropietarioResultAsync(
                int propietarioId,
                PropietarioGuardarRequest request,
                CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            if (propietarioId <= 0)
            {
                return ApiResult<bool>.Fail(
                    "No se recibió un propietario válido.");
            }

            try
            {
                using HttpResponseMessage response =
                    await httpClient.PutAsJsonAsync(
                        "api/parametrizacion-acceso/" +
                        $"propietarios/{propietarioId}",
                        request,
                        cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    return ApiResult<bool>.Fail(
                        await ObtenerMensajeAsync(
                            response,
                            "No fue posible actualizar el propietario.",
                            cancellationToken),
                        (int)response.StatusCode);
                }

                return ApiResult<bool>.Ok(
                    true,
                    "Propietario actualizado correctamente.");
            }
            catch (TaskCanceledException)
                when (!cancellationToken
                    .IsCancellationRequested)
            {
                return ApiResult<bool>.Fail(
                    "La solicitud tardó demasiado.");
            }
            catch (HttpRequestException)
            {
                return ApiResult<bool>.Fail(
                    "No fue posible conectarse con el servidor.");
            }
            catch (Exception)
            {
                return ApiResult<bool>.Fail(
                    "Ocurrió un error inesperado al actualizar " +
                    "el propietario.");
            }
        }

        private async Task<string> ObtenerMensajeAsync(
            HttpResponseMessage response,
            string predeterminado,
            CancellationToken cancellationToken)
        {
            try
            {
                string contenido =
                    await response.Content
                        .ReadAsStringAsync(
                            cancellationToken);

                if (!string.IsNullOrWhiteSpace(contenido))
                {
                    using JsonDocument documento =
                        JsonDocument.Parse(contenido);

                    JsonElement raiz =
                        documento.RootElement;

                    foreach (string nombre in new[]
                    {
                        "message",
                        "mensaje",
                        "error"
                    })
                    {
                        if (TryGetPropertyIgnoreCase(
                                raiz,
                                nombre,
                                out JsonElement valor) &&
                            valor.ValueKind ==
                            JsonValueKind.String)
                        {
                            string? texto =
                                valor.GetString();

                            if (!string.IsNullOrWhiteSpace(
                                    texto))
                            {
                                return texto;
                            }
                        }
                    }
                }
            }
            catch
            {
                // Se conserva el mensaje predeterminado.
            }

            return response.StatusCode switch
            {
                HttpStatusCode.Unauthorized =>
                    "La sesión no es válida o ha expirado.",

                HttpStatusCode.Forbidden =>
                    "No tiene permiso para realizar esta operación.",

                HttpStatusCode.Conflict =>
                    "Ya existe un propietario con esos datos.",

                HttpStatusCode.NotFound =>
                    "No se encontró el propietario.",

                _ => predeterminado
            };
        }

        private int ExtraerPropietarioId(
            string contenido)
        {
            if (string.IsNullOrWhiteSpace(contenido))
                return 0;

            try
            {
                using JsonDocument documento =
                    JsonDocument.Parse(contenido);

                JsonElement raiz =
                    documento.RootElement;

                if (TryGetPropertyIgnoreCase(
                        raiz,
                        "data",
                        out JsonElement data) &&
                    data.ValueKind ==
                    JsonValueKind.Object)
                {
                    raiz = data;
                }

                if (TryGetPropertyIgnoreCase(
                        raiz,
                        "propietarioId",
                        out JsonElement id))
                {
                    if (id.ValueKind ==
                            JsonValueKind.Number &&
                        id.TryGetInt32(
                            out int numero))
                    {
                        return numero;
                    }

                    if (id.ValueKind ==
                            JsonValueKind.String &&
                        int.TryParse(
                            id.GetString(),
                            out numero))
                    {
                        return numero;
                    }
                }
            }
            catch
            {
            }

            return 0;
        }

        private static bool TryGetPropertyIgnoreCase(
            JsonElement elemento,
            string nombre,
            out JsonElement valor)
        {
            if (elemento.ValueKind !=
                JsonValueKind.Object)
            {
                valor = default;
                return false;
            }

            foreach (JsonProperty propiedad
                     in elemento.EnumerateObject())
            {
                if (string.Equals(
                        propiedad.Name,
                        nombre,
                        StringComparison.OrdinalIgnoreCase))
                {
                    valor = propiedad.Value;
                    return true;
                }
            }

            valor = default;
            return false;
        }
    }
}
