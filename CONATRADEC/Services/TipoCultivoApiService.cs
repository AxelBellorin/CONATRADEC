using CONATRADEC.Models;
using System.Collections.ObjectModel;

namespace CONATRADEC.Services
{
    public class TipoCultivoApiService
    {
        private readonly HttpClient httpClient;

        public TipoCultivoApiService() : this(ApiClientService.Client) { }

        public TipoCultivoApiService(HttpClient httpClient)
        {
            this.httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        }

        public Task<ApiResult<ObservableCollection<TipoCultivoResponse>>> GetAsync(
            CancellationToken cancellationToken = default) =>
            ConfiguracionApiServiceHelper.GetCollectionAsync<TipoCultivoResponse>(
                httpClient,
                "api/configuracion/tipos-cultivo",
                "los tipos de cultivo",
                cancellationToken);

        public Task<ApiResult<bool>> CreateAsync(
            TipoCultivoRequest request,
            CancellationToken cancellationToken = default) =>
            ConfiguracionApiServiceHelper.SendAsync(
                httpClient,
                HttpMethod.Post,
                "api/configuracion/tipos-cultivo",
                request,
                "No fue posible crear el tipo de cultivo.",
                "Tipo de cultivo creado correctamente.",
                cancellationToken);

        public Task<ApiResult<bool>> UpdateAsync(
            TipoCultivoRequest request,
            CancellationToken cancellationToken = default) =>
            request.TipoCultivoId <= 0
                ? Task.FromResult(ApiResult<bool>.Fail("El identificador del tipo de cultivo no es válido."))
                : ConfiguracionApiServiceHelper.SendAsync(
                    httpClient,
                    HttpMethod.Put,
                    $"api/configuracion/tipos-cultivo/{request.TipoCultivoId}",
                    request,
                    "No fue posible actualizar el tipo de cultivo.",
                    "Tipo de cultivo actualizado correctamente.",
                    cancellationToken);

        public Task<ApiResult<bool>> DeleteAsync(
            int id,
            CancellationToken cancellationToken = default) =>
            id <= 0
                ? Task.FromResult(ApiResult<bool>.Fail("El identificador del tipo de cultivo no es válido."))
                : ConfiguracionApiServiceHelper.SendAsync<object>(
                    httpClient,
                    HttpMethod.Put,
                    $"api/configuracion/tipos-cultivo/{id}/eliminar",
                    null,
                    "No fue posible eliminar el tipo de cultivo.",
                    "Tipo de cultivo desactivado correctamente.",
                    cancellationToken);
    }
}
