using System.Text.Json.Serialization;

namespace CONATRADEC.Models
{
    public sealed class UserResponse
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
        public bool? EsInterno { get; set; }
        public string? UrlImagenUsuario { get; set; }

        [JsonPropertyName("ClaveHashUsuario")]
        public string? ClaveUsuario { get; set; }

        public string NombreMostrar =>
            string.IsNullOrWhiteSpace(NombreCompletoUsuario)
                ? NombreUsuario?.Trim() ?? "Usuario"
                : NombreCompletoUsuario.Trim();

        public string RolMostrar =>
            string.IsNullOrWhiteSpace(RolNombre)
                ? "Sin rol"
                : RolNombre.Trim();

        public string ProcedenciaMostrar =>
            string.IsNullOrWhiteSpace(ProcedenciaNombre)
                ? "Sin procedencia"
                : ProcedenciaNombre.Trim();

        public string CorreoMostrar =>
            string.IsNullOrWhiteSpace(CorreoUsuario)
                ? "Sin correo registrado"
                : CorreoUsuario.Trim();

        public string IdentificacionMostrar =>
            string.IsNullOrWhiteSpace(IdentificacionUsuario)
                ? "Sin identificación"
                : IdentificacionUsuario.Trim();

        public bool EsAdministradorProtegido =>
            string.Equals(
                RolNombre?.Trim(),
                "Administrador",
                StringComparison.OrdinalIgnoreCase);

        public bool PuedeDesactivar => !EsAdministradorProtegido;

        public string Iniciales
        {
            get
            {
                string texto = NombreMostrar;
                string[] partes = texto.Split(
                    ' ',
                    StringSplitOptions.RemoveEmptyEntries);

                if (partes.Length == 0)
                    return "US";

                if (partes.Length == 1)
                {
                    return partes[0][..Math.Min(2, partes[0].Length)]
                        .ToUpperInvariant();
                }

                return string.Concat(
                    partes[0][0],
                    partes[^1][0])
                    .ToUpperInvariant();
            }
        }

        public bool TieneImagen =>
            !string.IsNullOrWhiteSpace(UrlImagenUsuario);
    }
}
