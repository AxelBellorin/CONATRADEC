using CONATRADEC.Models;
using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

namespace CONATRADEC.Services
{
    public class BalanceNutricionalApiService
    {
        private readonly HttpClient httpClient;

        private readonly UrlApiService urlApiService = new UrlApiService();

        private readonly JsonSerializerOptions jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public BalanceNutricionalApiService()
        {
            httpClient = new HttpClient
            {
                BaseAddress = new Uri(urlApiService.BaseUrlApi)
            };
        }

        public async Task<BalanceNutricionalResponse?> CalcularAsync(BalanceNutricionalRequest request)
        {
            try
            {
                HttpResponseMessage response = await httpClient.PostAsJsonAsync(
                    "api/balance-nutricional/calcular",
                    request
                );

                string jsonRespuesta = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return new BalanceNutricionalResponse
                    {
                        Success = false,
                        Message = $"Error API ({(int)response.StatusCode}): {jsonRespuesta}"
                    };
                }

                BalanceNutricionalResponse? resultado =
                    JsonSerializer.Deserialize<BalanceNutricionalResponse>(
                        jsonRespuesta,
                        jsonOptions
                    );

                if (resultado == null)
                {
                    return new BalanceNutricionalResponse
                    {
                        Success = false,
                        Message = "La API respondió, pero no se pudo interpretar el resultado del balance nutricional."
                    };
                }

                resultado.Success = true;
                return resultado;
            }
            catch (Exception ex)
            {
                return new BalanceNutricionalResponse
                {
                    Success = false,
                    Message = $"No se pudo conectar con la API de balance nutricional: {ex.Message}"
                };
            }
        }
    }
}