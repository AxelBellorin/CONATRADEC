using CONATRADEC.Models;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;

namespace CONATRADEC.Services
{
    public class DepartamentoApiService
    {
        private readonly HttpClient httpClient;

        private sealed record CacheEntry(
            List<DepartamentoResponse> Items,
            DateTime CreadoUtc);

        private static readonly ConcurrentDictionary<int, CacheEntry>
            CachePorPais = new();

        private static readonly ConcurrentDictionary<int, SemaphoreSlim>
            BloqueosPorPais = new();

        private static readonly TimeSpan DuracionCache =
            TimeSpan.FromMinutes(30);

        public DepartamentoApiService()
            : this(ApiClientService.Client)
        {
        }

        public DepartamentoApiService(HttpClient httpClient)
        {
            this.httpClient = httpClient
                ?? throw new ArgumentNullException(nameof(httpClient));
        }

        public Task<ApiResult<ObservableCollection<DepartamentoResponse>>>
            GetDepartamentosResultAsync(
                int? paisId,
                CancellationToken cancellationToken = default)
        {
            if (!paisId.HasValue || paisId.Value <= 0)
            {
                return Task.FromResult(
                    ApiResult<ObservableCollection<DepartamentoResponse>>.Fail(
                        "No se recibió un país válido para cargar sus departamentos."));
            }

            return ApiServiceHelper.GetCollectionAsync<DepartamentoResponse>(
                httpClient,
                $"api/departamento/por-pais/{paisId.Value}",
                "los departamentos",
                cancellationToken);
        }

        public async Task<ApiResult<bool>> CreateDepartamentoResultAsync(
            DepartamentoRequest departamento,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(departamento);

            ApiResult<bool> result = await ApiServiceHelper.SendAsync(
                httpClient,
                HttpMethod.Post,
                "api/departamento/Crear",
                departamento,
                "crear el departamento",
                "Departamento creado correctamente.",
                cancellationToken);

            if (result.Success)
                LimpiarCache();

            return result;
        }

        public async Task<ApiResult<bool>> UpdateDepartamentoResultAsync(
            DepartamentoRequest departamento,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(departamento);

            if (!departamento.DepartamentoId.HasValue ||
                departamento.DepartamentoId.Value <= 0)
            {
                return ApiResult<bool>.Fail(
                    "No se recibió un identificador de departamento válido.");
            }

            ApiResult<bool> result = await ApiServiceHelper.SendAsync(
                httpClient,
                HttpMethod.Put,
                $"api/departamento/actualizar/{departamento.DepartamentoId.Value}",
                departamento,
                "actualizar el departamento",
                "Departamento actualizado correctamente.",
                cancellationToken);

            if (result.Success)
                LimpiarCache();

            return result;
        }

        public async Task<ApiResult<bool>> DeleteDepartamentoResultAsync(
            DepartamentoRequest departamento,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(departamento);

            if (!departamento.DepartamentoId.HasValue ||
                departamento.DepartamentoId.Value <= 0)
            {
                return ApiResult<bool>.Fail(
                    "No se recibió un identificador de departamento válido.");
            }

            ApiResult<bool> result = await ApiServiceHelper
                .SendAsync<DepartamentoRequest>(
                    httpClient,
                    HttpMethod.Delete,
                    $"api/departamento/eliminar/{departamento.DepartamentoId.Value}",
                    null,
                    "eliminar el departamento",
                    "Departamento eliminado correctamente.",
                    cancellationToken);

            if (result.Success)
                LimpiarCache();

            return result;
        }

        public async Task<ObservableCollection<DepartamentoResponse>>
            GetDepartamentosAsync(int? paisId)
        {
            if (!paisId.HasValue || paisId.Value <= 0)
                return new ObservableCollection<DepartamentoResponse>();

            int id = paisId.Value;

            if (ObtenerCacheVigente(id) is List<DepartamentoResponse> cache)
                return new ObservableCollection<DepartamentoResponse>(cache);

            SemaphoreSlim bloqueo = BloqueosPorPais.GetOrAdd(
                id,
                _ => new SemaphoreSlim(1, 1));

            await bloqueo.WaitAsync();

            try
            {
                if (ObtenerCacheVigente(id) is List<DepartamentoResponse> vigente)
                    return new ObservableCollection<DepartamentoResponse>(vigente);

                ApiResult<ObservableCollection<DepartamentoResponse>> result =
                    await GetDepartamentosResultAsync(id);

                List<DepartamentoResponse> items = result.Data?
                    .Where(x => x != null && x.DepartamentoId is > 0)
                    .ToList()
                    ?? new List<DepartamentoResponse>();

                CachePorPais[id] = new CacheEntry(items, DateTime.UtcNow);
                return new ObservableCollection<DepartamentoResponse>(items);
            }
            finally
            {
                bloqueo.Release();
            }
        }

        public async Task<bool> CreateDepartamentoAsync(
            DepartamentoRequest departamento)
        {
            ApiResult<bool> result =
                await CreateDepartamentoResultAsync(departamento);

            return result.Success && result.Data == true;
        }

        public async Task<bool> UpdateDepartamentoAsync(
            DepartamentoRequest departamento)
        {
            ApiResult<bool> result =
                await UpdateDepartamentoResultAsync(departamento);

            return result.Success && result.Data == true;
        }

        public async Task<bool> DeleteDepartamentoAsync(
            DepartamentoRequest departamento)
        {
            ApiResult<bool> result =
                await DeleteDepartamentoResultAsync(departamento);

            return result.Success && result.Data == true;
        }

        private static List<DepartamentoResponse>? ObtenerCacheVigente(int paisId)
        {
            if (!CachePorPais.TryGetValue(paisId, out CacheEntry? entry))
                return null;

            if (DateTime.UtcNow - entry.CreadoUtc >= DuracionCache)
            {
                CachePorPais.TryRemove(paisId, out _);
                return null;
            }

            return entry.Items;
        }

        private static void LimpiarCache()
        {
            CachePorPais.Clear();
        }
    }
}
