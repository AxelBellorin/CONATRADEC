using CONATRADEC.Models;
using System.Collections.ObjectModel;

namespace CONATRADEC.Services
{
    public class RolApiService
    {
        private readonly HttpClient httpClient;

        public RolApiService()
            : this(ApiClientService.Client)
        {
        }

        public RolApiService(HttpClient httpClient)
        {
            this.httpClient = httpClient
                ?? throw new ArgumentNullException(nameof(httpClient));
        }

        public Task<ApiResult<ObservableCollection<RolResponse>>> GetRolResultAsync(
            CancellationToken cancellationToken = default)
        {
            return ApiServiceHelper.GetCollectionAsync<RolResponse>(
                httpClient,
                "api/Rol/listarRoles",
                "los roles",
                cancellationToken);
        }

        public Task<ApiResult<bool>> CreateRolResultAsync(
            RolRequest rol,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(rol);

            return ApiServiceHelper.SendAsync(
                httpClient,
                HttpMethod.Post,
                "api/Rol/crearRol",
                rol,
                "crear el rol",
                "Rol creado correctamente.",
                cancellationToken);
        }

        public Task<ApiResult<bool>> UpdateRolResultAsync(
            RolRequest rol,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(rol);

            if (!rol.RolId.HasValue || rol.RolId.Value <= 0)
            {
                return Task.FromResult(
                    ApiResult<bool>.Fail(
                        "No se recibió un identificador de rol válido."));
            }

            return ApiServiceHelper.SendAsync(
                httpClient,
                HttpMethod.Put,
                $"api/Rol/editarRol/{rol.RolId.Value}",
                rol,
                "actualizar el rol",
                "Rol actualizado correctamente.",
                cancellationToken);
        }

        public Task<ApiResult<bool>> DeleteRolResultAsync(
            RolRequest rol,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(rol);

            if (!rol.RolId.HasValue || rol.RolId.Value <= 0)
            {
                return Task.FromResult(
                    ApiResult<bool>.Fail(
                        "No se recibió un identificador de rol válido."));
            }

            return ApiServiceHelper.SendAsync<RolRequest>(
                httpClient,
                HttpMethod.Delete,
                $"api/Rol/eliminarRol/{rol.RolId.Value}",
                null,
                "eliminar el rol",
                "Rol eliminado correctamente.",
                cancellationToken);
        }

        public async Task<ObservableCollection<RolResponse>> GetRolAsync()
        {
            var result = await GetRolResultAsync();
            return result.Data ?? new ObservableCollection<RolResponse>();
        }

        public async Task<bool> CreateRolAsync(RolRequest rol)
        {
            var result = await CreateRolResultAsync(rol);
            return result.Success && result.Data == true;
        }

        public async Task<bool> UpdateRolAsync(RolRequest rol)
        {
            var result = await UpdateRolResultAsync(rol);
            return result.Success && result.Data == true;
        }

        public async Task<bool> DeleteRolAsync(RolRequest rol)
        {
            var result = await DeleteRolResultAsync(rol);
            return result.Success && result.Data == true;
        }
    }
}
