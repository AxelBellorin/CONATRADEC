using CONATRADEC.Models;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.Net.Http.Json;

namespace CONATRADEC.Services
{
    class UnidadMedidaApiService
    {
        private readonly HttpClient httpClient;

        public UnidadMedidaApiService()
            : this(ApiClientService.Client)
        {
        }

        public UnidadMedidaApiService(HttpClient httpClient)
        {
            this.httpClient = httpClient
                ?? throw new ArgumentNullException(nameof(httpClient));
        }

        public async Task<ObservableCollection<UnidadMedidaResponse>> GetUnidadMedidaAsync()
        {
            try
            {
                var response = await httpClient.GetFromJsonAsync<ObservableCollection<UnidadMedidaResponse>>(
                    "api/unidad-medida/listar");

                return response ?? new ObservableCollection<UnidadMedidaResponse>();
            }
            catch
            {
                return new ObservableCollection<UnidadMedidaResponse>();
            }
        }

        public async Task<bool> CreateUnidadMedidaAsync(UnidadMedidaRequest unidadMedida)
        {
            try
            {
                var response = await httpClient.PostAsJsonAsync(
                    "api/unidad-medida/crear",
                    unidadMedida);

                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> UpdateUnidadMedidaAsync(UnidadMedidaRequest unidadMedida)
        {
            try
            {
                var response = await httpClient.PutAsJsonAsync(
                    $"api/unidad-medida/editar/{unidadMedida.UnidadMedidaId}",
                    unidadMedida);

                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> DeleteUnidadMedidaAsync(UnidadMedidaRequest unidadMedida)
        {
            try
            {
                var response = await httpClient.DeleteAsync(
                    $"api/unidad-medida/eliminar/{unidadMedida.UnidadMedidaId}");

                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
    }
}
