using CONATRADEC.Models;
using System;
using System.Collections.ObjectModel;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace CONATRADEC.Services
{
    // ===========================================================
    // ============ SERVICIO: UnidadMedidaApiService =============
    // ===========================================================
    // Este servicio encapsula las operaciones relacionadas con las
    // unidades de medida del sistema, comunicándose con los endpoints
    // del backend.
    class UnidadMedidaApiService
    {
        // ===========================================================
        // =================== DEPENDENCIAS / ESTADO =================
        // ===========================================================

        // Cliente HTTP utilizado para realizar solicitudes a la API.
        private readonly HttpClient httpClient;

        // Servicio auxiliar que proporciona la URL base de la API.
        private readonly UrlApiService urlApiService = new UrlApiService();

        // ===========================================================
        // ======================== CONSTRUCTOR ======================
        // ===========================================================

        public UnidadMedidaApiService()
        {
            // Inicializa el cliente HTTP con la dirección base de la API,
            // obtenida desde el servicio UrlApiService.
            httpClient = new HttpClient { BaseAddress = new Uri(urlApiService.BaseUrlApi) };
        }

        // ===========================================================
        // ====================== MÉTODOS PÚBLICOS ===================
        // ===========================================================

        // -----------------------------------------------------------
        // Método: GetUnidadMedidaAsync
        // Descripción:
        //   Obtiene la lista completa de unidades de medida desde la API.
        // Endpoint:
        //   GET api/unidad-medida/listar
        // Retorna:
        //   ObservableCollection<UnidadMedidaResponse>
        // -----------------------------------------------------------
        public async Task<ObservableCollection<UnidadMedidaResponse>> GetUnidadMedidaAsync()
        {
            try
            {
                var response = await httpClient.GetFromJsonAsync<ObservableCollection<UnidadMedidaResponse>>(
                    "api/unidad-medida/listar"
                );

                return response ?? new ObservableCollection<UnidadMedidaResponse>();
            }
            catch (Exception ex)
            {
                return new ObservableCollection<UnidadMedidaResponse>();
            }
        }

        // -----------------------------------------------------------
        // Método: CreateUnidadMedidaAsync
        // Descripción:
        //   Envía al servidor una nueva unidad de medida para registrar.
        // Nota:
        //   Se usará cuando creemos el CRUD.
        // -----------------------------------------------------------
        public async Task<bool> CreateUnidadMedidaAsync(UnidadMedidaRequest unidadMedida)
        {
            try
            {
                var response = await httpClient.PostAsJsonAsync(
                    "api/unidad-medida/crear",
                    unidadMedida
                );

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        // -----------------------------------------------------------
        // Método: UpdateUnidadMedidaAsync
        // Descripción:
        //   Actualiza los datos de una unidad de medida existente.
        // Nota:
        //   Se usará cuando creemos el CRUD.
        // -----------------------------------------------------------
        public async Task<bool> UpdateUnidadMedidaAsync(UnidadMedidaRequest unidadMedida)
        {
            try
            {
                var response = await httpClient.PutAsJsonAsync(
                    $"api/unidad-medida/editar/{unidadMedida.UnidadMedidaId}",
                    unidadMedida
                );

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        // -----------------------------------------------------------
        // Método: DeleteUnidadMedidaAsync
        // Descripción:
        //   Elimina o desactiva una unidad de medida mediante su ID.
        // Nota:
        //   Se usará cuando creemos el CRUD.
        // -----------------------------------------------------------
        public async Task<bool> DeleteUnidadMedidaAsync(UnidadMedidaRequest unidadMedida)
        {
            try
            {
                var response = await httpClient.DeleteAsync(
                    $"api/unidad-medida/eliminar/{unidadMedida.UnidadMedidaId}"
                );

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        // ===========================================================
        // ===================== MÉTODOS PRIVADOS ====================
        // ===========================================================
        // Actualmente no se requieren métodos privados en este servicio.
    }
}