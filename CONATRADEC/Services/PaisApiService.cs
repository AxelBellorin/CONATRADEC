using CONATRADEC.Models;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.Net.Http.Json;

namespace CONATRADEC.Services
{
    class PaisApiService
    {
        private readonly HttpClient httpClient;

        public PaisApiService()
            : this(ApiClientService.Client)
        {
        }

        public PaisApiService(HttpClient httpClient)
        {
            this.httpClient = httpClient
                ?? throw new ArgumentNullException(nameof(httpClient));
        }

        public async Task<ObservableCollection<PaisResponse>> GetPaisAsync()
        {
            try
            {
                var response = await httpClient.GetFromJsonAsync<ObservableCollection<PaisResponse>>(
                    "api/pais");

                return response ?? new ObservableCollection<PaisResponse>();
            }
            catch
            {
                return new ObservableCollection<PaisResponse>();
            }
        }

        public async Task<bool> CreatePaisAsync(PaisRequest pais)
        {
            try
            {
                var response = await httpClient.PostAsJsonAsync(
                    "api/pais/crearPais",
                    pais);

                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> UpdatePaisAsync(PaisRequest pais)
        {
            try
            {
                var response = await httpClient.PutAsJsonAsync(
                    $"api/pais/actualizarPais/{pais.PaisId}",
                    pais);

                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> DeletePaisAsync(PaisRequest pais)
        {
            try
            {
                var response = await httpClient.DeleteAsync(
                    $"api/pais/eliminarPais/{pais.PaisId}");

                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
    }
}
