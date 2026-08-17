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

        /// <summary>
        /// Endpoint histórico conservado para formularios y versiones previas.
        /// </summary>
        public Task<ApiResult<ObservableCollection<RangoNutrienteResponse>>> GetAsync(
            CancellationToken cancellationToken = default) =>
            ConfiguracionApiServiceHelper.GetCollectionAsync<RangoNutrienteResponse>(
                httpClient,
                "api/configuracion/rangos-nutrientes",
                "los rangos nutricionales",
                cancellationToken);

        /// <summary>
        /// CRUD histórico. Se mantiene sin cambios para no afectar clientes
        /// instalados que todavía consumen estas rutas.
        /// </summary>
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

        /// <summary>
        /// CRUD administrativo protegido usado por la interfaz moderna de
        /// Rangos nutricionales. Las rutas históricas anteriores permanecen
        /// disponibles para compatibilidad con versiones instaladas.
        /// </summary>
        public Task<ApiResult<bool>> CreateDesdeRangosAsync(
            RangoNutrienteRequest request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            return ConfiguracionApiServiceHelper.SendAsync(
                httpClient,
                HttpMethod.Post,
                "api/configuracion/rangos-nutrientes/rangos",
                request,
                "No fue posible crear el rango nutricional.",
                "Rango nutricional creado correctamente.",
                cancellationToken);
        }

        public Task<ApiResult<bool>> UpdateDesdeRangosAsync(
            RangoNutrienteRequest request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            return request.ParametroRangoNutrienteCultivoId <= 0
                ? Task.FromResult(
                    ApiResult<bool>.Fail(
                        "El identificador del rango nutricional no es válido."))
                : ConfiguracionApiServiceHelper.SendAsync(
                    httpClient,
                    HttpMethod.Put,
                    $"api/configuracion/rangos-nutrientes/rangos/{request.ParametroRangoNutrienteCultivoId}",
                    request,
                    "No fue posible actualizar el rango nutricional.",
                    "Rango nutricional actualizado correctamente.",
                    cancellationToken);
        }

        public Task<ApiResult<bool>> DeleteDesdeRangosAsync(
            int id,
            CancellationToken cancellationToken = default) =>
            id <= 0
                ? Task.FromResult(
                    ApiResult<bool>.Fail(
                        "El identificador del rango nutricional no es válido."))
                : ConfiguracionApiServiceHelper.SendAsync<object>(
                    httpClient,
                    HttpMethod.Put,
                    $"api/configuracion/rangos-nutrientes/rangos/{id}/eliminar",
                    null,
                    "No fue posible eliminar el rango nutricional.",
                    "Rango nutricional eliminado correctamente.",
                    cancellationToken);
    }
}
