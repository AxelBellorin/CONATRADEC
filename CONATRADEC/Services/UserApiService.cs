using CONATRADEC.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http.Headers;
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

        // Servicio auxiliar que proporciona la URL base de la API.
        private readonly UrlApiService urlApiService = new UrlApiService();

        // ===========================================================
        // ======================== CONSTRUCTOR ======================
        // ===========================================================
        public UserApiService()
        {
            // Inicializa HttpClient y establece la URL base del servicio.
            // Actualmente apunta a DummyJSON (entorno de pruebas).
            httpClient = new HttpClient { BaseAddress = new Uri(urlApiService.BaseUrlApi) };
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
        public async Task<ObservableCollection<UserResponse>> GetUsersAsync()
        {
            try
            {
                // Realiza una solicitud HTTP GET al endpoint “/users”
                // y deserializa la respuesta JSON automáticamente.
                var response = await httpClient.GetFromJsonAsync<ObservableCollection<UserResponse>>("api/usuarios/listar");

                // Devuelve el resultado obtenido o un objeto vacío
                // en caso de que la respuesta haya sido nula.
                return response ?? new ObservableCollection<UserResponse>();
            }
            catch (Exception ex)
            {
                // Devuelve una respuesta vacía para evitar interrupciones.
                return new ObservableCollection<UserResponse>();
            }
        }

        public async Task<(bool, UserRequest)> CreateUserAsync(UserRequest userRequest)
        {
            try
            {
                var response = await httpClient.PostAsJsonAsync($"api/usuarios/crear", userRequest);

                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    await Application.Current.MainPage.DisplayAlert(
                        "Error API",
                        $"Código: {response.StatusCode}\nDetalle: {errorBody}",
                        "OK");

                    return (false, null);
                }

                var userResponse = await response.Content.ReadFromJsonAsync<UserRequest>();
                return (true, userResponse);
            }
            catch (Exception ex)
            {
                return (false, new UserRequest());
            }
        }

        public async Task SubirImagenAsync(int? usuarioId, FileResult? ImagenSeleccionada)
        {
            if (ImagenSeleccionada == null)
                return;

            try
            {
                //Obtener y validar la extensión (solo .jpg y .png)
                var extension = Path.GetExtension(ImagenSeleccionada.FileName)?.ToLower();

                // 🔹 Abrir flujo de lectura del archivo
                using var stream = await ImagenSeleccionada.OpenReadAsync();

                // 🔹 Crear el contenido multipart/form-data
                var content = new MultipartFormDataContent();
                var streamContent = new StreamContent(stream);

                // 🔹 Asignar el tipo MIME correcto
                var mimeType = extension == ".png" ? "image/png" : "image/jpeg";
                streamContent.Headers.ContentType = new MediaTypeHeaderValue(mimeType);

                // 🔹 Agregar el archivo al cuerpo con el nombre "archivo"
                content.Add(streamContent, "archivo", ImagenSeleccionada.FileName);

                // 🔹 Enviar la solicitud al endpoint
                var response = await httpClient.PostAsync($"api/usuarios/{usuarioId}/SubirImagenUsuario", content);

                // 🔹 Verificar resultado
                if (!response.IsSuccessStatusCode)
                {
                    await Application.Current.MainPage.DisplayAlert("Advertencia", "No se pudo subir la imagen.", "OK");
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", $"Error al subir imagen: {ex.Message}", "OK");
            }
        }


        public async Task<bool> DeleteUserAsync(UserRequest user)
        {
            try
            {
                // Realiza una solicitud DELETE al endpoint con el ID del rol en la ruta.
                var response = await httpClient.DeleteAsync($"api/usuarios/eliminar/{user.UsuarioId}");

                // Retorna true si la API confirmó la eliminación correctamente.
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                return false;
            }
        }


        public async Task<(bool, UserRequest?)> UpdateUserAsync(UserRequest userRequest)
        {
            try
            {
                var response = await httpClient.PutAsJsonAsync($"api/usuarios/actualizar/{userRequest.UsuarioId}", userRequest);

                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    await Application.Current.MainPage.DisplayAlert(
                        "Error API",
                        $"Código: {response.StatusCode}\nDetalle: {errorBody}",
                        "OK");

                    return (false, null);
                }

                var userResponse = await response.Content.ReadFromJsonAsync<UserRequest>();
                return (true, userResponse);
            }
            catch (Exception ex)
            {
                return (false, new UserRequest());
            }
        }


        // ===========================================================
        // ===================== MÉTODOS PRIVADOS ====================
        // ===========================================================
        // (Actualmente no se requieren métodos privados en este servicio.)
    }
}
