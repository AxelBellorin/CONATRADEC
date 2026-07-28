using CONATRADEC.Models;
using Microsoft.Maui.Storage;
using System.Net;
using System.Net.Http.Json;

namespace CONATRADEC.Services
{
    /// <summary>
    /// Maneja la autenticación de usuarios contra la API.
    /// </summary>
    public sealed class LoginApiService
    {
        private readonly HttpClient httpClient;

        public LoginApiService()
            : this(ApiClientService.Client)
        {
        }

        public LoginApiService(HttpClient httpClient)
        {
            this.httpClient = httpClient
                ?? throw new ArgumentNullException(nameof(httpClient));
        }

        public async Task<LoginResponse> LoginAsync(
            LoginRequest request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            using HttpResponseMessage response =
                await httpClient.PostAsJsonAsync(
                    "api/auth/login",
                    request,
                    cancellationToken);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                throw new UnauthorizedAccessException(
                    "El usuario o la contraseña son incorrectos.");
            }

            if (!response.IsSuccessStatusCode)
            {
                string contenidoError =
                    await response.Content.ReadAsStringAsync(
                        cancellationToken);

                throw new HttpRequestException(
                    $"La API respondió con el código " +
                    $"{(int)response.StatusCode} ({response.StatusCode}). " +
                    contenidoError,
                    null,
                    response.StatusCode);
            }

            LoginResponse? loginResponse =
                await response.Content.ReadFromJsonAsync<LoginResponse>(
                    cancellationToken: cancellationToken);

            if (loginResponse == null)
            {
                throw new InvalidOperationException(
                    "La API respondió correctamente, pero no devolvió " +
                    "los datos del usuario.");
            }

            if (loginResponse.UsuarioId is > 0)
            {
                Preferences.Set(
                    SessionKeys.KeyUserId,
                    loginResponse.UsuarioId.Value.ToString());

                Preferences.Set(
                    SessionKeys.KeySessionVersion,
                    Math.Max(1, loginResponse.VersionSesion));

                SessionValidationService.Instance.Iniciar();
            }

            return loginResponse;
        }
    }
}
