// Espacio de nombres que agrupa todos los modelos del proyecto CONATRADEC.
namespace CONATRADEC.Models
{
    // Clase que representa la estructura de la respuesta (Response)
    // devuelta por el servidor después de un proceso de inicio de sesión.
    // Contiene información del usuario autenticado y los tokens de acceso.
    public class LoginResponse
    {
        // ===========================================================
        // =============== CAMPOS PRIVADOS DE LA CLASE ===============
        // ===========================================================

        // Campo que almacena el identificador único del usuario.
        private int? id;

        // Campo que almacena el nombre de usuario con el que el usuario inició sesión.
        private string? username;

        // Campo que almacena el correo electrónico del usuario autenticado.
        private string? email;

        // Campo que almacena el primer nombre del usuario.
        private string? firstName;

        // Campo que almacena el apellido del usuario.
        private string? lastName;

        // Campo que almacena la URL o ruta de la imagen de perfil del usuario (si existe).
        private string? image;

        // Campo que almacena el token de acceso (JWT o similar),
        // utilizado para autenticar solicitudes posteriores al servidor.
        private string? accessToken;

        // Campo que almacena el token de actualización (refresh token),
        // utilizado para obtener un nuevo access token cuando el actual expira.
        private string? refreshToken;


        // ===========================================================
        // ============= PROPIEDADES PÚBLICAS CON ENCAPSULAMIENTO ====
        // ===========================================================

        // Propiedad pública para acceder o modificar el ID del usuario.
        public int? Id { get => id; set => id = value; }

        // Propiedad pública para acceder o modificar el nombre de usuario.
        public string? Username { get => username; set => username = value; }

        // Propiedad pública para acceder o modificar el correo electrónico del usuario.
        public string? Email { get => email; set => email = value; }

        // Propiedad pública para acceder o modificar el primer nombre del usuario.
        public string? FirstName { get => firstName; set => firstName = value; }

        // Propiedad pública para acceder o modificar el apellido del usuario.
        public string? LastName { get => lastName; set => lastName = value; }

        // Propiedad pública para acceder o modificar la imagen del usuario.
        public string? Image { get => image; set => image = value; }

        // Propiedad pública para acceder o modificar el token de acceso (Access Token).
        public string? AccessToken { get => accessToken; set => accessToken = value; }

        // Propiedad pública para acceder o modificar el token de actualización (Refresh Token).
        public string? RefreshToken { get => refreshToken; set => refreshToken = value; }
    }
}
