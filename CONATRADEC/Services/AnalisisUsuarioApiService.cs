using CONATRADEC.Models;
using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace CONATRADEC.Services
{
    public sealed class AnalisisUsuarioApiService
    {
        private const string Endpoint =
            "api/guardar-todo/listar%20usuario";

        private readonly HttpClient httpClient;

        private readonly JsonSerializerOptions jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public AnalisisUsuarioApiService()
            : this(ApiClientService.Client)
        {
        }

        public AnalisisUsuarioApiService(HttpClient httpClient)
        {
            this.httpClient = httpClient
                ?? throw new ArgumentNullException(nameof(httpClient));
        }

        public async Task<AnalisisGuardadoUsuarioListaResponse> ListarAsync(
            int? usuarioId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                string endpoint = usuarioId.HasValue && usuarioId.Value > 0
                    ? $"{Endpoint}?usuarioId={usuarioId.Value}"
                    : Endpoint;

                using HttpResponseMessage response =
                    await httpClient.GetAsync(endpoint, cancellationToken);

                string json = await response.Content
                    .ReadAsStringAsync(cancellationToken);

                AnalisisGuardadoUsuarioListaResponse? resultado = null;

                if (!string.IsNullOrWhiteSpace(json))
                {
                    resultado =
                        JsonSerializer.Deserialize<AnalisisGuardadoUsuarioListaResponse>(
                            json,
                            jsonOptions);
                }

                if (!response.IsSuccessStatusCode)
                {
                    return new AnalisisGuardadoUsuarioListaResponse
                    {
                        Success = false,
                        Message =
                            $"No fue posible cargar los análisis. Código HTTP {(int)response.StatusCode}."
                    };
                }

                if (resultado == null)
                {
                    return new AnalisisGuardadoUsuarioListaResponse
                    {
                        Success = false,
                        Message =
                            "La API respondió, pero no se pudo interpretar el listado por usuario."
                    };
                }

                resultado.Data ??= new();
                return resultado;
            }
            catch (TaskCanceledException)
                when (!cancellationToken.IsCancellationRequested)
            {
                return new AnalisisGuardadoUsuarioListaResponse
                {
                    Success = false,
                    Message =
                        "La carga tardó demasiado. Revise la conexión e intente nuevamente."
                };
            }
            catch (HttpRequestException)
            {
                return new AnalisisGuardadoUsuarioListaResponse
                {
                    Success = false,
                    Message =
                        "No fue posible conectarse con el servidor para cargar los análisis."
                };
            }
            catch (Exception ex)
            {
                return new AnalisisGuardadoUsuarioListaResponse
                {
                    Success = false,
                    Message =
                        $"Ocurrió un error al cargar los análisis: {ex.Message}"
                };
            }
        }
    }
}
