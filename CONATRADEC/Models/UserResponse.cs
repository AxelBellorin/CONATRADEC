using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

// Espacio de nombres que agrupa todos los modelos del proyecto CONATRADEC.
namespace CONATRADEC.Models
{
    // ===========================================================
    // =============== MODELO DE USUARIO INDIVIDUAL ==============
    // ===========================================================

    // Clase que representa la estructura básica de un usuario (UserRP)
    // tal como se recibe desde la API o base de datos.
    // Este modelo se utiliza para mapear información individual del usuario.
    public class UserResponse
    {
        private int? usuarioId;

        // Campo que almacena el nombre de usuario con el que el usuario inició sesión.
        private string? nombreUsuario;

        // Campo que almacena la clave de usuario con el que el usuario inició sesión.
        private string? claveUsuario;

        // Campo que almacena la identificacion del usuario autenticado.
        private string? identificacionUsuario;

        // Campo que almacena el nombre completo del usuario autenticado.
        private string? nombreCompletoUsuario;

        // Campo que almacena el correo electrónico del usuario autenticado.
        private string? correoUsuario;

        //Campo que almacena el correo electrónico del usuario autenticado.
        private string? telefonoUsuario;

        //Campo que almacena la fecha de nacimiento del usuario autenticado.
        private DateOnly? fechaNacimientoUsuario;

        //Campo que almacena el rol del usuario autenticado.
        private int? rolId;

        //Campo que almacena el rol del usuario autenticado.
        private int? procedenciaId;

        //Campo que almacena el municipio del usuario autenticado.
        private int? municipioId;

        // Campo que almacena el nombre del rol asignado al usuario.
        private string? rolNombre;

        // Campo que almacena el nombre del la procedencia asignado al usuario.
        private string? procedenciaNombre;

        // Campo que almacena la url de la imagen asignado al usuario.
        private string? urlImagenUsuario;

        private bool? esInterno;

        // ===========================================================
        // ============= PROPIEDADES PÚBLICAS CON ENCAPSULAMIENTO ====
        // ===========================================================

        public int? UsuarioId { get => usuarioId; set => usuarioId = value; }
        public string? NombreUsuario { get => nombreUsuario; set => nombreUsuario = value; }
        public string? IdentificacionUsuario { get => identificacionUsuario; set => identificacionUsuario = value; }
        public string? NombreCompletoUsuario { get => nombreCompletoUsuario; set => nombreCompletoUsuario = value; }
        public string? CorreoUsuario { get => correoUsuario; set => correoUsuario = value; }
        public string? TelefonoUsuario { get => telefonoUsuario; set => telefonoUsuario = value; }
        public DateOnly? FechaNacimientoUsuario { get => fechaNacimientoUsuario; set => fechaNacimientoUsuario = value; }
        public int? RolId { get => rolId; set => rolId = value; }
        public int? ProcedenciaId { get => procedenciaId; set => procedenciaId = value; }
        public int? MunicipioId { get => municipioId; set => municipioId = value; }
        public string? RolNombre { get => rolNombre; set => rolNombre = value; }
        public string? ProcedenciaNombre { get => procedenciaNombre; set => procedenciaNombre = value; }
        public bool? EsInterno { get => esInterno; set => esInterno = value; }
        public string? UrlImagenUsuario { get => urlImagenUsuario; set => urlImagenUsuario = value; }

        // Propiedad pública para acceder o modificar la contraseña.
        [JsonPropertyName("ClaveHashUsuario")]
        public string? ClaveUsuario { get => claveUsuario; set => claveUsuario = value; }
    }


    // ===========================================================
    // ============ MODELO DE RESPUESTA CON COLECCIÓN ============
    // ===========================================================

    // Clase que representa la respuesta completa de usuarios devuelta por la API.
    // Contiene una colección (lista) de objetos UserRP.
    // El uso del constructor con inicialización evita nulos en la lista.
    //public class UserResponse()
    //{
    //    // Propiedad pública que almacena una lista de usuarios.
    //    // Se inicializa automáticamente para evitar referencias nulas.
    //    public List<UserRP> Users { get; set; } = new();
    //}
}
