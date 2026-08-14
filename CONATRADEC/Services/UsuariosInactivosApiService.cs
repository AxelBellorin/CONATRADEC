using CONATRADEC.Models;
using System.Net.Http.Json;
using System.Text.Json;

namespace CONATRADEC.Services
{
    /// <summary>
    /// Consultas paginadas exclusivas para Usuarios inactivos.
    /// El flujo común de reactivación continúa usando
    /// CatalogosEliminadosApiService para no duplicar reglas de negocio.
    /// </summary>
    public sealed class UsuariosInactivosApiService
    {
        private readonly HttpClient httpClient;

        private static readonly JsonSerializerOptions JsonOptions =
            new(JsonSerializerDefaults.Web)
            {
                PropertyNameCaseInsensitive = true
            };

        public UsuariosInactivosApiService()
            : this(ApiClientService.Client)
        {
        }

        public UsuariosInactivosApiService(HttpClient httpClient)
        {
            this.httpClient = httpClient
                ?? throw new ArgumentNullException(nameof(httpClient));
        }

        public async Task<ApiResult<UsuarioInactivoPaginaResponse>>
            BuscarAsync(
                string? buscar,
                int pagina,
                int tamanoPagina,
                CancellationToken cancellationToken = default)
        {
            pagina = Math.Max(1, pagina);
            tamanoPagina = Math.Clamp(tamanoPagina, 5, 100);

            string ruta =
                "api/administracion/usuarios/inactivos" +
                $"?pagina={pagina}" +
                $"&tamanoPagina={tamanoPagina}";

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
                    return ApiResult<UsuarioInactivoPaginaResponse>.Fail(
                        await ApiServiceHelper.ReadResponseMessageAsync(
                            response,
                            "No fue posible cargar los usuarios inactivos.",
                            cancellationToken),
                        (int)response.StatusCode);
                }

                UsuarioInactivoPaginaEnvelope? envelope =
                    await response.Content
                        .ReadFromJsonAsync<UsuarioInactivoPaginaEnvelope>(
                            JsonOptions,
                            cancellationToken);

                if (envelope?.Data == null)
                {
                    return ApiResult<UsuarioInactivoPaginaResponse>.Fail(
                        "El servidor respondió, pero no devolvió la página de usuarios inactivos.");
                }

                return ApiResult<UsuarioInactivoPaginaResponse>.Ok(
                    envelope.Data,
                    envelope.Message ?? string.Empty);
            }
            catch (TaskCanceledException)
                when (!cancellationToken.IsCancellationRequested)
            {
                return ApiResult<UsuarioInactivoPaginaResponse>.Fail(
                    "La carga tardó demasiado. Verifique su conexión.");
            }
            catch (OperationCanceledException)
            {
                return ApiResult<UsuarioInactivoPaginaResponse>.Fail(
                    "La operación fue cancelada.");
            }
            catch (HttpRequestException)
            {
                return ApiResult<UsuarioInactivoPaginaResponse>.Fail(
                    "No fue posible comunicarse con el servidor.");
            }
            catch (JsonException)
            {
                return ApiResult<UsuarioInactivoPaginaResponse>.Fail(
                    "El servidor respondió, pero la página de usuarios inactivos no tiene el formato esperado.");
            }
            catch
            {
                return ApiResult<UsuarioInactivoPaginaResponse>.Fail(
                    "Ocurrió un error inesperado al cargar los usuarios inactivos.");
            }
        }
    }
}
