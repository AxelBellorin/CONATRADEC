using CONATRADEC.Models;
using System.Collections.ObjectModel;

namespace CONATRADEC.Services
{
    /// <summary>
    /// Servicio encargado de consultar y guardar la matriz de permisos.
    /// Reutiliza el HttpClient compartido de la aplicación y devuelve
    /// resultados detallados mediante ApiResult.
    /// </summary>
    public class MatrizPermisosApiService
    {
        private readonly HttpClient httpClient;

        public MatrizPermisosApiService()
            : this(ApiClientService.Client)
        {
        }

        public MatrizPermisosApiService(HttpClient httpClient)
        {
            this.httpClient = httpClient
                ?? throw new ArgumentNullException(nameof(httpClient));
        }

        public Task<ApiResult<ObservableCollection<MatrizPermisosResponse>>> GetMatrizByRolResultAsync(
            RolRequest rolRequest,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(rolRequest);

            if (string.IsNullOrWhiteSpace(rolRequest.NombreRol))
            {
                return Task.FromResult(
                    ApiResult<ObservableCollection<MatrizPermisosResponse>>.Fail(
                        "Debe seleccionar un rol válido para consultar sus permisos."));
            }

            string nombreRolCodificado = Uri.EscapeDataString(
                rolRequest.NombreRol.Trim());

            string ruta =
                $"api/rol-interfaz/matriz-por-rol-nombre?nombreRol={nombreRolCodificado}";

            return ApiServiceHelper.GetCollectionAsync<MatrizPermisosResponse>(
                httpClient,
                ruta,
                "la matriz de permisos",
                cancellationToken);
        }

        public Task<ApiResult<bool>> GuardarMatrizResultAsync(
            MatrizPermisosRequest matrizPermisosRequest,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(matrizPermisosRequest);

            if (matrizPermisosRequest.Rol is null ||
                string.IsNullOrWhiteSpace(matrizPermisosRequest.Rol.NombreRol))
            {
                return Task.FromResult(
                    ApiResult<bool>.Fail(
                        "No se recibió un rol válido para guardar la matriz de permisos."));
            }

            if (matrizPermisosRequest.Interfaz is null ||
                matrizPermisosRequest.Interfaz.Count == 0)
            {
                return Task.FromResult(
                    ApiResult<bool>.Fail(
                        "No existen permisos para guardar."));
            }

            return ApiServiceHelper.SendAsync(
                httpClient,
                HttpMethod.Put,
                "api/rol-permisos/actualizar-interfaz",
                matrizPermisosRequest,
                "guardar la matriz de permisos",
                "Permisos guardados correctamente.",
                cancellationToken);
        }

        // Métodos anteriores conservados para no romper llamadas existentes.
        public async Task<ObservableCollection<MatrizPermisosResponse>> GetMatrizByRolAsync(
            RolRequest rolRequest)
        {
            var result = await GetMatrizByRolResultAsync(rolRequest);

            return result.Data
                ?? new ObservableCollection<MatrizPermisosResponse>();
        }

        public async Task<bool> GuardarMatrizAsync(
            MatrizPermisosRequest matrizPermisosRequest)
        {
            var result = await GuardarMatrizResultAsync(
                matrizPermisosRequest);

            return result.Success && result.Data == true;
        }
    }
}
