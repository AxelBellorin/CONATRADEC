using CONATRADEC.Models;
using System;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

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
                var response =
                    await httpClient.GetFromJsonAsync<ObservableCollection<FuenteNutrienteResponse>>(
                        "api/fuente-nutriente/listar"
                    );

                return response ?? new ObservableCollection<FuenteNutrienteResponse>();
            }
            catch
            {
                return new ObservableCollection<FuenteNutrienteResponse>();
            }
        }
    }
}