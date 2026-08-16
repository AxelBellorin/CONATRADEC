using CONATRADEC.Models;
using System.Net.Http.Json;
using System.Text.Json;

namespace CONATRADEC.Services
{
    public sealed class AdministracionConsultaApiService
    {
        private readonly HttpClient httpClient;

        private readonly JsonSerializerOptions jsonOptions =
            new(JsonSerializerDefaults.Web)
            {
                PropertyNameCaseInsensitive = true
            };

        public AdministracionConsultaApiService()
            : this(ApiClientService.Client)
        {
        }

        public AdministracionConsultaApiService(HttpClient httpClient)
        {
            this.httpClient =
                httpClient
                ?? throw new ArgumentNullException(nameof(httpClient));
        }

        public Task<ApiResult<UsuarioAdministracionPaginaResponse>>
            BuscarUsuariosAsync(
                string? buscar,
                int pagina,
                int tamanoPagina,
                CancellationToken cancellationToken = default)
        {
            string ruta =
                "api/administracion/usuarios/buscar" +
                $"?pagina={Math.Max(1, pagina)}" +
                $"&tamanoPagina={Math.Clamp(tamanoPagina, 5, 100)}";

            if (!string.IsNullOrWhiteSpace(buscar))
            {
                ruta +=
                    $"&buscar={Uri.EscapeDataString(buscar.Trim())}";
            }

            return GetAsync<UsuarioAdministracionPaginaResponse>(
                ruta,
                "No fue posible cargar los usuarios.",
                cancellationToken);
        }

        public Task<ApiResult<RolAdministracionPaginaResponse>>
            BuscarRolesAsync(
                string? buscar,
                int pagina,
                int tamanoPagina,
                CancellationToken cancellationToken = default)
        {
            string ruta =
                "api/administracion/roles/buscar" +
                $"?pagina={Math.Max(1, pagina)}" +
                $"&tamanoPagina={Math.Clamp(tamanoPagina, 5, 100)}";

            if (!string.IsNullOrWhiteSpace(buscar))
            {
                ruta +=
                    $"&buscar={Uri.EscapeDataString(buscar.Trim())}";
            }

            return GetAsync<RolAdministracionPaginaResponse>(
                ruta,
                "No fue posible cargar los roles.",
                cancellationToken);
        }

        /// <summary>
        /// Obtiene el catálogo liviano de roles activos utilizado únicamente
        /// por el selector de la Matriz de permisos. La Matriz necesita el
        /// conjunto completo, por lo que no se fuerza paginación artificial.
        /// </summary>
        public Task<ApiResult<List<RolResponse>>>
            ObtenerRolesMatrizAsync(
                CancellationToken cancellationToken = default) =>
            GetAsync<List<RolResponse>>(
                "api/administracion/permisos/roles",
                "No fue posible cargar los roles de la matriz.",
                cancellationToken);

        public async Task<ApiResult<MatrizPermisosResponse>>
            ObtenerPermisosRolAsync(
                int rolId,
                CancellationToken cancellationToken = default)
        {
            if (rolId <= 0)
            {
                return ApiResult<MatrizPermisosResponse>.Fail(
                    "El identificador del rol no es válido.");
            }

            return await GetAsync<MatrizPermisosResponse>(
                $"api/administracion/permisos/rol/{rolId}",
                "No fue posible cargar los permisos del rol.",
                cancellationToken);
        }

        private async Task<ApiResult<T>> GetAsync<T>(
            string ruta,
            string mensaje,
            CancellationToken cancellationToken)
            where T : class, new()
        {
            try
            {
                using HttpResponseMessage response =
                    await httpClient.GetAsync(ruta, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    return ApiResult<T>.Fail(
                        await ApiServiceHelper.ReadResponseMessageAsync(
                            response,
                            mensaje,
                            cancellationToken),
                        (int)response.StatusCode);
                }

                T? data =
                    await response.Content.ReadFromJsonAsync<T>(
                        jsonOptions,
                        cancellationToken);

                return ApiResult<T>.Ok(data ?? new T());
            }
            catch (TaskCanceledException)
                when (!cancellationToken.IsCancellationRequested)
            {
                return ApiResult<T>.Fail(
                    "La consulta tardó demasiado. Intente nuevamente.");
            }
            catch (OperationCanceledException)
            {
                return ApiResult<T>.Fail("La operación fue cancelada.");
            }
            catch (HttpRequestException)
            {
                return ApiResult<T>.Fail(
                    "No fue posible comunicarse con el servidor.");
            }
            catch (JsonException)
            {
                return ApiResult<T>.Fail(
                    "El servidor respondió con un formato inesperado.");
            }
            catch
            {
                return ApiResult<T>.Fail(
                    "Ocurrió un error inesperado al consultar la administración.");
            }
        }
    }
}
