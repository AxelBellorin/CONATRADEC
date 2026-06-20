using CONATRADEC.Models;
using System.Collections.ObjectModel;
using System.Net.Http.Json;

namespace CONATRADEC.Services
{
    public class FuenteNutrienteApiService
    {
        private readonly HttpClient httpClient;
        private readonly UrlApiService urlApiService = new UrlApiService();

        public FuenteNutrienteApiService()
        {
            httpClient = new HttpClient
            {
                BaseAddress = new Uri(urlApiService.BaseUrlApi)
            };
        }

        public async Task<ObservableCollection<FuenteNutrienteResponse>> GetFuenteNutrienteAsync()
        {
            try
            {
                var response = await httpClient.GetFromJsonAsync<ObservableCollection<FuenteNutrienteResponse>>(
                    "api/fuente-nutriente/listar");

                return response ?? new ObservableCollection<FuenteNutrienteResponse>();
            }
            catch
            {
                return new ObservableCollection<FuenteNutrienteResponse>();
            }
        }

        public async Task<ObservableCollection<FuenteNutrienteAporteTablaResponse>> GetAportesTablaAsync()
        {
            try
            {
                var response = await httpClient.GetFromJsonAsync<ObservableCollection<FuenteNutrienteAporteTablaResponse>>(
                    "api/fuente-nutriente/aportes-tabla");

                return response ?? new ObservableCollection<FuenteNutrienteAporteTablaResponse>();
            }
            catch
            {
                return new ObservableCollection<FuenteNutrienteAporteTablaResponse>();
            }
        }

        public async Task<bool> CreateFuenteNutrienteAsync(FuenteNutrienteRequest fuente)
        {
            try
            {
                var response = await httpClient.PostAsJsonAsync(
                    "api/fuente-nutriente/crear-con-elementos",
                    fuente);

                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> UpdateFuenteNutrienteAsync(FuenteNutrienteRequest fuente)
        {
            try
            {
                if (!fuente.FuenteNutrientesId.HasValue)
                    return false;

                var response = await httpClient.PutAsJsonAsync(
                    $"api/fuente-nutriente/editar-con-elementos/{fuente.FuenteNutrientesId.Value}",
                    fuente);

                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> DeleteFuenteNutrienteAsync(FuenteNutrienteRequest fuente)
        {
            try
            {
                if (!fuente.FuenteNutrientesId.HasValue)
                    return false;

                var response = await httpClient.DeleteAsync(
                    $"api/fuente-nutriente/eliminar/{fuente.FuenteNutrientesId.Value}");

                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
    }
}