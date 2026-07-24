using CONATRADEC.Models;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;

namespace CONATRADEC.Services
{
    public class AnalisisSueloApiService
    {
        private const string EndpointCalcular =
            "api/analisis-suelo/calcular";

        private const string EndpointGuardarCalculo =
            "api/analisis-suelo/guardar-calculo";

        private const string EndpointTipoCultivoListar =
            "api/analisis-suelo/tipo-cultivo/listar";

        private readonly HttpClient httpClient;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private static readonly SemaphoreSlim TiposCultivoLock = new(1, 1);
        private static List<TipoCultivoResponse>? tiposCultivoCache;
        private static DateTime tiposCultivoCacheUtc;
        private static readonly TimeSpan DuracionCache =
            TimeSpan.FromMinutes(20);

        public AnalisisSueloApiService()
            : this(ApiClientService.Client)
        {
        }

        public AnalisisSueloApiService(HttpClient httpClient)
        {
            this.httpClient = httpClient
                ?? throw new ArgumentNullException(nameof(httpClient));
        }

        public Task<AnalisisSueloCalculoResponse?> CalcularAsync(
            AnalisisSueloCalcularRequest request) =>
            PostAnalisisSueloAsync(
                EndpointCalcular,
                request);

        public Task<AnalisisSueloCalculoResponse?> GuardarCalculoAsync(
            AnalisisSueloGuardarCalculoRequest request) =>
            PostAnalisisSueloAsync(
                EndpointGuardarCalculo,
                request);

        /// <summary>
        /// El tipo de cultivo cambia poco. Se mantiene una copia temporal para
        /// que crear o editar un análisis no repita esta solicitud cada vez que
        /// se construye una nueva página.
        /// </summary>
        public async Task<ObservableCollection<TipoCultivoResponse>>
            ListarTiposCultivoAsync()
        {
            if (CacheTiposCultivoVigente())
                return CrearColeccionTiposCultivo();

            await TiposCultivoLock.WaitAsync()
                .ConfigureAwait(false);

            try
            {
                if (CacheTiposCultivoVigente())
                    return CrearColeccionTiposCultivo();

                using HttpResponseMessage response =
                    await httpClient.GetAsync(
                        EndpointTipoCultivoListar,
                        HttpCompletionOption.ResponseHeadersRead)
                    .ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    return new ObservableCollection<TipoCultivoResponse>();
                }

                string jsonRespuesta =
                    await response.Content.ReadAsStringAsync()
                        .ConfigureAwait(false);

                if (string.IsNullOrWhiteSpace(jsonRespuesta))
                {
                    return new ObservableCollection<TipoCultivoResponse>();
                }

                List<TipoCultivoResponse> items;
                string jsonTrim = jsonRespuesta.TrimStart();

                if (jsonTrim.StartsWith("[", StringComparison.Ordinal))
                {
                    items = JsonSerializer.Deserialize<
                        List<TipoCultivoResponse>>(
                            jsonRespuesta,
                            JsonOptions)
                        ?? new List<TipoCultivoResponse>();
                }
                else
                {
                    ApiListaResponse<TipoCultivoResponse>? envelope =
                        JsonSerializer.Deserialize<
                            ApiListaResponse<TipoCultivoResponse>>(
                                jsonRespuesta,
                                JsonOptions);

                    items = envelope?.Data ??
                        new List<TipoCultivoResponse>();
                }

                tiposCultivoCache = items
                    .Where(x => x != null &&
                                x.TipoCultivoId is > 0 &&
                                x.Activo != false)
                    .ToList();

                tiposCultivoCacheUtc = DateTime.UtcNow;
                return CrearColeccionTiposCultivo();
            }
            catch
            {
                return new ObservableCollection<TipoCultivoResponse>();
            }
            finally
            {
                TiposCultivoLock.Release();
            }
        }

        public static void LimpiarCacheTiposCultivo()
        {
            tiposCultivoCache = null;
            tiposCultivoCacheUtc = default;
        }

        private async Task<AnalisisSueloCalculoResponse?>
            PostAnalisisSueloAsync<TRequest>(
                string endpoint,
                TRequest request)
        {
            try
            {
                Debug.WriteLine(
                    $"========== REQUEST API: {endpoint} ==========");

                using HttpResponseMessage response =
                    await httpClient.PostAsJsonAsync(
                        endpoint,
                        request,
                        JsonOptions)
                    .ConfigureAwait(false);

                string jsonRespuesta =
                    await response.Content.ReadAsStringAsync()
                        .ConfigureAwait(false);

                Debug.WriteLine(
                    $"========== RESPONSE API: {endpoint} " +
                    $"({(int)response.StatusCode}) ==========");

                if (!response.IsSuccessStatusCode)
                {
                    return new AnalisisSueloCalculoResponse
                    {
                        Success = false,
                        Message =
                            $"Error API ({(int)response.StatusCode}): " +
                            jsonRespuesta
                    };
                }

                AnalisisSueloCalculoResponse? data =
                    JsonSerializer.Deserialize<
                        AnalisisSueloCalculoResponse>(
                            jsonRespuesta,
                            JsonOptions);

                return data ?? new AnalisisSueloCalculoResponse
                {
                    Success = false,
                    Message =
                        "La API respondió, pero no se pudo interpretar la respuesta."
                };
            }
            catch (Exception ex)
            {
                return new AnalisisSueloCalculoResponse
                {
                    Success = false,
                    Message =
                        $"No se pudo conectar con la API: {ex.Message}"
                };
            }
        }

        private static bool CacheTiposCultivoVigente() =>
            tiposCultivoCache != null &&
            DateTime.UtcNow - tiposCultivoCacheUtc < DuracionCache;

        private static ObservableCollection<TipoCultivoResponse>
            CrearColeccionTiposCultivo() =>
            new(tiposCultivoCache ??
                Enumerable.Empty<TipoCultivoResponse>());

        private sealed class ApiListaResponse<T>
        {
            public bool Success { get; set; }
            public string? Message { get; set; }
            public List<T>? Data { get; set; }
        }
    }
}
