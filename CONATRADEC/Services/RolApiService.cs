using CONATRADEC.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using static CONATRADEC.Models.UserResponse;

namespace CONATRADEC.Services
{
    // ===========================================================
    // ================= SERVICIO: RolApiService =================
    // ===========================================================
    // Este servicio encapsula todas las operaciones CRUD (Crear,
    // Leer, Actualizar y Eliminar) relacionadas con los Roles del
    // sistema, comunicándose con los endpoints del backend.
    class RolApiService
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

        public RolApiService()
        {
            // Inicializa el cliente HTTP con la dirección base de la API,
            // obtenida desde el servicio UrlApiService.
            httpClient = new HttpClient { BaseAddress = new Uri(urlApiService.BaseUrlApi) };
        }

        // ===========================================================
        // ====================== MÉTODOS PÚBLICOS ===================
        // ===========================================================

        // -----------------------------------------------------------
        // Método: GetRolAsync
        // Descripción:
        //   Obtiene la lista completa de roles disponibles desde la API.
        // Retorna:
        //   ObservableCollection<RolResponse> (vacía si hay error o sin datos).
        // -----------------------------------------------------------
        public async Task<ObservableCollection<RolResponse>> GetRolAsync()
        {
            try
            {
                // Realiza una solicitud GET al endpoint correspondiente
                // y convierte automáticamente la respuesta JSON en una colección de roles.
                var response = await httpClient.GetFromJsonAsync<ObservableCollection<RolResponse>>("api/Rol/listarRoles");

                // Devuelve la colección obtenida o una vacía si la respuesta fue nula.
                return response ?? new ObservableCollection<RolResponse>();
            }
            catch (Exception ex)
            {
                // Devuelve una lista vacía para evitar interrupciones en la UI.
                return new ObservableCollection<RolResponse>();
            }
        }

        // -----------------------------------------------------------
        // Método: CreateRolAsync
        // Descripción:
        //   Envía al servidor un nuevo rol para ser registrado.
        // Parámetros:
        //   rol: objeto RolRequest con los datos del rol a crear.
        // Retorna:
        //   true si la API respondió con éxito (código 2xx).
        // -----------------------------------------------------------
        public async Task<bool> CreateRolAsync(RolRequest rol)
        {
            try
            {
                // Envía el objeto RolRequest como JSON mediante una solicitud POST.
                var response = await httpClient.PostAsJsonAsync($"api/Rol/crearRol", rol);

                // Devuelve true si el servidor respondió con un código de éxito (200-299).
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        // -----------------------------------------------------------
        // Método: DeleteRolAsync
        // Descripción:
        //   Elimina un rol existente en la base de datos mediante su ID.
        // Parámetros:
        //   rol: objeto RolRequest que contiene el RolId a eliminar.
        // Retorna:
        //   true si la operación fue exitosa (código HTTP 2xx).
        // -----------------------------------------------------------
        public async Task<bool> DeleteRolAsync(RolRequest rol)
        {
            try
            {
                // Realiza una solicitud DELETE al endpoint con el ID del rol en la ruta.
                var response = await httpClient.DeleteAsync($"api/Rol/eliminarRol/{rol.RolId}");

                // Retorna true si la API confirmó la eliminación correctamente.
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        // -----------------------------------------------------------
        // Método: UpdateRolAsync
        // Descripción:
        //   Actualiza los datos de un rol existente en el servidor.
        // Parámetros:
        //   rol: objeto RolRequest con el ID y los nuevos valores.
        // Retorna:
        //   true si la API respondió con éxito (código HTTP 2xx).
        // -----------------------------------------------------------
        public async Task<bool> UpdateRolAsync(RolRequest rol)
        {
            try
            {
                // Envía el objeto actualizado como JSON mediante una solicitud PUT,
                // incluyendo el ID del rol en la URL.
                var response = await httpClient.PutAsJsonAsync($"api/Rol/editarRol/{rol.RolId}", rol);

                // Retorna true si el servidor confirma éxito (2xx).
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
        // (Actualmente no se requieren métodos privados en este servicio.)
    }
}
