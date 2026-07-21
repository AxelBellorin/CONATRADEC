using System.Text.Json.Serialization;

namespace CONATRADEC.Models
{
    public class UserRequest
    {
        public int? UsuarioId { get; set; }
        public string? NombreUsuario { get; set; }
        public string? IdentificacionUsuario { get; set; }
        public string? NombreCompletoUsuario { get; set; }
        public string? CorreoUsuario { get; set; }
        public string? TelefonoUsuario { get; set; }
        public DateOnly? FechaNacimientoUsuario { get; set; }
        public int? RolId { get; set; }
        public int? ProcedenciaId { get; set; }
        public int? MunicipioId { get; set; }
        public string? RolNombre { get; set; }
        public string? ProcedenciaNombre { get; set; }
        public string? UrlImagenUsuario { get; set; }
        public bool? EsInterno { get; set; }

        // Se utiliza únicamente al crear un usuario.
        [JsonPropertyName("clave")]
        public string? ClaveUsuario { get; set; }

        // Se utiliza únicamente al editar cuando se desea cambiar la contraseña.
        // Si se envía null o vacío, el backend conserva la contraseña actual.
        [JsonPropertyName("nuevaClave")]
        public string? NuevaClaveUsuario { get; set; }

        public UserRequest()
        {
        }

        public UserRequest(UserResponse user)
        {
            UsuarioId = user.UsuarioId;
            NombreUsuario = user.NombreUsuario;
            IdentificacionUsuario = user.IdentificacionUsuario;
            NombreCompletoUsuario = user.NombreCompletoUsuario;
            CorreoUsuario = user.CorreoUsuario;
            TelefonoUsuario = user.TelefonoUsuario;
            FechaNacimientoUsuario = user.FechaNacimientoUsuario;
            RolId = user.RolId;
            ProcedenciaId = user.ProcedenciaId;
            MunicipioId = user.MunicipioId;
            RolNombre = user.RolNombre;
            ProcedenciaNombre = user.ProcedenciaNombre;
            UrlImagenUsuario = user.UrlImagenUsuario;
            EsInterno = user.EsInterno;

            // Nunca se carga una contraseña desde una respuesta.
            ClaveUsuario = string.Empty;
            NuevaClaveUsuario = string.Empty;
        }
    }
}
