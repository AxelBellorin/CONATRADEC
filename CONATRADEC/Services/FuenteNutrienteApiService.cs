using CONATRADEC.Models;
using System.Collections.ObjectModel;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CONATRADEC.Services
{
    public class FuenteNutrienteApiService
    {
        private readonly HttpClient httpClient;
        private readonly UrlApiService urlApiService = new UrlApiService();

        private readonly JsonSerializerOptions jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public FuenteNutrienteApiService()
        {
            httpClient = new HttpClient
            {
                BaseAddress = new Uri(urlApiService.BaseUrlApi)
            };
        }

        public async Task<ObservableCollection<FuenteNutrienteResponse>> GetFuenteNutrienteAsync()
        {
            try
            {
                var response = await httpClient.GetFromJsonAsync<ObservableCollection<FuenteNutrienteResponse>>(
                    "api/fuente-nutriente/listar",
                    jsonOptions);

                return response ?? new ObservableCollection<FuenteNutrienteResponse>();
            }
            catch
            {
                return new ObservableCollection<FuenteNutrienteResponse>();
            }
        }

        public async Task<ObservableCollection<FuenteNutrienteAporteTablaResponse>> GetAportesTablaAsync()
        {
            try
            {
                var response = await httpClient.GetFromJsonAsync<ObservableCollection<FuenteNutrienteAporteTablaResponse>>(
                    "api/fuente-nutriente/aportes-tabla",
                    jsonOptions);

                return response ?? new ObservableCollection<FuenteNutrienteAporteTablaResponse>();
            }
            catch
            {
                return new ObservableCollection<FuenteNutrienteAporteTablaResponse>();
            }
        }

        public async Task<bool> CreateFuenteNutrienteAsync(FuenteNutrienteRequest fuente)
        {
            FuenteNutrienteResponse? creada = await CreateFuenteNutrienteConRespuestaAsync(fuente);

            return creada?.FuenteNutrientesId != null && creada.FuenteNutrientesId > 0;
        }

        public async Task<FuenteNutrienteResponse?> CreateFuenteNutrienteConRespuestaAsync(FuenteNutrienteRequest fuente)
        {
            try
            {
                HttpResponseMessage response = await httpClient.PostAsJsonAsync(
                    "api/fuente-nutriente/crear-con-elementos",
                    fuente,
                    jsonOptions);

                string jsonRespuesta = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode || string.IsNullOrWhiteSpace(jsonRespuesta))
                    return null;

                ApiDataResponse<FuenteNutrienteResponse>? respuesta =
                    JsonSerializer.Deserialize<ApiDataResponse<FuenteNutrienteResponse>>(
                        jsonRespuesta,
                        jsonOptions);

                if (respuesta?.Data != null)
                    return respuesta.Data;

                FuenteNutrienteResponse? respuestaDirecta =
                    JsonSerializer.Deserialize<FuenteNutrienteResponse>(
                        jsonRespuesta,
                        jsonOptions);

                return respuestaDirecta;
            }
            catch
            {
                return null;
            }
        }

        public async Task<bool> UpdateFuenteNutrienteAsync(FuenteNutrienteRequest fuente)
        {
            try
            {
                if (!fuente.FuenteNutrientesId.HasValue)
                    return false;

                var response = await httpClient.PutAsJsonAsync(
                    $"api/fuente-nutriente/editar-con-elementos/{fuente.FuenteNutrientesId.Value}",
                    fuente,
                    jsonOptions);

                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> DeleteFuenteNutrienteAsync(FuenteNutrienteRequest fuente)
        {
            try
            {
                if (!fuente.FuenteNutrientesId.HasValue)
                    return false;

                var response = await httpClient.DeleteAsync(
                    $"api/fuente-nutriente/eliminar/{fuente.FuenteNutrientesId.Value}");

                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> HabilitarEnmiendaCalcareaAsync(
            int fuenteNutrientesId,
            HabilitarEnmiendaCalcareaRequest request)
        {
            try
            {
                HttpResponseMessage response = await httpClient.PostAsJsonAsync(
                    $"api/fuente-nutriente/{fuenteNutrientesId}/habilitar-enmienda-calcarea",
                    request,
                    jsonOptions);

                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> DeshabilitarEnmiendaCalcareaAsync(int fuenteNutrientesId)
        {
            try
            {
                HttpResponseMessage response = await httpClient.PutAsync(
                    $"api/fuente-nutriente/deshabilitar-enmienda-calcarea/{fuenteNutrientesId}",
                    null);

                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> HabilitarFertilizacionMixtaAsync(int fuenteNutrientesId)
        {
            try
            {
                HttpResponseMessage response = await httpClient.PostAsync(
                    $"api/fuente-nutriente/habilitar-fertilizacion-mixta/{fuenteNutrientesId}",
                    null);

                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> DeshabilitarFertilizacionMixtaAsync(int fuenteNutrientesId)
        {
            try
            {
                HttpResponseMessage response = await httpClient.PutAsync(
                    $"api/fuente-nutriente/deshabilitar-fertilizacion-mixta/{fuenteNutrientesId}",
                    null);

                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        private class ApiDataResponse<T>
        {
            [JsonPropertyName("success")]
            public bool? Success { get; set; }

            [JsonPropertyName("message")]
            public string? Message { get; set; }

            [JsonPropertyName("mensaje")]
            public string? Mensaje { get; set; }

            [JsonPropertyName("data")]
            public T? Data { get; set; }
        }
    }
}