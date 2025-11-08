using CONATRADEC.Models;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Net.Http.Json;

namespace CONATRADEC.Services
{
    // ===========================================================
    // =============== SERVICIO: MunicipioApiService ==============
    // ===========================================================
    // Este servicio se encarga de realizar todas las operaciones
    // CRUD (Crear, Leer, Actualizar y Eliminar) sobre la entidad
    // "Municipio" consumiendo los endpoints correspondientes del
    // backend API de CONATRADEC.
    // 
    // Cada método maneja su propio try/catch y muestra mensajes
    // de error en pantalla mediante DisplayAlert, además de
    // devolver valores seguros (colección vacía o false) en caso
    // de falla, para evitar interrupciones en la interfaz de usuario.
    // ===========================================================
    class MunicipioApiService
    {
        // ===========================================================
        // =================== DEPENDENCIAS / ESTADO =================
        // ===========================================================

        // Cliente HTTP que realiza las peticiones hacia la API.
        private readonly HttpClient httpClient;

        // Servicio auxiliar que proporciona la URL base del backend.
        private readonly UrlApiService urlApiService = new UrlApiService();

        // ===========================================================
        // ======================== CONSTRUCTOR ======================
        // ===========================================================
        public MunicipioApiService()
        {
            // Inicializa el cliente HTTP y configura la URL base.
            // Esto permite realizar peticiones relativas (por ejemplo: "api/municipio/...").
            httpClient = new HttpClient
            {
                BaseAddress = new Uri(urlApiService.BaseUrlApi)
            };
        }

        // ===========================================================
        // ====================== MÉTODOS PÚBLICOS ===================
        // ===========================================================

        // -----------------------------------------------------------
        // Método: GetMunicipiosAsync
        // Descripción:
        //   Obtiene la lista de municipios pertenecientes a un
        //   departamento específico.
        // Parámetro:
        //   departamentoRequest -> objeto que contiene el ID del
        //   departamento desde el cual se listarán los municipios.
        // Retorna:
        //   ObservableCollection<MunicipioResponse> con los datos o
        //   una colección vacía si ocurre un error o no hay resultados.
        // -----------------------------------------------------------
        public async Task<ObservableCollection<MunicipioResponse>> GetMunicipiosAsync(DepartamentoRequest departamentoRequest)
        {
            try
            {
                // Realiza una solicitud GET al endpoint correspondiente.
                var response = await httpClient.GetFromJsonAsync<ObservableCollection<MunicipioResponse>>(
                    $"/por-departamento/{departamentoRequest.DepartamentoId}");

                // Devuelve la respuesta o una colección vacía si no hay datos.
                return response ?? new ObservableCollection<MunicipioResponse>();
            }
            catch (Exception ex)
            {
                // Muestra un mensaje descriptivo en caso de error.
                await Application.Current.MainPage.DisplayAlert("Error", ex.Message, "OK");
                return new ObservableCollection<MunicipioResponse>();
            }
        }

        // -----------------------------------------------------------
        // Método: CreateMunicipioAsync
        // Descripción:
        //   Envía un nuevo municipio al servidor para registrarlo
        //   en la base de datos.
        // Parámetro:
        //   municipio -> objeto con la información a crear.
        // Retorna:
        //   true si la operación fue exitosa; false en caso contrario.
        // -----------------------------------------------------------
        public async Task<bool> CreateMunicipioAsync(MunicipioRequest municipio)
        {
            try
            {
                // Envía la solicitud POST al endpoint.
                var response = await httpClient.PostAsJsonAsync("/crear", municipio);

                // Retorna true si la respuesta HTTP indica éxito (2xx).
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                // Muestra un mensaje de error al usuario.
                await Application.Current.MainPage.DisplayAlert("Error", ex.Message, "OK");
                return false;
            }
        }

        // -----------------------------------------------------------
        // Método: UpdateMunicipioAsync
        // Descripción:
        //   Actualiza la información de un municipio existente.
        // Parámetro:
        //   municipio -> objeto con los nuevos valores (incluye su ID).
        // Retorna:
        //   true si la operación fue exitosa; false en caso contrario.
        // -----------------------------------------------------------
        public async Task<bool> UpdateMunicipioAsync(MunicipioRequest municipio)
        {
            try
            {
                // Envía la solicitud PUT al endpoint con el ID del municipio.
                var response = await httpClient.PutAsJsonAsync(
                    $"/actualizar/{municipio.MunicipioId}", municipio);

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", ex.Message, "OK");
                return false;
            }
        }

        // -----------------------------------------------------------
        // Método: DeleteMunicipioAsync
        // Descripción:
        //   Elimina un municipio del servidor según su ID.
        // Parámetro:
        //   municipio -> objeto que contiene el ID del municipio a eliminar.
        // Retorna:
        //   true si la eliminación fue exitosa; false si falló.
        // -----------------------------------------------------------
        public async Task<bool> DeleteMunicipioAsync(MunicipioRequest municipio)
        {
            try
            {
                // Realiza una solicitud DELETE al endpoint correspondiente.
                var response = await httpClient.DeleteAsync(
                    $"/eliminar/{municipio.MunicipioId}");

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                // Muestra el error y devuelve false.
                await Application.Current.MainPage.DisplayAlert("Error", ex.Message, "OK");
                return false;
            }
        }
    }
}
