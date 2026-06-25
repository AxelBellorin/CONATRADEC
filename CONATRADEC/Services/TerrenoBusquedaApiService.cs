using CONATRADEC.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace CONATRADEC.Services
{
    // ===========================================================
    // ========= SERVICIO: TerrenoBusquedaApiService =============
    // ===========================================================
    // Servicio usado para buscar terrenos mediante la nueva API:
    // GET api/terreno/buscar
    //
    // Parámetros usados desde la interfaz:
    // texto, paisId, departamentoId, municipioId, page, pageSize.
    //
    // Los demás parámetros de la API quedan disponibles para futuro:
    // codigoTerreno, nombrePropietario, identificacionPropietario,
    // direccion.
    // ===========================================================

    class TerrenoBusquedaApiService
    {
        private readonly HttpClient httpClient;

        private readonly UrlApiService urlApiService = new UrlApiService();

        private readonly JsonSerializerOptions jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public TerrenoBusquedaApiService()
        {
            httpClient = new HttpClient
            {
                BaseAddress = new Uri(urlApiService.BaseUrlApi)
            };
        }

        public async Task<ObservableCollection<TerrenoResponse>> BuscarTerrenosAsync(
            string? texto,
            int? paisId,
            int? departamentoId,
            int? municipioId,
            int page = 1,
            int pageSize = 50)
        {
            try
            {
                string endpoint = ConstruirEndpointBusqueda(
                    texto: texto,
                    codigoTerreno: null,
                    nombrePropietario: null,
                    identificacionPropietario: null,
                    direccion: null,
                    paisId: paisId,
                    departamentoId: departamentoId,
                    municipioId: municipioId,
                    page: page,
                    pageSize: pageSize
                );

                HttpResponseMessage response = await httpClient.GetAsync(endpoint);

                string jsonRespuesta = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    return new ObservableCollection<TerrenoResponse>();

                if (string.IsNullOrWhiteSpace(jsonRespuesta))
                    return new ObservableCollection<TerrenoResponse>();

                TerrenoBusquedaPaginadaResponse? resultado =
                    JsonSerializer.Deserialize<TerrenoBusquedaPaginadaResponse>(
                        jsonRespuesta,
                        jsonOptions
                    );

                if (resultado?.Data == null)
                    return new ObservableCollection<TerrenoResponse>();

                return new ObservableCollection<TerrenoResponse>(resultado.Data);
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
            AgregarParametroTexto(parametros, "codigoTerreno", codigoTerreno);
            AgregarParametroTexto(parametros, "nombrePropietario", nombrePropietario);
            AgregarParametroTexto(parametros, "identificacionPropietario", identificacionPropietario);
            AgregarParametroTexto(parametros, "direccion", direccion);

            AgregarParametroEntero(parametros, "paisId", paisId);
            AgregarParametroEntero(parametros, "departamentoId", departamentoId);
            AgregarParametroEntero(parametros, "municipioId", municipioId);

            parametros.Add($"page={page.ToString(CultureInfo.InvariantCulture)}");
            parametros.Add($"pageSize={pageSize.ToString(CultureInfo.InvariantCulture)}");

            return $"api/terreno/buscar?{string.Join("&", parametros)}";
        }

        private static void AgregarParametroTexto(List<string> parametros, string nombre, string? valor)
        {
            if (string.IsNullOrWhiteSpace(valor))
                return;

            parametros.Add($"{nombre}={Uri.EscapeDataString(valor.Trim())}");
        }

        private static void AgregarParametroEntero(List<string> parametros, string nombre, int? valor)
        {
            if (valor == null || valor <= 0)
                return;

            parametros.Add($"{nombre}={valor.Value.ToString(CultureInfo.InvariantCulture)}");
        }
    }
}