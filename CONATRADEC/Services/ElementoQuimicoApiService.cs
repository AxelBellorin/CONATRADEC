using CONATRADEC.Models;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.Net.Http.Json;

namespace CONATRADEC.Services
{
    class ElementoQuimicoApiService
    {
        private readonly HttpClient httpClient;

        public ElementoQuimicoApiService()
            : this(ApiClientService.Client)
        {
        }

        public ElementoQuimicoApiService(HttpClient httpClient)
        {
            this.httpClient = httpClient
                ?? throw new ArgumentNullException(nameof(httpClient));
        }

        public async Task<ObservableCollection<ElementoQuimicoResponse>> GetElementoQuimicoAsync()
        {
            try
            {
                var response = await httpClient.GetFromJsonAsync<ObservableCollection<ElementoQuimicoResponse>>(
                    "api/elemento-quimico/listar");

                return response ?? new ObservableCollection<ElementoQuimicoResponse>();
            }
            catch
            {
                return new ObservableCollection<ElementoQuimicoResponse>();
            }
        }

        public async Task<bool> CreateElementoQuimicoAsync(ElementoQuimicoRequest elemento)
        {
            try
            {
                var response = await httpClient.PostAsJsonAsync(
                    "api/elemento-quimico/crear",
                    elemento);

                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> DeleteElementoQuimicoAsync(ElementoQuimicoRequest elemento)
        {
            try
            {
                var response = await httpClient.DeleteAsync(
                    $"api/elemento-quimico/eliminar/{elemento.ElementoQuimicosId}");

                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> UpdateElementoQuimicoAsync(ElementoQuimicoRequest elemento)
        {
            try
            {
                var response = await httpClient.PutAsJsonAsync(
                    $"api/elemento-quimico/editar/{elemento.ElementoQuimicosId}",
                    elemento);

                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
    }
}
