using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using CONATRADEC.Models;

namespace CONATRADEC.Services
{
    public class AnalisisSueloApiService
    {
        private const string EndpointCalcular = "api/analisis-suelo/calcular";
        private const string EndpointGuardarCalculo = "api/analisis-suelo/guardar-calculo";
        private const string EndpointTipoCultivoListar = "api/analisis-suelo/tipo-cultivo/listar";

        private readonly HttpClient httpClient;

        private readonly JsonSerializerOptions jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public AnalisisSueloApiService()
            : this(ApiClientService.Client)
        {
        }

        public AnalisisSueloApiService(HttpClient httpClient)
        {
            this.httpClient = httpClient
                ?? throw new ArgumentNullException(nameof(httpClient));
        }

        public async Task<AnalisisSueloCalculoResponse?> CalcularAsync(AnalisisSueloCalcularRequest request)
        {
            return await PostAnalisisSueloAsync(EndpointCalcular, request);
        }

        public async Task<AnalisisSueloCalculoResponse?> GuardarCalculoAsync(AnalisisSueloGuardarCalculoRequest request)
        {
            return await PostAnalisisSueloAsync(EndpointGuardarCalculo, request);
        }

        public async Task<ObservableCollection<TipoCultivoResponse>> ListarTiposCultivoAsync()
        {
            try
            {
                HttpResponseMessage response = await httpClient.GetAsync(EndpointTipoCultivoListar);

                string jsonRespuesta = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    return new ObservableCollection<TipoCultivoResponse>();

                if (string.IsNullOrWhiteSpace(jsonRespuesta))
                    return new ObservableCollection<TipoCultivoResponse>();

                string jsonTrim = jsonRespuesta.Trim();

                if (jsonTrim.StartsWith("["))
                {
                    ObservableCollection<TipoCultivoResponse>? listaDirecta =
                        JsonSerializer.Deserialize<ObservableCollection<TipoCultivoResponse>>(
                            jsonRespuesta,
                            jsonOptions
                        );

                    return listaDirecta ?? new ObservableCollection<TipoCultivoResponse>();
                }

                ApiListaResponse<TipoCultivoResponse>? respuesta =
                    JsonSerializer.Deserialize<ApiListaResponse<TipoCultivoResponse>>(
                        jsonRespuesta,
                        jsonOptions
                    );

                if (respuesta?.Data == null)
                    return new ObservableCollection<TipoCultivoResponse>();

                return new ObservableCollection<TipoCultivoResponse>(respuesta.Data);
            }
            catch
            {
                return new ObservableCollection<TipoCultivoResponse>();
            }
        }

        private async Task<AnalisisSueloCalculoResponse?> PostAnalisisSueloAsync<TRequest>(
            string endpoint,
            TRequest request)
        {
            try
            {
                string jsonRequest = JsonSerializer.Serialize(request, jsonOptions);

                Debug.WriteLine($"========== REQUEST API: {endpoint} ==========");
                Debug.WriteLine(jsonRequest);

                using StringContent content = new StringContent(
                    jsonRequest,
                    Encoding.UTF8,
                    "application/json"
                );

                HttpResponseMessage response = await httpClient.PostAsync(endpoint, content);

                string jsonRespuesta = await response.Content.ReadAsStringAsync();

                Debug.WriteLine($"========== RESPONSE API: {endpoint} ({(int)response.StatusCode}) ==========");
                Debug.WriteLine(jsonRespuesta);

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

        private class ApiListaResponse<T>
        {
            public bool Success { get; set; }

            public string? Message { get; set; }

            public List<T>? Data { get; set; }
        }
    }
}
