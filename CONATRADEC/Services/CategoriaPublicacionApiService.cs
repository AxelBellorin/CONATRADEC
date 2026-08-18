using CONATRADEC.Models;
using Microsoft.Maui.ApplicationModel;
using System.Collections.ObjectModel;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace CONATRADEC.Services
{
    public sealed class CategoriaPublicacionApiService
    {
        private const string RutaBase =
            "api/administracion/categorias-publicacion";

        private const string OpcionReactivar =
            "Reactivar y usar estos datos";

        private readonly HttpClient httpClient;

        public CategoriaPublicacionApiService()
            : this(ApiClientService.Client)
        {
        }

        public CategoriaPublicacionApiService(
            HttpClient httpClient)
        {
            this.httpClient =
                httpClient ??
                throw new ArgumentNullException(
                    nameof(httpClient));
        }

        public Task<ApiResult<ObservableCollection<
            CategoriaPublicacionCatalogoResponse>>> GetAsync(
                string? buscar,
                CancellationToken cancellationToken = default)
        {
            string ruta =
                RutaBase;

            if (!string.IsNullOrWhiteSpace(buscar))
            {
                ruta +=
                    "?buscar=" +
                    Uri.EscapeDataString(
                        buscar.Trim());
            }

            return ConfiguracionApiServiceHelper
                .GetCollectionAsync<
                    CategoriaPublicacionCatalogoResponse>(
                        httpClient,
                        ruta,
                        "los tipos de publicación",
                        cancellationToken);
        }

        public async Task<ApiResult<
            CategoriaPublicacionCatalogoResponse>>
            ObtenerAsync(
                int categoriaId,
                CancellationToken cancellationToken = default)
        {
            if (categoriaId <= 0)
            {
                return ApiResult<
                    CategoriaPublicacionCatalogoResponse>.Fail(
                        "El tipo de publicación seleccionado no es válido.");
            }

            try
            {
                using HttpResponseMessage response =
                    await httpClient.GetAsync(
                        $"{RutaBase}/{categoriaId}",
                        cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    string mensaje =
                        await ApiServiceHelper
                            .ReadResponseMessageAsync(
                                response,
                                "No fue posible obtener el tipo de publicación.",
                                cancellationToken);

                    return ApiResult<
                        CategoriaPublicacionCatalogoResponse>.Fail(
                            mensaje,
                            (int)response.StatusCode);
                }

                CategoriaPublicacionCatalogoResponse? data =
                    await response.Content
                        .ReadFromJsonAsync<
                            CategoriaPublicacionCatalogoResponse>(
                                cancellationToken:
                                    cancellationToken);

                if (data == null)
                {
                    return ApiResult<
                        CategoriaPublicacionCatalogoResponse>.Fail(
                            "El servidor no devolvió los datos del tipo de publicación.");
                }

                return ApiResult<
                    CategoriaPublicacionCatalogoResponse>.Ok(
                        data);
            }
            catch (TaskCanceledException)
                when (!cancellationToken.IsCancellationRequested)
            {
                return ApiResult<
                    CategoriaPublicacionCatalogoResponse>.Fail(
                        "La solicitud tardó demasiado. Verifique su conexión e intente nuevamente.");
            }
            catch (OperationCanceledException)
            {
                return ApiResult<
                    CategoriaPublicacionCatalogoResponse>.Fail(
                        "La operación fue cancelada.");
            }
            catch (HttpRequestException)
            {
                return ApiResult<
                    CategoriaPublicacionCatalogoResponse>.Fail(
                        "No fue posible conectarse con el servidor. Verifique su conexión.");
            }
            catch (JsonException)
            {
                return ApiResult<
                    CategoriaPublicacionCatalogoResponse>.Fail(
                        "El servidor respondió, pero los datos del tipo de publicación no tienen el formato esperado.");
            }
            catch
            {
                return ApiResult<
                    CategoriaPublicacionCatalogoResponse>.Fail(
                        "Ocurrió un error inesperado al obtener el tipo de publicación.");
            }
        }

        public async Task<ApiResult<bool>> CrearAsync(
            CategoriaPublicacionGuardarRequest request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            try
            {
                using HttpResponseMessage response =
                    await httpClient.PostAsJsonAsync(
                        RutaBase,
                        request,
                        cancellationToken);

                string contenido =
                    await response.Content.ReadAsStringAsync(
                        cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    PublicacionListadoEstadoService
                        .MarcarActualizacion();

                    return ApiResult<bool>.Ok(
                        true,
                        ObtenerMensaje(
                            contenido,
                            "Tipo de publicación creado correctamente."));
                }

                if (response.StatusCode !=
                    HttpStatusCode.Conflict)
                {
                    return ApiResult<bool>.Fail(
                        ApiErrorMessageParser.Parse(
                            response.StatusCode,
                            contenido,
                            "No fue posible crear el tipo de publicación."),
                        (int)response.StatusCode);
                }

                int registroId =
                    ObtenerEnteroJson(
                        contenido,
                        "registroId");

                string registroNombre =
                    ObtenerTextoJson(
                        contenido,
                        "registroNombre");

                if (registroId <= 0)
                {
                    return ApiResult<bool>.Fail(
                        ApiErrorMessageParser.Parse(
                            response.StatusCode,
                            contenido,
                            "Ya existe un tipo de publicación con ese nombre."),
                        (int)response.StatusCode);
                }

                bool reactivar =
                    await ConfirmarReactivacionAsync(
                        string.IsNullOrWhiteSpace(
                            registroNombre)
                            ? request
                                .NombreCategoriaPublicacion
                            : registroNombre);

                if (!reactivar)
                {
                    return ApiResult<bool>.Fail(
                        "La creación fue cancelada.");
                }

                ApiResult<bool> resultado =
                    await ConfiguracionApiServiceHelper
                        .SendAsync(
                            httpClient,
                            HttpMethod.Put,
                            $"{RutaBase}/{registroId}/reactivar-con-datos",
                            request,
                            "No fue posible reactivar el tipo de publicación.",
                            "Tipo de publicación reactivado correctamente.",
                            cancellationToken);

                if (resultado.Success)
                {
                    PublicacionListadoEstadoService
                        .MarcarActualizacion();
                }

                return resultado;
            }
            catch (TaskCanceledException)
                when (!cancellationToken.IsCancellationRequested)
            {
                return ApiResult<bool>.Fail(
                    "La solicitud tardó demasiado. Verifique su conexión e intente nuevamente.");
            }
            catch (OperationCanceledException)
            {
                return ApiResult<bool>.Fail(
                    "La operación fue cancelada.");
            }
            catch (HttpRequestException)
            {
                return ApiResult<bool>.Fail(
                    "No fue posible conectarse con el servidor. Verifique su conexión.");
            }
            catch (JsonException)
            {
                return ApiResult<bool>.Fail(
                    "El servidor respondió, pero el conflicto no tiene el formato esperado.");
            }
            catch
            {
                return ApiResult<bool>.Fail(
                    "No fue posible crear el tipo de publicación.");
            }
        }

        public async Task<ApiResult<bool>> ActualizarAsync(
            int categoriaId,
            CategoriaPublicacionGuardarRequest request,
            CancellationToken cancellationToken = default)
        {
            if (categoriaId <= 0)
            {
                return ApiResult<bool>.Fail(
                    "El tipo de publicación seleccionado no es válido.");
            }

            ApiResult<bool> result =
                await ConfiguracionApiServiceHelper
                    .SendAsync(
                        httpClient,
                        HttpMethod.Put,
                        $"{RutaBase}/{categoriaId}",
                        request,
                        "No fue posible actualizar el tipo de publicación.",
                        "Tipo de publicación actualizado correctamente.",
                        cancellationToken);

            if (result.Success)
            {
                PublicacionListadoEstadoService
                    .MarcarActualizacion();
            }

            return result;
        }

        public async Task<ApiResult<bool>> DesactivarAsync(
            int categoriaId,
            CancellationToken cancellationToken = default)
        {
            if (categoriaId <= 0)
            {
                return ApiResult<bool>.Fail(
                    "El tipo de publicación seleccionado no es válido.");
            }

            ApiResult<bool> result =
                await ConfiguracionApiServiceHelper
                    .SendAsync<object>(
                        httpClient,
                        HttpMethod.Put,
                        $"{RutaBase}/{categoriaId}/eliminar",
                        null,
                        "No fue posible desactivar el tipo de publicación.",
                        "Tipo de publicación desactivado correctamente.",
                        cancellationToken);

            if (result.Success)
            {
                PublicacionListadoEstadoService
                    .MarcarActualizacion();
            }

            return result;
        }

        private static async Task<bool>
            ConfirmarReactivacionAsync(
                string nombre)
        {
            return await MainThread
                .InvokeOnMainThreadAsync(
                    async () =>
                    {
                        Page? pagina =
                            Application.Current?
                                .Windows
                                .FirstOrDefault()?
                                .Page;

                        if (pagina == null)
                            return false;

                        string? opcion =
                            await pagina.DisplayActionSheet(
                                "Ya existe un tipo de publicación eliminado " +
                                $"que coincide con “{nombre}”. " +
                                "Puede reactivarlo conservando su identificador " +
                                "e historial.",
                                "Cancelar",
                                null,
                                OpcionReactivar);

                        return string.Equals(
                            opcion,
                            OpcionReactivar,
                            StringComparison.Ordinal);
                    });
        }

        private static string ObtenerMensaje(
            string contenido,
            string fallback)
        {
            string mensaje =
                ObtenerTextoJson(
                    contenido,
                    "message");

            if (string.IsNullOrWhiteSpace(mensaje))
            {
                mensaje =
                    ObtenerTextoJson(
                        contenido,
                        "mensaje");
            }

            return string.IsNullOrWhiteSpace(mensaje)
                ? fallback
                : mensaje;
        }

        private static string ObtenerTextoJson(
            string contenido,
            string propiedad)
        {
            if (string.IsNullOrWhiteSpace(contenido))
                return string.Empty;

            using JsonDocument document =
                JsonDocument.Parse(contenido);

            if (!TryGetProperty(
                    document.RootElement,
                    propiedad,
                    out JsonElement valor))
            {
                return string.Empty;
            }

            return valor.ValueKind ==
                    JsonValueKind.String
                ? valor.GetString() ?? string.Empty
                : valor.ToString();
        }

        private static int ObtenerEnteroJson(
            string contenido,
            string propiedad)
        {
            if (string.IsNullOrWhiteSpace(contenido))
                return 0;

            using JsonDocument document =
                JsonDocument.Parse(contenido);

            if (!TryGetProperty(
                    document.RootElement,
                    propiedad,
                    out JsonElement valor))
            {
                return 0;
            }

            if (valor.ValueKind ==
                    JsonValueKind.Number &&
                valor.TryGetInt32(out int numero))
            {
                return numero;
            }

            return int.TryParse(
                    valor.ToString(),
                    out numero)
                ? numero
                : 0;
        }

        private static bool TryGetProperty(
            JsonElement elemento,
            string nombre,
            out JsonElement valor)
        {
            foreach (
                JsonProperty propiedad
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
