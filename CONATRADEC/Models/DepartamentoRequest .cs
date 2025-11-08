using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// Espacio de nombres donde se agrupan todos los modelos del proyecto CONATRADEC.
namespace CONATRADEC.Models
{
    // ===============================================================
    // Clase: DepartamentoRequest
    // Descripción:
    //   Representa la estructura de un objeto de solicitud (Request)
    //   utilizado para enviar o recibir información relacionada con la
    //   entidad "Departamento" hacia/desde la API.
    //   Se usa comúnmente en las operaciones CRUD (Crear, Editar, Eliminar).
    // ===============================================================
    public class DepartamentoRequest
    {
        // ===========================================================
        // =============== CAMPOS PRIVADOS DE LA CLASE ===============
        // ===========================================================

        // Campo que almacena el identificador único del departamento.
        // Se declara como nullable (int?) para permitir valores nulos
        // cuando el registro aún no ha sido creado en la base de datos.
        private int? departamentoId;

        // Campo que almacena el nombre del departamento (ejemplo: "Matagalpa", "León").
        private string? nombreDepartamento;

        // Campo que almacena el identificador del país al que pertenece el departamento.
        private int? paisId;


        // ===========================================================
        // ============= PROPIEDADES PÚBLICAS CON ENCAPSULAMIENTO ====
        // ===========================================================

        // Propiedad pública para acceder o modificar el ID del departamento.
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


        // ===========================================================
        // ================ CONSTRUCTORES DE LA CLASE ================
        // ===========================================================

        // Constructor vacío.
        // Permite crear una instancia sin inicializar los valores (por ejemplo, al crear un nuevo registro).
        public DepartamentoRequest() { }

        // Constructor que inicializa una nueva instancia de DepartamentoRequest
        // tomando como base un objeto de tipo DepartamentoResponse.
        // Esto facilita la conversión entre el modelo de respuesta de la API
        // y el modelo de solicitud, útil para operaciones de actualización.
        public DepartamentoRequest(DepartamentoResponse departamentoRP)
        {
            DepartamentoId = departamentoRP.DepartamentoId;
            NombreDepartamento = departamentoRP.NombreDepartamento;
            PaisId = departamentoRP.PaisId;
        }
    }
}
