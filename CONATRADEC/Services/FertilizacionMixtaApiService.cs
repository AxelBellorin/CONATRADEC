using CONATRADEC.Models;
using System.Collections.ObjectModel;
using System.Net.Http.Json;

namespace CONATRADEC.Services
{
    internal class FertilizacionMixtaApiService
    {
        private readonly HttpClient httpClient;

        private static readonly SemaphoreSlim FuentesLock = new(1, 1);
        private static List<FuenteNutrienteFertilizacionMixtaResponse>?
            fuentesCache;
        private static DateTime fuentesCacheUtc;
        private static readonly TimeSpan DuracionCache =
            TimeSpan.FromMinutes(1);

        public FertilizacionMixtaApiService()
            : this(ApiClientService.Client)
        {
        }

        public FertilizacionMixtaApiService(HttpClient httpClient)
        {
            this.httpClient = httpClient
                ?? throw new ArgumentNullException(nameof(httpClient));
        }

        public async Task<ObservableCollection<
            FuenteNutrienteFertilizacionMixtaResponse>>
            ListarFuentesFertilizacionMixtaAsync()
        {
            if (CacheVigente())
                return CrearColeccionCache();

            await FuentesLock.WaitAsync()
                .ConfigureAwait(false);

            try
            {
                if (CacheVigente())
                    return CrearColeccionCache();

                ObservableCollection<
                    FuenteNutrienteFertilizacionMixtaResponse>? response =
                    await httpClient.GetFromJsonAsync<
                        ObservableCollection<
                            FuenteNutrienteFertilizacionMixtaResponse>>(
                                "api/fuente-nutriente/listar-fertilizacion-mixta")
                        .ConfigureAwait(false);

                fuentesCache = response?
                    .Where(x => x != null &&
                                x.FuenteNutrientesId is > 0)
                    .ToList()
                    ?? new List<
                        FuenteNutrienteFertilizacionMixtaResponse>();

                fuentesCacheUtc = DateTime.UtcNow;
                return CrearColeccionCache();
            }
            catch
            {
                return new ObservableCollection<
                    FuenteNutrienteFertilizacionMixtaResponse>();
            }
            finally
            {
                FuentesLock.Release();
            }
        }

        public static void LimpiarCacheFuentes()
        {
            fuentesCache = null;
            fuentesCacheUtc = default;
        }

        public async Task<FertilizacionMixtaCalculoResponse?> CalcularAsync(
            FertilizacionMixtaCalcularRequest request)
        {
            try
            {
                using HttpResponseMessage response =
                    await httpClient.PostAsJsonAsync(
                        "api/fertilizacion-mixta/calcular",
                        request)
                    .ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    string errorApi = await response.Content
                        .ReadAsStringAsync()
                        .ConfigureAwait(false);

                    return new FertilizacionMixtaCalculoResponse
                    {
                        Success = false,
                        Message =
                            $"Error API ({(int)response.StatusCode}): " +
                            errorApi
                    };
                }

                FertilizacionMixtaCalculoResponse? resultado =
                    await response.Content.ReadFromJsonAsync<
                        FertilizacionMixtaCalculoResponse>()
                    .ConfigureAwait(false);

                if (resultado == null)
                {
                    return new FertilizacionMixtaCalculoResponse
                    {
                        Success = false,
                        Message =
                            "La API respondió, pero no se pudo interpretar la respuesta."
                    };
                }

                resultado.Success = true;
                resultado.Message =
                    "Cálculo realizado correctamente.";

                return resultado;
            }
            catch (Exception ex)
            {
                return new FertilizacionMixtaCalculoResponse
                {
                    Success = false,
                    Message =
                        $"No se pudo conectar con la API: {ex.Message}"
                };
            }
        }

        private static bool CacheVigente() =>
            fuentesCache != null &&
            DateTime.UtcNow - fuentesCacheUtc < DuracionCache;

        private static ObservableCollection<
            FuenteNutrienteFertilizacionMixtaResponse>
            CrearColeccionCache() =>
            new(fuentesCache ??
                Enumerable.Empty<
                    FuenteNutrienteFertilizacionMixtaResponse>());
    }
}
