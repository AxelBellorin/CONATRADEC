using CONATRADEC.Models;
using System.Net.Http.Json;
using System.Text.Json;

namespace CONATRADEC.Services
{
    public sealed class MotivoDevolucionTecnicoApiService
    {
        private const string RutaHistorica =
            "api/configuracion/motivos-devolucion-tecnico";

        private const string RutaV2 =
            "api/configuracion/motivos-devolucion-tecnico/v2";

        private readonly HttpClient client = ApiClientService.Client;

        /// <summary>
        /// Selector operativo utilizado por el analizador. Siempre consulta al
        /// servidor para evitar conservar motivos obsoletos entre visitas.
        /// El parámetro forzar se conserva para no romper consumidores previos.
        /// </summary>
        public Task<ApiResult<List<MotivoDevolucionTecnicoItem>>>
            ListarActivosAsync(
                bool forzar = false,
                CancellationToken cancellationToken = default)
        {
            _ = forzar;
            return ObtenerListaV2Async(
                $"{RutaV2}/selector-activos",
                cancellationToken);
        }

        /// <summary>
        /// Método histórico conservado para compatibilidad con código anterior.
        /// La interfaz auditada usa las operaciones v2 que separan activos e
        /// inactivos.
        /// </summary>
        public Task<ApiResult<List<MotivoDevolucionTecnicoItem>>>
            ListarAdministracionAsync(
                bool incluirInactivos,
                string? buscar,
                CancellationToken cancellationToken = default)
        {
            string ruta =
                $"{RutaHistorica}?incluirInactivos={incluirInactivos.ToString().ToLowerInvariant()}";

            if (!string.IsNullOrWhiteSpace(buscar))
                ruta += $"&buscar={Uri.EscapeDataString(buscar.Trim())}";

            return ObtenerListaHistoricaAsync(ruta, cancellationToken);
        }

        public Task<ApiResult<List<MotivoDevolucionTecnicoItem>>>
            ListarAdministracionV2Async(
                string? buscar,
                CancellationToken cancellationToken = default)
        {
            string ruta = RutaV2;
            if (!string.IsNullOrWhiteSpace(buscar))
                ruta += $"?buscar={Uri.EscapeDataString(buscar.Trim())}";

            return ObtenerListaV2Async(ruta, cancellationToken);
        }

        public Task<ApiResult<List<MotivoDevolucionTecnicoItem>>>
            ListarEliminadosV2Async(
                string? buscar,
                CancellationToken cancellationToken = default)
        {
            string ruta = $"{RutaV2}/eliminados";
            if (!string.IsNullOrWhiteSpace(buscar))
                ruta += $"?buscar={Uri.EscapeDataString(buscar.Trim())}";

            return ObtenerListaV2Async(ruta, cancellationToken);
        }

        public Task<ApiResult<MotivoDevolucionTecnicoItem>> ObtenerV2Async(
            int id,
            CancellationToken cancellationToken = default) =>
            ObtenerUnoV2Async($"{RutaV2}/{id}", cancellationToken);

        public Task<ApiResult<MotivoDevolucionTecnicoItem>> CrearV2Async(
            MotivoDevolucionTecnicoRequest request,
            CancellationToken cancellationToken = default) =>
            EnviarV2Async(
                HttpMethod.Post,
                RutaV2,
                request,
                cancellationToken);

        public Task<ApiResult<MotivoDevolucionTecnicoItem>> ActualizarV2Async(
            int id,
            MotivoDevolucionTecnicoRequest request,
            CancellationToken cancellationToken = default) =>
            EnviarV2Async(
                HttpMethod.Put,
                $"{RutaV2}/{id}",
                request,
                cancellationToken);

        public Task<ApiResult<bool>> EliminarV2Async(
            int id,
            string rowVersion,
            CancellationToken cancellationToken = default) =>
            CambiarEstadoV2Async(
                $"{RutaV2}/{id}/eliminar",
                rowVersion,
                cancellationToken);

        public Task<ApiResult<bool>> RecuperarV2Async(
            int id,
            string rowVersion,
            CancellationToken cancellationToken = default) =>
            CambiarEstadoV2Async(
                $"{RutaV2}/{id}/recuperar",
                rowVersion,
                cancellationToken);

        // Operaciones históricas. Se conservan sin cambiar sus rutas.
        public Task<ApiResult<MotivoDevolucionTecnicoItem>> CrearAsync(
            MotivoDevolucionTecnicoRequest request,
            CancellationToken cancellationToken = default) =>
            EnviarHistoricoAsync(
                HttpMethod.Post,
                RutaHistorica,
                request,
                cancellationToken);

        public Task<ApiResult<MotivoDevolucionTecnicoItem>> ActualizarAsync(
            int id,
            MotivoDevolucionTecnicoRequest request,
            CancellationToken cancellationToken = default) =>
            EnviarHistoricoAsync(
                HttpMethod.Put,
                $"{RutaHistorica}/{id}",
                request,
                cancellationToken);

        public Task<ApiResult<bool>> EliminarAsync(
            int id,
            CancellationToken cancellationToken = default) =>
            CambiarEstadoHistoricoAsync(
                $"{RutaHistorica}/{id}/eliminar",
                cancellationToken);

        public Task<ApiResult<bool>> RecuperarAsync(
            int id,
            CancellationToken cancellationToken = default) =>
            CambiarEstadoHistoricoAsync(
                $"{RutaHistorica}/{id}/recuperar",
                cancellationToken);

        /// <summary>
        /// Se conserva para compatibilidad binaria/fuente. La versión auditada
        /// ya no mantiene caché global de este catálogo.
        /// </summary>
        public static void LimpiarCache()
        {
        }

        private async Task<ApiResult<List<MotivoDevolucionTecnicoItem>>>
            ObtenerListaV2Async(
                string ruta,
                CancellationToken cancellationToken)
        {
            try
            {
                SesionInactividadService.Instance.RegistrarActividad();

                using HttpResponseMessage response =
                    await client.GetAsync(ruta, cancellationToken);

                ApiEnvelope<List<MotivoDevolucionTecnicoItem>>? envelope =
                    await LeerEnvelopeAsync<List<MotivoDevolucionTecnicoItem>>(
                        response,
                        cancellationToken);

                if (!response.IsSuccessStatusCode || envelope?.Data == null)
                {
                    return ApiResult<List<MotivoDevolucionTecnicoItem>>.Fail(
                        envelope?.Message ??
                        "No fue posible cargar los motivos de devolución.",
                        (int)response.StatusCode);
                }

                return ApiResult<List<MotivoDevolucionTecnicoItem>>.Ok(
                    envelope.Data,
                    envelope.Message);
            }
            catch (OperationCanceledException)
            {
                return ApiResult<List<MotivoDevolucionTecnicoItem>>.Fail(
                    "La operación fue cancelada.");
            }
            catch (HttpRequestException)
            {
                return ApiResult<List<MotivoDevolucionTecnicoItem>>.Fail(
                    "No fue posible comunicarse con el servidor.");
            }
            catch (JsonException)
            {
                return ApiResult<List<MotivoDevolucionTecnicoItem>>.Fail(
                    "El servidor respondió con un formato no esperado.");
            }
        }

        private async Task<ApiResult<MotivoDevolucionTecnicoItem>>
            ObtenerUnoV2Async(
                string ruta,
                CancellationToken cancellationToken)
        {
            try
            {
                SesionInactividadService.Instance.RegistrarActividad();

                using HttpResponseMessage response =
                    await client.GetAsync(ruta, cancellationToken);

                ApiEnvelope<MotivoDevolucionTecnicoItem>? envelope =
                    await LeerEnvelopeAsync<MotivoDevolucionTecnicoItem>(
                        response,
                        cancellationToken);

                if (!response.IsSuccessStatusCode || envelope?.Data == null)
                {
                    return ApiResult<MotivoDevolucionTecnicoItem>.Fail(
                        envelope?.Message ??
                        "No fue posible cargar el motivo de devolución.",
                        (int)response.StatusCode);
                }

                return ApiResult<MotivoDevolucionTecnicoItem>.Ok(
                    envelope.Data,
                    envelope.Message);
            }
            catch (OperationCanceledException)
            {
                return ApiResult<MotivoDevolucionTecnicoItem>.Fail(
                    "La operación fue cancelada.");
            }
            catch (HttpRequestException)
            {
                return ApiResult<MotivoDevolucionTecnicoItem>.Fail(
                    "No fue posible comunicarse con el servidor.");
            }
            catch (JsonException)
            {
                return ApiResult<MotivoDevolucionTecnicoItem>.Fail(
                    "El servidor respondió con un formato no esperado.");
            }
        }

        private async Task<ApiResult<MotivoDevolucionTecnicoItem>> EnviarV2Async(
            HttpMethod method,
            string ruta,
            MotivoDevolucionTecnicoRequest request,
            CancellationToken cancellationToken)
        {
            try
            {
                SesionInactividadService.Instance.RegistrarActividad();

                using var mensaje = new HttpRequestMessage(method, ruta)
                {
                    Content = JsonContent.Create(request)
                };

                using HttpResponseMessage response =
                    await client.SendAsync(mensaje, cancellationToken);

                ApiEnvelope<MotivoDevolucionTecnicoItem>? envelope =
                    await LeerEnvelopeAsync<MotivoDevolucionTecnicoItem>(
                        response,
                        cancellationToken);

                if (!response.IsSuccessStatusCode || envelope?.Data == null)
                {
                    return ApiResult<MotivoDevolucionTecnicoItem>.Fail(
                        envelope?.Message ??
                        "No fue posible guardar el motivo de devolución.",
                        (int)response.StatusCode);
                }

                return ApiResult<MotivoDevolucionTecnicoItem>.Ok(
                    envelope.Data,
                    envelope.Message);
            }
            catch (OperationCanceledException)
            {
                return ApiResult<MotivoDevolucionTecnicoItem>.Fail(
                    "La operación fue cancelada.");
            }
            catch (HttpRequestException)
            {
                return ApiResult<MotivoDevolucionTecnicoItem>.Fail(
                    "No fue posible comunicarse con el servidor.");
            }
            catch (JsonException)
            {
                return ApiResult<MotivoDevolucionTecnicoItem>.Fail(
                    "El servidor respondió con un formato no esperado.");
            }
        }

        private async Task<ApiResult<bool>> CambiarEstadoV2Async(
            string ruta,
            string rowVersion,
            CancellationToken cancellationToken)
        {
            try
            {
                SesionInactividadService.Instance.RegistrarActividad();

                using var request = new HttpRequestMessage(HttpMethod.Put, ruta)
                {
                    Content = JsonContent.Create(new { rowVersion })
                };

                using HttpResponseMessage response =
                    await client.SendAsync(request, cancellationToken);

                ApiEnvelope<object>? envelope =
                    await LeerEnvelopeAsync<object>(response, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    return ApiResult<bool>.Fail(
                        envelope?.Message ??
                        "No fue posible cambiar el estado del motivo.",
                        (int)response.StatusCode);
                }

                return ApiResult<bool>.Ok(
                    true,
                    envelope?.Message ?? string.Empty);
            }
            catch (OperationCanceledException)
            {
                return ApiResult<bool>.Fail("La operación fue cancelada.");
            }
            catch (HttpRequestException)
            {
                return ApiResult<bool>.Fail(
                    "No fue posible comunicarse con el servidor.");
            }
        }

        private async Task<ApiResult<List<MotivoDevolucionTecnicoItem>>>
            ObtenerListaHistoricaAsync(
                string ruta,
                CancellationToken cancellationToken)
        {
            try
            {
                using HttpResponseMessage response =
                    await client.GetAsync(ruta, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    return ApiResult<List<MotivoDevolucionTecnicoItem>>.Fail(
                        await ApiServiceHelper.ReadResponseMessageAsync(
                            response,
                            "No fue posible cargar los motivos de devolución.",
                            cancellationToken),
                        (int)response.StatusCode);
                }

                List<MotivoDevolucionTecnicoItem>? data =
                    await response.Content.ReadFromJsonAsync<
                        List<MotivoDevolucionTecnicoItem>>(
                            cancellationToken: cancellationToken);

                return ApiResult<List<MotivoDevolucionTecnicoItem>>.Ok(
                    data ?? []);
            }
            catch (OperationCanceledException)
            {
                return ApiResult<List<MotivoDevolucionTecnicoItem>>.Fail(
                    "La operación fue cancelada.");
            }
            catch (HttpRequestException)
            {
                return ApiResult<List<MotivoDevolucionTecnicoItem>>.Fail(
                    "No fue posible comunicarse con el servidor.");
            }
            catch (JsonException)
            {
                return ApiResult<List<MotivoDevolucionTecnicoItem>>.Fail(
                    "El servidor respondió con un formato no esperado.");
            }
        }

        private async Task<ApiResult<MotivoDevolucionTecnicoItem>>
            EnviarHistoricoAsync(
                HttpMethod method,
                string ruta,
                MotivoDevolucionTecnicoRequest request,
                CancellationToken cancellationToken)
        {
            try
            {
                using var mensaje = new HttpRequestMessage(method, ruta)
                {
                    Content = JsonContent.Create(request)
                };
                using HttpResponseMessage response =
                    await client.SendAsync(mensaje, cancellationToken);

                ApiEnvelope<MotivoDevolucionTecnicoItem>? envelope =
                    await LeerEnvelopeAsync<MotivoDevolucionTecnicoItem>(
                        response,
                        cancellationToken);

                if (!response.IsSuccessStatusCode || envelope?.Data == null)
                {
                    return ApiResult<MotivoDevolucionTecnicoItem>.Fail(
                        envelope?.Message ??
                        "No fue posible guardar el motivo de devolución.",
                        (int)response.StatusCode);
                }

                return ApiResult<MotivoDevolucionTecnicoItem>.Ok(
                    envelope.Data,
                    envelope.Message);
            }
            catch (OperationCanceledException)
            {
                return ApiResult<MotivoDevolucionTecnicoItem>.Fail(
                    "La operación fue cancelada.");
            }
            catch (HttpRequestException)
            {
                return ApiResult<MotivoDevolucionTecnicoItem>.Fail(
                    "No fue posible comunicarse con el servidor.");
            }
            catch (JsonException)
            {
                return ApiResult<MotivoDevolucionTecnicoItem>.Fail(
                    "El servidor respondió con un formato no esperado.");
            }
        }

        private async Task<ApiResult<bool>> CambiarEstadoHistoricoAsync(
            string ruta,
            CancellationToken cancellationToken)
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Put, ruta)
                {
                    Content = JsonContent.Create(new { })
                };
                using HttpResponseMessage response =
                    await client.SendAsync(request, cancellationToken);
                ApiEnvelope<object>? envelope =
                    await LeerEnvelopeAsync<object>(response, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    return ApiResult<bool>.Fail(
                        envelope?.Message ??
                        "No fue posible cambiar el estado del motivo.",
                        (int)response.StatusCode);
                }

                return ApiResult<bool>.Ok(
                    true,
                    envelope?.Message ?? string.Empty);
            }
            catch (OperationCanceledException)
            {
                return ApiResult<bool>.Fail("La operación fue cancelada.");
            }
            catch (HttpRequestException)
            {
                return ApiResult<bool>.Fail(
                    "No fue posible comunicarse con el servidor.");
            }
        }

        private static async Task<ApiEnvelope<T>?> LeerEnvelopeAsync<T>(
            HttpResponseMessage response,
            CancellationToken cancellationToken)
        {
            try
            {
                return await response.Content.ReadFromJsonAsync<ApiEnvelope<T>>(
                    cancellationToken: cancellationToken);
            }
            catch
            {
                return null;
            }
        }

        private sealed class ApiEnvelope<T>
        {
            public bool Success { get; set; }
            public string Message { get; set; } = string.Empty;
            public T? Data { get; set; }
        }
    }
}
