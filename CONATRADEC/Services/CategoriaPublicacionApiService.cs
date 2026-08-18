using CONATRADEC.Models;
using System.Collections.ObjectModel;
using System.Net.Http.Json;
using System.Text.Json;

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

        public async Task<ApiResult<CategoriaPublicacionCatalogoResponse>>
            ObtenerAsync(
                int categoriaId,
                CancellationToken cancellationToken = default)
        {
            if (categoriaId <= 0)
            {
                return ApiResult<
                    CategoriaPublicacionCatalogoResponse>.Fail(
                        "El tipo de publicación seleccionado no es válido.");
            }

            try
            {
                using HttpResponseMessage response =
                    await httpClient.GetAsync(
                        $"api/configuracion/categorias-publicacion/{categoriaId}",
                        cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    string mensaje =
                        await ApiServiceHelper.ReadResponseMessageAsync(
                            response,
                            "No fue posible obtener el tipo de publicación.",
                            cancellationToken);

                    return ApiResult<
                        CategoriaPublicacionCatalogoResponse>.Fail(
                            mensaje,
                            (int)response.StatusCode);
                }

                CategoriaPublicacionCatalogoResponse? data =
                    await response.Content.ReadFromJsonAsync<
                        CategoriaPublicacionCatalogoResponse>(
                            cancellationToken: cancellationToken);

                if (data == null)
                {
                    return ApiResult<
                        CategoriaPublicacionCatalogoResponse>.Fail(
                            "El servidor no devolvió los datos del tipo de publicación.");
                }

                return ApiResult<
                    CategoriaPublicacionCatalogoResponse>.Ok(data);
            }
            catch (TaskCanceledException)
                when (!cancellationToken.IsCancellationRequested)
            {
                return ApiResult<
                    CategoriaPublicacionCatalogoResponse>.Fail(
                        "La solicitud tardó demasiado. Verifique su conexión e intente nuevamente.");
            }
            catch (OperationCanceledException)
            {
                return ApiResult<
                    CategoriaPublicacionCatalogoResponse>.Fail(
                        "La operación fue cancelada.");
            }
            catch (HttpRequestException)
            {
                return ApiResult<
                    CategoriaPublicacionCatalogoResponse>.Fail(
                        "No fue posible conectarse con el servidor. Verifique su conexión.");
            }
            catch (JsonException)
            {
                return ApiResult<
                    CategoriaPublicacionCatalogoResponse>.Fail(
                        "El servidor respondió, pero los datos del tipo de publicación no tienen el formato esperado.");
            }
            catch
            {
                return ApiResult<
                    CategoriaPublicacionCatalogoResponse>.Fail(
                        "Ocurrió un error inesperado al obtener el tipo de publicación.");
            }
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
