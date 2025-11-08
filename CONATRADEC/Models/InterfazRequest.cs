using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CONATRADEC.Models;

// Espacio de nombres que agrupa todos los modelos utilizados dentro del proyecto CONATRADEC.
namespace CONATRADEC.Models
{
    // Clase que representa una solicitud (Request) para enviar información
    // relacionada con las interfaces o permisos del sistema hacia la API.
    // Hereda de la clase "Permiso", por lo que incluye automáticamente
    // las propiedades de control de acceso: Leer, Agregar, Actualizar y Eliminar.
    public class InterfazRequest : Permiso
    {
        // ===========================================================
        // =============== CAMPOS PRIVADOS DE LA CLASE ===============
        // ===========================================================

        // Campo que almacena el identificador único del permiso o interfaz.
        private int interfazId;

        // Campo que almacena el nombre de la interfaz o permiso del sistema.
        // Se inicializa con una cadena vacía para evitar valores nulos.
        private string nombreInterfaz = string.Empty;


        // ===========================================================
        // ============= PROPIEDADES PÚBLICAS CON ENCAPSULAMIENTO ====
        // ===========================================================

        // Propiedad pública para acceder o modificar el ID del permiso.
        public int InterfazId { get => interfazId; set => interfazId = value; }

        // Propiedad pública para acceder o modificar el nombre de la interfaz o permiso.
        public string NombreInterfaz { get => nombreInterfaz; set => nombreInterfaz = value; }


        // ===========================================================
        // ==================== CONSTRUCTORES ========================
        // ===========================================================

        // Constructor vacío: permite crear una instancia del modelo sin datos iniciales.
        // Es útil para inicializar formularios o declarar objetos antes de asignar valores.
        public InterfazRequest()
        {
        }

        // Constructor que inicializa una nueva instancia de InterfazRequest
        // tomando los datos desde un objeto InterfazResponse.
        // Este patrón es muy útil al transformar los datos recibidos de la API (Response)
        // en un modelo que se utilizará para enviar nuevamente al servidor (Request).
        public InterfazRequest(InterfazResponse interfazResponse)
        {
            // Asigna el ID del permiso recibido.
            InterfazId = interfazResponse.InterfazId;

            // Asigna el nombre del permiso recibido.
            NombreInterfaz = interfazResponse.NombreInterfaz;

            // Las siguientes asignaciones copian los valores de acceso
            // (heredados de la clase base Permiso) desde el modelo de respuesta.
            Leer = interfazResponse.Leer;
            Agregar = interfazResponse.Agregar;
            Actualizar = interfazResponse.Actualizar;
            Eliminar = interfazResponse.Eliminar;
        }
    }
}
