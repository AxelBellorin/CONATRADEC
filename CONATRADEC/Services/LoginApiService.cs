using CONATRADEC.Models;
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

        /// <summary>
        /// Mantiene compatibilidad con el LoginViewModel actual.
        /// Utiliza el HttpClient compartido de la aplicación.
        /// </summary>
        public LoginApiService()
            : this(ApiClientService.Client)
        {
        }

        /// <summary>
        /// Permite suministrar un HttpClient desde pruebas o desde
        /// inyección de dependencias en una etapa posterior.
        /// </summary>
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

            using HttpResponseMessage response = await httpClient.PostAsJsonAsync(
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
                string contenidoError = await response.Content.ReadAsStringAsync(
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

            return loginResponse;
        }
    }
}
