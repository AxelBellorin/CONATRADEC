using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CONATRADEC.Models;

// Espacio de nombres que agrupa los modelos del proyecto CONATRADEC.
namespace CONATRADEC.Models
{
    // Clase que representa la estructura de respuesta (Response)
    // devuelta por la API cuando se consulta la matriz de permisos de un rol específico.
    public class MatrizPermisosResponse
    {
        // ===========================================================
        // =============== CAMPOS PRIVADOS DE LA CLASE ===============
        // ===========================================================

        // Campo que almacena la información del rol asociado a la matriz.
        // Contiene propiedades como Id y Nombre del rol.
        private RolResponse rol;

        // Campo que almacena la colección de permisos (interfaces) asociadas a ese rol.
        // Se usa ObservableCollection para que los cambios en la lista
        // se reflejen automáticamente en la interfaz de usuario (binding en MVVM).
        private ObservableCollection<InterfazResponse> interfaz;


        // ===========================================================
        // ============= PROPIEDADES PÚBLICAS CON ENCAPSULAMIENTO ====
        // ===========================================================

        // Propiedad pública que expone la información del rol recibido desde la API.
        public RolResponse Rol { get => rol; set => rol = value; }

        // Propiedad pública que expone la colección de permisos del rol.
        // Cada elemento representa una interfaz con sus permisos (leer, agregar, etc.).
        public ObservableCollection<InterfazResponse> Interfaz { get => interfaz; set => interfaz = value; }
    }
}
