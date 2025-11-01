using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// Espacio de nombres donde se agrupan todos los modelos del proyecto CONATRADEC.
namespace CONATRADEC.Models
{
    // Clase que representa la estructura de respuesta (Response)
    // utilizada para recibir información de la entidad "Cargo" desde la API.
    // Es decir, este modelo refleja los datos devueltos por el servidor.
    public class CargoResponse
    {
        // ===========================================================
        // =============== CAMPOS PRIVADOS DE LA CLASE ===============
        // ===========================================================

        // Campo que almacena el identificador único del cargo.
        // Nullable (int?) para manejar casos en que el valor no esté presente o sea opcional.
        private int? cargoId;

        // Campo que almacena el nombre del cargo (por ejemplo: "Gerente de Planta").
        private string? nombreCargo;

        // Campo que almacena la descripción detallada del cargo.
        private string? descripcionCargo;


        // ===========================================================
        // ============= PROPIEDADES PÚBLICAS CON ENCAPSULAMIENTO ====
        // ===========================================================

        // Propiedad pública que permite acceder o modificar el ID del cargo.
        public int? CargoId { get => cargoId; set => cargoId = value; }

        // Propiedad pública para acceder o modificar el nombre del cargo.
        public string? NombreCargo { get => nombreCargo; set => nombreCargo = value; }

        // Propiedad pública para acceder o modificar la descripción del cargo.
        public string? DescripcionCargo { get => descripcionCargo; set => descripcionCargo = value; }
    
        // Constructor vacio
        public CargoResponse() { }
    }
}
