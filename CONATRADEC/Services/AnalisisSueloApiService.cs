using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using CONATRADEC.Models;

namespace CONATRADEC.Services
{
    public class AnalisisSueloApiService
    {
        private const string EndpointCalcular = "api/analisis-suelo/calcular";
        private const string EndpointGuardarCalculo = "api/analisis-suelo/guardar-calculo";

        private readonly HttpClient httpClient;

        private readonly UrlApiService urlApiService = new UrlApiService();

        private readonly JsonSerializerOptions jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public AnalisisSueloApiService()
        {
            httpClient = new HttpClient
            {
                BaseAddress = new Uri(urlApiService.BaseUrlApi)
            };
        }

        public async Task<AnalisisSueloCalculoResponse?> CalcularAsync(AnalisisSueloCalcularRequest request)
        {
            return await PostAnalisisSueloAsync(EndpointCalcular, request);
        }

        public async Task<AnalisisSueloCalculoResponse?> GuardarCalculoAsync(AnalisisSueloGuardarCalculoRequest request)
        {
            return await PostAnalisisSueloAsync(EndpointGuardarCalculo, request);
        }

        private async Task<AnalisisSueloCalculoResponse?> PostAnalisisSueloAsync<TRequest>(
            string endpoint,
            TRequest request)
        {
            try
            {
                HttpResponseMessage response = await httpClient.PostAsJsonAsync(
                    endpoint,
                    request,
                    jsonOptions
                );

                string jsonRespuesta = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return new AnalisisSueloCalculoResponse
                    {
                        Success = false,
                        Message = $"Error API ({(int)response.StatusCode}): {jsonRespuesta}"
                    };
                }

                AnalisisSueloCalculoResponse? data =
                    JsonSerializer.Deserialize<AnalisisSueloCalculoResponse>(
                        jsonRespuesta,
                        jsonOptions
                    );

                if (data == null)
                {
                    return new AnalisisSueloCalculoResponse
                    {
                        Success = false,
                        Message = "La API respondió, pero no se pudo interpretar la respuesta."
                    };
                }

                return data;
            }
            catch (Exception ex)
            {
                return new AnalisisSueloCalculoResponse
                {
                    Success = false,
                    Message = $"No se pudo conectar con la API: {ex.Message}"
                };
            }
        }
    }
}