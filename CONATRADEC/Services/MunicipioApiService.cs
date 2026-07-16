using CONATRADEC.Models;
using System.Collections.ObjectModel;

namespace CONATRADEC.Services
{
    public class MunicipioApiService
    {
        private readonly HttpClient httpClient;

        public MunicipioApiService()
            : this(ApiClientService.Client)
        {
        }

        public MunicipioApiService(HttpClient httpClient)
        {
            this.httpClient = httpClient
                ?? throw new ArgumentNullException(nameof(httpClient));
        }

        public Task<ApiResult<ObservableCollection<MunicipioResponse>>> GetMunicipiosResultAsync(
            int? departamentoId,
            CancellationToken cancellationToken = default)
        {
            if (!departamentoId.HasValue || departamentoId.Value <= 0)
            {
                return Task.FromResult(
                    ApiResult<ObservableCollection<MunicipioResponse>>.Fail(
                        "No se recibió un departamento válido para cargar sus municipios."));
            }

            // Se conservan las rutas actuales del backend.
            return ApiServiceHelper.GetCollectionAsync<MunicipioResponse>(
                httpClient,
                $"/por-departamento/{departamentoId.Value}",
                "los municipios",
                cancellationToken);
        }

        public Task<ApiResult<bool>> CreateMunicipioResultAsync(
            MunicipioRequest municipio,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(municipio);

            return ApiServiceHelper.SendAsync(
                httpClient,
                HttpMethod.Post,
                "/crear",
                municipio,
                "crear el municipio",
                "Municipio creado correctamente.",
                cancellationToken);
        }

        public Task<ApiResult<bool>> UpdateMunicipioResultAsync(
            MunicipioRequest municipio,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(municipio);

            if (!municipio.MunicipioId.HasValue || municipio.MunicipioId.Value <= 0)
            {
                return Task.FromResult(
                    ApiResult<bool>.Fail(
                        "No se recibió un identificador de municipio válido."));
            }

            return ApiServiceHelper.SendAsync(
                httpClient,
                HttpMethod.Put,
                $"/actualizar/{municipio.MunicipioId.Value}",
                municipio,
                "actualizar el municipio",
                "Municipio actualizado correctamente.",
                cancellationToken);
        }

        public Task<ApiResult<bool>> DeleteMunicipioResultAsync(
            MunicipioRequest municipio,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(municipio);

            if (!municipio.MunicipioId.HasValue || municipio.MunicipioId.Value <= 0)
            {
                return Task.FromResult(
                    ApiResult<bool>.Fail(
                        "No se recibió un identificador de municipio válido."));
            }

            return ApiServiceHelper.SendAsync<MunicipioRequest>(
                httpClient,
                HttpMethod.Delete,
                $"/eliminar/{municipio.MunicipioId.Value}",
                null,
                "eliminar el municipio",
                "Municipio eliminado correctamente.",
                cancellationToken);
        }

        public async Task<ObservableCollection<MunicipioResponse>> GetMunicipiosAsync(
            int? departamentoId)
        {
            var result = await GetMunicipiosResultAsync(departamentoId);
            return result.Data ?? new ObservableCollection<MunicipioResponse>();
        }

        public async Task<bool> CreateMunicipioAsync(MunicipioRequest municipio)
        {
            var result = await CreateMunicipioResultAsync(municipio);
            return result.Success && result.Data == true;
        }

        public async Task<bool> UpdateMunicipioAsync(MunicipioRequest municipio)
        {
            var result = await UpdateMunicipioResultAsync(municipio);
            return result.Success && result.Data == true;
        }

        public async Task<bool> DeleteMunicipioAsync(MunicipioRequest municipio)
        {
            var result = await DeleteMunicipioResultAsync(municipio);
            return result.Success && result.Data == true;
        }
    }
}
