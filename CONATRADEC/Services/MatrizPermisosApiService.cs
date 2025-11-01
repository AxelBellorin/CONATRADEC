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
    // ========= SERVICIO: MatrizPermisosApiService ===============
    // ===========================================================
    // Encapsula llamadas HTTP relacionadas con:
    // - Consultar matriz de permisos por rol.
    // - Crear y eliminar roles.
    // - Guardar (PUT) la matriz de permisos.
    class MatrizPermisosApiService
    {
        // ===========================================================
        // =================== DEPENDENCIAS / ESTADO =================
        // ===========================================================
        private readonly HttpClient httpClient;                // Cliente HTTP para consumir la API.
        private readonly UrlApiService urlApiService = new UrlApiService(); // Servicio para obtener la BaseUrl de la API.

        // ===========================================================
        // ======================== CONSTRUCTOR ======================
        // ===========================================================
        public MatrizPermisosApiService()
        {
            // Inicializa el HttpClient con la URL base proveída por UrlApiService.
            httpClient = new HttpClient { BaseAddress = new Uri(urlApiService.BaseUrlApi) };
        }

        // ===========================================================
        // ====================== MÉTODOS PÚBLICOS ===================
        // ===========================================================

        // -----------------------------------------------------------
        // Método: GetMatrizByRolAsync
        // Descripción:
        //   Consulta la matriz de permisos para un rol específico por nombre.
        // Parámetros:
        //   rolRequest: contiene el NombreRol a enviar como querystring.
        // Retorna:
        //   ObservableCollection<MatrizPermisosResponse> (colección vacía si falla).
        // Manejo de errores:
        //   Diferencia entre errores de conexión, formato no JSON y excepciones genéricas.
        // -----------------------------------------------------------
        public async Task<ObservableCollection<MatrizPermisosResponse>> GetMatrizByRolAsync(RolRequest rolRequest)
        {
            try
            {
                // Codifica el nombre del rol para uso seguro en URL (manejo de espacios/acentos).
                var encodedRol = Uri.EscapeDataString(rolRequest.NombreRol);

                // Realiza GET al endpoint que retorna la matriz por nombre de rol.
                var response = await httpClient.GetFromJsonAsync<ObservableCollection<MatrizPermisosResponse>>(
                    $"api/rol-permisos/matriz-por-rol-nombre?nombreRol={encodedRol}");

                // Si la respuesta es nula, devuelve una colección vacía para evitar null reference.
                return response ?? new ObservableCollection<MatrizPermisosResponse>();
            }
            catch (HttpRequestException ex)
            {
                // Error típico de red/conectividad (timeout, DNS, etc.).
                await Application.Current.MainPage.DisplayAlert("Error de conexión", ex.Message, "OK");
                return new ObservableCollection<MatrizPermisosResponse>();
            }
            catch (NotSupportedException ex)
            {
                // El contenido devuelto no es JSON o no coincide con el tipo destino.
                await Application.Current.MainPage.DisplayAlert("Error de formato", "Respuesta no JSON.", "OK");
                return new ObservableCollection<MatrizPermisosResponse>();
            }
            catch (Exception ex)
            {
                // Cualquier otro error no contemplado arriba.
                await Application.Current.MainPage.DisplayAlert("Error inesperado", ex.Message, "OK");
                return new ObservableCollection<MatrizPermisosResponse>();
            }
        }

        // -----------------------------------------------------------
        // Método: GuardarMatrizAsync
        // Descripción:
        //   Envía al backend la lista de matrices de permisos (PUT masivo).
        // Parámetros:
        //   matrizPermisosRequest: lista de roles con sus permisos para actualización.
        // Retorna:
        //   true si la actualización fue exitosa; false en error.
        // -----------------------------------------------------------
        public async Task<bool> GuardarMatrizAsync(List<MatrizPermisosRequest> matrizPermisosRequest)
        {
            try
            {
                // Envía el payload como JSON mediante PUT al endpoint de actualización de permisos.
                var response = await httpClient.PutAsJsonAsync(
                    "api/rol-permisos/actualizar-permisos/",
                    matrizPermisosRequest);

                // Indica éxito si el código HTTP es 2xx.
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                // Muestra un mensaje de error y retorna false para indicar fallo.
                await Application.Current.MainPage.DisplayAlert("Error", $"{ex}", "OK");
                return false;
            }
        }

        // ===========================================================
        // ===================== MÉTODOS PRIVADOS ====================
        // ===========================================================
        // (No hay métodos privados en este servicio por ahora.)
    }
}
    