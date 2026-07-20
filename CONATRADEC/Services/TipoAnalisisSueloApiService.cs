using CONATRADEC.Models;
using System.Collections.ObjectModel;

namespace CONATRADEC.Services
{
    public class TipoAnalisisSueloApiService
    {
        private readonly HttpClient httpClient;

        public TipoAnalisisSueloApiService() : this(ApiClientService.Client) { }

        public TipoAnalisisSueloApiService(HttpClient httpClient)
        {
            this.httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        }

        public Task<ApiResult<ObservableCollection<TipoAnalisisSueloResponse>>> GetAsync(
            CancellationToken cancellationToken = default) =>
            ConfiguracionApiServiceHelper.GetCollectionAsync<TipoAnalisisSueloResponse>(
                httpClient,
                "api/configuracion/tipos-analisis-suelo",
                "los tipos de análisis de suelo",
                cancellationToken);

        public Task<ApiResult<bool>> CreateAsync(
            TipoAnalisisSueloRequest request,
            CancellationToken cancellationToken = default) =>
            ConfiguracionApiServiceHelper.SendAsync(
                httpClient,
                HttpMethod.Post,
                "api/configuracion/tipos-analisis-suelo",
                request,
                "No fue posible crear el tipo de análisis de suelo.",
                "Tipo de análisis de suelo creado correctamente.",
                cancellationToken);

        public Task<ApiResult<bool>> UpdateAsync(
            TipoAnalisisSueloRequest request,
            CancellationToken cancellationToken = default) =>
            request.TipoAnalisisSueloId <= 0
                ? Task.FromResult(ApiResult<bool>.Fail("El identificador del tipo de análisis no es válido."))
                : ConfiguracionApiServiceHelper.SendAsync(
                    httpClient,
                    HttpMethod.Put,
                    $"api/configuracion/tipos-analisis-suelo/{request.TipoAnalisisSueloId}",
                    request,
                    "No fue posible actualizar el tipo de análisis de suelo.",
                    "Tipo de análisis de suelo actualizado correctamente.",
                    cancellationToken);

        public Task<ApiResult<bool>> DeleteAsync(
            int id,
            CancellationToken cancellationToken = default) =>
            id <= 0
                ? Task.FromResult(ApiResult<bool>.Fail("El identificador del tipo de análisis no es válido."))
                : ConfiguracionApiServiceHelper.SendAsync<object>(
                    httpClient,
                    HttpMethod.Put,
                    $"api/configuracion/tipos-analisis-suelo/{id}/eliminar",
                    null,
                    "No fue posible eliminar el tipo de análisis de suelo.",
                    "Tipo de análisis de suelo desactivado correctamente.",
                    cancellationToken);
    }
}
