using CONATRADEC.Models;
using System;
using System.Collections.ObjectModel;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace CONATRADEC.Services
{
    class ElementoQuimicoApiService
    {
        private readonly HttpClient httpClient;
        private readonly UrlApiService urlApiService = new UrlApiService();

        public ElementoQuimicoApiService()
        {
            httpClient = new HttpClient { BaseAddress = new Uri(urlApiService.BaseUrlApi) };
        }

        // LISTAR
        public async Task<ObservableCollection<ElementoQuimicoResponse>> GetElementoQuimicoAsync()
        {
            try
            {
                // Ajusta la ruta según tu controlador real
                var response = await httpClient.GetFromJsonAsync<ObservableCollection<ElementoQuimicoResponse>>(
                    "api/elemento-quimico/listar");

                return response ?? new ObservableCollection<ElementoQuimicoResponse>();
            }
            catch
            {
                return new ObservableCollection<ElementoQuimicoResponse>();
            }
        }

        // CREAR
        public async Task<bool> CreateElementoQuimicoAsync(ElementoQuimicoRequest elemento)
        {
            try
            {
                var response = await httpClient.PostAsJsonAsync("api/elemento-quimico/crear", elemento);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        // ELIMINAR
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

        // ACTUALIZAR
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
