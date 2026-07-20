using CONATRADEC.Models;
using System.Collections.ObjectModel;

namespace CONATRADEC.Services
{
    public class ExtraccionNutrienteApiService
    {
        private readonly HttpClient httpClient;

        public ExtraccionNutrienteApiService() : this(ApiClientService.Client) { }

        public ExtraccionNutrienteApiService(HttpClient httpClient)
        {
            this.httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        }

        public Task<ApiResult<ObservableCollection<ExtraccionNutrienteResponse>>> GetAsync(
            CancellationToken cancellationToken = default) =>
            ConfiguracionApiServiceHelper.GetCollectionAsync<ExtraccionNutrienteResponse>(
                httpClient,
                "api/configuracion/extraccion-nutrientes",
                "los parámetros de extracción",
                cancellationToken);

        public Task<ApiResult<bool>> CreateAsync(
            ExtraccionNutrienteRequest request,
            CancellationToken cancellationToken = default) =>
            ConfiguracionApiServiceHelper.SendAsync(
                httpClient,
                HttpMethod.Post,
                "api/configuracion/extraccion-nutrientes",
                request,
                "No fue posible crear el parámetro de extracción.",
                "Parámetro de extracción creado correctamente.",
                cancellationToken);

        public Task<ApiResult<bool>> UpdateAsync(
            ExtraccionNutrienteRequest request,
            CancellationToken cancellationToken = default) =>
            request.ParametroExtraccionNutrienteCafeId <= 0
                ? Task.FromResult(ApiResult<bool>.Fail("El identificador del parámetro de extracción no es válido."))
                : ConfiguracionApiServiceHelper.SendAsync(
                    httpClient,
                    HttpMethod.Put,
                    $"api/configuracion/extraccion-nutrientes/{request.ParametroExtraccionNutrienteCafeId}",
                    request,
                    "No fue posible actualizar el parámetro de extracción.",
                    "Parámetro de extracción actualizado correctamente.",
                    cancellationToken);

        public Task<ApiResult<bool>> DeleteAsync(
            int id,
            CancellationToken cancellationToken = default) =>
            id <= 0
                ? Task.FromResult(ApiResult<bool>.Fail("El identificador del parámetro de extracción no es válido."))
                : ConfiguracionApiServiceHelper.SendAsync<object>(
                    httpClient,
                    HttpMethod.Put,
                    $"api/configuracion/extraccion-nutrientes/{id}/eliminar",
                    null,
                    "No fue posible eliminar el parámetro de extracción.",
                    "Parámetro de extracción eliminado correctamente.",
                    cancellationToken);
    }
}
