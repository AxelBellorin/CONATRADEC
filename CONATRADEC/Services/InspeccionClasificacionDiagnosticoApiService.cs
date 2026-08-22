using CONATRADEC.Models;
using System.Net.Http.Json;
using System.Text.Json;

namespace CONATRADEC.Services
{
    /// <summary>
    /// Cliente de las clasificaciones independientes por diagnóstico dentro de
    /// la Inspección Fitosanitaria.
    /// </summary>
    public sealed class InspeccionClasificacionDiagnosticoApiService
    {
        private readonly HttpClient httpClient;

        private static readonly JsonSerializerOptions JsonOptions =
            new(JsonSerializerDefaults.Web)
            {
                PropertyNameCaseInsensitive = true
            };

        public InspeccionClasificacionDiagnosticoApiService()
            : this(ApiClientService.Client)
        {
        }

        public InspeccionClasificacionDiagnosticoApiService(
            HttpClient httpClient)
        {
            this.httpClient = httpClient ??
                throw new ArgumentNullException(nameof(httpClient));
        }

        public async Task<ApiResult<
            List<InspeccionClasificacionDiagnosticoV2>>>
            ObtenerAsync(
                int inspeccionId,
                CancellationToken cancellationToken = default)
        {
            if (inspeccionId <= 0)
            {
                return ApiResult<
                    List<InspeccionClasificacionDiagnosticoV2>>.Fail(
                        "La inspección seleccionada no es válida.");
            }

            try
            {
                using HttpResponseMessage response =
                    await httpClient.GetAsync(
                        "api/inspecciones-fitosanitarias/" +
                        $"{inspeccionId}/clasificaciones-diagnosticos",
                        cancellationToken);

                return await LeerRespuestaAsync(
                    response,
                    "cargar las clasificaciones por diagnóstico",
                    cancellationToken);
            }
            catch (TaskCanceledException)
                when (!cancellationToken.IsCancellationRequested)
            {
                return ApiResult<
                    List<InspeccionClasificacionDiagnosticoV2>>.Fail(
                        "La consulta de clasificaciones tardó demasiado.");
            }
            catch (OperationCanceledException)
            {
                return ApiResult<
                    List<InspeccionClasificacionDiagnosticoV2>>.Fail(
                        "La consulta fue cancelada.");
            }
            catch (HttpRequestException)
            {
                return ApiResult<
                    List<InspeccionClasificacionDiagnosticoV2>>.Fail(
                        "No fue posible conectarse con el servidor.");
            }
            catch
            {
                return ApiResult<
                    List<InspeccionClasificacionDiagnosticoV2>>.Fail(
                        "No fue posible cargar las clasificaciones por diagnóstico.");
            }
        }

        public async Task<ApiResult<
            List<InspeccionClasificacionDiagnosticoV2>>>
            ResolverAsync(
                int inspeccionId,
                int fotografiaId,
                ResolverInspeccionClasificacionDiagnosticoRequest request,
                CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            try
            {
                using HttpResponseMessage response =
                    await httpClient.PostAsJsonAsync(
                        "api/inspecciones-fitosanitarias/" +
                        $"{inspeccionId}/fotografias/{fotografiaId}/" +
                        "clasificaciones-diagnosticos/resolver",
                        request,
                        JsonOptions,
                        cancellationToken);

                return await LeerRespuestaAsync(
                    response,
                    "guardar la clasificación del diagnóstico",
                    cancellationToken);
            }
            catch (HttpRequestException)
            {
                return ApiResult<
                    List<InspeccionClasificacionDiagnosticoV2>>.Fail(
                        "No fue posible conectarse con el servidor.");
            }
            catch (OperationCanceledException)
            {
                return ApiResult<
                    List<InspeccionClasificacionDiagnosticoV2>>.Fail(
                        "La operación fue cancelada.");
            }
            catch
            {
                return ApiResult<
                    List<InspeccionClasificacionDiagnosticoV2>>.Fail(
                        "No fue posible guardar la clasificación del diagnóstico.");
            }
        }

        private static async Task<ApiResult<
            List<InspeccionClasificacionDiagnosticoV2>>>
            LeerRespuestaAsync(
                HttpResponseMessage response,
                string accion,
                CancellationToken cancellationToken)
        {
            try
            {
                ApiEnvelope<
                    List<InspeccionClasificacionDiagnosticoV2>>?
                    envelope = await response.Content.ReadFromJsonAsync<
                        ApiEnvelope<
                            List<InspeccionClasificacionDiagnosticoV2>>>(
                                JsonOptions,
                                cancellationToken);

                if (!response.IsSuccessStatusCode ||
                    envelope == null ||
                    !envelope.Success)
                {
                    return ApiResult<
                        List<InspeccionClasificacionDiagnosticoV2>>.Fail(
                            envelope?.Message ??
                            $"No fue posible {accion}.",
                            (int)response.StatusCode);
                }

                return ApiResult<
                    List<InspeccionClasificacionDiagnosticoV2>>.Ok(
                        envelope.Data ?? [],
                        envelope.Message);
            }
            catch (JsonException)
            {
                return ApiResult<
                    List<InspeccionClasificacionDiagnosticoV2>>.Fail(
                        "El servidor respondió con un formato no válido.",
                        (int)response.StatusCode);
            }
        }
    }
}
