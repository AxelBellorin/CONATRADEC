using CONATRADEC.Services;
using System.Text.Json.Serialization;

namespace CONATRADEC.Models
{
    /// <summary>
    /// Respuesta devuelta por la API después de iniciar sesión.
    /// </summary>
    public class LoginResponse
    {
        public int? UsuarioId { get; set; }
        public string? NombreUsuario { get; set; }
        public string? IdentificacionUsuario { get; set; }
        public string? NombreCompletoUsuario { get; set; }
        public string? CorreoUsuario { get; set; }
        public int? RolId { get; set; }
        public int? ProcedenciaId { get; set; }
        public string? RolNombre { get; set; }
        public string? ProcedenciaNombre { get; set; }
        public bool? EsInterno { get; set; }

        [JsonPropertyName("token")]
        public string? AccessToken { get; set; }

        [JsonPropertyName("expiraTokenUtc")]
        public DateTime? ExpiraTokenUtc { get; set; }

        [JsonPropertyName("minutosInactividad")]
        public int MinutosInactividad { get; set; } = 15;

        public string? UrlImagenUsuario { get; set; }

        /// <summary>
        /// Versión que permite detectar cambios de rol o permisos.
        /// </summary>
        public int VersionSesion { get; set; } = 1;

        public List<UserPermissionDTO>? permisos { get; set; }
    }
}
