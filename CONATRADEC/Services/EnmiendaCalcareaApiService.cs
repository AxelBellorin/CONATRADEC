using CONATRADEC.Models;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.Net.Http.Json;

namespace CONATRADEC.Services
{
    class EnmiendaCalcareaApiService
    {
        private readonly HttpClient httpClient;

        public EnmiendaCalcareaApiService()
            : this(ApiClientService.Client)
        {
        }

        public EnmiendaCalcareaApiService(HttpClient httpClient)
        {
            this.httpClient = httpClient
                ?? throw new ArgumentNullException(nameof(httpClient));
        }

        public async Task<ObservableCollection<ParametroEnmiendaCalcareaResponse>> GetEnmiendasCalcareasAsync()
        {
            try
            {
                var response = await httpClient.GetFromJsonAsync<ObservableCollection<ParametroEnmiendaCalcareaResponse>>(
                    "api/fuente-nutriente/enmiendas-calcareas");

                return response ?? new ObservableCollection<ParametroEnmiendaCalcareaResponse>();
            }
            catch
            {
                return new ObservableCollection<ParametroEnmiendaCalcareaResponse>();
            }
        }

        public async Task<EnmiendaCalcareaCalcularResponse?> CalcularEnmiendaCalcareaAsync(
            EnmiendaCalcareaCalcularRequest request)
        {
            try
            {
                var response = await httpClient.PostAsJsonAsync(
                    "api/enmiendas-calcareas/calcular",
                    request);

                if (!response.IsSuccessStatusCode)
                    return null;

                return await response.Content.ReadFromJsonAsync<EnmiendaCalcareaCalcularResponse>();
            }
            catch
            {
                return null;
            }
        }
    }
}
