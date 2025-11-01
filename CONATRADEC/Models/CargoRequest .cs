using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// Espacio de nombres donde se agrupan todos los modelos del proyecto CONATRADEC.
namespace CONATRADEC.Models
{
    // Clase que representa la estructura de un objeto de solicitud (Request)
    // para enviar o recibir información relacionada con la entidad "Cargo" hacia/desde la API.
    public class CargoRequest
    {
        // ===========================================================
        // =============== CAMPOS PRIVADOS DE LA CLASE ===============
        // ===========================================================

        // Campo que almacena el identificador único del cargo.
        // Se declara como nullable (int?) para permitir valores nulos cuando el cargo aún no existe (por ejemplo, al crear uno nuevo).
        private int? cargoId;

        // Campo que almacena el nombre del cargo (por ejemplo: "Gerente", "Supervisor").
        // Nullable por si el dato no ha sido asignado o recibido.
        private string? nombreCargo;

        // Campo que almacena la descripción del cargo.
        private string? descripcionCargo;


        // ===========================================================
        // ============= PROPIEDADES PÚBLICAS CON ENCAPSULAMIENTO ====
        // ===========================================================

        // Propiedad pública para acceder o modificar el identificador del cargo.
        // Utiliza expresión lambda para simplificar el get y set.
        public int? CargoId { get => cargoId; set => cargoId = value; }

        // Propiedad pública para acceder o modificar el nombre del cargo.
        public string? NombreCargo { get => nombreCargo; set => nombreCargo = value; }

        // Propiedad pública para acceder o modificar la descripción del cargo.
        public string? DescripcionCargo { get => descripcionCargo; set => descripcionCargo = value; }


        // ===========================================================
        // ================ CONSTRUCTOR CON PARÁMETRO ================
        // ===========================================================

        // Constructor que inicializa una nueva instancia de CargoRequest
        // tomando como base un objeto de tipo CargoResponse.
        // Esto facilita la conversión entre el modelo de respuesta de la API
        // y el modelo de solicitud, útil para operaciones de actualización.
        public CargoRequest(CargoResponse cargoRP)
        {
            // Asigna el ID del cargo recibido desde el objeto de respuesta.
            CargoId = cargoRP.CargoId;

            // Asigna el nombre del cargo recibido desde el objeto de respuesta.
            NombreCargo = cargoRP.NombreCargo;

            // Asigna la descripción del cargo recibida desde el objeto de respuesta.
            DescripcionCargo = cargoRP.DescripcionCargo;
        }

        //    sería útil agregar un constructor vacío:
        public CargoRequest() { }
    }
}
