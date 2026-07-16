using CONATRADEC.Models;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.Net.Http.Json;

namespace CONATRADEC.Services
{
    class RolApiService
    {
        private readonly HttpClient httpClient;

        public RolApiService()
            : this(ApiClientService.Client)
        {
        }

        public RolApiService(HttpClient httpClient)
        {
            this.httpClient = httpClient
                ?? throw new ArgumentNullException(nameof(httpClient));
        }

        public async Task<ObservableCollection<RolResponse>> GetRolAsync()
        {
            try
            {
                var response = await httpClient.GetFromJsonAsync<ObservableCollection<RolResponse>>(
                    "api/Rol/listarRoles");

                return response ?? new ObservableCollection<RolResponse>();
            }
            catch
            {
                return new ObservableCollection<RolResponse>();
            }
        }

        public async Task<bool> CreateRolAsync(RolRequest rol)
        {
            try
            {
                var response = await httpClient.PostAsJsonAsync(
                    "api/Rol/crearRol",
                    rol);

                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> DeleteRolAsync(RolRequest rol)
        {
            try
            {
                var response = await httpClient.DeleteAsync(
                    $"api/Rol/eliminarRol/{rol.RolId}");

                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> UpdateRolAsync(RolRequest rol)
        {
            try
            {
                var response = await httpClient.PutAsJsonAsync(
                    $"api/Rol/editarRol/{rol.RolId}",
                    rol);

                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
    }
}
