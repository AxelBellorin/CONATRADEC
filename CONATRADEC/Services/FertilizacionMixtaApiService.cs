using CONATRADEC.Models;
using System;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace CONATRADEC.Services
{
    // ===========================================================
    // ======== SERVICIO: FertilizacionMixtaApiService ===========
    // ===========================================================
    // Este servicio se encarga de consumir los endpoints de
    // Fertilización Mixta.
    //
    // Funciones principales:
    // 1. Listar las fuentes de nutrientes configuradas para
    //    fertilización mixta.
    // 2. Enviar los elementos exportables y las fuentes seleccionadas
    //    para calcular la fertilización mixta.
    // ===========================================================

    class FertilizacionMixtaApiService
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

        public FertilizacionMixtaApiService()
        {
            // Inicializa el cliente HTTP con la dirección base del servidor API.
            httpClient = new HttpClient
            {
                BaseAddress = new Uri(urlApiService.BaseUrlApi)
            };
        }

        // ===========================================================
        // ====================== MÉTODOS PÚBLICOS ===================
        // ===========================================================

        // -----------------------------------------------------------
        // Método: ListarFuentesFertilizacionMixtaAsync
        // Descripción:
        //   Obtiene desde la API únicamente las fuentes de nutrientes
        //   configuradas para ser usadas en fertilización mixta.
        //
        // Endpoint:
        //   GET api/fuente-nutriente/listar-fertilizacion-mixta
        //
        // Retorna:
        //   ObservableCollection<FuenteNutrienteFertilizacionMixtaResponse>
        // -----------------------------------------------------------
        public async Task<ObservableCollection<FuenteNutrienteFertilizacionMixtaResponse>> ListarFuentesFertilizacionMixtaAsync()
        {
            try
            {
                var response =
                    await httpClient.GetFromJsonAsync<ObservableCollection<FuenteNutrienteFertilizacionMixtaResponse>>(
                        "api/fuente-nutriente/listar-fertilizacion-mixta"
                    );

                return response ?? new ObservableCollection<FuenteNutrienteFertilizacionMixtaResponse>();
            }
            catch (Exception ex)
            {
                // Devuelve una colección vacía para evitar interrupciones en la UI.
                return new ObservableCollection<FuenteNutrienteFertilizacionMixtaResponse>();
            }
        }

        // -----------------------------------------------------------
        // Método: CalcularAsync
        // Descripción:
        //   Envía a la API los elementos químicos exportables y las
        //   fuentes seleccionadas por el usuario para calcular la
        //   fertilización mixta.
        //
        // Endpoint:
        //   POST api/fertilizacion-mixta/calcular
        //
        // Parámetro:
        //   FertilizacionMixtaCalcularRequest request
        //
        // Retorna:
        //   FertilizacionMixtaCalculoResponse
        // -----------------------------------------------------------
        public async Task<FertilizacionMixtaCalculoResponse?> CalcularAsync(FertilizacionMixtaCalcularRequest request)
        {
            try
            {
                var response = await httpClient.PostAsJsonAsync(
                    "api/fertilizacion-mixta/calcular",
                    request
                );

                if (!response.IsSuccessStatusCode)
                {
                    string errorApi = await response.Content.ReadAsStringAsync();

                    return new FertilizacionMixtaCalculoResponse
                    {
                        Success = false,
                        Message = $"Error API ({(int)response.StatusCode}): {errorApi}"
                    };
                }

                FertilizacionMixtaCalculoResponse? resultado =
                    await response.Content.ReadFromJsonAsync<FertilizacionMixtaCalculoResponse>();

                if (resultado == null)
                {
                    return new FertilizacionMixtaCalculoResponse
                    {
                        Success = false,
                        Message = "La API respondió, pero no se pudo interpretar la respuesta."
                    };
                }

                resultado.Success = true;
                resultado.Message = "Cálculo realizado correctamente.";

                return resultado;
            }
            catch (Exception ex)
            {
                return new FertilizacionMixtaCalculoResponse
                {
                    Success = false,
                    Message = $"No se pudo conectar con la API: {ex.Message}"
                };
            }
        }

        // ===========================================================
        // ===================== MÉTODOS PRIVADOS ====================
        // ===========================================================
        // Actualmente no se requieren métodos privados.
    }
}