using CONATRADEC.Models;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.Net.Http.Json;

namespace CONATRADEC.Services
{
    class DepartamentoApiService
    {
        private readonly HttpClient httpClient;

        public DepartamentoApiService()
            : this(ApiClientService.Client)
        {
        }

        public DepartamentoApiService(HttpClient httpClient)
        {
            this.httpClient = httpClient
                ?? throw new ArgumentNullException(nameof(httpClient));
        }

        public async Task<ObservableCollection<DepartamentoResponse>> GetDepartamentosAsync(int? paisId)
        {
            try
            {
                var response = await httpClient.GetFromJsonAsync<ObservableCollection<DepartamentoResponse>>(
                    $"api/departamento/por-pais/{paisId}");

                return response ?? new ObservableCollection<DepartamentoResponse>();
            }
            catch
            {
                return new ObservableCollection<DepartamentoResponse>();
            }
        }

        public async Task<bool> CreateDepartamentoAsync(DepartamentoRequest departamentoRequest)
        {
            try
            {
                var response = await httpClient.PostAsJsonAsync(
                    "api/departamento/Crear",
                    departamentoRequest);

                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> UpdateDepartamentoAsync(DepartamentoRequest departamento)
        {
            try
            {
                var response = await httpClient.PutAsJsonAsync(
                    $"api/departamento/actualizar/{departamento.DepartamentoId}",
                    departamento);

                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> DeleteDepartamentoAsync(DepartamentoRequest dpto)
        {
            try
            {
                var response = await httpClient.DeleteAsync(
                    $"api/departamento/eliminar/{dpto.DepartamentoId}");

                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
    }
}
