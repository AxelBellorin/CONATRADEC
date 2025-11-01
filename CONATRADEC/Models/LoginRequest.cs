// Espacio de nombres que contiene los modelos utilizados en el proyecto CONATRADEC.
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
        private string? username;

        // Campo que almacena la contraseña del usuario.
        // También es nullable, aunque en la práctica siempre debería tener un valor al autenticar.
        private string? password;

        // Campo que indica el tiempo de expiración del token (en minutos) que se solicita al servidor.
        // Puede ser útil para personalizar la duración de la sesión del usuario.
        private int? expiresInMins;


        // ===========================================================
        // ============= PROPIEDADES PÚBLICAS CON ENCAPSULAMIENTO ====
        // ===========================================================

        // Propiedad pública para acceder o modificar el nombre de usuario.
        public string? Username { get => username; set => username = value; }

        // Propiedad pública para acceder o modificar la contraseña.
        public string? Password { get => password; set => password = value; }

        // Propiedad pública para acceder o modificar el tiempo de expiración del token.
        public int? ExpiresInMins { get => expiresInMins; set => expiresInMins = value; }
    }
}
