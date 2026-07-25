using CONATRADEC.Models;
using Microsoft.Maui.Devices;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace CONATRADEC.Services
{
    internal sealed class TerrenoBusquedaApiService
    {
        private readonly HttpClient httpClient;

        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
        {
            PropertyNameCaseInsensitive = true
        };

        public TerrenoBusquedaApiService()
            : this(ApiClientService.Client)
        {
        }

        public TerrenoBusquedaApiService(HttpClient httpClient)
        {
            this.httpClient = httpClient
                ?? throw new ArgumentNullException(nameof(httpClient));
        }

        /// <summary>
        /// Mantiene compatibilidad con las pantallas existentes, como
        /// NuevoAnalisisFormViewModel, que utilizan la búsqueda simplificada.
        /// Internamente aprovecha el nuevo endpoint paginado y los mismos
        /// controles de cancelación de la búsqueda avanzada.
        /// </summary>
        public async Task<ObservableCollection<TerrenoResponse>> BuscarTerrenosAsync(
            string? texto,
            int? paisId,
            int? departamentoId,
            int? municipioId,
            int page = 1,
            int pageSize = 50,
            CancellationToken cancellationToken = default)
        {
            int limiteDispositivo =
                DeviceInfo.Current.Platform == DevicePlatform.WinUI
                    ? 50
                    : 24;

            int tamanoPagina = Math.Clamp(
                pageSize,
                1,
                limiteDispositivo);

            ApiResult<TerrenoBusquedaPaginadaResponse> resultado =
                await BuscarAsync(
                    texto: texto,
                    codigoTerreno: null,
                    nombrePropietario: null,
                    identificacionPropietario: null,
                    direccion: null,
                    paisId: paisId,
                    departamentoId: departamentoId,
                    municipioId: municipioId,
                    fechaDesde: null,
                    fechaHasta: null,
                    extensionMinima: null,
                    extensionMaxima: null,
                    ordenarPor: "codigo",
                    descendente: false,
                    page: Math.Max(1, page),
                    pageSize: tamanoPagina,
                    cancellationToken: cancellationToken);

            if (!resultado.Success || resultado.Data?.Data == null)
                return new ObservableCollection<TerrenoResponse>();

            return new ObservableCollection<TerrenoResponse>(
                resultado.Data.Data);
        }

        public async Task<ApiResult<TerrenoBusquedaPaginadaResponse>> BuscarAsync(
            string? texto,
            string? codigoTerreno,
            string? nombrePropietario,
            string? identificacionPropietario,
            string? direccion,
            int? paisId,
            int? departamentoId,
            int? municipioId,
            DateOnly? fechaDesde,
            DateOnly? fechaHasta,
            decimal? extensionMinima,
            decimal? extensionMaxima,
            string? ordenarPor,
            bool descendente,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            try
            {
                string endpoint = ConstruirEndpoint(
                    texto,
                    codigoTerreno,
                    nombrePropietario,
                    identificacionPropietario,
                    direccion,
                    paisId,
                    departamentoId,
                    municipioId,
                    fechaDesde,
                    fechaHasta,
                    extensionMinima,
                    extensionMaxima,
                    ordenarPor,
                    descendente,
                    Math.Max(1, page),
                    Math.Clamp(pageSize, 1, 100));

                using HttpResponseMessage response = await httpClient.GetAsync(
                    endpoint,
                    HttpCompletionOption.ResponseHeadersRead,
                    cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    string mensaje = await ObtenerMensajeErrorAsync(
                        response,
                        "No fue posible consultar los terrenos.",
                        cancellationToken);

                    return ApiResult<TerrenoBusquedaPaginadaResponse>.Fail(
                        mensaje,
                        (int)response.StatusCode);
                }

                TerrenoBusquedaPaginadaResponse? resultado =
                    await response.Content.ReadFromJsonAsync<TerrenoBusquedaPaginadaResponse>(
                        JsonOptions,
                        cancellationToken);

                if (resultado == null)
                {
                    return ApiResult<TerrenoBusquedaPaginadaResponse>.Fail(
                        "El servidor respondió, pero la lista de terrenos no tiene el formato esperado.");
                }

                resultado.Data ??= new List<TerrenoResponse>();

                return ApiResult<TerrenoBusquedaPaginadaResponse>.Ok(resultado);
            }
            catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                return ApiResult<TerrenoBusquedaPaginadaResponse>.Fail(
                    "La consulta tardó demasiado. Verifique su conexión e intente nuevamente.");
            }
            catch (OperationCanceledException)
            {
                return ApiResult<TerrenoBusquedaPaginadaResponse>.Fail(
                    "La operación fue cancelada.");
            }
            catch (HttpRequestException)
            {
                return ApiResult<TerrenoBusquedaPaginadaResponse>.Fail(
                    "No fue posible conectarse con el servidor. Verifique su conexión.");
            }
            catch (JsonException)
            {
                return ApiResult<TerrenoBusquedaPaginadaResponse>.Fail(
                    "El servidor respondió, pero los datos de terrenos no tienen el formato esperado.");
            }
            catch
            {
                return ApiResult<TerrenoBusquedaPaginadaResponse>.Fail(
                    "Ocurrió un error inesperado al consultar los terrenos.");
            }
        }

        private static string ConstruirEndpoint(
            string? texto,
            string? codigoTerreno,
            string? nombrePropietario,
            string? identificacionPropietario,
            string? direccion,
            int? paisId,
            int? departamentoId,
            int? municipioId,
            DateOnly? fechaDesde,
            DateOnly? fechaHasta,
            decimal? extensionMinima,
            decimal? extensionMaxima,
            string? ordenarPor,
            bool descendente,
            int page,
            int pageSize)
        {
            var parametros = new List<string>();

            AgregarTexto(parametros, "texto", texto);
            AgregarTexto(parametros, "codigoTerreno", codigoTerreno);
            AgregarTexto(parametros, "nombrePropietario", nombrePropietario);
            AgregarTexto(parametros, "identificacionPropietario", identificacionPropietario);
            AgregarTexto(parametros, "direccion", direccion);

            AgregarEntero(parametros, "paisId", paisId);
            AgregarEntero(parametros, "departamentoId", departamentoId);
            AgregarEntero(parametros, "municipioId", municipioId);

            AgregarFecha(parametros, "fechaDesde", fechaDesde);
            AgregarFecha(parametros, "fechaHasta", fechaHasta);
            AgregarDecimal(parametros, "extensionMinima", extensionMinima);
            AgregarDecimal(parametros, "extensionMaxima", extensionMaxima);
            AgregarTexto(parametros, "ordenarPor", ordenarPor);

            parametros.Add($"descendente={descendente.ToString().ToLowerInvariant()}");
            parametros.Add($"page={page.ToString(CultureInfo.InvariantCulture)}");
            parametros.Add($"pageSize={pageSize.ToString(CultureInfo.InvariantCulture)}");

            return $"api/terreno/buscar?{string.Join("&", parametros)}";
        }

        private static void AgregarTexto(
            ICollection<string> parametros,
            string nombre,
            string? valor)
        {
            if (string.IsNullOrWhiteSpace(valor))
                return;

            parametros.Add(
                $"{nombre}={Uri.EscapeDataString(valor.Trim())}");
        }

        private static void AgregarEntero(
            ICollection<string> parametros,
            string nombre,
            int? valor)
        {
            if (!valor.HasValue || valor.Value <= 0)
                return;

            parametros.Add(
                $"{nombre}={valor.Value.ToString(CultureInfo.InvariantCulture)}");
        }

        private static void AgregarFecha(
            ICollection<string> parametros,
            string nombre,
            DateOnly? valor)
        {
            if (!valor.HasValue)
                return;

            parametros.Add(
                $"{nombre}={valor.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}");
        }

        private static void AgregarDecimal(
            ICollection<string> parametros,
            string nombre,
            decimal? valor)
        {
            if (!valor.HasValue)
                return;

            parametros.Add(
                $"{nombre}={valor.Value.ToString(CultureInfo.InvariantCulture)}");
        }

        private static async Task<string> ObtenerMensajeErrorAsync(
            HttpResponseMessage response,
            string mensajePredeterminado,
            CancellationToken cancellationToken)
        {
            try
            {
                string contenido = await response.Content.ReadAsStringAsync(cancellationToken);

                if (!string.IsNullOrWhiteSpace(contenido))
                {
                    using JsonDocument document = JsonDocument.Parse(contenido);
                    JsonElement root = document.RootElement;

                    foreach (string nombre in new[] { "mensaje", "message", "error" })
                    {
                        if (TryGetPropertyIgnoreCase(root, nombre, out JsonElement propiedad) &&
                            propiedad.ValueKind == JsonValueKind.String)
                        {
                            string? mensaje = propiedad.GetString();

                            if (!string.IsNullOrWhiteSpace(mensaje))
                                return mensaje;
                        }
                    }
                }
            }
            catch
            {
                // Se conserva el mensaje amigable predeterminado.
            }

            return response.StatusCode switch
            {
                HttpStatusCode.BadRequest =>
                    "Revise los filtros enviados e intente nuevamente.",
                HttpStatusCode.Unauthorized =>
                    "La sesión no es válida o ha expirado.",
                HttpStatusCode.Forbidden =>
                    "No tiene permisos para consultar los terrenos.",
                HttpStatusCode.ServiceUnavailable or HttpStatusCode.BadGateway or HttpStatusCode.GatewayTimeout =>
                    "El servidor no está disponible temporalmente.",
                _ => mensajePredeterminado
            };
        }

        private static bool TryGetPropertyIgnoreCase(
            JsonElement element,
            string propertyName,
            out JsonElement value)
        {
            if (element.ValueKind == JsonValueKind.Object)
            {
                foreach (JsonProperty property in element.EnumerateObject())
                {
                    if (string.Equals(
                            property.Name,
                            propertyName,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        value = property.Value;
                        return true;
                    }
                }
            }

            value = default;
            return false;
        }
    }
}