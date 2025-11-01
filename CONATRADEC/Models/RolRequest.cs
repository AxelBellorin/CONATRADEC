using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// Espacio de nombres que agrupa todos los modelos del proyecto CONATRADEC.
namespace CONATRADEC.Models
{
    // Clase que representa la estructura del objeto de solicitud (Request)
    // utilizado para enviar información de roles hacia la API.
    public class RolRequest
    {
        // ===========================================================
        // =============== CAMPOS PRIVADOS DE LA CLASE ===============
        // ===========================================================

        // Campo que almacena el identificador único del rol.
        // Se declara como nullable (int?) para permitir valores nulos al crear un nuevo rol.
        private int? rolId;

        // Campo que almacena el nombre del rol (por ejemplo: "Administrador", "Supervisor").
        private string? nombreRol;

        // Campo que almacena la descripción o propósito del rol.
        private string? descripcionRol;


        // ===========================================================
        // ============= PROPIEDADES PÚBLICAS CON ENCAPSULAMIENTO ====
        // ===========================================================

        // Propiedad pública para acceder o modificar el ID del rol.
        public int? RolId { get => rolId; set => rolId = value; }

        // Propiedad pública para acceder o modificar el nombre del rol.
        public string? NombreRol { get => nombreRol; set => nombreRol = value; }

        // Propiedad pública para acceder o modificar la descripción del rol.
        public string? DescripcionRol { get => descripcionRol; set => descripcionRol = value; }


        // ===========================================================
        // ==================== CONSTRUCTORES ========================
        // ===========================================================

        // Constructor vacío: permite crear instancias sin inicializar propiedades.
        // Útil cuando se desea crear un nuevo rol desde cero.
        public RolRequest()
        {
        }

        // Constructor que inicializa una nueva instancia de RolRequest
        // a partir de un objeto de tipo RolResponse.
        // Facilita la conversión entre modelos de respuesta y de solicitud,
        // especialmente en escenarios de edición.
        public RolRequest(RolResponse rolRP)
        {
            // Copia el ID del rol desde el modelo de respuesta.
            RolId = rolRP.RolId;

            // Copia el nombre del rol desde el modelo de respuesta.
            NombreRol = rolRP.NombreRol;

            // Copia la descripción del rol desde el modelo de respuesta.
            DescripcionRol = rolRP.DescripcionRol;
        }
    }
}
