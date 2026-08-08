using CONATRADEC.Models;
using System.Globalization;
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

            /*
             * El paquete sin conexión ya descarga todos los terrenos. En vez
             * de crear una segunda copia específica para fitosanitaria, se usa
             * el mismo endpoint local que utiliza el resto de CONATRADEC y se
             * transforma el resultado al modelo visual del módulo.
             */
            if (ModoSesionService.EsOffline)
            {
                return await BuscarLocalAsync(filtro, cancellationToken);
            }

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

        private static async Task<TerrenoBusquedaIAPagina> BuscarLocalAsync(
            TerrenoBusquedaIAFiltro filtro,
            CancellationToken cancellationToken)
        {
            var api = new TerrenoBusquedaApiService();

            string texto = string.Join(
                " ",
                new[]
                {
                    filtro.Texto,
                    filtro.Ubicacion
                }.Where(value => !string.IsNullOrWhiteSpace(value)));

            ApiResult<TerrenoBusquedaPaginadaResponse> resultado =
                await api.BuscarAsync(
                    texto: texto,
                    codigoTerreno: filtro.Codigo,
                    nombrePropietario: filtro.Propietario,
                    identificacionPropietario:
                        filtro.IdentificacionPropietario,
                    direccion: filtro.Direccion,
                    paisId: null,
                    departamentoId: null,
                    municipioId: null,
                    fechaDesde: null,
                    fechaHasta: null,
                    extensionMinima: filtro.ExtensionMinima,
                    extensionMaxima: filtro.ExtensionMaxima,
                    ordenarPor: "codigo",
                    descendente: false,
                    page: Math.Max(1, filtro.Pagina),
                    pageSize: Math.Clamp(filtro.TamanoPagina, 5, 50),
                    cancellationToken: cancellationToken);

            if (!resultado.Success || resultado.Data == null)
            {
                throw new InvalidOperationException(
                    string.IsNullOrWhiteSpace(resultado.Message)
                        ? "No fue posible consultar los terrenos descargados."
                        : resultado.Message);
            }

            TerrenoBusquedaPaginadaResponse pagina = resultado.Data;

            return new TerrenoBusquedaIAPagina
            {
                Pagina = pagina.Page,
                TamanoPagina = pagina.PageSize,
                Total = pagina.Total,
                TotalPaginas = pagina.TotalPages,
                Items = (pagina.Data ?? [])
                    .Where(item => item.TerrenoId is > 0)
                    .Select(Mapear)
                    .ToList()
            };
        }

        private static TerrenoBusquedaIAItem Mapear(TerrenoResponse item) =>
            new()
            {
                TerrenoId = item.TerrenoId ?? 0,
                CodigoTerreno = item.CodigoTerreno?.Trim() ?? string.Empty,
                DireccionTerreno = item.DireccionTerreno?.Trim() ?? string.Empty,
                ExtensionManzanaTerreno = item.ExtensionManzanaTerreno ?? 0m,
                FechaIngresoTerreno = item.FechaIngresoTerreno.HasValue
                    ? item.FechaIngresoTerreno.Value.ToDateTime(TimeOnly.MinValue)
                    : DateTime.MinValue,
                CantidadPlantasTerreno = item.CantidadPlantasTerreno ?? 0,
                CantidadQuintalesOro = item.CantidadQuintalesOro ?? 0m,
                Latitud = Convert.ToDecimal(item.Latitud ?? 0d),
                Longitud = Convert.ToDecimal(item.Longitud ?? 0d),
                PropietarioId = item.PropietarioId,
                IdentificacionPropietario =
                    item.Propietario?.Identificacion?.Trim() ?? string.Empty,
                Propietario =
                    item.Propietario?.NombreCompleto?.Trim() ?? string.Empty,
                Pais = item.Ubicacion?.NombrePais?.Trim() ?? string.Empty,
                Departamento =
                    item.Ubicacion?.NombreDepartamento?.Trim() ?? string.Empty,
                Municipio =
                    item.Ubicacion?.NombreMunicipio?.Trim() ?? string.Empty
            };

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
