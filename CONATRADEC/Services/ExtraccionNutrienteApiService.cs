using CONATRADEC.Models;
using System.Collections.ObjectModel;

namespace CONATRADEC.Services
{
    public class ExtraccionNutrienteApiService
    {
        private const string RutaAdministrativa =
            "api/administracion/extraccion-nutrientes";

        private readonly HttpClient httpClient;

        public ExtraccionNutrienteApiService()
            : this(ApiClientService.Client)
        {
        }

        public ExtraccionNutrienteApiService(
            HttpClient httpClient)
        {
            this.httpClient = httpClient
                ?? throw new ArgumentNullException(
                    nameof(httpClient));
        }

        /// <summary>
        /// Se conserva la consulta histórica completa porque el formulario la
        /// utiliza para determinar qué elementos ya tienen una extracción activa.
        /// </summary>
        public Task<ApiResult<ObservableCollection<ExtraccionNutrienteResponse>>> GetAsync(
            CancellationToken cancellationToken = default) =>
            ConfiguracionApiServiceHelper.GetCollectionAsync<ExtraccionNutrienteResponse>(
                httpClient,
                "api/configuracion/extraccion-nutrientes",
                "los parámetros de extracción",
                cancellationToken);

        /// <summary>
        /// Consulta administrativa completa utilizada por el formulario para
        /// determinar los elementos ocupados, con permiso Leer en Backend.
        /// </summary>
        public Task<ApiResult<ObservableCollection<ExtraccionNutrienteResponse>>> GetAdministracionAsync(
            CancellationToken cancellationToken = default) =>
            ConfiguracionApiServiceHelper.GetCollectionAsync<ExtraccionNutrienteResponse>(
                httpClient,
                $"{RutaAdministrativa}/todos",
                "los parámetros de extracción",
                cancellationToken);

        /// <summary>
        /// La creación conserva la ruta registrada en el flujo común de
        /// Eliminados. ConfiguracionApiServiceHelper la redirige al endpoint
        /// protegido que resuelve crear/reactivar sin afectar versiones previas.
        /// </summary>
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
                ? Task.FromResult(
                    ApiResult<bool>.Fail(
                        "El identificador del parámetro de extracción no es válido."))
                : ConfiguracionApiServiceHelper.SendAsync(
                    httpClient,
                    HttpMethod.Put,
                    $"{RutaAdministrativa}/{request.ParametroExtraccionNutrienteCafeId}",
                    request,
                    "No fue posible actualizar el parámetro de extracción.",
                    "Parámetro de extracción actualizado correctamente.",
                    cancellationToken);

        public Task<ApiResult<bool>> DeleteAsync(
            int id,
            CancellationToken cancellationToken = default) =>
            id <= 0
                ? Task.FromResult(
                    ApiResult<bool>.Fail(
                        "El identificador del parámetro de extracción no es válido."))
                : ConfiguracionApiServiceHelper.SendAsync<object>(
                    httpClient,
                    HttpMethod.Put,
                    $"{RutaAdministrativa}/{id}/eliminar",
                    null,
                    "No fue posible eliminar el parámetro de extracción.",
                    "Parámetro de extracción eliminado correctamente.",
                    cancellationToken);
    }
}
