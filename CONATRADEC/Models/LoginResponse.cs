// Espacio de nombres que agrupa todos los modelos del proyecto CONATRADEC.
using CONATRADEC.Services;

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
        private int? usuarioId;

        // Campo que almacena el nombre de usuario con el que el usuario inició sesión.
        private string? nombreUsuario;

        // Campo que almacena la identificacion del usuario autenticado.
        private string? identificacionUsuario;

        // Campo que almacena el nombre completo del usuario autenticado.
        private string? nombreCompletoUsuario;

        // Campo que almacena el correo electrónico del usuario autenticado.
        private string? correoUsuario;

        //Campo que almacena el rol del usuario autenticado.
        private int? rolId;

        // Campo que almacena el nombre del rol asignado al usuario.
        private string? rolNombre;


        //Campo que almacena el rol del usuario autenticado.
        private int? procedenciaId;

        // Campo que almacena el nombre del la procedencia asignado al usuario.
        private string? procedenciaNombre;

        private bool? esInterno;


        // Campo que almacena la URL o ruta de la imagen de perfil del usuario (si existe).
        //private string? image;

        // Campo que almacena el token de acceso (JWT o similar),
        // utilizado para autenticar solicitudes posteriores al servidor.
        private string? accessToken;

        // Campo que almacena el token de actualización (refresh token),
        // utilizado para obtener un nuevo access token cuando el actual expira.
        //private string? refreshToken;


        // ===========================================================
        // ============= PROPIEDADES PÚBLICAS CON ENCAPSULAMIENTO ====
        // ===========================================================

        public int? UsuarioId { get => usuarioId; set => usuarioId = value; }
        public string? NombreUsuario { get => nombreUsuario; set => nombreUsuario = value; }
        public string? IdentificacionUsuario { get => identificacionUsuario; set => identificacionUsuario = value; }
        public string? NombreCompletoUsuario { get => nombreCompletoUsuario; set => nombreCompletoUsuario = value; }
        public string? CorreoUsuario { get => correoUsuario; set => correoUsuario = value; }
        public int? RolId { get => rolId; set => rolId = value; }
        public int? ProcedenciaId { get => procedenciaId; set => procedenciaId = value; }
        public string? RolNombre { get => rolNombre; set => rolNombre = value; }
        public string? ProcedenciaNombre { get => procedenciaNombre; set => procedenciaNombre = value; }
        public bool? EsInterno { get => esInterno; set => esInterno = value; }
        public string? AccessToken { get => accessToken; set => accessToken = value; }

        public List<UserPermissionDTO>? permisos { get; set; }
        public LoginResponse()
        {
        }
    }
}
