using CONATRADEC.Models;
using System;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace CONATRADEC.Services
{
    // ===========================================================
    // =============== SERVICIO: PaisApiService ==================
    // ===========================================================
    // Este servicio se encarga de gestionar las operaciones CRUD
    // (Crear, Leer, Actualizar y Eliminar) para la entidad "País",
    // consumiendo los endpoints correspondientes del backend API.
    class PaisApiService
    {
        // ===========================================================
        // =================== DEPENDENCIAS / ESTADO =================
        // ===========================================================

        // Cliente HTTP encargado de realizar las peticiones hacia la API.
        private readonly HttpClient httpClient;

        // Servicio auxiliar que proporciona la URL base de la API.
        private readonly UrlApiService urlApiService = new UrlApiService();

        // ===========================================================
        // ======================== CONSTRUCTOR ======================
        // ===========================================================

        public PaisApiService()
        {
            // Inicializa el cliente HTTP con la dirección base del servidor API.
            // Esto permite que las peticiones usen rutas relativas (por ejemplo: "api/pais/...").
            httpClient = new HttpClient
            {
                BaseAddress = new Uri(urlApiService.BaseUrlApi)
            };
        }

        // ===========================================================
        // ====================== MÉTODOS PÚBLICOS ===================
        // ===========================================================

        // -----------------------------------------------------------
        // Método: GetPaisAsync
        // Descripción: Obtiene la lista completa de países desde la API.
        // Retorna: ObservableCollection<PaisResponse>
        // -----------------------------------------------------------
        public async Task<ObservableCollection<PaisResponse>> GetPaisAsync()
        {
            try
            {
                // Realiza una solicitud GET al endpoint correspondiente.
                // Convierte automáticamente la respuesta JSON en una colección de objetos PaisResponse.
                var response = await httpClient.GetFromJsonAsync<ObservableCollection<PaisResponse>>("api/pais");

                // Si la respuesta es nula (por error o sin datos), devuelve una colección vacía.
                return response ?? new ObservableCollection<PaisResponse>();
            }
            catch (Exception ex)
            {
                // Muestra un mensaje de error al usuario.
                await Application.Current.MainPage.DisplayAlert("Error", $"{ex}", "OK");

                // Devuelve una colección vacía para evitar interrupciones en la UI.
                return new ObservableCollection<PaisResponse>();
            }
        }

        // -----------------------------------------------------------
        // Método: CreatePaisAsync
        // Descripción: Envía un nuevo país al servidor para ser creado.
        // Parámetro: PaisRequest pais - objeto con la información del país.
        // Retorna: bool (true si la operación fue exitosa)
        // -----------------------------------------------------------
        public async Task<bool> CreatePaisAsync(PaisRequest pais)
        {
            try
            {
                // Envía el objeto "pais" como JSON mediante una solicitud POST al endpoint.
                var response = await httpClient.PostAsJsonAsync("api/pais/crearPais", pais);

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
        // Método: UpdatePaisAsync
        // Descripción: Actualiza la información de un país existente en el servidor.
        // Parámetro: PaisRequest pais - objeto con los nuevos datos del país.
        // Retorna: bool (true si la operación fue exitosa)
        // -----------------------------------------------------------
        public async Task<bool> UpdatePaisAsync(PaisRequest pais)
        {
            try
            {
                // Envía una solicitud PUT al endpoint correspondiente,
                // incluyendo el ID del país en la URL.
                var response = await httpClient.PutAsJsonAsync($"api/pais/actualizarPais/{pais.PaisId}", pais);

                // Retorna true si la operación fue exitosa (código 2xx).
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
        // Método: DeletePaisAsync
        // Descripción: Elimina un país del servidor por su ID.
        // Parámetro: PaisRequest pais - contiene el ID del país a eliminar.
        // Retorna: bool (true si la eliminación fue exitosa)
        // -----------------------------------------------------------
        public async Task<bool> DeletePaisAsync(PaisRequest pais)
        {
            try
            {
                // Envía una solicitud DELETE al endpoint correspondiente con el ID del país.
                var response = await httpClient.DeleteAsync($"api/pais/eliminarPais/{pais.PaisId}");

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
        // (Actualmente no se requieren métodos privados en este servicio.)
    }
}
