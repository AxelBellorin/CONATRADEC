using CONATRADEC.Models;
using System.Text.Json;

namespace CONATRADEC.Services
{
    /// <summary>
    /// Carga en una sola llamada las capas de señalización de las fotografías
    /// fitosanitarias publicadas dentro de una subcategoría del Álbum Botánico.
    /// </summary>
    public sealed class AlbumFitosanitarioSenalizacionApiService
    {
        private static readonly JsonSerializerOptions JsonOptions =
            new(JsonSerializerDefaults.Web)
            {
                PropertyNameCaseInsensitive = true
            };

        private readonly HttpClient client;

        public AlbumFitosanitarioSenalizacionApiService()
            : this(ApiClientService.Client)
        {
        }

        public AlbumFitosanitarioSenalizacionApiService(HttpClient client)
        {
            this.client = client ?? throw new ArgumentNullException(nameof(client));
        }

        public async Task<List<AlbumFotoSenalizacionResponse>> ObtenerAsync(
            int albumBotanicoCafeId,
            CancellationToken cancellationToken = default)
        {
            if (albumBotanicoCafeId <= 0)
                return [];

            SesionInactividadService.Instance.RegistrarActividad();

            using HttpResponseMessage response = await client.GetAsync(
                $"api/album-botanico/{albumBotanicoCafeId}/" +
                "senalizaciones-fitosanitarias",
                cancellationToken);

            string contenido = await response.Content.ReadAsStringAsync(
                cancellationToken);

            ApiEnvelope<List<AlbumFotoSenalizacionResponse>>? envelope = null;
            if (!string.IsNullOrWhiteSpace(contenido))
            {
                try
                {
                    envelope = JsonSerializer.Deserialize<
                        ApiEnvelope<List<AlbumFotoSenalizacionResponse>>>(
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
                        ? "No fue posible cargar la señalización fitosanitaria del álbum."
                        : envelope.Message);
            }

            return envelope?.Data ?? [];
        }
    }
}
