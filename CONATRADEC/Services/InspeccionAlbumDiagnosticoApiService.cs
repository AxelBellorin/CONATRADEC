using CONATRADEC.Models;
using System.Net.Http.Json;
using System.Text.Json;

namespace CONATRADEC.Services
{
    /// <summary>
    /// Cliente aditivo del flujo de clasificación/publicación por diagnóstico.
    /// Las rutas históricas por fotografía permanecen disponibles para clientes
    /// anteriores; la aplicación actual utiliza estas operaciones para no
    /// colapsar varios diagnósticos en una sola clasificación.
    /// </summary>
    public sealed class InspeccionAlbumDiagnosticoApiService
    {
        private static readonly JsonSerializerOptions JsonOptions =
            new(JsonSerializerDefaults.Web)
            {
                PropertyNameCaseInsensitive = true
            };

        private readonly HttpClient client;

        public InspeccionAlbumDiagnosticoApiService()
            : this(ApiClientService.Client)
        {
        }

        public InspeccionAlbumDiagnosticoApiService(HttpClient client)
        {
            this.client = client ?? throw new ArgumentNullException(nameof(client));
        }

        public Task<List<ClasificacionDiagnosticoAlbumItem>>
            ObtenerEstadoAsync(
                int inspeccionId,
                int fotografiaId,
                CancellationToken cancellationToken = default) =>
            EnviarAsync<List<ClasificacionDiagnosticoAlbumItem>>(
                new HttpRequestMessage(
                    HttpMethod.Get,
                    $"api/inspecciones-fitosanitarias/{inspeccionId}/" +
                    $"fotografias/{fotografiaId}/publicaciones-diagnosticos"),
                cancellationToken);

        public Task<List<ClasificacionDiagnosticoAlbumItem>>
            ResolverClasificacionAsync(
                int inspeccionId,
                int fotografiaId,
                ResolverClasificacionDiagnosticoAlbumRequest request,
                CancellationToken cancellationToken = default) =>
            EnviarAsync<List<ClasificacionDiagnosticoAlbumItem>>(
                new HttpRequestMessage(
                    HttpMethod.Post,
                    $"api/inspecciones-fitosanitarias/{inspeccionId}/" +
                    $"fotografias/{fotografiaId}/" +
                    "clasificaciones-diagnosticos/resolver")
                {
                    Content = JsonContent.Create(request)
                },
                cancellationToken);

        public Task<List<ClasificacionDiagnosticoAlbumItem>>
            SincronizarPublicacionesAsync(
                int inspeccionId,
                int fotografiaId,
                SincronizarPublicacionesDiagnosticosAlbumRequest request,
                CancellationToken cancellationToken = default) =>
            EnviarAsync<List<ClasificacionDiagnosticoAlbumItem>>(
                new HttpRequestMessage(
                    HttpMethod.Post,
                    $"api/inspecciones-fitosanitarias/{inspeccionId}/" +
                    $"fotografias/{fotografiaId}/" +
                    "publicaciones-diagnosticos/sincronizar")
                {
                    Content = JsonContent.Create(request)
                },
                cancellationToken);

        private async Task<T> EnviarAsync<T>(
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

                ApiEnvelope<T>? envelope = null;
                if (!string.IsNullOrWhiteSpace(contenido))
                {
                    try
                    {
                        envelope = JsonSerializer.Deserialize<ApiEnvelope<T>>(
                            contenido,
                            JsonOptions);
                    }
                    catch (JsonException)
                    {
                    }
                }

                if (!response.IsSuccessStatusCode)
                {
                    throw new InvalidOperationException(
                        string.IsNullOrWhiteSpace(envelope?.Message)
                            ? "No fue posible actualizar la clasificación del diagnóstico."
                            : envelope.Message);
                }

                if (envelope != null && envelope.Data != null)
                    return envelope.Data;

                throw new InvalidOperationException(
                    "El servidor devolvió información incompleta para la clasificación fitosanitaria.");
            }
        }
    }
}
