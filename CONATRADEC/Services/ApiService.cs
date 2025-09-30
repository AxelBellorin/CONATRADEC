using CONATRADEC.Models;
using System.Net.Http.Json;

namespace CONATRADEC.Services
{
    class ApiService
    {
        private readonly HttpClient httpClient;

        public ApiService()
        {
            httpClient = new HttpClient { BaseAddress = new Uri("https://dummyjson.com/") };
        }

        public async Task<LoginResponse> LoginAsync(LoginRequest request)
        {
            var response = await httpClient.PostAsJsonAsync("auth/login", request);
            if (response.IsSuccessStatusCode)
            {
                var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponse>();
                return loginResponse;
            }
            else
            {
                // opcional: leer mensaje de error
                var err = await response.Content.ReadAsStringAsync();
                throw new Exception($"Login failed: {response.StatusCode} - {err}");
            }
        }
    }
}
