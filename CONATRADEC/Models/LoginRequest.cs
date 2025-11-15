// Espacio de nombres que contiene los modelos utilizados en el proyecto CONATRADEC.
using System.Text.Json.Serialization;

namespace CONATRADEC.Models
{
    // Clase que representa la estructura del objeto de solicitud (Request)
    // utilizado para enviar las credenciales de inicio de sesión hacia la API.
    public class LoginRequest
    {
        // ===========================================================
        // =============== CAMPOS PRIVADOS DE LA CLASE ===============
        // ===========================================================

        // Campo que almacena el nombre de usuario o identificador con el que el usuario inicia sesión.
        // Se declara como nullable (?) para permitir que el valor sea opcional durante la creación del objeto.
        private string? nombreUsuario;

        // Campo que almacena la contraseña del usuario.
        // También es nullable, aunque en la práctica siempre debería tener un valor al autenticar.
        private string? claveUsuario;


        // ===========================================================
        // ============= PROPIEDADES PÚBLICAS CON ENCAPSULAMIENTO ====
        // ===========================================================

        // Propiedad pública para acceder o modificar el nombre de usuario.
        [JsonPropertyName("UsuarioOEmail")]
        public string? NombreUsuario { get => nombreUsuario; set => nombreUsuario = value; }

        // Propiedad pública para acceder o modificar la contraseña.
        [JsonPropertyName("Clave")]
        public string? ClaveUsuario { get => claveUsuario; set => claveUsuario = value; }

    }
}
