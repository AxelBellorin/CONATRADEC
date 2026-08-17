using CONATRADEC.Models;
using Microsoft.Maui.ApplicationModel;
using System.Collections.ObjectModel;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace CONATRADEC.Services
{
    public sealed class ElementoQuimicoApiService
    {
        private const string RutaAdministrativa =
            "api/administracion/elementos-quimicos";

        private const string CodigoInactivoExistente =
            "ELEMENTO_QUIMICO_INACTIVO_EXISTENTE";

        private const string OpcionReactivar =
            "Reactivar y usar estos datos";

        private const string OpcionCrearNuevo =
            "Crear un registro diferente";

        private readonly HttpClient httpClient;

        private static readonly SemaphoreSlim CacheLock =
            new(1, 1);

        private static List<ElementoQuimicoResponse>?
            cacheFormulario;

        private static DateTime cacheCreadoUtc;

        private static readonly TimeSpan DuracionCache =
            TimeSpan.FromMinutes(20);

        private static readonly JsonSerializerOptions JsonOptions =
            new(JsonSerializerDefaults.Web)
            {
                PropertyNameCaseInsensitive = true
            };

        public ElementoQuimicoApiService()
            : this(ApiClientService.Client)
        {
        }

        public ElementoQuimicoApiService(
            HttpClient httpClient)
        {
            this.httpClient =
                httpClient
                ?? throw new ArgumentNullException(
                    nameof(httpClient));
        }

        /// <summary>
        /// Listado completo conservado para análisis, fuentes de nutrientes
        /// y demás formularios que utilizan este catálogo como selector.
        /// Este endpoint histórico no se sustituye por la API administrativa.
        /// </summary>
        public async Task<ApiResult<
            ObservableCollection<ElementoQuimicoResponse>>>
            GetElementoQuimicoResultAsync(
                CancellationToken cancellationToken = default)
        {
            /*
             * En una sesión offline se utiliza directamente el motor local.
             * Esto evita reutilizar una lista vacía guardada por otra sesión y
             * reduce el tiempo de apertura de la edición de análisis.
             */
            if (ModoSesionService.EsOffline)
            {
                ObservableCollection<ElementoQuimicoResponse> locales =
                    await AnalisisCatalogosOfflineDirectService
                        .ObtenerElementosAsync(
                            cancellationToken);

                return locales.Count > 0
                    ? ApiResult<ObservableCollection<
                        ElementoQuimicoResponse>>.Ok(locales)
                    : ApiResult<ObservableCollection<
                        ElementoQuimicoResponse>>.Fail(
                            "El motor local no contiene elementos químicos. " +
                            "Inicie una sesión en línea y utilice Descargar todo.");
            }

            if (CacheVigente())
            {
                return ApiResult<ObservableCollection<
                    ElementoQuimicoResponse>>
                    .Ok(
                        CrearColeccionCache());
            }

            await CacheLock.WaitAsync(
                cancellationToken);

            try
            {
                if (CacheVigente())
                {
                    return ApiResult<ObservableCollection<
                        ElementoQuimicoResponse>>
                        .Ok(
                            CrearColeccionCache());
                }

                ApiResult<ObservableCollection<
                    ElementoQuimicoResponse>> result =
                    await ApiServiceHelper
                        .GetCollectionAsync<ElementoQuimicoResponse>(
                            httpClient,
                            "api/elemento-quimico/listar",
                            "los elementos químicos",
                            cancellationToken);

                if (!result.Success ||
                    result.Data == null)
                {
                    return result;
                }

                cacheFormulario =
                    result.Data
                        .Where(elemento =>
                            elemento.ElementoQuimicosId is > 0)
                        .OrderBy(elemento =>
                            elemento.NombreElementoQuimico)
                        .ToList();

                cacheCreadoUtc = DateTime.UtcNow;

                return ApiResult<ObservableCollection<
                    ElementoQuimicoResponse>>
                    .Ok(
                        CrearColeccionCache(),
                        result.Message);
            }
            finally
            {
                CacheLock.Release();
            }
        }

        /// <summary>
        /// Consulta paginada exclusiva de la pantalla administrativa moderna.
        /// Solo la página solicitada viaja al cliente y permanece en memoria.
        /// </summary>
        public async Task<ApiResult<ElementoQuimicoPaginaResponse>>
            BuscarElementosAsync(
                string? buscar,
                int pagina,
                int tamanoPagina,
                CancellationToken cancellationToken = default)
        {
            pagina = Math.Max(1, pagina);
            tamanoPagina = Math.Clamp(tamanoPagina, 5, 100);

            string ruta =
                RutaAdministrativa +
                $"?pagina={pagina}" +
                $"&tamanoPagina={tamanoPagina}";

            if (!string.IsNullOrWhiteSpace(buscar))
            {
                ruta +=
                    $"&buscar={Uri.EscapeDataString(buscar.Trim())}";
            }

            try
            {
                using HttpResponseMessage response =
                    await httpClient.GetAsync(
                        ruta,
                        cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    return ApiResult<ElementoQuimicoPaginaResponse>
                        .Fail(
                            await ApiServiceHelper
                                .ReadResponseMessageAsync(
                                    response,
                                    "No fue posible cargar los elementos químicos.",
                                    cancellationToken),
                            (int)response.StatusCode);
                }

                ElementoQuimicoPaginaResponse? data =
                    await response.Content
                        .ReadFromJsonAsync<ElementoQuimicoPaginaResponse>(
                            JsonOptions,
                            cancellationToken);

                return ApiResult<ElementoQuimicoPaginaResponse>
                    .Ok(
                        data ??
                        new ElementoQuimicoPaginaResponse());
            }
            catch (TaskCanceledException)
                when (!cancellationToken.IsCancellationRequested)
            {
                return ApiResult<ElementoQuimicoPaginaResponse>
                    .Fail(
                        "La carga de elementos químicos tardó demasiado. Intente nuevamente.");
            }
            catch (OperationCanceledException)
            {
                return ApiResult<ElementoQuimicoPaginaResponse>
                    .Fail(
                        "La operación fue cancelada.");
            }
            catch (HttpRequestException)
            {
                return ApiResult<ElementoQuimicoPaginaResponse>
                    .Fail(
                        "No fue posible comunicarse con el servidor para cargar los elementos químicos.");
            }
            catch (JsonException)
            {
                return ApiResult<ElementoQuimicoPaginaResponse>
                    .Fail(
                        "El servidor respondió, pero el listado de elementos químicos no tiene el formato esperado.");
            }
            catch
            {
                return ApiResult<ElementoQuimicoPaginaResponse>
                    .Fail(
                        "Ocurrió un error inesperado al cargar los elementos químicos.");
            }
        }

        /// <summary>
        /// Obtiene un registro activo directamente desde la API administrativa.
        /// Ver y Editar utilizan esta consulta para no abrir datos antiguos
        /// conservados en la tarjeta de la página.
        /// </summary>
        public async Task<ApiResult<ElementoQuimicoResponse>>
            GetElementoQuimicoAdminByIdResultAsync(
                int id,
                CancellationToken cancellationToken = default)
        {
            if (id <= 0)
            {
                return ApiResult<ElementoQuimicoResponse>.Fail(
                    "El identificador del elemento químico no es válido.");
            }

            try
            {
                using HttpResponseMessage response =
                    await httpClient.GetAsync(
                        $"{RutaAdministrativa}/{id}",
                        cancellationToken);

                string contenido =
                    await response.Content.ReadAsStringAsync(
                        cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    return ApiResult<ElementoQuimicoResponse>.Fail(
                        ApiErrorMessageParser.Parse(
                            response.StatusCode,
                            contenido,
                            "No fue posible cargar el elemento químico."),
                        (int)response.StatusCode);
                }

                ElementoQuimicoResponse? data =
                    JsonSerializer.Deserialize<ElementoQuimicoResponse>(
                        contenido,
                        JsonOptions);

                if (data?.ElementoQuimicosId is not > 0)
                {
                    return ApiResult<ElementoQuimicoResponse>.Fail(
                        "El servidor respondió, pero no devolvió un elemento químico válido.");
                }

                return ApiResult<ElementoQuimicoResponse>.Ok(data);
            }
            catch (TaskCanceledException)
                when (!cancellationToken.IsCancellationRequested)
            {
                return ApiResult<ElementoQuimicoResponse>.Fail(
                    "La carga del elemento químico tardó demasiado. Intente nuevamente.");
            }
            catch (OperationCanceledException)
            {
                return ApiResult<ElementoQuimicoResponse>.Fail(
                    "La operación fue cancelada.");
            }
            catch (HttpRequestException)
            {
                return ApiResult<ElementoQuimicoResponse>.Fail(
                    "No fue posible comunicarse con el servidor para cargar el elemento químico.");
            }
            catch (JsonException)
            {
                return ApiResult<ElementoQuimicoResponse>.Fail(
                    "El servidor respondió, pero el elemento químico no tiene el formato esperado.");
            }
            catch
            {
                return ApiResult<ElementoQuimicoResponse>.Fail(
                    "Ocurrió un error inesperado al cargar el elemento químico.");
            }
        }

        /// <summary>
        /// Crea mediante la API administrativa. Si la identidad coincide con
        /// un registro inactivo conserva el comportamiento histórico: permite
        /// reactivarlo con los datos escritos o crear un registro diferente.
        /// </summary>
        public async Task<ApiResult<ElementoQuimicoResponse>>
            CreateElementoQuimicoAdminResultAsync(
                ElementoQuimicoRequest elemento,
                CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(elemento);

            ApiResult<ElementoQuimicoResponse> result =
                await CrearConResolucionAsync(
                    elemento,
                    cancellationToken);

            if (result.Success)
                LimpiarCache();

            return result;
        }

        public async Task<ApiResult<ElementoQuimicoResponse>>
            UpdateElementoQuimicoAdminResultAsync(
                ElementoQuimicoRequest elemento,
                CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(elemento);

            if (!elemento.ElementoQuimicosId.HasValue ||
                elemento.ElementoQuimicosId.Value <= 0)
            {
                return ApiResult<ElementoQuimicoResponse>.Fail(
                    "No se recibió un identificador de elemento químico válido.");
            }

            ApiResult<ElementoQuimicoResponse> result =
                await EnviarYLeerElementoAsync(
                    HttpMethod.Put,
                    $"{RutaAdministrativa}/{elemento.ElementoQuimicosId.Value}",
                    elemento,
                    "No fue posible actualizar el elemento químico.",
                    "Elemento químico actualizado correctamente.",
                    cancellationToken);

            if (result.Success)
                LimpiarCache();

            return result;
        }

        public async Task<ApiResult<bool>>
            DeleteElementoQuimicoResultAsync(
                ElementoQuimicoRequest elemento,
                CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(elemento);

            if (!elemento.ElementoQuimicosId.HasValue ||
                elemento.ElementoQuimicosId.Value <= 0)
            {
                return ApiResult<bool>.Fail(
                    "No se recibió un identificador de elemento químico válido.");
            }

            ApiResult<bool> result =
                await ApiServiceHelper
                    .SendAsync<object>(
                        httpClient,
                        HttpMethod.Delete,
                        $"{RutaAdministrativa}/{elemento.ElementoQuimicosId.Value}",
                        null,
                        "eliminar el elemento químico",
                        "Elemento químico eliminado correctamente.",
                        cancellationToken);

            if (result.Success)
                LimpiarCache();

            return result;
        }

        public async Task<ObservableCollection<ElementoQuimicoResponse>>
            GetElementoQuimicoAsync()
        {
            ApiResult<ObservableCollection<ElementoQuimicoResponse>>
                result =
                    await GetElementoQuimicoResultAsync();

            return result.Data ??
                new ObservableCollection<ElementoQuimicoResponse>();
        }

        // Firmas históricas conservadas para no afectar otros consumidores.
        public async Task<ApiResult<bool>>
            CreateElementoQuimicoResultAsync(
                ElementoQuimicoRequest elemento,
                CancellationToken cancellationToken = default)
        {
            ApiResult<ElementoQuimicoResponse> result =
                await CreateElementoQuimicoAdminResultAsync(
                    elemento,
                    cancellationToken);

            return result.Success &&
                   result.Data?.ElementoQuimicosId is > 0
                ? ApiResult<bool>.Ok(
                    true,
                    result.Message)
                : ApiResult<bool>.Fail(
                    result.Message,
                    result.StatusCode);
        }

        public async Task<ApiResult<bool>>
            UpdateElementoQuimicoResultAsync(
                ElementoQuimicoRequest elemento,
                CancellationToken cancellationToken = default)
        {
            ApiResult<ElementoQuimicoResponse> result =
                await UpdateElementoQuimicoAdminResultAsync(
                    elemento,
                    cancellationToken);

            return result.Success &&
                   result.Data?.ElementoQuimicosId is > 0
                ? ApiResult<bool>.Ok(
                    true,
                    result.Message)
                : ApiResult<bool>.Fail(
                    result.Message,
                    result.StatusCode);
        }

        public async Task<bool> CreateElementoQuimicoAsync(
            ElementoQuimicoRequest elemento)
        {
            ApiResult<bool> result =
                await CreateElementoQuimicoResultAsync(
                    elemento);

            return result.Success &&
                   result.Data == true;
        }

        public async Task<bool> UpdateElementoQuimicoAsync(
            ElementoQuimicoRequest elemento)
        {
            ApiResult<bool> result =
                await UpdateElementoQuimicoResultAsync(
                    elemento);

            return result.Success &&
                   result.Data == true;
        }

        public async Task<bool> DeleteElementoQuimicoAsync(
            ElementoQuimicoRequest elemento)
        {
            ApiResult<bool> result =
                await DeleteElementoQuimicoResultAsync(
                    elemento);

            return result.Success &&
                   result.Data == true;
        }

        public static void InvalidarCache()
        {
            LimpiarCache();
        }

        private async Task<ApiResult<ElementoQuimicoResponse>>
            CrearConResolucionAsync(
                ElementoQuimicoRequest elemento,
                CancellationToken cancellationToken)
        {
            try
            {
                using HttpResponseMessage response =
                    await httpClient.PostAsJsonAsync(
                        RutaAdministrativa,
                        elemento,
                        JsonOptions,
                        cancellationToken);

                string contenido =
                    await response.Content.ReadAsStringAsync(
                        cancellationToken);

                OperacionEnvelope<ElementoQuimicoResponse>? envelope =
                    DeserializarEnvelope(contenido);

                if (response.IsSuccessStatusCode)
                {
                    return CrearResultadoExitoso(
                        envelope,
                        "Elemento químico creado correctamente.");
                }

                bool esInactivoCoincidente =
                    response.StatusCode == HttpStatusCode.Conflict &&
                    string.Equals(
                        envelope?.Code,
                        CodigoInactivoExistente,
                        StringComparison.OrdinalIgnoreCase) &&
                    envelope?.Data?.ElementoQuimicosId is > 0;

                if (!esInactivoCoincidente)
                {
                    return ApiResult<ElementoQuimicoResponse>.Fail(
                        ApiErrorMessageParser.Parse(
                            response.StatusCode,
                            contenido,
                            "No fue posible crear el elemento químico."),
                        (int)response.StatusCode);
                }

                ElementoQuimicoResponse inactivo =
                    envelope!.Data!;

                string? decision =
                    await MostrarOpcionesInactivoAsync(
                        inactivo);

                if (decision == OpcionReactivar)
                {
                    return await EnviarYLeerElementoAsync(
                        HttpMethod.Put,
                        $"{RutaAdministrativa}/{inactivo.ElementoQuimicosId!.Value}/reactivar",
                        elemento,
                        "No fue posible reactivar el elemento químico.",
                        "Elemento químico reactivado correctamente.",
                        cancellationToken);
                }

                if (decision == OpcionCrearNuevo)
                {
                    return await EnviarYLeerElementoAsync(
                        HttpMethod.Post,
                        RutaAdministrativa +
                        "?crearNuevoSiExisteInactivo=true",
                        elemento,
                        "No fue posible crear el elemento químico.",
                        "Elemento químico creado correctamente.",
                        cancellationToken);
                }

                return ApiResult<ElementoQuimicoResponse>.Fail(
                    "La creación fue cancelada.");
            }
            catch (TaskCanceledException)
                when (!cancellationToken.IsCancellationRequested)
            {
                return ApiResult<ElementoQuimicoResponse>.Fail(
                    "La solicitud tardó demasiado. Intente nuevamente.");
            }
            catch (OperationCanceledException)
            {
                return ApiResult<ElementoQuimicoResponse>.Fail(
                    "La operación fue cancelada.");
            }
            catch (HttpRequestException)
            {
                return ApiResult<ElementoQuimicoResponse>.Fail(
                    "No fue posible comunicarse con el servidor.");
            }
            catch (JsonException)
            {
                return ApiResult<ElementoQuimicoResponse>.Fail(
                    "El servidor respondió, pero los datos del elemento químico no tienen el formato esperado.");
            }
            catch
            {
                return ApiResult<ElementoQuimicoResponse>.Fail(
                    "Ocurrió un error inesperado al crear el elemento químico.");
            }
        }

        private async Task<ApiResult<ElementoQuimicoResponse>>
            EnviarYLeerElementoAsync(
                HttpMethod method,
                string route,
                ElementoQuimicoRequest request,
                string errorMessage,
                string successMessage,
                CancellationToken cancellationToken)
        {
            try
            {
                using var message =
                    new HttpRequestMessage(
                        method,
                        route)
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

                string contenido =
                    await response.Content.ReadAsStringAsync(
                        cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    return ApiResult<ElementoQuimicoResponse>.Fail(
                        ApiErrorMessageParser.Parse(
                            response.StatusCode,
                            contenido,
                            errorMessage),
                        (int)response.StatusCode);
                }

                OperacionEnvelope<ElementoQuimicoResponse>? envelope =
                    DeserializarEnvelope(contenido);

                return CrearResultadoExitoso(
                    envelope,
                    successMessage);
            }
            catch (TaskCanceledException)
                when (!cancellationToken.IsCancellationRequested)
            {
                return ApiResult<ElementoQuimicoResponse>.Fail(
                    "La solicitud tardó demasiado. Intente nuevamente.");
            }
            catch (OperationCanceledException)
            {
                return ApiResult<ElementoQuimicoResponse>.Fail(
                    "La operación fue cancelada.");
            }
            catch (HttpRequestException)
            {
                return ApiResult<ElementoQuimicoResponse>.Fail(
                    "No fue posible comunicarse con el servidor.");
            }
            catch (JsonException)
            {
                return ApiResult<ElementoQuimicoResponse>.Fail(
                    "El servidor respondió, pero el elemento químico no tiene el formato esperado.");
            }
            catch
            {
                return ApiResult<ElementoQuimicoResponse>.Fail(
                    errorMessage);
            }
        }

        private static ApiResult<ElementoQuimicoResponse>
            CrearResultadoExitoso(
                OperacionEnvelope<ElementoQuimicoResponse>? envelope,
                string successMessage)
        {
            ElementoQuimicoResponse? data =
                envelope?.Data;

            if (data?.ElementoQuimicosId is not > 0)
            {
                return ApiResult<ElementoQuimicoResponse>.Fail(
                    "La operación se procesó, pero el servidor no devolvió el elemento químico actualizado.");
            }

            return ApiResult<ElementoQuimicoResponse>.Ok(
                data,
                string.IsNullOrWhiteSpace(envelope?.Message)
                    ? successMessage
                    : envelope!.Message);
        }

        private static OperacionEnvelope<ElementoQuimicoResponse>?
            DeserializarEnvelope(
                string contenido)
        {
            if (string.IsNullOrWhiteSpace(contenido))
                return null;

            return JsonSerializer.Deserialize<
                OperacionEnvelope<ElementoQuimicoResponse>>(
                    contenido,
                    JsonOptions);
        }

        private static async Task<string?>
            MostrarOpcionesInactivoAsync(
                ElementoQuimicoResponse registro)
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

                    string titulo =
                        string.IsNullOrWhiteSpace(
                            registro.NombreElementoQuimico)
                            ? registro.SimboloElementoQuimico
                            : registro.NombreElementoQuimico;

                    string mensaje =
                        "Ya existe un elemento químico eliminado que coincide " +
                        $"con '{titulo}'.\n\n" +
                        "Puede reactivarlo conservando su identificador e " +
                        "historial, o crear un registro diferente.";

                    return await pagina.DisplayActionSheet(
                        mensaje,
                        "Cancelar",
                        null,
                        OpcionReactivar,
                        OpcionCrearNuevo);
                });
        }

        private static bool CacheVigente() =>
            cacheFormulario != null &&
            DateTime.UtcNow - cacheCreadoUtc < DuracionCache;

        private static ObservableCollection<ElementoQuimicoResponse>
            CrearColeccionCache() =>
            new(
                cacheFormulario ??
                Enumerable.Empty<ElementoQuimicoResponse>());

        private static void LimpiarCache()
        {
            cacheFormulario = null;
            cacheCreadoUtc = default;
        }

        private sealed class OperacionEnvelope<T>
        {
            public bool Success { get; set; }
            public string Code { get; set; } = string.Empty;
            public string Message { get; set; } = string.Empty;
            public T? Data { get; set; }
        }
    }
}
