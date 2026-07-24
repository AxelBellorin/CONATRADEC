using CONATRADEC.Models;
using System.Collections.ObjectModel;
using System.Net.Http.Json;

namespace CONATRADEC.Services
{
    internal class UnidadMedidaApiService
    {
        private readonly HttpClient httpClient;

        private static readonly SemaphoreSlim CacheLock = new(1, 1);
        private static List<UnidadMedidaResponse>? cacheFormulario;
        private static DateTime cacheCreadoUtc;
        private static readonly TimeSpan DuracionCache =
            TimeSpan.FromMinutes(20);

        public UnidadMedidaApiService()
            : this(ApiClientService.Client)
        {
        }

        public UnidadMedidaApiService(HttpClient httpClient)
        {
            this.httpClient = httpClient
                ?? throw new ArgumentNullException(nameof(httpClient));
        }

        public async Task<ObservableCollection<UnidadMedidaResponse>>
            GetUnidadMedidaAsync()
        {
            if (CacheVigente())
                return CrearColeccionCache();

            await CacheLock.WaitAsync();

            try
            {
                if (CacheVigente())
                    return CrearColeccionCache();

                ObservableCollection<UnidadMedidaResponse>? response =
                    await httpClient.GetFromJsonAsync<
                        ObservableCollection<UnidadMedidaResponse>>(
                            "api/unidad-medida/listar");

                cacheFormulario = response?
                    .Where(x => x != null && x.Activo != false)
                    .ToList()
                    ?? new List<UnidadMedidaResponse>();

                cacheCreadoUtc = DateTime.UtcNow;
                return CrearColeccionCache();
            }
            catch
            {
                return new ObservableCollection<UnidadMedidaResponse>();
            }
            finally
            {
                CacheLock.Release();
            }
        }

        public async Task<bool> CreateUnidadMedidaAsync(
            UnidadMedidaRequest unidadMedida)
        {
            try
            {
                using HttpResponseMessage response =
                    await httpClient.PostAsJsonAsync(
                        "api/unidad-medida/crear",
                        unidadMedida);

                if (response.IsSuccessStatusCode)
                    LimpiarCache();

                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> UpdateUnidadMedidaAsync(
            UnidadMedidaRequest unidadMedida)
        {
            try
            {
                using HttpResponseMessage response =
                    await httpClient.PutAsJsonAsync(
                        $"api/unidad-medida/editar/{unidadMedida.UnidadMedidaId}",
                        unidadMedida);

                if (response.IsSuccessStatusCode)
                    LimpiarCache();

                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> DeleteUnidadMedidaAsync(
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

        private static bool CacheVigente() =>
            cacheFormulario != null &&
            DateTime.UtcNow - cacheCreadoUtc < DuracionCache;

        private static ObservableCollection<UnidadMedidaResponse>
            CrearColeccionCache() =>
            new(cacheFormulario ?? Enumerable.Empty<UnidadMedidaResponse>());

        private static void LimpiarCache()
        {
            cacheFormulario = null;
            cacheCreadoUtc = default;
        }
    }
}
