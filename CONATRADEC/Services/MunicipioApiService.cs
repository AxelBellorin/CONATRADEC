using CONATRADEC.Models;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;

namespace CONATRADEC.Services
{
    public class MunicipioApiService
    {
        private readonly HttpClient httpClient;

        private sealed record CacheEntry(
            List<MunicipioResponse> Items,
            DateTime CreadoUtc);

        private static readonly ConcurrentDictionary<int, CacheEntry>
            CachePorDepartamento = new();

        private static readonly ConcurrentDictionary<int, SemaphoreSlim>
            BloqueosPorDepartamento = new();

        private static readonly TimeSpan DuracionCache =
            TimeSpan.FromMinutes(30);

        public MunicipioApiService()
            : this(ApiClientService.Client)
        {
        }

        public MunicipioApiService(HttpClient httpClient)
        {
            this.httpClient = httpClient
                ?? throw new ArgumentNullException(nameof(httpClient));
        }

        public Task<ApiResult<ObservableCollection<MunicipioResponse>>>
            GetMunicipiosResultAsync(
                int? departamentoId,
                CancellationToken cancellationToken = default)
        {
            if (!departamentoId.HasValue || departamentoId.Value <= 0)
            {
                return Task.FromResult(
                    ApiResult<ObservableCollection<MunicipioResponse>>.Fail(
                        "Seleccione un departamento válido."));
            }

            return ApiServiceHelper.GetCollectionAsync<MunicipioResponse>(
                httpClient,
                $"/por-departamento/{departamentoId.Value}",
                "los municipios",
                cancellationToken);
        }

        public Task<ApiResult<ObservableCollection<MunicipioResponse>>>
            GetMunicipiosConUbicacionResultAsync(
                CancellationToken cancellationToken = default)
        {
            return ApiServiceHelper.GetCollectionAsync<MunicipioResponse>(
                httpClient,
                "/listarTodos-por-departamento-por-pais",
                "los municipios",
                cancellationToken);
        }

        public async Task<ApiResult<bool>> CreateMunicipioResultAsync(
            MunicipioRequest municipio,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(municipio);

            ApiResult<bool> result = await ApiServiceHelper.SendAsync(
                httpClient,
                HttpMethod.Post,
                "/crear",
                municipio,
                "crear el municipio",
                "Municipio creado correctamente.",
                cancellationToken);

            if (result.Success)
                LimpiarCache();

            return result;
        }

        public async Task<ApiResult<bool>> UpdateMunicipioResultAsync(
            MunicipioRequest municipio,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(municipio);

            if (!municipio.MunicipioId.HasValue ||
                municipio.MunicipioId.Value <= 0)
            {
                return ApiResult<bool>.Fail(
                    "No se recibió un identificador de municipio válido.");
            }

            ApiResult<bool> result = await ApiServiceHelper.SendAsync(
                httpClient,
                HttpMethod.Put,
                $"/actualizar/{municipio.MunicipioId.Value}",
                municipio,
                "actualizar el municipio",
                "Municipio actualizado correctamente.",
                cancellationToken);

            if (result.Success)
                LimpiarCache();

            return result;
        }

        public async Task<ApiResult<bool>> DeleteMunicipioResultAsync(
            MunicipioRequest municipio,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(municipio);

            if (!municipio.MunicipioId.HasValue ||
                municipio.MunicipioId.Value <= 0)
            {
                return ApiResult<bool>.Fail(
                    "No se recibió un identificador de municipio válido.");
            }

            ApiResult<bool> result = await ApiServiceHelper
                .SendAsync<MunicipioRequest>(
                    httpClient,
                    HttpMethod.Delete,
                    $"/eliminar/{municipio.MunicipioId.Value}",
                    null,
                    "eliminar el municipio",
                    "Municipio eliminado correctamente.",
                    cancellationToken);

            if (result.Success)
                LimpiarCache();

            return result;
        }

        public async Task<ObservableCollection<MunicipioResponse>>
            GetMunicipiosAsync(int? departamentoId)
        {
            if (!departamentoId.HasValue || departamentoId.Value <= 0)
                return new ObservableCollection<MunicipioResponse>();

            int id = departamentoId.Value;

            if (ObtenerCacheVigente(id) is List<MunicipioResponse> cache)
                return new ObservableCollection<MunicipioResponse>(cache);

            SemaphoreSlim bloqueo = BloqueosPorDepartamento.GetOrAdd(
                id,
                _ => new SemaphoreSlim(1, 1));

            await bloqueo.WaitAsync();

            try
            {
                if (ObtenerCacheVigente(id) is List<MunicipioResponse> vigente)
                    return new ObservableCollection<MunicipioResponse>(vigente);

                ApiResult<ObservableCollection<MunicipioResponse>> result =
                    await GetMunicipiosResultAsync(id);

                List<MunicipioResponse> items = result.Data?
                    .Where(x => x != null && x.MunicipioId is > 0)
                    .ToList()
                    ?? new List<MunicipioResponse>();

                CachePorDepartamento[id] =
                    new CacheEntry(items, DateTime.UtcNow);

                return new ObservableCollection<MunicipioResponse>(items);
            }
            finally
            {
                bloqueo.Release();
            }
        }

        public async Task<bool> CreateMunicipioAsync(
            MunicipioRequest municipio)
        {
            ApiResult<bool> result =
                await CreateMunicipioResultAsync(municipio);

            return result.Success && result.Data == true;
        }

        public async Task<bool> UpdateMunicipioAsync(
            MunicipioRequest municipio)
        {
            ApiResult<bool> result =
                await UpdateMunicipioResultAsync(municipio);

            return result.Success && result.Data == true;
        }

        public async Task<bool> DeleteMunicipioAsync(
            MunicipioRequest municipio)
        {
            ApiResult<bool> result =
                await DeleteMunicipioResultAsync(municipio);

            return result.Success && result.Data == true;
        }

        private static List<MunicipioResponse>? ObtenerCacheVigente(
            int departamentoId)
        {
            if (!CachePorDepartamento.TryGetValue(
                    departamentoId,
                    out CacheEntry? entry))
            {
                return null;
            }

            if (DateTime.UtcNow - entry.CreadoUtc >= DuracionCache)
            {
                CachePorDepartamento.TryRemove(departamentoId, out _);
                return null;
            }

            return entry.Items;
        }

        private static void LimpiarCache()
        {
            CachePorDepartamento.Clear();
        }
    }
}
