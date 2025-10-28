using CONATRADEC.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using static CONATRADEC.Models.UserResponse;

namespace CONATRADEC.Services
{
    class MatrizPermisosApiService  
    {
        private readonly HttpClient httpClient;
        private readonly UrlApiService urlApiService = new UrlApiService();

        public MatrizPermisosApiService()
        {
            httpClient = new HttpClient { BaseAddress = new Uri(urlApiService.BaseUrlApi) };
        }

        public async Task<ObservableCollection<MatrizPermisosResponse>> GetMatrizByRolAsync(RolRequest rolRequest)
        {
            try
            {
                var encodedRol = Uri.EscapeDataString(rolRequest.NombreRol);
                var response = await httpClient.GetFromJsonAsync<ObservableCollection<MatrizPermisosResponse>>($"api/rol-permisos/matriz-por-rol-nombre?nombreRol={encodedRol}");
                return response ?? new ObservableCollection<MatrizPermisosResponse>();
            }
            catch (HttpRequestException ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error de conexión", ex.Message, "OK");
                return new ObservableCollection<MatrizPermisosResponse> ();
            }
            catch (NotSupportedException ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error de formato", "Respuesta no JSON.", "OK");
                return new ObservableCollection<MatrizPermisosResponse>();
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error inesperado", ex.Message, "OK");
                return new  ObservableCollection<MatrizPermisosResponse>();
            }
        }

        public async Task<bool> CreateRolAsync(RolRequest rol)
        {
            try
            {
                var response = await httpClient.PostAsJsonAsync($"api/Rol/crearRol", rol);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", $"{ex}", "OK");
                return false;
            }
        }

        public async Task<bool> DeleteRolAsyn(RolRequest rol)
        {
            try
            {
                var response = await httpClient.DeleteAsync($"api/Rol/eliminarRol/{rol.RolId}");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", $"{ex}", "OK");
                return false;
            }
        }
        public async Task<bool> GuardarMatrizAsync(List<MatrizPermisosRequest> matrizPermisosRequest)
        {
            try
            {
                var response = await httpClient.PutAsJsonAsync("api/rol-permisos/actualizar-permisos/", matrizPermisosRequest);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", $"{ex}", "OK");
                return false;
            }
        }
    }
}
