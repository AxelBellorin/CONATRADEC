using CONATRADEC.Models;

namespace CONATRADEC.Services
{
    public sealed class MatrizPermisosApiService
    {
        private readonly HttpClient httpClient;
        private readonly AdministracionConsultaApiService
            consultaApiService;

        public MatrizPermisosApiService()
            : this(ApiClientService.Client)
        {
        }

        public MatrizPermisosApiService(HttpClient httpClient)
        {
            this.httpClient =
                httpClient
                ?? throw new ArgumentNullException(nameof(httpClient));

            consultaApiService =
                new AdministracionConsultaApiService(httpClient);
        }

        public Task<ApiResult<MatrizPermisosResponse>>
            GetMatrizByRolIdResultAsync(
                int rolId,
                CancellationToken cancellationToken = default) =>
            consultaApiService.ObtenerPermisosRolAsync(
                rolId,
                cancellationToken);

        // Compatibilidad con llamadas anteriores.
        public async Task<ApiResult<
            System.Collections.ObjectModel.ObservableCollection<
                MatrizPermisosResponse>>>
            GetMatrizByRolResultAsync(
                RolRequest rolRequest,
                CancellationToken cancellationToken = default)
        {
            if (rolRequest?.RolId is not > 0)
            {
                return ApiResult<
                    System.Collections.ObjectModel.ObservableCollection<
                        MatrizPermisosResponse>>
                    .Fail("Debe seleccionar un rol válido.");
            }

            ApiResult<MatrizPermisosResponse> resultado =
                await GetMatrizByRolIdResultAsync(
                    rolRequest.RolId.Value,
                    cancellationToken);

            if (!resultado.Success || resultado.Data == null)
            {
                return ApiResult<
                    System.Collections.ObjectModel.ObservableCollection<
                        MatrizPermisosResponse>>
                    .Fail(
                        resultado.Message,
                        resultado.StatusCode);
            }

            return ApiResult<
                System.Collections.ObjectModel.ObservableCollection<
                    MatrizPermisosResponse>>
                .Ok(new(
                    new[]
                    {
                        resultado.Data
                    }));
        }

        public Task<ApiResult<bool>> GuardarMatrizResultAsync(
            MatrizPermisosRequest request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            if (request.Rol?.RolId is not > 0)
            {
                return Task.FromResult(
                    ApiResult<bool>.Fail(
                        "No se recibió un rol válido."));
            }

            if (request.Interfaz == null ||
                request.Interfaz.Count == 0)
            {
                return Task.FromResult(
                    ApiResult<bool>.Fail(
                        "No existen permisos para guardar."));
            }

            return ApiServiceHelper.SendAsync(
                httpClient,
                HttpMethod.Put,
                "api/rol-permisos/actualizar-interfaz",
                request,
                "guardar la matriz de permisos",
                "Permisos guardados correctamente.",
                cancellationToken);
        }

        public async Task<bool> GuardarMatrizAsync(
            MatrizPermisosRequest request)
        {
            ApiResult<bool> resultado =
                await GuardarMatrizResultAsync(request);

            return resultado.Success &&
                   resultado.Data == true;
        }
    }
}
