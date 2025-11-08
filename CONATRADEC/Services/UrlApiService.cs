using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CONATRADEC.Services
{
    // ===========================================================
    // ==================== SERVICIO: UrlApiService ===============
    // ===========================================================
    // Este servicio actúa como una capa centralizada para definir
    // y exponer la URL base (BaseUrlApi) utilizada por los distintos
    // servicios API dentro de la aplicación CONATRADEC.
    //
    // Su propósito es evitar la duplicación de rutas base en los
    // distintos servicios (RolApiService, CargoApiService, etc.)
    // y facilitar los cambios entre entornos (producción, desarrollo, etc.).
    // ===========================================================
    public class UrlApiService
    {
        // ===========================================================
        // ==================== CAMPOS PRIVADOS ======================
        // ===========================================================

        // Constante que almacena la dirección base del servidor remoto (Azure).
        // Esta URL se utiliza cuando la aplicación se ejecuta en entorno de producción.
        private const string baseUrlApi = "https://conatradecnic.azurewebsites.net/";

        // Alternativa comentada: URL local usada durante el desarrollo y pruebas.
        // Puede activarse según necesidad para conectar con un servidor local.
        //private const string baseUrlApi = "https://localhost:7176/";

        // ===========================================================
        // ======================= CONSTRUCTOR =======================
        // ===========================================================
        // Constructor vacío (no requiere inicialización).
        // Se incluye para mantener la estructura estándar de los servicios.
        public UrlApiService()
        {
        }

        // ===========================================================
        // =================== PROPIEDADES PÚBLICAS ==================
        // ===========================================================

        // Propiedad de solo lectura que expone la URL base a otros servicios.
        // Retorna la constante baseUrlApi definida anteriormente.
        public string BaseUrlApi { get => baseUrlApi; }
    }
}
