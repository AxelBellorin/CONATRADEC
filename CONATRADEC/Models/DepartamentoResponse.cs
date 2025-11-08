using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// Espacio de nombres donde se agrupan todos los modelos del proyecto CONATRADEC.
namespace CONATRADEC.Models
{
    // ===============================================================
    // Clase: DepartamentoResponse
    // Descripción:
    //   Representa la estructura de respuesta (Response) utilizada para
    //   recibir información de la entidad "Departamento" desde la API.
    //   Este modelo refleja los datos devueltos por el servidor, usualmente
    //   al listar, obtener o consultar detalles de departamentos.
    // ===============================================================
    public class DepartamentoResponse
    {
        // ===========================================================
        // =============== CAMPOS PRIVADOS DE LA CLASE ===============
        // ===========================================================

        // Campo que almacena el identificador único del departamento.
        // Nullable (int?) para manejar casos en que el valor no esté presente o sea opcional.
        private int? departamentoId;

        // Campo que almacena el nombre del departamento (por ejemplo: "Matagalpa", "León").
        private string? nombreDepartamento;

        // Campo que almacena el identificador del país al que pertenece el departamento.
        private int? paisId;

        // ===========================================================
        // ============= PROPIEDADES PÚBLICAS CON ENCAPSULAMIENTO ====
        // ===========================================================

        // Propiedad pública que permite acceder o modificar el ID del departamento.
        public int? DepartamentoId
        {
            get => departamentoId;
            set => departamentoId = value;
        }

        // Propiedad pública para acceder o modificar el nombre del departamento.
        public string? NombreDepartamento
        {
            get => nombreDepartamento;
            set => nombreDepartamento = value;
        }

        // Propiedad pública para acceder o modificar el país relacionado.
        public int? PaisId
        {
            get => paisId;
            set => paisId = value;
        }

        // Propiedad pública para acceder o modificar el estado (activo/inactivo).

        // ===========================================================
        // ===================== CONSTRUCTOR =========================
        // ===========================================================

        // Constructor vacío.
        // Se usa al crear instancias sin datos iniciales (por ejemplo, al inicializar un formulario).
        public DepartamentoResponse() { }
    }
}
