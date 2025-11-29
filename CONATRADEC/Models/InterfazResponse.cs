using CONATRADEC.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// Espacio de nombres que contiene los modelos del proyecto CONATRADEC.
namespace CONATRADEC.Models
{
    // Clase que representa la estructura de respuesta (Response)
    // para los permisos o interfaces obtenidas desde la API.
    // Hereda de la clase "Permiso", lo que le otorga las propiedades:
    // Leer, Agregar, Actualizar, Eliminar e IsDirty (indicador de cambios).
    public class InterfazResponse : Permiso
    {
        // ===========================================================
        // =============== CAMPOS PRIVADOS DE LA CLASE ===============
        // ===========================================================

        // Campo que almacena el identificador único del permiso o interfaz.
        private int interfazId;

        // Campo que almacena el nombre del permiso o interfaz (por ejemplo: "usuarioPage", "rolPage", etc.).
        // Se inicializa con una cadena vacía para evitar valores nulos.
        private string nombreInterfaz = string.Empty;

        private bool _isExpanded;


        // ===========================================================
        // ============= PROPIEDADES PÚBLICAS CON NOTIFICACIÓN =======
        // ===========================================================

        // Propiedad pública para acceder o modificar el ID del permiso.
        // Incluye OnPropertyChanged() para notificar cambios en la interfaz de usuario (binding).
        public int InterfazId { get => interfazId; set { interfazId = value; OnPropertyChanged(); } }

        // Propiedad pública para acceder o modificar el nombre del permiso.
        // También notifica cambios al UI cuando su valor cambia.
        public string NombreInterfaz { get => nombreInterfaz; set { nombreInterfaz = value; OnPropertyChanged(); } }

        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                if (_isExpanded == value)
                    return;

                _isExpanded = value;
                OnPropertyChanged(nameof(IsExpanded));
            }
        }


        // ===========================================================
        // ==================== CONSTRUCTORES ========================
        // ===========================================================

        // Constructor principal: permite inicializar un objeto InterfazResponse
        // con todos los valores requeridos. Ideal para poblar la matriz de permisos.
        public InterfazResponse(int id, string nombre, bool leer, bool agregar, bool actualizar, bool eliminar)
        {
            // Asignación de propiedades básicas del permiso.
            InterfazId = id;
            NombreInterfaz = nombre;

            // Propiedades heredadas desde la clase base "Permiso".
            Leer = leer;
            Agregar = agregar;
            Actualizar = actualizar;
            Eliminar = eliminar;

            // Marca el objeto como no modificado (sin cambios pendientes).
            IsDirty = false;
        }

        // Constructor vacío: necesario para inicializaciones sin parámetros
        // (por ejemplo, deserialización JSON o creación manual en código).
        public InterfazResponse()
        {

        }


        // ===========================================================
        // ====================== MÉTODOS ============================
        // ===========================================================

        // Método que permite asignar el mismo valor (true/false)
        // a todas las propiedades de permisos (leer, agregar, actualizar, eliminar).
        // Es útil cuando se desea marcar o desmarcar todos los permisos de una fila o columna en la matriz.
        public void SetAll(bool valor)
        {
            Leer = valor;
            Agregar = valor;
            Actualizar = valor;
            Eliminar = valor;
        }

        // Método que establece el indicador "IsDirty" en false.
        // Se utiliza después de guardar los cambios, indicando que el objeto ya está sincronizado con la base de datos.
        public void AcceptChanges() => IsDirty = false;
    }
}
