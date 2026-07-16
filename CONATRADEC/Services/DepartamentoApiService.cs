using CONATRADEC.Models;
using System.Collections.ObjectModel;

namespace CONATRADEC.Services
{
    public class DepartamentoApiService
    {
        private readonly HttpClient httpClient;

        public DepartamentoApiService()
            : this(ApiClientService.Client)
        {
        }

        public DepartamentoApiService(HttpClient httpClient)
        {
            this.httpClient = httpClient
                ?? throw new ArgumentNullException(nameof(httpClient));
        }

        public Task<ApiResult<ObservableCollection<DepartamentoResponse>>> GetDepartamentosResultAsync(
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

        public Task<ApiResult<bool>> CreateDepartamentoResultAsync(
            DepartamentoRequest departamento,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(departamento);

            return ApiServiceHelper.SendAsync(
                httpClient,
                HttpMethod.Post,
                "api/departamento/Crear",
                departamento,
                "crear el departamento",
                "Departamento creado correctamente.",
                cancellationToken);
        }

        public Task<ApiResult<bool>> UpdateDepartamentoResultAsync(
            DepartamentoRequest departamento,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(departamento);

            if (!departamento.DepartamentoId.HasValue ||
                departamento.DepartamentoId.Value <= 0)
            {
                return Task.FromResult(
                    ApiResult<bool>.Fail(
                        "No se recibió un identificador de departamento válido."));
            }

            return ApiServiceHelper.SendAsync(
                httpClient,
                HttpMethod.Put,
                $"api/departamento/actualizar/{departamento.DepartamentoId.Value}",
                departamento,
                "actualizar el departamento",
                "Departamento actualizado correctamente.",
                cancellationToken);
        }

        public Task<ApiResult<bool>> DeleteDepartamentoResultAsync(
            DepartamentoRequest departamento,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(departamento);

            if (!departamento.DepartamentoId.HasValue ||
                departamento.DepartamentoId.Value <= 0)
            {
                return Task.FromResult(
                    ApiResult<bool>.Fail(
                        "No se recibió un identificador de departamento válido."));
            }

            return ApiServiceHelper.SendAsync<DepartamentoRequest>(
                httpClient,
                HttpMethod.Delete,
                $"api/departamento/eliminar/{departamento.DepartamentoId.Value}",
                null,
                "eliminar el departamento",
                "Departamento eliminado correctamente.",
                cancellationToken);
        }

        public async Task<ObservableCollection<DepartamentoResponse>> GetDepartamentosAsync(
            int? paisId)
        {
            var result = await GetDepartamentosResultAsync(paisId);
            return result.Data ?? new ObservableCollection<DepartamentoResponse>();
        }

        public async Task<bool> CreateDepartamentoAsync(
            DepartamentoRequest departamento)
        {
            var result = await CreateDepartamentoResultAsync(departamento);
            return result.Success && result.Data == true;
        }

        public async Task<bool> UpdateDepartamentoAsync(
            DepartamentoRequest departamento)
        {
            var result = await UpdateDepartamentoResultAsync(departamento);
            return result.Success && result.Data == true;
        }

        public async Task<bool> DeleteDepartamentoAsync(
            DepartamentoRequest departamento)
        {
            var result = await DeleteDepartamentoResultAsync(departamento);
            return result.Success && result.Data == true;
        }
    }
}
