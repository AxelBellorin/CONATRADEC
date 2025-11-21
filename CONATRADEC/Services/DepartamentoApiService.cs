using CONATRADEC.Models;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Net.Http.Json;

namespace CONATRADEC.Services
{
    // ===========================================================
    // Servicio: DepartamentoApiService
    // - CRUD contra la API para "Departamento"
    // - Rutas en cada método (sin constantes), ajusta si difieren
    // ===========================================================
    class DepartamentoApiService
    {
        private readonly HttpClient httpClient;
        private readonly UrlApiService urlApiService = new UrlApiService();

        public DepartamentoApiService()
        {
            httpClient = new HttpClient
            {
                BaseAddress = new Uri(urlApiService.BaseUrlApi)
            };
        }

        // Listar
        public async Task<ObservableCollection<DepartamentoResponse>> GetDepartamentosAsync(int? paisId)
        {
            try
            {
                // Ajusta la ruta si tu API usa otra convención
                var response = await httpClient.GetFromJsonAsync<ObservableCollection<DepartamentoResponse>>($"api/departamento/por-pais/{paisId}");
                return response ?? new ObservableCollection<DepartamentoResponse>();
            }
            catch (Exception ex)
            {
                return new ObservableCollection<DepartamentoResponse>();
            }
        }

        // Crear
        public async Task<bool> CreateDepartamentoAsync(DepartamentoRequest departamentoRequest)
        {
            try
            {
                var response = await httpClient.PostAsJsonAsync("api/departamento/Crear", departamentoRequest);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        // Actualizar
        public async Task<bool> UpdateDepartamentoAsync(DepartamentoRequest departamento)
        {
            try
            {
                // Cambia "actualizar" si tu endpoint es diferente (p. ej. actualizarDepartamento1)
                var response = await httpClient.PutAsJsonAsync($"api/departamento/actualizar/{departamento.DepartamentoId}", departamento);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        // Eliminar
        public async Task<bool> DeleteDepartamentoAsync(DepartamentoRequest dpto)
        {
            try
            {
                // Cambia "eliminar" si tu endpoint es diferente (p. ej. eliminarDepartamento)
                var response = await httpClient.DeleteAsync($"api/departamento/eliminar/{dpto.DepartamentoId}");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
    }
}
