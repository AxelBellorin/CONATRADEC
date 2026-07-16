using CONATRADEC.Models;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.Net.Http.Json;

namespace CONATRADEC.Services
{
    class FertilizacionMixtaApiService
    {
        private readonly HttpClient httpClient;

        public FertilizacionMixtaApiService()
            : this(ApiClientService.Client)
        {
        }

        public FertilizacionMixtaApiService(HttpClient httpClient)
        {
            this.httpClient = httpClient
                ?? throw new ArgumentNullException(nameof(httpClient));
        }

        public async Task<ObservableCollection<FuenteNutrienteFertilizacionMixtaResponse>>
            ListarFuentesFertilizacionMixtaAsync()
        {
            try
            {
                var response = await httpClient.GetFromJsonAsync<ObservableCollection<FuenteNutrienteFertilizacionMixtaResponse>>(
                    "api/fuente-nutriente/listar-fertilizacion-mixta");

                return response
                    ?? new ObservableCollection<FuenteNutrienteFertilizacionMixtaResponse>();
            }
            catch
            {
                return new ObservableCollection<FuenteNutrienteFertilizacionMixtaResponse>();
            }
        }

        public async Task<FertilizacionMixtaCalculoResponse?> CalcularAsync(
            FertilizacionMixtaCalcularRequest request)
        {
            try
            {
                var response = await httpClient.PostAsJsonAsync(
                    "api/fertilizacion-mixta/calcular",
                    request);

                if (!response.IsSuccessStatusCode)
                {
                    string errorApi = await response.Content.ReadAsStringAsync();

                    return new FertilizacionMixtaCalculoResponse
                    {
                        Success = false,
                        Message = $"Error API ({(int)response.StatusCode}): {errorApi}"
                    };
                }

                FertilizacionMixtaCalculoResponse? resultado =
                    await response.Content.ReadFromJsonAsync<FertilizacionMixtaCalculoResponse>();

                if (resultado == null)
                {
                    return new FertilizacionMixtaCalculoResponse
                    {
                        Success = false,
                        Message = "La API respondió, pero no se pudo interpretar la respuesta."
                    };
                }

                resultado.Success = true;
                resultado.Message = "Cálculo realizado correctamente.";

                return resultado;
            }
            catch (Exception ex)
            {
                return new FertilizacionMixtaCalculoResponse
                {
                    Success = false,
                    Message = $"No se pudo conectar con la API: {ex.Message}"
                };
            }
        }
    }
}
