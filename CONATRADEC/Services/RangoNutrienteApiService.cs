using CONATRADEC.Models;
using System.Collections.ObjectModel;

namespace CONATRADEC.Services
{
    public class RangoNutrienteApiService
    {
        private readonly HttpClient httpClient;

        public RangoNutrienteApiService() : this(ApiClientService.Client) { }

        public RangoNutrienteApiService(HttpClient httpClient)
        {
            this.httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        }

        public Task<ApiResult<ObservableCollection<RangoNutrienteResponse>>> GetAsync(
            CancellationToken cancellationToken = default) =>
            ConfiguracionApiServiceHelper.GetCollectionAsync<RangoNutrienteResponse>(
                httpClient,
                "api/configuracion/rangos-nutrientes",
                "los rangos nutricionales",
                cancellationToken);

        public Task<ApiResult<bool>> CreateAsync(
            RangoNutrienteRequest request,
            CancellationToken cancellationToken = default) =>
            ConfiguracionApiServiceHelper.SendAsync(
                httpClient,
                HttpMethod.Post,
                "api/configuracion/rangos-nutrientes",
                request,
                "No fue posible crear el rango nutricional.",
                "Rango nutricional creado correctamente.",
                cancellationToken);

        public Task<ApiResult<bool>> UpdateAsync(
            RangoNutrienteRequest request,
            CancellationToken cancellationToken = default) =>
            request.ParametroRangoNutrienteCultivoId <= 0
                ? Task.FromResult(ApiResult<bool>.Fail("El identificador del rango nutricional no es válido."))
                : ConfiguracionApiServiceHelper.SendAsync(
                    httpClient,
                    HttpMethod.Put,
                    $"api/configuracion/rangos-nutrientes/{request.ParametroRangoNutrienteCultivoId}",
                    request,
                    "No fue posible actualizar el rango nutricional.",
                    "Rango nutricional actualizado correctamente.",
                    cancellationToken);

        public Task<ApiResult<bool>> DeleteAsync(
            int id,
            CancellationToken cancellationToken = default) =>
            id <= 0
                ? Task.FromResult(ApiResult<bool>.Fail("El identificador del rango nutricional no es válido."))
                : ConfiguracionApiServiceHelper.SendAsync<object>(
                    httpClient,
                    HttpMethod.Put,
                    $"api/configuracion/rangos-nutrientes/{id}/eliminar",
                    null,
                    "No fue posible eliminar el rango nutricional.",
                    "Rango nutricional eliminado correctamente.",
                    cancellationToken);
    }
}
