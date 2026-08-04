using CONATRADEC.Models;
using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;

namespace CONATRADEC.Services
{
    public sealed class TerrenoBusquedaIAApiService
    {
        private static readonly JsonSerializerOptions JsonOptions =
            new(JsonSerializerDefaults.Web)
            {
                PropertyNameCaseInsensitive = true
            };

        private readonly HttpClient client;

        public TerrenoBusquedaIAApiService()
            : this(ApiClientService.Client)
        {
        }

        public TerrenoBusquedaIAApiService(HttpClient client)
        {
            this.client = client ??
                throw new ArgumentNullException(nameof(client));
        }

        public async Task<TerrenoBusquedaIAPagina> BuscarAsync(
            TerrenoBusquedaIAFiltro filtro,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(filtro);

            var parametros = new List<string>
            {
                $"pagina={Math.Max(1, filtro.Pagina)}",
                $"tamanoPagina={Math.Clamp(filtro.TamanoPagina, 5, 50)}"
            };

            Agregar(parametros, "texto", filtro.Texto);
            Agregar(parametros, "codigo", filtro.Codigo);
            Agregar(parametros, "propietario", filtro.Propietario);
            Agregar(
                parametros,
                "identificacionPropietario",
                filtro.IdentificacionPropietario);
            Agregar(parametros, "ubicacion", filtro.Ubicacion);
            Agregar(parametros, "direccion", filtro.Direccion);

            if (filtro.ExtensionMinima.HasValue)
            {
                parametros.Add(
                    "extensionMinima=" +
                    filtro.ExtensionMinima.Value.ToString(
                        CultureInfo.InvariantCulture));
            }

            if (filtro.ExtensionMaxima.HasValue)
            {
                parametros.Add(
                    "extensionMaxima=" +
                    filtro.ExtensionMaxima.Value.ToString(
                        CultureInfo.InvariantCulture));
            }

            string ruta =
                "api/diagnostico-ia/terrenos/buscar?" +
                string.Join("&", parametros);

            using HttpResponseMessage response = await client.GetAsync(
                ruta,
                cancellationToken);

            string json = await response.Content.ReadAsStringAsync(
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(
                    ApiErrorMessageParser.Parse(
                        response.StatusCode,
                        json,
                        "No fue posible buscar los terrenos."));
            }

            DiagnosticoIAApiEnvelope<TerrenoBusquedaIAPagina>? envelope =
                JsonSerializer.Deserialize<
                    DiagnosticoIAApiEnvelope<TerrenoBusquedaIAPagina>>(
                        json,
                        JsonOptions);

            return envelope?.Data ?? new TerrenoBusquedaIAPagina();
        }

        private static void Agregar(
            ICollection<string> parametros,
            string nombre,
            string? valor)
        {
            if (string.IsNullOrWhiteSpace(valor))
                return;

            parametros.Add(
                $"{nombre}={Uri.EscapeDataString(valor.Trim())}");
        }
    }
}
