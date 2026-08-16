using CONATRADEC.Models;
using Microsoft.Maui.ApplicationModel;
using System.Collections.ObjectModel;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace CONATRADEC.Services
{
    public class RolApiService
    {
        private const string CodigoRolInactivo =
            "ROL_INACTIVO_EXISTENTE";

        private const string OpcionReactivar =
            "Reactivar y usar estos datos";

        private const string OpcionCrearNuevo =
            "Crear un registro diferente";

        private readonly HttpClient httpClient;

        private readonly JsonSerializerOptions jsonOptions =
            new(JsonSerializerDefaults.Web)
            {
                PropertyNameCaseInsensitive = true
            };

        public RolApiService()
            : this(ApiClientService.Client)
        {
        }

        public RolApiService(HttpClient httpClient)
        {
            this.httpClient = httpClient
                ?? throw new ArgumentNullException(nameof(httpClient));
        }

        // ==========================================================
        // CONTRATOS HISTÓRICOS
        // ==========================================================
        // Se conservan para compatibilidad con consumidores existentes.

        public Task<ApiResult<ObservableCollection<RolResponse>>> GetRolResultAsync(
            CancellationToken cancellationToken = default)
        {
            return ApiServiceHelper.GetCollectionAsync<RolResponse>(
                httpClient,
                "api/Rol/listarRoles",
                "los roles",
                cancellationToken);
        }

        public Task<ApiResult<bool>> CreateRolResultAsync(
            RolRequest rol,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(rol);

            return ApiServiceHelper.SendAsync(
                httpClient,
                HttpMethod.Post,
                "api/Rol/crearRol",
                rol,
                "crear el rol",
                "Rol creado correctamente.",
                cancellationToken);
        }

        public Task<ApiResult<bool>> UpdateRolResultAsync(
            RolRequest rol,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(rol);

            if (!rol.RolId.HasValue || rol.RolId.Value <= 0)
            {
                return Task.FromResult(
                    ApiResult<bool>.Fail(
                        "No se recibió un identificador de rol válido."));
            }

            return ApiServiceHelper.SendAsync(
                httpClient,
                HttpMethod.Put,
                $"api/Rol/editarRol/{rol.RolId.Value}",
                rol,
                "actualizar el rol",
                "Rol actualizado correctamente.",
                cancellationToken);
        }

        public Task<ApiResult<bool>> DeleteRolResultAsync(
            RolRequest rol,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(rol);

            if (!rol.RolId.HasValue || rol.RolId.Value <= 0)
            {
                return Task.FromResult(
                    ApiResult<bool>.Fail(
                        "No se recibió un identificador de rol válido."));
            }

            return ApiServiceHelper.SendAsync<RolRequest>(
                httpClient,
                HttpMethod.Delete,
                $"api/Rol/eliminarRol/{rol.RolId.Value}",
                null,
                "eliminar el rol",
                "Rol eliminado correctamente.",
                cancellationToken);
        }

        public async Task<ObservableCollection<RolResponse>> GetRolAsync()
        {
            var result = await GetRolResultAsync();
            return result.Data ?? new ObservableCollection<RolResponse>();
        }

        public async Task<bool> CreateRolAsync(RolRequest rol)
        {
            var result = await CreateRolResultAsync(rol);
            return result.Success && result.Data == true;
        }

        public async Task<bool> UpdateRolAsync(RolRequest rol)
        {
            var result = await UpdateRolResultAsync(rol);
            return result.Success && result.Data == true;
        }

        public async Task<bool> DeleteRolAsync(RolRequest rol)
        {
            var result = await DeleteRolResultAsync(rol);
            return result.Success && result.Data == true;
        }

        // ==========================================================
        // ADMINISTRACIÓN ACTUAL DE ROLES
        // ==========================================================

        public async Task<ApiResult<RolAdministracionPaginaResponse>>
            BuscarPaginadoAsync(
                string? buscar,
                bool incluirInactivos,
                int pagina,
                int tamanoPagina,
                CancellationToken cancellationToken = default)
        {
            string ruta =
                incluirInactivos
                    ? "api/Rol/administracion/inactivos/paginado"
                    : "api/Rol/administracion/paginado";

            ruta +=
                $"?pagina={Math.Max(1, pagina)}" +
                $"&tamanoPagina={Math.Clamp(tamanoPagina, 5, 100)}";

            if (!string.IsNullOrWhiteSpace(buscar))
            {
                ruta +=
                    $"&buscar={Uri.EscapeDataString(buscar.Trim())}";
            }

            try
            {
                using HttpResponseMessage response =
                    await httpClient.GetAsync(
                        ruta,
                        cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    return ApiResult<RolAdministracionPaginaResponse>.Fail(
                        await ApiServiceHelper.ReadResponseMessageAsync(
                            response,
                            incluirInactivos
                                ? "No fue posible cargar los roles eliminados."
                                : "No fue posible cargar los roles.",
                            cancellationToken),
                        (int)response.StatusCode);
                }

                RolAdministracionPaginaResponse? data =
                    await response.Content
                        .ReadFromJsonAsync<RolAdministracionPaginaResponse>(
                            jsonOptions,
                            cancellationToken);

                return ApiResult<RolAdministracionPaginaResponse>.Ok(
                    data ?? new RolAdministracionPaginaResponse());
            }
            catch (TaskCanceledException)
                when (!cancellationToken.IsCancellationRequested)
            {
                return ApiResult<RolAdministracionPaginaResponse>.Fail(
                    "La consulta tardó demasiado. Intente nuevamente.");
            }
            catch (OperationCanceledException)
            {
                return ApiResult<RolAdministracionPaginaResponse>.Fail(
                    "La operación fue cancelada.");
            }
            catch (HttpRequestException)
            {
                return ApiResult<RolAdministracionPaginaResponse>.Fail(
                    "No fue posible comunicarse con el servidor.");
            }
            catch (JsonException)
            {
                return ApiResult<RolAdministracionPaginaResponse>.Fail(
                    "El servidor respondió, pero el listado de roles no tiene el formato esperado.");
            }
            catch
            {
                return ApiResult<RolAdministracionPaginaResponse>.Fail(
                    "Ocurrió un error inesperado al consultar los roles.");
            }
        }

        public async Task<ApiResult<RolResponse>>
            CrearRolAdministracionResultAsync(
                RolRequest rol,
                CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(rol);

            ApiResult<RolResponse> resultado =
                await EnviarRolAsync(
                    HttpMethod.Post,
                    "api/Rol/administracion/crear",
                    rol,
                    "crear el rol",
                    "Rol creado correctamente.",
                    cancellationToken,
                    manejarRolInactivo: true);

            return resultado;
        }

        public Task<ApiResult<RolResponse>>
            ActualizarRolAdministracionResultAsync(
                RolRequest rol,
                CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(rol);

            if (!rol.RolId.HasValue ||
                rol.RolId.Value <= 0)
            {
                return Task.FromResult(
                    ApiResult<RolResponse>.Fail(
                        "No se recibió un identificador de rol válido."));
            }

            return EnviarRolAsync(
                HttpMethod.Put,
                $"api/Rol/administracion/{rol.RolId.Value}",
                rol,
                "actualizar el rol",
                "Rol actualizado correctamente.",
                cancellationToken);
        }

        public Task<ApiResult<RolResponse>>
            ReactivarRolAdministracionResultAsync(
                RolRequest rol,
                CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(rol);

            if (!rol.RolId.HasValue ||
                rol.RolId.Value <= 0)
            {
                return Task.FromResult(
                    ApiResult<RolResponse>.Fail(
                        "No se recibió un identificador de rol válido."));
            }

            return EnviarRolAsync(
                HttpMethod.Put,
                $"api/Rol/administracion/{rol.RolId.Value}/reactivar",
                rol,
                "reactivar el rol",
                "Rol reactivado correctamente.",
                cancellationToken);
        }

        public async Task<ApiResult<bool>>
            EliminarRolAdministracionResultAsync(
                int rolId,
                CancellationToken cancellationToken = default)
        {
            if (rolId <= 0)
            {
                return ApiResult<bool>.Fail(
                    "No se recibió un identificador de rol válido.");
            }

            try
            {
                using HttpResponseMessage response =
                    await httpClient.DeleteAsync(
                        $"api/Rol/administracion/{rolId}",
                        cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    return ApiResult<bool>.Fail(
                        await ApiServiceHelper.ReadResponseMessageAsync(
                            response,
                            "No fue posible eliminar el rol.",
                            cancellationToken),
                        (int)response.StatusCode);
                }

                RolOperacionEnvelope? envelope =
                    await LeerEnvelopeAsync(
                        response,
                        cancellationToken);

                return ApiResult<bool>.Ok(
                    true,
                    string.IsNullOrWhiteSpace(envelope?.Message)
                        ? "Rol eliminado correctamente."
                        : envelope.Message);
            }
            catch (TaskCanceledException)
                when (!cancellationToken.IsCancellationRequested)
            {
                return ApiResult<bool>.Fail(
                    "La solicitud tardó demasiado. Intente nuevamente.");
            }
            catch (OperationCanceledException)
            {
                return ApiResult<bool>.Fail(
                    "La operación fue cancelada.");
            }
            catch (HttpRequestException)
            {
                return ApiResult<bool>.Fail(
                    "No fue posible comunicarse con el servidor.");
            }
            catch (JsonException)
            {
                return ApiResult<bool>.Fail(
                    "El servidor respondió con un formato inesperado.");
            }
            catch
            {
                return ApiResult<bool>.Fail(
                    "Ocurrió un error inesperado al eliminar el rol.");
            }
        }

        private async Task<ApiResult<RolResponse>> EnviarRolAsync(
            HttpMethod method,
            string route,
            RolRequest rol,
            string accion,
            string mensajeExito,
            CancellationToken cancellationToken,
            bool manejarRolInactivo = false)
        {
            try
            {
                using var request =
                    new HttpRequestMessage(
                        method,
                        route)
                    {
                        Content =
                            JsonContent.Create(
                                rol,
                                options: jsonOptions)
                    };

                using HttpResponseMessage response =
                    await httpClient.SendAsync(
                        request,
                        cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    RolOperacionEnvelope? envelope =
                        await LeerEnvelopeAsync(
                            response,
                            cancellationToken);

                    if (envelope?.Data == null ||
                        envelope.Data.RolId is not > 0)
                    {
                        return ApiResult<RolResponse>.Fail(
                            "La operación se procesó, pero el servidor no devolvió el rol actualizado.");
                    }

                    return ApiResult<RolResponse>.Ok(
                        envelope.Data,
                        string.IsNullOrWhiteSpace(envelope.Message)
                            ? mensajeExito
                            : envelope.Message);
                }

                if (manejarRolInactivo &&
                    response.StatusCode == HttpStatusCode.Conflict)
                {
                    RolOperacionEnvelope? conflicto =
                        await LeerEnvelopeAsync(
                            response,
                            cancellationToken);

                    if (string.Equals(
                            conflicto?.Code,
                            CodigoRolInactivo,
                            StringComparison.OrdinalIgnoreCase) &&
                        conflicto?.Data?.RolId is > 0)
                    {
                        return await ResolverRolInactivoAsync(
                            rol,
                            conflicto.Data,
                            cancellationToken);
                    }
                }

                return ApiResult<RolResponse>.Fail(
                    await ApiServiceHelper.ReadResponseMessageAsync(
                        response,
                        $"No fue posible {accion}.",
                        cancellationToken),
                    (int)response.StatusCode);
            }
            catch (TaskCanceledException)
                when (!cancellationToken.IsCancellationRequested)
            {
                return ApiResult<RolResponse>.Fail(
                    "La solicitud tardó demasiado. Intente nuevamente.");
            }
            catch (OperationCanceledException)
            {
                return ApiResult<RolResponse>.Fail(
                    "La operación fue cancelada.");
            }
            catch (HttpRequestException)
            {
                return ApiResult<RolResponse>.Fail(
                    "No fue posible comunicarse con el servidor.");
            }
            catch (JsonException)
            {
                return ApiResult<RolResponse>.Fail(
                    "El servidor respondió con un formato inesperado.");
            }
            catch
            {
                return ApiResult<RolResponse>.Fail(
                    $"Ocurrió un error inesperado al {accion}.");
            }
        }

        private async Task<ApiResult<RolResponse>>
            ResolverRolInactivoAsync(
                RolRequest nuevoRol,
                RolResponse inactivo,
                CancellationToken cancellationToken)
        {
            string? decision =
                await MostrarOpcionesRolInactivoAsync(
                    inactivo);

            if (decision == OpcionReactivar)
            {
                var request =
                    new RolRequest
                    {
                        RolId = inactivo.RolId,
                        NombreRol = nuevoRol.NombreRol,
                        DescripcionRol = nuevoRol.DescripcionRol
                    };

                return await ReactivarRolAdministracionResultAsync(
                    request,
                    cancellationToken);
            }

            if (decision == OpcionCrearNuevo)
            {
                return await EnviarRolAsync(
                    HttpMethod.Post,
                    "api/Rol/administracion/crear?crearNuevoSiExisteInactivo=true",
                    nuevoRol,
                    "crear el rol",
                    "Rol creado correctamente.",
                    cancellationToken);
            }

            return ApiResult<RolResponse>.Fail(
                "La creación fue cancelada.");
        }

        private static async Task<string?>
            MostrarOpcionesRolInactivoAsync(
                RolResponse rol)
        {
            return await MainThread.InvokeOnMainThreadAsync(
                async () =>
                {
                    Page? pagina =
                        Application.Current?
                            .Windows
                            .FirstOrDefault()?
                            .Page;

                    if (pagina == null)
                        return null;

                    string nombre =
                        string.IsNullOrWhiteSpace(rol.NombreMostrar)
                            ? "el rol eliminado"
                            : $"'{rol.NombreMostrar}'";

                    return await pagina.DisplayActionSheet(
                        $"Ya existe {nombre}. Puede reactivarlo conservando su identificador e historial de permisos.",
                        "Cancelar",
                        null,
                        OpcionReactivar,
                        OpcionCrearNuevo);
                });
        }

        private async Task<RolOperacionEnvelope?>
            LeerEnvelopeAsync(
                HttpResponseMessage response,
                CancellationToken cancellationToken)
        {
            string contenido =
                await response.Content.ReadAsStringAsync(
                    cancellationToken);

            if (string.IsNullOrWhiteSpace(contenido))
                return null;

            return JsonSerializer.Deserialize<RolOperacionEnvelope>(
                contenido,
                jsonOptions);
        }

        private sealed class RolOperacionEnvelope
        {
            public bool Success { get; set; }
            public string? Code { get; set; }
            public string? Message { get; set; }
            public RolResponse? Data { get; set; }
        }
    }
}
