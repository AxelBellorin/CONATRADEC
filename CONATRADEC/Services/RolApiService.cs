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
    class RolApiService
    {
        private readonly HttpClient httpClient;

        public RolApiService()
        {
            httpClient = new HttpClient { BaseAddress = new Uri("https://localhost:7176/") };
        }

        public async Task<List<RolRP>> GetRolAsync()
        {
            try
            {
                var response = await httpClient.GetFromJsonAsync<List<RolRP>>("api/Rol/listarRoles");
                return response ?? new List<RolRP>();
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", $"{ex}", "OK");
                return new List<RolRP>();
            }
        }
    }
}
