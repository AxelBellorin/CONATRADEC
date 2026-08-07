using CONATRADEC.Models;
using System.Net.Http.Json;
using System.Text.Json;

namespace CONATRADEC.Services
{
    /// <summary>
    /// Cliente específico del ciclo analizador → técnico. Se mantiene separado
    /// del cliente histórico para no mezclar el cierre global anterior con la
    /// finalización de la revisión humana completa.
    /// </summary>
    public sealed class InspeccionRevisionApiService
    {
        private static readonly JsonSerializerOptions JsonOptions =
            new(JsonSerializerDefaults.Web)
            {
                PropertyNameCaseInsensitive = true
            };

        private readonly HttpClient client = ApiClientService.Client;

        public Task<ContextoRevisionAnalizadorV2> ObtenerContextoAsync(
            int inspeccionId,
            CancellationToken cancellationToken = default) =>
            EnviarAsync<ContextoRevisionAnalizadorV2>(
                new HttpRequestMessage(
                    HttpMethod.Get,
                    $"api/revision-fitosanitaria/{inspeccionId}/contexto"),
                cancellationToken);

        public Task<DevolucionTecnicoFotografiaV2> DevolverTecnicoAsync(
            int inspeccionId,
            int fotografiaId,
            int motivoId,
            string instrucciones,
            CancellationToken cancellationToken = default) =>
            EnviarAsync<DevolucionTecnicoFotografiaV2>(
                new HttpRequestMessage(
                    HttpMethod.Post,
                    $"api/revision-fitosanitaria/{inspeccionId}/devolver-tecnico")
                {
                    Content = JsonContent.Create(
                        new
                        {
                            fotografiaId,
                            motivoDevolucionTecnicoId = motivoId,
                            instrucciones = instrucciones.Trim()
                        },
                        options: JsonOptions)
                },
                cancellationToken);

        public async Task ResolverDevolucionAsync(
            int inspeccionId,
            int fotografiaId,
            string tipoFotografia,
            DateTime fechaIdentificacionCampo,
            string respuestaTecnico,
            CancellationToken cancellationToken = default)
        {
            await EnviarSinDatosAsync(
                new HttpRequestMessage(
                    HttpMethod.Post,
                    $"api/revision-fitosanitaria/{inspeccionId}/resolver-devolucion")
                {
                    Content = JsonContent.Create(
                        new
                        {
                            fotografiaId,
                            tipoFotografia = tipoFotografia.Trim(),
                            fechaIdentificacionCampo =
                                fechaIdentificacionCampo.Date,
                            respuestaTecnico = respuestaTecnico.Trim()
                        },
                        options: JsonOptions)
                },
                cancellationToken);
        }

        public Task<ContextoRevisionAnalizadorV2> FinalizarAnalizadorAsync(
            int inspeccionId,
            CancellationToken cancellationToken = default) =>
            EnviarAsync<ContextoRevisionAnalizadorV2>(
                new HttpRequestMessage(
                    HttpMethod.Post,
                    $"api/revision-fitosanitaria/{inspeccionId}/finalizar-analizador")
                {
                    Content = JsonContent.Create(new { }, options: JsonOptions)
                },
                cancellationToken);

        public Task<ContextoRevisionAnalizadorV2> EnviarAprobadorAsync(
            int inspeccionId,
            IReadOnlyCollection<int> fotografiaIds,
            CancellationToken cancellationToken = default)
        {
            if (fotografiaIds == null || fotografiaIds.Count == 0)
            {
                throw new ArgumentException(
                    "Seleccione al menos una fotografía revisada.",
                    nameof(fotografiaIds));
            }

            int[] ids = fotografiaIds
                .Where(id => id > 0)
                .Distinct()
                .ToArray();

            if (ids.Length == 0)
            {
                throw new ArgumentException(
                    "La selección de fotografías no es válida.",
                    nameof(fotografiaIds));
            }

            return EnviarAsync<ContextoRevisionAnalizadorV2>(
                new HttpRequestMessage(
                    HttpMethod.Post,
                    $"api/revision-fitosanitaria/{inspeccionId}/enviar-aprobador")
                {
                    Content = JsonContent.Create(
                        new { fotografiaIds = ids },
                        options: JsonOptions)
                },
                cancellationToken);
        }

        private async Task<T> EnviarAsync<T>(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
            where T : class
        {
            SesionInactividadService.Instance.RegistrarActividad();

            using (request)
            using (HttpResponseMessage response = await client.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken))
            {
                string contenido = await response.Content.ReadAsStringAsync(
                    cancellationToken);

                ApiEnvelopeRevision<T>? envelope = null;
                if (!string.IsNullOrWhiteSpace(contenido))
                {
                    try
                    {
                        envelope = JsonSerializer.Deserialize<
                            ApiEnvelopeRevision<T>>(contenido, JsonOptions);
                    }
                    catch (JsonException)
                    {
                    }
                }

                if (!response.IsSuccessStatusCode)
                {
                    throw new InvalidOperationException(
                        string.IsNullOrWhiteSpace(envelope?.Message)
                            ? "El servidor rechazó la operación de revisión."
                            : envelope!.Message);
                }

                if (envelope?.Data is not null)
                    return envelope.Data;

                throw new InvalidOperationException(
                    "El servidor devolvió una respuesta incompleta.");
            }
        }

        private async Task EnviarSinDatosAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            SesionInactividadService.Instance.RegistrarActividad();

            using (request)
            using (HttpResponseMessage response = await client.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken))
            {
                string contenido = await response.Content.ReadAsStringAsync(
                    cancellationToken);

                if (response.IsSuccessStatusCode)
                    return;

                string mensaje =
                    "El servidor rechazó la operación de revisión.";

                if (!string.IsNullOrWhiteSpace(contenido))
                {
                    try
                    {
                        using System.Text.Json.JsonDocument documento =
                            System.Text.Json.JsonDocument.Parse(contenido);

                        if (documento.RootElement.TryGetProperty(
                                "message",
                                out System.Text.Json.JsonElement valor) &&
                            valor.ValueKind ==
                                System.Text.Json.JsonValueKind.String)
                        {
                            mensaje = valor.GetString() ?? mensaje;
                        }
                    }
                    catch (System.Text.Json.JsonException)
                    {
                    }
                }

                throw new InvalidOperationException(mensaje);
            }
        }

        private sealed class ApiEnvelopeRevision<T>
            where T : class
        {
            public bool Success { get; set; }
            public string Message { get; set; } = string.Empty;
            public T? Data { get; set; }
        }
    }
}
