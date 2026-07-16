using CONATRADEC.Models;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.Net.Http.Json;

namespace CONATRADEC.Services
{
    class MatrizPermisosApiService
    {
        private readonly HttpClient httpClient;

        public MatrizPermisosApiService()
            : this(ApiClientService.Client)
        {
        }

        public MatrizPermisosApiService(HttpClient httpClient)
        {
            this.httpClient = httpClient
                ?? throw new ArgumentNullException(nameof(httpClient));
        }

        public async Task<ObservableCollection<MatrizPermisosResponse>> GetMatrizByRolAsync(
            RolRequest rolRequest)
        {
            try
            {
                string encodedRol = Uri.EscapeDataString(rolRequest.NombreRol);

                var response = await httpClient.GetFromJsonAsync<ObservableCollection<MatrizPermisosResponse>>(
                    $"api/rol-interfaz/matriz-por-rol-nombre?nombreRol={encodedRol}");

                return response ?? new ObservableCollection<MatrizPermisosResponse>();
            }
            catch
            {
                return new ObservableCollection<MatrizPermisosResponse>();
            }
        }

        public async Task<bool> GuardarMatrizAsync(MatrizPermisosRequest matrizPermisosRequest)
        {
            try
            {
                var response = await httpClient.PutAsJsonAsync(
                    "api/rol-permisos/actualizar-interfaz",
                    matrizPermisosRequest);

                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
    }
}
