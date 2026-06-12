using CONATRADEC.Models;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace CONATRADEC.Services
{
    class TerrenoApiService
    {
        private readonly HttpClient httpClient;
        private readonly UrlApiService urlApiService = new UrlApiService();

        public TerrenoApiService()
        {
            httpClient = new HttpClient { BaseAddress = new Uri(urlApiService.BaseUrlApi) };
        }

        public async Task<ObservableCollection<TerrenoResponse>> GetTerrenosAsync()
        {
            try
            {
                var response = await httpClient.GetFromJsonAsync<ObservableCollection<TerrenoResponse>>(
                    "api/terreno/listar");

                if (response == null)
                    return new ObservableCollection<TerrenoResponse>();

                return new ObservableCollection<TerrenoResponse>(
                    response.Where(t => t.Activo != false)
                );
            }
            catch
            {
                return new ObservableCollection<TerrenoResponse>();
            }
        }

        public async Task<bool> CreateTerrenoAsync(TerrenoRequest terreno)
        {
            try
            {
                var response = await httpClient.PostAsJsonAsync("api/terreno/crear", terreno);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> UpdateTerrenoAsync(TerrenoRequest terreno)
        {
            try
            {
                var response = await httpClient.PutAsJsonAsync(
                    $"api/terreno/editar/{terreno.TerrenoId}",
                    terreno);

                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> DeleteTerrenoAsync(TerrenoRequest terreno)
        {
            try
            {
                var response = await httpClient.DeleteAsync(
                    $"api/terreno/eliminar/{terreno.TerrenoId}");

                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
    }
}