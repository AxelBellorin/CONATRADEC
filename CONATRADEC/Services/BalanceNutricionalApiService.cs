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

        private readonly JsonSerializerOptions jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public BalanceNutricionalApiService()
            : this(ApiClientService.Client)
        {
        }

        public BalanceNutricionalApiService(HttpClient httpClient)
        {
            this.httpClient = httpClient
                ?? throw new ArgumentNullException(nameof(httpClient));
        }

        public async Task<BalanceNutricionalResponse?> CalcularAsync(BalanceNutricionalRequest request)
        {
            try
            {
                HttpResponseMessage response = await httpClient.PostAsJsonAsync(
                    "api/formula-nutricional/calcular",
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
                    await Task.Run(() =>
                        JsonSerializer.Deserialize<
                            BalanceNutricionalResponse>(
                                jsonRespuesta,
                                jsonOptions));

                if (resultado == null)
                {
                    return new BalanceNutricionalResponse
                    {
                        Success = false,
                        Message = "La API respondió, pero no se pudo interpretar el resultado del balance de fórmula."
                    };
                }

                resultado.Success = true;
                resultado.Message = "Balance de fórmula calculado correctamente.";

                return resultado;
            }
            catch (Exception ex)
            {
                return new BalanceNutricionalResponse
                {
                    Success = false,
                    Message = $"No se pudo conectar con la API de fórmula nutricional: {ex.Message}"
                };
            }
        }
    }
}
