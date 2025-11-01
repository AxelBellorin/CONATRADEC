using CONATRADEC.Models;
using System.Net.Http.Json;

namespace CONATRADEC.Services
{
    // ===========================================================
    // =============== SERVICIO: LoginApiService =================
    // ===========================================================
    // Este servicio maneja la autenticación de usuarios contra el
    // endpoint remoto. Envía las credenciales y obtiene el token
    // de acceso o los datos del usuario autenticado.
    class LoginApiService
    {
        // ===========================================================
        // =================== DEPENDENCIAS / ESTADO =================
        // ===========================================================

        // Cliente HTTP utilizado para realizar solicitudes a la API.
        private readonly HttpClient httpClient;

        // ===========================================================
        // ======================== CONSTRUCTOR ======================
        // ===========================================================

        public LoginApiService()
        {
            // Inicializa el cliente HTTP con la dirección base de la API.
            // En este caso, se utiliza "dummyjson.com" como entorno de prueba.
            httpClient = new HttpClient { BaseAddress = new Uri("https://dummyjson.com/") };

            httpClient.Timeout = TimeSpan.FromSeconds(20); // Espera máxima de respuesta
        }

        // ===========================================================
        // ====================== MÉTODOS PÚBLICOS ===================
        // ===========================================================

        // -----------------------------------------------------------
        // Método: LoginAsync
        // Descripción:
        //     Envía las credenciales del usuario al endpoint de autenticación.
        //     Si la respuesta es exitosa, devuelve un objeto LoginResponse
        //     con los datos del usuario autenticado y tokens de sesión.
        //
        // Parámetros:
        //     request - Objeto de tipo LoginRequest que contiene el username y password.
        //
        // Retorna:
        //     LoginResponse (si la autenticación fue exitosa).
        //
        // Excepciones:
        //     Lanza una Exception si la respuesta HTTP indica un error (401, 500, etc.).
        // -----------------------------------------------------------
        public async Task<LoginResponse> LoginAsync(LoginRequest request)
        {
            // Realiza una solicitud POST enviando el objeto LoginRequest en formato JSON.
            var response = await httpClient.PostAsJsonAsync("auth/login", request);

            // Si la API responde con éxito (código 200 OK, etc.)
            if (response.IsSuccessStatusCode)
            {
                // Deserializa el cuerpo de la respuesta JSON a LoginResponse.
                var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponse>();

                // Devuelve el objeto con los datos del usuario autenticado.
                return loginResponse;
            }
            else
            {
                // Si ocurre un error, opcionalmente se lee el contenido de error como texto.
                var err = await response.Content.ReadAsStringAsync();

                // Lanza una excepción detallando el código HTTP y el mensaje recibido.
                throw new Exception($"Login failed: {response.StatusCode} - {err}");
            }
        }

        // ===========================================================
        // ===================== MÉTODOS PRIVADOS ====================
        // ===========================================================
        // (No se requieren métodos privados actualmente.)
    }
}
