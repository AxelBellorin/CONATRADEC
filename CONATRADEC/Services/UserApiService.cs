using CONATRADEC.Models;
using System.Collections.ObjectModel;
using System.Net.Http.Headers;
using Microsoft.Maui.Storage;

namespace CONATRADEC.Services
{
    public class UserApiService
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

        public Task<ApiResult<ObservableCollection<UserResponse>>> GetUsersResultAsync(
            CancellationToken cancellationToken = default)
        {
            return ApiServiceHelper.GetCollectionAsync<UserResponse>(
                httpClient,
                "api/usuarios/listar",
                "los usuarios",
                cancellationToken);
        }

        public Task<ApiResult<UserRequest>> CreateUserResultAsync(
            UserRequest userRequest,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(userRequest);

            return ApiServiceHelper.SendAndReadAsync<UserRequest, UserRequest>(
                httpClient,
                HttpMethod.Post,
                "api/usuarios/crear",
                userRequest,
                "crear el usuario",
                "Usuario creado correctamente.",
                cancellationToken);
        }

        public Task<ApiResult<UserRequest>> UpdateUserResultAsync(
            UserRequest userRequest,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(userRequest);

            if (!userRequest.UsuarioId.HasValue || userRequest.UsuarioId.Value <= 0)
            {
                return Task.FromResult(
                    ApiResult<UserRequest>.Fail(
                        "No se recibió un identificador de usuario válido."));
            }

            return ApiServiceHelper.SendAndReadAsync<UserRequest, UserRequest>(
                httpClient,
                HttpMethod.Put,
                $"api/usuarios/actualizar/{userRequest.UsuarioId.Value}",
                userRequest,
                "actualizar el usuario",
                "Usuario actualizado correctamente.",
                cancellationToken);
        }

        public Task<ApiResult<bool>> DeleteUserResultAsync(
            UserRequest user,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(user);

            if (!user.UsuarioId.HasValue || user.UsuarioId.Value <= 0)
            {
                return Task.FromResult(
                    ApiResult<bool>.Fail(
                        "No se recibió un identificador de usuario válido."));
            }

            return ApiServiceHelper.SendAsync<UserRequest>(
                httpClient,
                HttpMethod.Delete,
                $"api/usuarios/eliminar/{user.UsuarioId.Value}",
                null,
                "eliminar el usuario",
                "Usuario eliminado correctamente.",
                cancellationToken);
        }

        public async Task<ApiResult<bool>> SubirImagenResultAsync(
            int? usuarioId,
            FileResult? imagenSeleccionada,
            CancellationToken cancellationToken = default)
        {
            if (!usuarioId.HasValue || usuarioId.Value <= 0)
                return ApiResult<bool>.Fail("El identificador del usuario no es válido.");

            if (imagenSeleccionada == null)
                return ApiResult<bool>.Fail("No se seleccionó una imagen.");

            string extension = Path
                .GetExtension(imagenSeleccionada.FileName)
                .ToLowerInvariant();

            if (extension is not ".jpg" and not ".jpeg" and not ".png")
            {
                return ApiResult<bool>.Fail(
                    "La imagen debe tener formato JPG, JPEG o PNG.");
            }

            try
            {
                await using var stream = await imagenSeleccionada.OpenReadAsync();
                using var content = new MultipartFormDataContent();
                using var streamContent = new StreamContent(stream);

                string mimeType = extension == ".png"
                    ? "image/png"
                    : "image/jpeg";

                streamContent.Headers.ContentType =
                    new MediaTypeHeaderValue(mimeType);

                content.Add(
                    streamContent,
                    "archivo",
                    imagenSeleccionada.FileName);

                using var response = await httpClient.PostAsync(
                    $"api/usuarios/{usuarioId.Value}/SubirImagenUsuario",
                    content,
                    cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    return ApiResult<bool>.Fail(
                        ApiServiceHelper.GetHttpMessage(
                            response.StatusCode,
                            "subir la imagen del usuario"),
                        (int)response.StatusCode);
                }

                return ApiResult<bool>.Ok(
                    true,
                    "Imagen subida correctamente.");
            }
            catch (TaskCanceledException)
                when (!cancellationToken.IsCancellationRequested)
            {
                return ApiResult<bool>.Fail(
                    "La subida de la imagen tardó demasiado.");
            }
            catch (OperationCanceledException)
            {
                return ApiResult<bool>.Fail("La operación fue cancelada.");
            }
            catch (HttpRequestException)
            {
                return ApiResult<bool>.Fail(
                    "No fue posible conectarse con el servidor.");
            }
            catch (Exception)
            {
                return ApiResult<bool>.Fail(
                    "Ocurrió un error inesperado al subir la imagen.");
            }
        }

        // =========================================================
        // MÉTODOS ANTERIORES CONSERVADOS PARA LOS FORMULARIOS
        // =========================================================

        public async Task<ObservableCollection<UserResponse>> GetUsersAsync()
        {
            var result = await GetUsersResultAsync();
            return result.Data ?? new ObservableCollection<UserResponse>();
        }

        public async Task<(bool, UserRequest)> CreateUserAsync(
            UserRequest userRequest)
        {
            var result = await CreateUserResultAsync(userRequest);
            return (result.Success, result.Data ?? new UserRequest());
        }

        public async Task<(bool, UserRequest?)> UpdateUserAsync(
            UserRequest userRequest)
        {
            var result = await UpdateUserResultAsync(userRequest);
            return (result.Success, result.Data);
        }

        public async Task<bool> DeleteUserAsync(UserRequest user)
        {
            var result = await DeleteUserResultAsync(user);
            return result.Success && result.Data == true;
        }

        public async Task SubirImagenAsync(
            int? usuarioId,
            FileResult? imagenSeleccionada)
        {
            var result = await SubirImagenResultAsync(
                usuarioId,
                imagenSeleccionada);

            if (!result.Success)
                await GlobalService.MostrarToastAsync(result.Message);
        }
    }
}
