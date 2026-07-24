using CONATRADEC.Models;
using Microsoft.Maui.Devices;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;

namespace CONATRADEC.Services
{
    internal class TerrenoBusquedaApiService
    {
        private readonly HttpClient httpClient;

        private static readonly JsonSerializerOptions JsonOptions = new()
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

        public async Task<ObservableCollection<TerrenoResponse>>
            BuscarTerrenosAsync(
                string? texto,
                int? paisId,
                int? departamentoId,
                int? municipioId,
                int page = 1,
                int pageSize = 50,
                CancellationToken cancellationToken = default)
        {
            try
            {
                int limiteDispositivo =
                    DeviceInfo.Current.Platform == DevicePlatform.WinUI
                        ? 50
                        : 24;

                int tamanoPagina = Math.Clamp(
                    pageSize,
                    1,
                    limiteDispositivo);

                string endpoint = ConstruirEndpointBusqueda(
                    texto: texto,
                    codigoTerreno: null,
                    nombrePropietario: null,
                    identificacionPropietario: null,
                    direccion: null,
                    paisId: paisId,
                    departamentoId: departamentoId,
                    municipioId: municipioId,
                    page: Math.Max(1, page),
                    pageSize: tamanoPagina);

                using HttpResponseMessage response =
                    await httpClient.GetAsync(
                        endpoint,
                        HttpCompletionOption.ResponseHeadersRead,
                        cancellationToken);

                if (!response.IsSuccessStatusCode)
                    return new ObservableCollection<TerrenoResponse>();

                TerrenoBusquedaPaginadaResponse? resultado =
                    await response.Content.ReadFromJsonAsync<
                        TerrenoBusquedaPaginadaResponse>(
                            JsonOptions,
                            cancellationToken);

                return new ObservableCollection<TerrenoResponse>(
                    resultado?.Data ??
                    Enumerable.Empty<TerrenoResponse>());
            }
            catch (OperationCanceledException)
            {
                return new ObservableCollection<TerrenoResponse>();
            }
            catch
            {
                return new ObservableCollection<TerrenoResponse>();
            }
        }

        private static string ConstruirEndpointBusqueda(
            string? texto,
            string? codigoTerreno,
            string? nombrePropietario,
            string? identificacionPropietario,
            string? direccion,
            int? paisId,
            int? departamentoId,
            int? municipioId,
            int page,
            int pageSize)
        {
            var parametros = new List<string>();

            AgregarParametroTexto(parametros, "texto", texto);
            AgregarParametroTexto(
                parametros,
                "codigoTerreno",
                codigoTerreno);
            AgregarParametroTexto(
                parametros,
                "nombrePropietario",
                nombrePropietario);
            AgregarParametroTexto(
                parametros,
                "identificacionPropietario",
                identificacionPropietario);
            AgregarParametroTexto(
                parametros,
                "direccion",
                direccion);

            AgregarParametroEntero(parametros, "paisId", paisId);
            AgregarParametroEntero(
                parametros,
                "departamentoId",
                departamentoId);
            AgregarParametroEntero(
                parametros,
                "municipioId",
                municipioId);

            parametros.Add(
                $"page={page.ToString(CultureInfo.InvariantCulture)}");
            parametros.Add(
                $"pageSize={pageSize.ToString(CultureInfo.InvariantCulture)}");

            return $"api/terreno/buscar?{string.Join("&", parametros)}";
        }

        private static void AgregarParametroTexto(
            ICollection<string> parametros,
            string nombre,
            string? valor)
        {
            if (string.IsNullOrWhiteSpace(valor))
                return;

            parametros.Add(
                $"{nombre}={Uri.EscapeDataString(valor.Trim())}");
        }

        private static void AgregarParametroEntero(
            ICollection<string> parametros,
            string nombre,
            int? valor)
        {
            if (!valor.HasValue || valor.Value <= 0)
                return;

            parametros.Add(
                $"{nombre}=" +
                valor.Value.ToString(CultureInfo.InvariantCulture));
        }
    }
}
