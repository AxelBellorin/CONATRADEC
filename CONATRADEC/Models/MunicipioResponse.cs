using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// Espacio de nombres donde se agrupan todos los modelos del proyecto CONATRADEC.
namespace CONATRADEC.Models
{
    // ===============================================================
    // Clase: MunicipioResponse
    // Descripción:
    //   Representa la estructura de respuesta (Response) utilizada para
    //   recibir información de la entidad "Municipio" desde la API.
    //   Este modelo refleja los datos devueltos por el servidor, usualmente
    //   al listar, obtener o consultar detalles de los municipios.
    // ===============================================================
    public class MunicipioResponse
    {
        // ===========================================================
        // =============== CAMPOS PRIVADOS DE LA CLASE ===============
        // ===========================================================

        // Campo que almacena el identificador único del municipio.
        // Nullable (int?) para manejar casos en que el valor no esté presente
        // o sea opcional (por ejemplo, al crear un nuevo registro).
        private int? municipioId;

        // Campo que almacena el nombre del municipio (por ejemplo: "San Ramón", "Sébaco").
        private string? nombreMunicipio;

        // Campo que almacena el identificador del departamento al que pertenece el municipio.
        private int? departamentoId;

        // ===========================================================
        // ============= PROPIEDADES PÚBLICAS CON ENCAPSULAMIENTO ====
        // ===========================================================

        // Propiedad pública que permite acceder o modificar el ID del municipio.
        public int? MunicipioId
        {
            get => municipioId;
            set => municipioId = value;
        }

        // Propiedad pública para acceder o modificar el nombre del municipio.
        public string? NombreMunicipio
        {
            get => nombreMunicipio;
            set => nombreMunicipio = value;
        }

        // Propiedad pública para acceder o modificar el ID del departamento asociado.
        public int? DepartamentoId
        {
            get => departamentoId;
            set => departamentoId = value;
        }

        // ===========================================================
        // ===================== CONSTRUCTOR =========================
        // ===========================================================

        // Constructor vacío.
        // Se usa al crear instancias sin datos iniciales
        // (por ejemplo, al inicializar un formulario o antes de asignar valores).
        public MunicipioResponse() { }
    }
}
