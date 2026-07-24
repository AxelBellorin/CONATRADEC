using CONATRADEC.Models;
using System.Collections.ObjectModel;

namespace CONATRADEC.Services
{
    public sealed class CategoriaPublicacionApiService
    {
        private readonly HttpClient httpClient;

        public CategoriaPublicacionApiService()
            : this(ApiClientService.Client)
        {
        }

        public CategoriaPublicacionApiService(HttpClient httpClient)
        {
            this.httpClient = httpClient ??
                throw new ArgumentNullException(nameof(httpClient));
        }

        public Task<ApiResult<ObservableCollection<
            CategoriaPublicacionCatalogoResponse>>> GetAsync(
                bool incluirInactivas,
                string? buscar,
                CancellationToken cancellationToken = default)
        {
            string ruta =
                "api/configuracion/categorias-publicacion" +
                $"?incluirInactivas={incluirInactivas.ToString().ToLowerInvariant()}";

            if (!string.IsNullOrWhiteSpace(buscar))
            {
                ruta += "&buscar=" +
                    Uri.EscapeDataString(buscar.Trim());
            }

            return ConfiguracionApiServiceHelper
                .GetCollectionAsync<CategoriaPublicacionCatalogoResponse>(
                    httpClient,
                    ruta,
                    "los tipos de publicación",
                    cancellationToken);
        }

        public async Task<ApiResult<bool>> CrearAsync(
            CategoriaPublicacionGuardarRequest request,
            CancellationToken cancellationToken = default)
        {
            ApiResult<bool> result =
                await ConfiguracionApiServiceHelper.SendAsync(
                    httpClient,
                    HttpMethod.Post,
                    "api/configuracion/categorias-publicacion",
                    request,
                    "No fue posible crear el tipo de publicación.",
                    "Tipo de publicación creado correctamente.",
                    cancellationToken);

            if (result.Success)
                PublicacionListadoEstadoService.MarcarActualizacion();

            return result;
        }

        public async Task<ApiResult<bool>> ActualizarAsync(
            int categoriaId,
            CategoriaPublicacionGuardarRequest request,
            CancellationToken cancellationToken = default)
        {
            if (categoriaId <= 0)
            {
                return ApiResult<bool>.Fail(
                    "El tipo de publicación seleccionado no es válido.");
            }

            ApiResult<bool> result =
                await ConfiguracionApiServiceHelper.SendAsync(
                    httpClient,
                    HttpMethod.Put,
                    $"api/configuracion/categorias-publicacion/{categoriaId}",
                    request,
                    "No fue posible actualizar el tipo de publicación.",
                    "Tipo de publicación actualizado correctamente.",
                    cancellationToken);

            if (result.Success)
                PublicacionListadoEstadoService.MarcarActualizacion();

            return result;
        }

        public async Task<ApiResult<bool>> CambiarEstadoAsync(
            int categoriaId,
            bool activo,
            CancellationToken cancellationToken = default)
        {
            if (categoriaId <= 0)
            {
                return ApiResult<bool>.Fail(
                    "El tipo de publicación seleccionado no es válido.");
            }

            ApiResult<bool> result =
                await ConfiguracionApiServiceHelper.SendAsync(
                    httpClient,
                    HttpMethod.Patch,
                    $"api/configuracion/categorias-publicacion/{categoriaId}/estado",
                    new { activo },
                    activo
                        ? "No fue posible reactivar el tipo de publicación."
                        : "No fue posible desactivar el tipo de publicación.",
                    activo
                        ? "Tipo de publicación reactivado correctamente."
                        : "Tipo de publicación desactivado correctamente.",
                    cancellationToken);

            if (result.Success)
                PublicacionListadoEstadoService.MarcarActualizacion();

            return result;
        }
    }
}
