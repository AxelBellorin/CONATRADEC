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
    // ===========================================================
    // =============== SERVICIO: CargoApiService =================
    // ===========================================================
    // Este servicio se encarga de gestionar las operaciones CRUD
    // (Crear, Leer, Actualizar y Eliminar) para los "Cargos"
    // consumiendo los endpoints correspondientes del backend API.
    class CargoApiService
    {
        // ===========================================================
        // =================== DEPENDENCIAS / ESTADO =================
        // ===========================================================

        // Cliente HTTP que realiza las peticiones a la API.
        private readonly HttpClient httpClient;

        // Servicio que proporciona la URL base de la API.
        private readonly UrlApiService urlApiService = new UrlApiService();

        // ===========================================================
        // ======================== CONSTRUCTOR ======================
        // ===========================================================

        public CargoApiService()
        {
            // Inicializa el cliente HTTP con la URL base de la API.
            // Esto permite realizar peticiones relativas (por ejemplo: "api/Cargos/...").
            httpClient = new HttpClient { BaseAddress = new Uri(urlApiService.BaseUrlApi) };
        }

        // ===========================================================
        // ====================== MÉTODOS PÚBLICOS ===================
        // ===========================================================

        // -----------------------------------------------------------
        // Método: GetCargoAsync
        // Descripción: Obtiene la lista completa de cargos desde la API.
        // Retorna: ObservableCollection<CargoResponse>
        // -----------------------------------------------------------
        public async Task<ObservableCollection<CargoResponse>> GetCargoAsync()
        {
            try
            {
                // Realiza una solicitud GET al endpoint correspondiente
                // y convierte automáticamente la respuesta JSON en una colección de cargos.
                var response = await httpClient.GetFromJsonAsync<ObservableCollection<CargoResponse>>("api/Cargos/listarCargos");

                // Si la respuesta es nula (por error o sin datos), devuelve una colección vacía.
                return response ?? new ObservableCollection<CargoResponse>();
            }
            catch (Exception ex)
            {
                // Muestra un mensaje de error en la interfaz de usuario.
                await Application.Current.MainPage.DisplayAlert("Error", $"{ex}", "OK");

                // Devuelve una colección vacía para evitar interrupciones en la UI.
                return new ObservableCollection<CargoResponse>();
            }
        }

        // -----------------------------------------------------------
        // Método: CreateCargoAsync
        // Descripción: Envía un nuevo cargo al servidor para ser creado.
        // Parámetro: CargoRequest cargo - objeto con la información del cargo.
        // Retorna: bool (true si la operación fue exitosa)
        // -----------------------------------------------------------
        public async Task<bool> CreateCargoAsync(CargoRequest cargo)
        {
            try
            {
                // Envía el objeto "cargo" como JSON mediante una solicitud POST.
                var response = await httpClient.PostAsJsonAsync($"api/Cargos/crearCargo", cargo);

                // Devuelve true si la respuesta del servidor indica éxito (código HTTP 2xx).
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                // En caso de error, muestra un mensaje al usuario.
                await Application.Current.MainPage.DisplayAlert("Error", $"{ex}", "OK");

                // Retorna false para indicar fallo en la operación.
                return false;
            }
        }

        // -----------------------------------------------------------
        // Método: UpdateCargoAsync
        // Descripción: Actualiza la información de un cargo existente en el servidor.
        // Parámetro: CargoRequest cargo - objeto con los nuevos datos del cargo.
        // Retorna: bool (true si la operación fue exitosa)
        // -----------------------------------------------------------
        public async Task<bool> UpdateCargoAsync(CargoRequest cargo)
        {
            try
            {
                // Envía una solicitud PUT al endpoint correspondiente,
                // incluyendo el ID del cargo en la URL.
                var response = await httpClient.PutAsJsonAsync($"api/Cargos/editarCargo/{cargo.CargoId}", cargo);

                // Retorna true si la operación fue exitosa (2xx).
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                // Notifica el error y retorna false.
                await Application.Current.MainPage.DisplayAlert("Error", $"{ex}", "OK");
                return false;
            }
        }

        // -----------------------------------------------------------
        // Método: DeleteCargoAsyn
        // Descripción: Elimina un cargo del servidor por su ID.
        // Parámetro: CargoRequest cargo - contiene el ID del cargo a eliminar.
        // Retorna: bool (true si la eliminación fue exitosa)
        // -----------------------------------------------------------
        public async Task<bool> DeleteCargoAsyn(CargoRequest cargo)
        {
            try
            {
                // Envía una solicitud DELETE al endpoint con el ID del cargo.
                var response = await httpClient.DeleteAsync($"api/Cargos/eliminarCargo/{cargo.CargoId}");

                // Retorna true si la API confirmó la eliminación (código HTTP 2xx).
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                // Muestra mensaje de error y devuelve false.
                await Application.Current.MainPage.DisplayAlert("Error", $"{ex}", "OK");
                return false;
            }
        }

        // ===========================================================
        // ===================== MÉTODOS PRIVADOS ====================
        // ===========================================================
        // (No hay métodos privados actualmente en este servicio.)
    }
}

