using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CONATRADEC.Models;

// Espacio de nombres que contiene los modelos del proyecto CONATRADEC.
namespace CONATRADEC.Models
{
    // Clase que representa la estructura del objeto de solicitud (Request)
    // utilizado para enviar la información completa de la matriz de permisos
    // desde la aplicación hacia la API.
    public class MatrizPermisosRequest
    {
        // ===========================================================
        // =============== CAMPOS PRIVADOS DE LA CLASE ===============
        // ===========================================================

        // Campo que almacena la información del rol asociado a la matriz.
        // Contiene datos como el identificador y el nombre del rol.
        private RolRequest rol;

        // Campo que almacena la lista de permisos asociados al rol.
        // Cada permiso representa una interfaz del sistema con sus acciones (leer, agregar, etc.).
        private List<InterfazRequest> permisos;


        // ===========================================================
        // ============= PROPIEDADES PÚBLICAS CON ENCAPSULAMIENTO ====
        // ===========================================================

        // Propiedad pública que expone el rol asociado a la matriz.
        public RolRequest Rol { get => rol; set => rol = value; }

        // Propiedad pública que expone la lista de permisos del rol.
        public List<InterfazRequest> Permisos { get => permisos; set => permisos = value; }


        // ===========================================================
        // ==================== CONSTRUCTORES ========================
        // ===========================================================

        // Constructor vacío: permite crear instancias sin datos iniciales.
        // Útil al construir manualmente la matriz o preparar un nuevo objeto para enviar.
        public MatrizPermisosRequest()
        {

        }

        // Constructor que inicializa una nueva instancia de MatrizPermisosRequest
        // a partir de un objeto MatrizPermisosResponse.
        // Este patrón facilita la conversión entre los modelos de respuesta y solicitud,
        // especialmente al editar o actualizar datos existentes.
        public MatrizPermisosRequest(MatrizPermisosResponse matrizPermisosResponse)
        {
            // Se crea un nuevo objeto RolRequest basado en la información del rol
            // contenida en la respuesta.
            Rol = new RolRequest(matrizPermisosResponse.Rol);

            // Se transforma la lista de InterfazResponse recibida en una lista de InterfazRequest,
            // mapeando cada elemento con su respectivo constructor.
            Permisos = matrizPermisosResponse.Interfaz
                .Select(p => new InterfazRequest(p))
                .ToList();
        }
    }
}
