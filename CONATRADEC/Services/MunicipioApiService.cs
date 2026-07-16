using CONATRADEC.Models;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.Net.Http.Json;

namespace CONATRADEC.Services
{
    class MunicipioApiService
    {
        private readonly HttpClient httpClient;

        public MunicipioApiService()
            : this(ApiClientService.Client)
        {
        }

        public MunicipioApiService(HttpClient httpClient)
        {
            this.httpClient = httpClient
                ?? throw new ArgumentNullException(nameof(httpClient));
        }

        public async Task<ObservableCollection<MunicipioResponse>> GetMunicipiosAsync(int? departamentoId)
        {
            try
            {
                var response = await httpClient.GetFromJsonAsync<ObservableCollection<MunicipioResponse>>(
                    $"/por-departamento/{departamentoId}");

                return response ?? new ObservableCollection<MunicipioResponse>();
            }
            catch
            {
                return new ObservableCollection<MunicipioResponse>();
            }
        }

        public async Task<bool> CreateMunicipioAsync(MunicipioRequest municipio)
        {
            try
            {
                var response = await httpClient.PostAsJsonAsync("/crear", municipio);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> UpdateMunicipioAsync(MunicipioRequest municipio)
        {
            try
            {
                var response = await httpClient.PutAsJsonAsync(
                    $"/actualizar/{municipio.MunicipioId}",
                    municipio);

                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> DeleteMunicipioAsync(MunicipioRequest municipio)
        {
            try
            {
                var response = await httpClient.DeleteAsync(
                    $"/eliminar/{municipio.MunicipioId}");

                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
    }
}
