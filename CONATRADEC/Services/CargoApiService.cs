using CONATRADEC.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using static CONATRADEC.Models.CargoResponse;

namespace CONATRADEC.Services
{
    class CargoApiService
    {
        private readonly HttpClient httpClient;

        public CargoApiService()
        {
            httpClient = new HttpClient { BaseAddress = new Uri("https://localhost:7176/") };
        }

        public async Task<List<CargoRP>> GetCargoAsync()
        {
            try
            {
                var response = await httpClient.GetFromJsonAsync<List<CargoRP>>("api/Cargos/listarCargos");
                return response ?? new List<CargoRP>();
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", $"{ex}", "OK");
                return new List<CargoRP>();
            }
        }

        public async Task<bool> CreateCargoAsync(CargoRequest cargo)
        {
            try
            {
                var response = await httpClient.PostAsJsonAsync($"api/Cargos/crearCargo", cargo);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", $"{ex}", "OK");
                return false;
            }
        }

        public async Task<bool> DeleteCargoAsyn(CargoRequest cargo)
        {
            try
            {
                var response = await httpClient.DeleteAsync($"api/Cargos/eliminarCargo/{cargo.CargoId}");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", $"{ex}", "OK");
                return false;
            }
        }

        public async Task<bool> UpdateCargoAsync(CargoRequest cargo)
        {
            try
            {
                var response = await httpClient.PutAsJsonAsync($"api/Cargos/editarCargo/{cargo.CargoId}", cargo);
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
