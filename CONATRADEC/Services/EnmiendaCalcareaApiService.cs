using CONATRADEC.Models;
using System;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace CONATRADEC.Services
{
    // ===========================================================
    // ======= SERVICIO: EnmiendaCalcareaApiService ==============
    // ===========================================================
    // Este servicio se encarga de consumir los endpoints relacionados
    // con el cálculo de enmiendas calcáreas.
    //
    // Endpoints usados:
    // GET  api/fuente-nutriente/enmiendas-calcareas
    // POST api/enmiendas-calcareas/calcular
    // ===========================================================

    class EnmiendaCalcareaApiService
    {
        // ===========================================================
        // =================== DEPENDENCIAS / ESTADO =================
        // ===========================================================

        // Cliente HTTP encargado de realizar las peticiones hacia la API.
        private readonly HttpClient httpClient;

        // Servicio auxiliar que proporciona la URL base de la API.
        private readonly UrlApiService urlApiService = new UrlApiService();

        // ===========================================================
        // ======================== CONSTRUCTOR ======================
        // ===========================================================

        public EnmiendaCalcareaApiService()
        {
            // Inicializa el cliente HTTP con la dirección base del servidor API.
            // Esto permite que las peticiones usen rutas relativas.
            httpClient = new HttpClient
            {
                BaseAddress = new Uri(urlApiService.BaseUrlApi)
            };
        }

        // ===========================================================
        // ====================== MÉTODOS PÚBLICOS ===================
        // ===========================================================

        // -----------------------------------------------------------
        // Método: GetEnmiendasCalcareasAsync
        // Descripción: Obtiene la lista de tipos de cal disponibles
        // para el cálculo de enmienda calcárea.
        // Retorna: ObservableCollection<ParametroEnmiendaCalcareaResponse>
        // -----------------------------------------------------------
        public async Task<ObservableCollection<ParametroEnmiendaCalcareaResponse>> GetEnmiendasCalcareasAsync()
        {
            try
            {
                var response =
                    await httpClient.GetFromJsonAsync<ObservableCollection<ParametroEnmiendaCalcareaResponse>>(
                        "api/fuente-nutriente/enmiendas-calcareas"
                    );

                return response ?? new ObservableCollection<ParametroEnmiendaCalcareaResponse>();
            }
            catch (Exception ex)
            {
                return new ObservableCollection<ParametroEnmiendaCalcareaResponse>();
            }
        }

        // -----------------------------------------------------------
        // Método: CalcularEnmiendaCalcareaAsync
        // Descripción: Envía los datos CICE y el tipo de cal seleccionado
        // para calcular la necesidad de encalado.
        // Parámetro: EnmiendaCalcareaCalcularRequest request
        // Retorna: EnmiendaCalcareaCalcularResponse? si la API responde correctamente.
        // -----------------------------------------------------------
        public async Task<EnmiendaCalcareaCalcularResponse?> CalcularEnmiendaCalcareaAsync(EnmiendaCalcareaCalcularRequest request)
        {
            try
            {
                var response = await httpClient.PostAsJsonAsync(
                    "api/enmiendas-calcareas/calcular",
                    request
                );

                if (!response.IsSuccessStatusCode)
                    return null;

                var resultado =
                    await response.Content.ReadFromJsonAsync<EnmiendaCalcareaCalcularResponse>();

                return resultado;
            }
            catch (Exception ex)
            {
                return null;
            }
        }
    }
}