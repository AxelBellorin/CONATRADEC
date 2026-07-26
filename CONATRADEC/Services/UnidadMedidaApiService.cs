using CONATRADEC.Models;
using System.Collections.ObjectModel;
using System.Net.Http.Json;
using System.Text.Json;

namespace CONATRADEC.Services
{
    /// <summary>
    /// Servicio del catálogo base de unidades de medida.
    /// </summary>
    internal sealed class UnidadMedidaApiService
    {
        private readonly HttpClient httpClient;

        private static readonly SemaphoreSlim
            CacheLock = new(1, 1);

        private static List<UnidadMedidaResponse>?
            cacheFormulario;

        private static DateTime cacheCreadoUtc;

        private static readonly TimeSpan DuracionCache =
            TimeSpan.FromMinutes(20);

        private readonly JsonSerializerOptions jsonOptions =
            new(JsonSerializerDefaults.Web)
            {
                PropertyNameCaseInsensitive = true
            };

        public UnidadMedidaApiService()
            : this(ApiClientService.Client)
        {
        }

        public UnidadMedidaApiService(
            HttpClient httpClient)
        {
            this.httpClient =
                httpClient ??
                throw new ArgumentNullException(
                    nameof(httpClient));
        }

        public async Task<
            ObservableCollection<UnidadMedidaResponse>>
            GetUnidadMedidaAsync(
                bool forzarRecarga = false,
                CancellationToken cancellationToken =
                    default)
        {
            if (!forzarRecarga &&
                CacheVigente())
            {
                return CrearColeccionCache();
            }

            await CacheLock.WaitAsync(
                cancellationToken);

            try
            {
                if (!forzarRecarga &&
                    CacheVigente())
                {
                    return CrearColeccionCache();
                }

                ObservableCollection<
                    UnidadMedidaResponse>?
                    response =
                        await httpClient
                            .GetFromJsonAsync<
                                ObservableCollection<
                                    UnidadMedidaResponse>>(
                                        "api/unidad-medida/listar",
                                        jsonOptions,
                                        cancellationToken);

                cacheFormulario =
                    response?.Where(x =>
                            x != null &&
                            x.Activo != false)
                        .ToList()
                    ??
                    new List<
                        UnidadMedidaResponse>();

                cacheCreadoUtc =
                    DateTime.UtcNow;

                return CrearColeccionCache();
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch
            {
                return new ObservableCollection<
                    UnidadMedidaResponse>();
            }
            finally
            {
                CacheLock.Release();
            }
        }

        /// <summary>
        /// Crea una unidad y conserva el mensaje exacto devuelto por la API.
        /// </summary>
        public async Task<
            UnidadMedidaApiOperationResult<
                UnidadMedidaResponse>>
            CrearUnidadMedidaDetalladaAsync(
                string nombreUnidad,
                CancellationToken cancellationToken =
                    default)
        {
            string nombre =
                (nombreUnidad ??
                 string.Empty)
                    .Trim();

            if (string.IsNullOrWhiteSpace(
                    nombre))
            {
                return UnidadMedidaApiOperationResult<
                    UnidadMedidaResponse>
                    .Fail(
                        "El nombre de la unidad es obligatorio.");
            }

            try
            {
                using HttpResponseMessage response =
                    await httpClient.PostAsJsonAsync(
                        "api/unidad-medida/crear",
                        new UnidadMedidaRequest
                        {
                            NombreUnidadMedida =
                                nombre,
                            Activo = true
                        },
                        jsonOptions,
                        cancellationToken);

                string contenido =
                    await response.Content
                        .ReadAsStringAsync(
                            cancellationToken);

                string mensaje =
                    ExtraerMensaje(
                        contenido);

                UnidadMedidaResponse? data =
                    ExtraerData<
                        UnidadMedidaResponse>(
                            contenido);

                if (!response.IsSuccessStatusCode)
                {
                    return UnidadMedidaApiOperationResult<
                        UnidadMedidaResponse>
                        .Fail(
                            string.IsNullOrWhiteSpace(
                                mensaje)
                                ? "No fue posible crear la unidad de medida."
                                : mensaje);
                }

                LimpiarCache();

                return UnidadMedidaApiOperationResult<
                    UnidadMedidaResponse>
                    .Ok(
                        data,
                        string.IsNullOrWhiteSpace(
                            mensaje)
                            ? "Unidad de medida creada correctamente."
                            : mensaje);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (HttpRequestException)
            {
                return UnidadMedidaApiOperationResult<
                    UnidadMedidaResponse>
                    .Fail(
                        "No fue posible conectarse con la API para crear la unidad de medida.");
            }
            catch (Exception ex)
            {
                return UnidadMedidaApiOperationResult<
                    UnidadMedidaResponse>
                    .Fail(
                        $"No fue posible crear la unidad de medida: {ex.Message}");
            }
        }

        public async Task<bool>
            CreateUnidadMedidaAsync(
                UnidadMedidaRequest unidadMedida)
        {
            UnidadMedidaApiOperationResult<
                UnidadMedidaResponse>
                resultado =
                    await CrearUnidadMedidaDetalladaAsync(
                        unidadMedida
                            .NombreUnidadMedida ??
                        string.Empty);

            return resultado.Success;
        }

        public async Task<bool>
            UpdateUnidadMedidaAsync(
                UnidadMedidaRequest unidadMedida)
        {
            try
            {
                using HttpResponseMessage response =
                    await httpClient.PutAsJsonAsync(
                        $"api/unidad-medida/editar/{unidadMedida.UnidadMedidaId}",
                        unidadMedida,
                        jsonOptions);

                if (response.IsSuccessStatusCode)
                    LimpiarCache();

                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool>
            DeleteUnidadMedidaAsync(
                UnidadMedidaRequest unidadMedida)
        {
            try
            {
                using HttpResponseMessage response =
                    await httpClient.DeleteAsync(
                        $"api/unidad-medida/eliminar/{unidadMedida.UnidadMedidaId}");

                if (response.IsSuccessStatusCode)
                    LimpiarCache();

                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public static void InvalidarCache()
        {
            LimpiarCache();
        }

        private static bool CacheVigente() =>
            cacheFormulario != null &&
            DateTime.UtcNow -
                cacheCreadoUtc <
                DuracionCache;

        private static
            ObservableCollection<
                UnidadMedidaResponse>
            CrearColeccionCache() =>
                new(
                    cacheFormulario ??
                    Enumerable.Empty<
                        UnidadMedidaResponse>());

        private static void LimpiarCache()
        {
            cacheFormulario = null;
            cacheCreadoUtc = default;
        }

        private string ExtraerMensaje(
            string contenido)
        {
            if (string.IsNullOrWhiteSpace(
                    contenido))
            {
                return string.Empty;
            }

            try
            {
                using JsonDocument documento =
                    JsonDocument.Parse(
                        contenido);

                JsonElement raiz =
                    documento.RootElement;

                if (raiz.TryGetProperty(
                        "mensaje",
                        out JsonElement mensaje))
                {
                    return mensaje.GetString() ??
                        string.Empty;
                }

                if (raiz.TryGetProperty(
                        "message",
                        out JsonElement message))
                {
                    return message.GetString() ??
                        string.Empty;
                }
            }
            catch (JsonException)
            {
                // Se utiliza el mensaje genérico.
            }

            return string.Empty;
        }

        private T? ExtraerData<T>(
            string contenido)
        {
            if (string.IsNullOrWhiteSpace(
                    contenido))
            {
                return default;
            }

            try
            {
                using JsonDocument documento =
                    JsonDocument.Parse(
                        contenido);

                JsonElement raiz =
                    documento.RootElement;

                if (!raiz.TryGetProperty(
                        "data",
                        out JsonElement data))
                {
                    return default;
                }

                return data.Deserialize<T>(
                    jsonOptions);
            }
            catch (JsonException)
            {
                return default;
            }
        }
    }

    internal sealed class
        UnidadMedidaApiOperationResult<T>
    {
        private UnidadMedidaApiOperationResult(
            bool success,
            string message,
            T? data)
        {
            Success = success;
            Message = message;
            Data = data;
        }

        public bool Success { get; }

        public string Message { get; }

        public T? Data { get; }

        public static
            UnidadMedidaApiOperationResult<T>
            Ok(
                T? data,
                string message) =>
                    new(
                        true,
                        message,
                        data);

        public static
            UnidadMedidaApiOperationResult<T>
            Fail(
                string message) =>
                    new(
                        false,
                        message,
                        default);
    }
}
