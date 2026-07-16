using CONATRADEC.Models;
using System.Collections.ObjectModel;

namespace CONATRADEC.Services
{
    public class PaisApiService
    {
        private readonly HttpClient httpClient;

        public PaisApiService()
            : this(ApiClientService.Client)
        {
        }

        public PaisApiService(HttpClient httpClient)
        {
            this.httpClient = httpClient
                ?? throw new ArgumentNullException(nameof(httpClient));
        }

        public Task<ApiResult<ObservableCollection<PaisResponse>>> GetPaisResultAsync(
            CancellationToken cancellationToken = default)
        {
            return ApiServiceHelper.GetCollectionAsync<PaisResponse>(
                httpClient,
                "api/pais",
                "los países",
                cancellationToken);
        }

        public Task<ApiResult<bool>> CreatePaisResultAsync(
            PaisRequest pais,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(pais);

            return ApiServiceHelper.SendAsync(
                httpClient,
                HttpMethod.Post,
                "api/pais/crearPais",
                pais,
                "crear el país",
                "País creado correctamente.",
                cancellationToken);
        }

        public Task<ApiResult<bool>> UpdatePaisResultAsync(
            PaisRequest pais,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(pais);

            if (pais.PaisId <= 0)
            {
                return Task.FromResult(
                    ApiResult<bool>.Fail(
                        "No se recibió un identificador de país válido."));
            }

            return ApiServiceHelper.SendAsync(
                httpClient,
                HttpMethod.Put,
                $"api/pais/actualizarPais/{pais.PaisId}",
                pais,
                "actualizar el país",
                "País actualizado correctamente.",
                cancellationToken);
        }

        public Task<ApiResult<bool>> DeletePaisResultAsync(
            PaisRequest pais,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(pais);

            if (pais.PaisId <= 0)
            {
                return Task.FromResult(
                    ApiResult<bool>.Fail(
                        "No se recibió un identificador de país válido."));
            }

            return ApiServiceHelper.SendAsync<PaisRequest>(
                httpClient,
                HttpMethod.Delete,
                $"api/pais/eliminarPais/{pais.PaisId}",
                null,
                "eliminar el país",
                "País eliminado correctamente.",
                cancellationToken);
        }

        public async Task<ObservableCollection<PaisResponse>> GetPaisAsync()
        {
            var result = await GetPaisResultAsync();
            return result.Data ?? new ObservableCollection<PaisResponse>();
        }

        public async Task<bool> CreatePaisAsync(PaisRequest pais)
        {
            var result = await CreatePaisResultAsync(pais);
            return result.Success && result.Data == true;
        }

        public async Task<bool> UpdatePaisAsync(PaisRequest pais)
        {
            var result = await UpdatePaisResultAsync(pais);
            return result.Success && result.Data == true;
        }

        public async Task<bool> DeletePaisAsync(PaisRequest pais)
        {
            var result = await DeletePaisResultAsync(pais);
            return result.Success && result.Data == true;
        }
    }
}
