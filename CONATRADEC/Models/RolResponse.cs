using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// Espacio de nombres que contiene los modelos del proyecto CONATRADEC.
namespace CONATRADEC.Models
{
    // Clase que representa la estructura de respuesta (Response)
    // que la API devuelve al consultar la información de un rol.
    // Contiene las propiedades básicas que identifican y describen al rol.
    public class RolResponse
    {
        // ===========================================================
        // =============== CAMPOS PRIVADOS DE LA CLASE ===============
        // ===========================================================

        // Campo que almacena el identificador único del rol.
        // Nullable (int?) para permitir compatibilidad con respuestas
        // que puedan omitir este campo o cuando el valor aún no esté asignado.
        private int? rolId;

        // Campo que almacena el nombre del rol (por ejemplo: "Administrador").
        private string? nombreRol;

        // Campo que almacena la descripción del rol (por ejemplo: "Tiene acceso a todos los módulos").
        private string? descripcionRol;


        // ===========================================================
        // ============= PROPIEDADES PÚBLICAS CON ENCAPSULAMIENTO ====
        // ===========================================================

        // Propiedad pública para acceder o modificar el identificador del rol.
        public int? RolId { get => rolId; set => rolId = value; }

        // Propiedad pública para acceder o modificar el nombre del rol.
        public string? NombreRol { get => nombreRol; set => nombreRol = value; }

        // Propiedad pública para acceder o modificar la descripción del rol.
        public string? DescripcionRol { get => descripcionRol; set => descripcionRol = value; }

        // Útil cuando se desea crear un nuevo rol desde cero.
        public RolResponse() { }
    }
}
