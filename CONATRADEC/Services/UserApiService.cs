using CONATRADEC.Models;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using System.Collections.ObjectModel;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace CONATRADEC.Services
{
    class UserApiService
    {
        private readonly HttpClient httpClient;

        public UserApiService()
            : this(ApiClientService.Client)
        {
        }

        public UserApiService(HttpClient httpClient)
        {
            this.httpClient = httpClient
                ?? throw new ArgumentNullException(nameof(httpClient));
        }

        public async Task<ObservableCollection<UserResponse>> GetUsersAsync()
        {
            try
            {
                var response = await httpClient.GetFromJsonAsync<ObservableCollection<UserResponse>>(
                    "api/usuarios/listar");

                return response ?? new ObservableCollection<UserResponse>();
            }
            catch
            {
                return new ObservableCollection<UserResponse>();
            }
        }

        public async Task<(bool, UserRequest)> CreateUserAsync(UserRequest userRequest)
        {
            try
            {
                var response = await httpClient.PostAsJsonAsync(
                    "api/usuarios/crear",
                    userRequest);

                if (!response.IsSuccessStatusCode)
                {
                    string errorBody = await response.Content.ReadAsStringAsync();

                    await Application.Current.MainPage.DisplayAlert(
                        "Error API",
                        $"Código: {response.StatusCode}\nDetalle: {errorBody}",
                        "OK");

                    return (false, null);
                }

                UserRequest? userResponse =
                    await response.Content.ReadFromJsonAsync<UserRequest>();

                return (true, userResponse);
            }
            catch
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
                string? extension = Path.GetExtension(ImagenSeleccionada.FileName)?.ToLower();

                using var stream = await ImagenSeleccionada.OpenReadAsync();
                var content = new MultipartFormDataContent();
                var streamContent = new StreamContent(stream);

                string mimeType = extension == ".png"
                    ? "image/png"
                    : "image/jpeg";

                streamContent.Headers.ContentType = new MediaTypeHeaderValue(mimeType);
                content.Add(streamContent, "archivo", ImagenSeleccionada.FileName);

                var response = await httpClient.PostAsync(
                    $"api/usuarios/{usuarioId}/SubirImagenUsuario",
                    content);

                if (!response.IsSuccessStatusCode)
                {
                    _ = GlobalService.MostrarToastAsync(
                        "Advertencia" + "No se pudo subir la imagen.");
                }
            }
            catch (Exception ex)
            {
                _ = GlobalService.MostrarToastAsync(
                    "Error" + $"Error al subir imagen: {ex.Message}");
            }
        }

        public async Task<bool> DeleteUserAsync(UserRequest user)
        {
            try
            {
                var response = await httpClient.DeleteAsync(
                    $"api/usuarios/eliminar/{user.UsuarioId}");

                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<(bool, UserRequest?)> UpdateUserAsync(UserRequest userRequest)
        {
            try
            {
                var response = await httpClient.PutAsJsonAsync(
                    $"api/usuarios/actualizar/{userRequest.UsuarioId}",
                    userRequest);

                if (!response.IsSuccessStatusCode)
                {
                    string errorBody = await response.Content.ReadAsStringAsync();

                    await Application.Current.MainPage.DisplayAlert(
                        "Error API",
                        $"Código: {response.StatusCode}\nDetalle: {errorBody}",
                        "OK");

                    return (false, null);
                }

                UserRequest? userResponse =
                    await response.Content.ReadFromJsonAsync<UserRequest>();

                return (true, userResponse);
            }
            catch
            {
                return (false, new UserRequest());
            }
        }
    }
}
