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
    class UserApiService
    {
        private readonly HttpClient httpClient;

        public UserApiService()
        {
            httpClient = new HttpClient { BaseAddress = new Uri("https://dummyjson.com/") };
        }

        public async Task<UserResponse> GetUsersAsync()
        {
            try
            {
                var response = await httpClient.GetFromJsonAsync<UserResponse>("users");
                return response ?? new UserResponse();
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", $"{ex}", "OK");
                return new UserResponse();
            }
        }
    }
}
