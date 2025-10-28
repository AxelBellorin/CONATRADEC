using CONATRADEC.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace CONATRADEC.Services
{
    class CargoApiService
    {
        private readonly HttpClient httpClient;
        private readonly UrlApiService urlApiService = new UrlApiService();
        public CargoApiService()
        {
            httpClient = new HttpClient { BaseAddress = new Uri(urlApiService.BaseUrlApi) };
        }

        public async Task<ObservableCollection<CargoResponse>> GetCargoAsync()
        {
            try
            {
                var response = await httpClient.GetFromJsonAsync<ObservableCollection<CargoResponse>>("api/Cargos/listarCargos");
                return response ?? new ObservableCollection<CargoResponse>();
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", $"{ex}", "OK");
                return new ObservableCollection<CargoResponse>();
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
