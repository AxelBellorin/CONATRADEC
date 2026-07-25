using CONATRADEC.Models;
using Microsoft.Maui.ApplicationModel;
using System.Collections.ObjectModel;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace CONATRADEC.Services
{
    /// <summary>
    /// Cliente común para consultar, reactivar y resolver coincidencias
    /// con registros eliminados lógicamente.
    /// </summary>
    public sealed class CatalogosEliminadosApiService
    {
        private readonly HttpClient httpClient;

        private static readonly JsonSerializerOptions JsonOptions =
            new(JsonSerializerDefaults.Web)
            {
                PropertyNameCaseInsensitive = true
            };

        public CatalogosEliminadosApiService()
            : this(ApiClientService.Client)
        {
        }

        public CatalogosEliminadosApiService(
            HttpClient httpClient)
        {
            this.httpClient =
                httpClient ??
                throw new ArgumentNullException(
                    nameof(httpClient));
        }

        public async Task<ApiResult<ObservableCollection<CatalogoEliminadoItem>>>
            ListarAsync(
                string catalogo,
                CancellationToken cancellationToken = default)
        {
            if (!CatalogoEliminadoCodigos.TryGet(
                    catalogo,
                    out _))
            {
                return ApiResult<
                    ObservableCollection<CatalogoEliminadoItem>>
                    .Fail(
                        "El catálogo solicitado no admite reactivación.");
            }

            try
            {
                using HttpResponseMessage response =
                    await httpClient.GetAsync(
                        $"api/catalogos-eliminados/{Uri.EscapeDataString(catalogo)}",
                        cancellationToken);

                string contenido =
                    await response.Content.ReadAsStringAsync(
                        cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    return ApiResult<
                        ObservableCollection<CatalogoEliminadoItem>>
                        .Fail(
                            ApiErrorMessageParser.Parse(
                                response.StatusCode,
                                contenido,
                                "No fue posible cargar los registros eliminados."),
                            (int)response.StatusCode);
                }

                CatalogoEliminadoEnvelope? envelope =
                    JsonSerializer.Deserialize<CatalogoEliminadoEnvelope>(
                        contenido,
                        JsonOptions);

                return ApiResult<
                    ObservableCollection<CatalogoEliminadoItem>>
                    .Ok(
                        new ObservableCollection<CatalogoEliminadoItem>(
                            envelope?.Data ??
                            Enumerable.Empty<CatalogoEliminadoItem>()),
                        envelope?.Message ??
                        string.Empty);
            }
            catch (TaskCanceledException)
                when (!cancellationToken.IsCancellationRequested)
            {
                return ApiResult<
                    ObservableCollection<CatalogoEliminadoItem>>
                    .Fail(
                        "La carga tardó demasiado. Verifique su conexión.");
            }
            catch (OperationCanceledException)
            {
                return ApiResult<
                    ObservableCollection<CatalogoEliminadoItem>>
                    .Fail(
                        "La operación fue cancelada.");
            }
            catch (HttpRequestException)
            {
                return ApiResult<
                    ObservableCollection<CatalogoEliminadoItem>>
                    .Fail(
                        "No fue posible comunicarse con el servidor.");
            }
            catch (JsonException)
            {
                return ApiResult<
                    ObservableCollection<CatalogoEliminadoItem>>
                    .Fail(
                        "El servidor respondió, pero el listado no tiene el formato esperado.");
            }
            catch
            {
                return ApiResult<
                    ObservableCollection<CatalogoEliminadoItem>>
                    .Fail(
                        "Ocurrió un error inesperado al cargar los registros eliminados.");
            }
        }

        public async Task<ApiResult<bool>> ReactivarAsync(
            string catalogo,
            int id,
            CancellationToken cancellationToken = default)
        {
            if (id <= 0)
            {
                return ApiResult<bool>.Fail(
                    "El registro seleccionado no es válido.");
            }

            ApiResult<bool> resultado =
                await EnviarSinDatosAsync<object>(
                    HttpMethod.Put,
                    $"api/catalogos-eliminados/{Uri.EscapeDataString(catalogo)}/{id}/reactivar",
                    null,
                    "No fue posible reactivar el registro.",
                    "Registro reactivado correctamente.",
                    cancellationToken);

            if (resultado.Success)
            {
                CatalogoCacheInvalidator.Limpiar(
                    catalogo);
            }

            return resultado;
        }

        /// <summary>
        /// Sustituye el POST normal de los catálogos compatibles.
        /// Cuando encuentra un registro inactivo permite reactivarlo
        /// y actualizarlo con los datos escritos en el formulario.
        /// </summary>
        public async Task<ApiResult<bool>> CrearConResolucionAsync<TRequest>(
            string catalogo,
            TRequest request,
            string errorMessage,
            string successMessage,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            try
            {
                using HttpResponseMessage response =
                    await httpClient.PostAsJsonAsync(
                        $"api/catalogos-eliminados/{Uri.EscapeDataString(catalogo)}/crear",
                        request,
                        JsonOptions,
                        cancellationToken);

                string contenido =
                    await response.Content.ReadAsStringAsync(
                        cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    string mensaje =
                        ObtenerMensaje(
                            contenido,
                            successMessage);

                    return ApiResult<bool>.Ok(
                        true,
                        mensaje);
                }

                if (response.StatusCode != HttpStatusCode.Conflict)
                {
                    return ApiResult<bool>.Fail(
                        ApiErrorMessageParser.Parse(
                            response.StatusCode,
                            contenido,
                            errorMessage),
                        (int)response.StatusCode);
                }

                CatalogoConflictoEnvelope? conflicto =
                    JsonSerializer.Deserialize<CatalogoConflictoEnvelope>(
                        contenido,
                        JsonOptions);

                CatalogoEliminadoItem? registro =
                    conflicto?.Data?.Registro;

                if (registro == null ||
                    registro.Id <= 0)
                {
                    return ApiResult<bool>.Fail(
                        ApiErrorMessageParser.Parse(
                            response.StatusCode,
                            contenido,
                            errorMessage),
                        (int)response.StatusCode);
                }

                string? decision =
                    await MostrarOpcionesAsync(
                        registro,
                        conflicto?.Data?.PuedeCrearNuevo == true);

                if (decision ==
                    OpcionReactivar)
                {
                    return await EnviarSinDatosAsync(
                        HttpMethod.Put,
                        $"api/catalogos-eliminados/{Uri.EscapeDataString(catalogo)}/{registro.Id}/reactivar-con-datos",
                        request,
                        "No fue posible reactivar el registro.",
                        "Registro reactivado correctamente.",
                        cancellationToken);
                }

                if (decision ==
                    OpcionCrearNuevo)
                {
                    return await EnviarSinDatosAsync(
                        HttpMethod.Post,
                        $"api/catalogos-eliminados/{Uri.EscapeDataString(catalogo)}/crear-confirmado",
                        request,
                        errorMessage,
                        successMessage,
                        cancellationToken);
                }

                return ApiResult<bool>.Fail(
                    "La creación fue cancelada.");
            }
            catch (TaskCanceledException)
                when (!cancellationToken.IsCancellationRequested)
            {
                return ApiResult<bool>.Fail(
                    "La solicitud tardó demasiado. Intente nuevamente.");
            }
            catch (OperationCanceledException)
            {
                return ApiResult<bool>.Fail(
                    "La operación fue cancelada.");
            }
            catch (HttpRequestException)
            {
                return ApiResult<bool>.Fail(
                    "No fue posible comunicarse con el servidor.");
            }
            catch (JsonException)
            {
                return ApiResult<bool>.Fail(
                    "El servidor respondió, pero el conflicto no tiene el formato esperado.");
            }
            catch
            {
                return ApiResult<bool>.Fail(
                    errorMessage);
            }
        }


        /// <summary>
        /// Antes de crear un usuario verifica si nombre, correo o
        /// identificación pertenecen a un usuario inactivo. Los usuarios
        /// nunca se duplican y su contraseña anterior se conserva.
        /// Devuelve null cuando no existe una coincidencia inactiva y el
        /// servicio debe continuar con el endpoint normal de creación.
        /// </summary>
        public async Task<ApiResult<TResponse>?>
            IntentarResolverUsuarioInactivoAsync<TRequest, TResponse>(
                TRequest request,
                CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            try
            {
                using HttpResponseMessage verificar =
                    await httpClient.PostAsJsonAsync(
                        "api/catalogos-eliminados/usuario/coincidencia",
                        request,
                        JsonOptions,
                        cancellationToken);

                if (verificar.StatusCode ==
                    HttpStatusCode.NoContent)
                {
                    return null;
                }

                string contenido =
                    await verificar.Content.ReadAsStringAsync(
                        cancellationToken);

                if (!verificar.IsSuccessStatusCode)
                {
                    return ApiResult<TResponse>.Fail(
                        ApiErrorMessageParser.Parse(
                            verificar.StatusCode,
                            contenido,
                            "No fue posible validar si el usuario ya existía."),
                        (int)verificar.StatusCode);
                }

                CatalogoConflictoEnvelope? conflicto =
                    JsonSerializer.Deserialize<CatalogoConflictoEnvelope>(
                        contenido,
                        JsonOptions);

                CatalogoEliminadoItem? registro =
                    conflicto?.Data?.Registro;

                if (registro == null ||
                    registro.Id <= 0)
                {
                    return ApiResult<TResponse>.Fail(
                        "El servidor encontró una coincidencia, pero no devolvió el usuario inactivo.");
                }

                string? decision =
                    await MostrarOpcionesAsync(
                        registro,
                        false);

                if (decision != OpcionReactivar)
                {
                    return ApiResult<TResponse>.Fail(
                        "La creación fue cancelada.");
                }

                using var message =
                    new HttpRequestMessage(
                        HttpMethod.Put,
                        $"api/catalogos-eliminados/usuario/{registro.Id}/reactivar-con-datos")
                    {
                        Content =
                            JsonContent.Create(
                                request,
                                options: JsonOptions)
                    };

                using HttpResponseMessage response =
                    await httpClient.SendAsync(
                        message,
                        cancellationToken);

                string respuesta =
                    await response.Content.ReadAsStringAsync(
                        cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    return ApiResult<TResponse>.Fail(
                        ApiErrorMessageParser.Parse(
                            response.StatusCode,
                            respuesta,
                            "No fue posible reactivar el usuario."),
                        (int)response.StatusCode);
                }

                CatalogoOperacionEnvelope<TResponse>? envelope =
                    JsonSerializer.Deserialize<
                        CatalogoOperacionEnvelope<TResponse>>(
                            respuesta,
                            JsonOptions);

                if (envelope is null ||
                    envelope.Data is null)
                {
                    return ApiResult<TResponse>.Fail(
                        "El usuario fue procesado, pero la API no devolvió sus datos.");
                }

                CatalogoCacheInvalidator.Limpiar(
                    CatalogoEliminadoCodigos.Usuario);

                return ApiResult<TResponse>.Ok(
                    envelope.Data,
                    string.IsNullOrWhiteSpace(
                        envelope.Message)
                        ? "Usuario reactivado correctamente."
                        : envelope.Message);
            }
            catch (TaskCanceledException)
                when (!cancellationToken.IsCancellationRequested)
            {
                return ApiResult<TResponse>.Fail(
                    "La solicitud tardó demasiado. Intente nuevamente.");
            }
            catch (OperationCanceledException)
            {
                return ApiResult<TResponse>.Fail(
                    "La operación fue cancelada.");
            }
            catch (HttpRequestException)
            {
                return ApiResult<TResponse>.Fail(
                    "No fue posible comunicarse con el servidor.");
            }
            catch (JsonException)
            {
                return ApiResult<TResponse>.Fail(
                    "El servidor respondió, pero los datos del usuario no tienen el formato esperado.");
            }
            catch
            {
                return ApiResult<TResponse>.Fail(
                    "Ocurrió un error inesperado al validar el usuario.");
            }
        }

        private const string OpcionReactivar =
            "Reactivar y usar estos datos";

        private const string OpcionCrearNuevo =
            "Crear un registro diferente";

        private static async Task<string?> MostrarOpcionesAsync(
            CatalogoEliminadoItem registro,
            bool puedeCrearNuevo)
        {
            return await MainThread.InvokeOnMainThreadAsync(
                async () =>
                {
                    Page? pagina =
                        Application.Current?
                            .Windows
                            .FirstOrDefault()?
                            .Page;

                    if (pagina == null)
                        return null;

                    string mensaje =
                        $"Ya existe un registro eliminado que coincide con " +
                        $"'{registro.Titulo}'.\n\n" +
                        "Puede reactivarlo conservando su identificador " +
                        "e historial.";

                    if (puedeCrearNuevo)
                    {
                        return await pagina.DisplayActionSheet(
                            mensaje,
                            "Cancelar",
                            null,
                            OpcionReactivar,
                            OpcionCrearNuevo);
                    }

                    return await pagina.DisplayActionSheet(
                        mensaje,
                        "Cancelar",
                        null,
                        OpcionReactivar);
                });
        }

        private async Task<ApiResult<bool>> EnviarSinDatosAsync<TRequest>(
            HttpMethod method,
            string route,
            TRequest? request,
            string errorMessage,
            string successMessage,
            CancellationToken cancellationToken)
        {
            try
            {
                using var message =
                    new HttpRequestMessage(
                        method,
                        route);

                if (request is not null)
                {
                    message.Content =
                        JsonContent.Create(
                            request,
                            options: JsonOptions);
                }

                using HttpResponseMessage response =
                    await httpClient.SendAsync(
                        message,
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
                            errorMessage),
                        (int)response.StatusCode);
                }

                return ApiResult<bool>.Ok(
                    true,
                    ObtenerMensaje(
                        contenido,
                        successMessage));
            }
            catch (TaskCanceledException)
                when (!cancellationToken.IsCancellationRequested)
            {
                return ApiResult<bool>.Fail(
                    "La solicitud tardó demasiado. Intente nuevamente.");
            }
            catch (OperationCanceledException)
            {
                return ApiResult<bool>.Fail(
                    "La operación fue cancelada.");
            }
            catch (HttpRequestException)
            {
                return ApiResult<bool>.Fail(
                    "No fue posible comunicarse con el servidor.");
            }
            catch
            {
                return ApiResult<bool>.Fail(
                    errorMessage);
            }
        }

        private static string ObtenerMensaje(
            string contenido,
            string valorPredeterminado)
        {
            if (string.IsNullOrWhiteSpace(contenido))
                return valorPredeterminado;

            try
            {
                using JsonDocument document =
                    JsonDocument.Parse(contenido);

                if (document.RootElement.ValueKind ==
                        JsonValueKind.Object &&
                    document.RootElement.TryGetProperty(
                        "message",
                        out JsonElement message) &&
                    message.ValueKind ==
                        JsonValueKind.String)
                {
                    return message.GetString() ??
                        valorPredeterminado;
                }

                if (document.RootElement.ValueKind ==
                        JsonValueKind.Object &&
                    document.RootElement.TryGetProperty(
                        "mensaje",
                        out JsonElement mensaje) &&
                    mensaje.ValueKind ==
                        JsonValueKind.String)
                {
                    return mensaje.GetString() ??
                        valorPredeterminado;
                }
            }
            catch
            {
                // La respuesta correcta puede no contener JSON.
            }

            return valorPredeterminado;
        }
    }
}