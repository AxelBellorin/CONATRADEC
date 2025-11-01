using CONATRADEC.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using static CONATRADEC.Models.UserResponse;

namespace CONATRADEC.Services
{
    // ===========================================================
    // =================== SERVICIO: UserApiService ===============
    // ===========================================================
    // Este servicio se encarga de consumir los datos de usuarios
    // desde una API remota (en este caso, https://dummyjson.com/),
    // y devolverlos en forma de un objeto UserResponse.
    //
    // En una versión productiva, este servicio se conectaría
    // directamente con la API de CONATRADEC.
    // ===========================================================
    class UserApiService
    {
        // ===========================================================
        // =================== DEPENDENCIAS / ESTADO =================
        // ===========================================================

        // Cliente HTTP utilizado para ejecutar solicitudes REST.
        private readonly HttpClient httpClient;

        // ===========================================================
        // ======================== CONSTRUCTOR ======================
        // ===========================================================
        public UserApiService()
        {
            // Inicializa HttpClient y establece la URL base del servicio.
            // Actualmente apunta a DummyJSON (entorno de pruebas).
            httpClient = new HttpClient { BaseAddress = new Uri("https://dummyjson.com/") };
        }

        // ===========================================================
        // ====================== MÉTODOS PÚBLICOS ===================
        // ===========================================================

        // -----------------------------------------------------------
        // Método: GetUsersAsync
        // Descripción:
        //   Recupera la lista de usuarios desde la API remota.
        //
        // Retorna:
        //   Un objeto UserResponse que contiene una lista de UserRP.
        //
        // Manejo de errores:
        //   Si ocurre una excepción, muestra un mensaje en pantalla
        //   y devuelve un objeto UserResponse vacío.
        // -----------------------------------------------------------
        public async Task<UserResponse> GetUsersAsync()
        {
            try
            {
                // Realiza una solicitud HTTP GET al endpoint “/users”
                // y deserializa la respuesta JSON automáticamente.
                var response = await httpClient.GetFromJsonAsync<UserResponse>("users");

                // Devuelve el resultado obtenido o un objeto vacío
                // en caso de que la respuesta haya sido nula.
                return response ?? new UserResponse();
            }
            catch (Exception ex)
            {
                // Si ocurre un error de conexión, formato o red,
                // muestra una alerta al usuario.
                await Application.Current.MainPage.DisplayAlert("Error", $"{ex}", "OK");

                // Devuelve una respuesta vacía para evitar interrupciones.
                return new UserResponse();
            }
        }

        // ===========================================================
        // ===================== MÉTODOS PRIVADOS ====================
        // ===========================================================
        // (Actualmente no se requieren métodos privados en este servicio.)
    }
}
