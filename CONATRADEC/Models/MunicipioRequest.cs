using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// Espacio de nombres donde se agrupan todos los modelos del proyecto CONATRADEC.
namespace CONATRADEC.Models
{
    // ===============================================================
    // Clase: MunicipioRequest
    // Descripción:
    //   Representa la estructura de un objeto de solicitud (Request)
    //   utilizado para enviar o recibir información relacionada con la
    //   entidad "Municipio" hacia/desde la API.
    //   Se usa comúnmente en las operaciones CRUD (Crear, Editar, Eliminar).
    // ===============================================================
    public class MunicipioRequest
    {
        // ===========================================================
        // =============== CAMPOS PRIVADOS DE LA CLASE ===============
        // ===========================================================

        // Campo que almacena el identificador único del municipio.
        // Se declara como nullable (int?) para permitir valores nulos
        // cuando el registro aún no ha sido creado en la base de datos.
        private int? municipioId;

        // Campo que almacena el nombre del municipio (por ejemplo: "San Ramón", "Sébaco").
        private string? nombreMunicipio;

        // Campo que almacena el identificador del departamento al que pertenece el municipio.
        private int? departamentoId;

        // ===========================================================
        // ============= PROPIEDADES PÚBLICAS CON ENCAPSULAMIENTO ====
        // ===========================================================

        // Propiedad pública para acceder o modificar el ID del municipio.
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

        // Propiedad pública para acceder o modificar el departamento relacionado.
        public int? DepartamentoId
        {
            get => departamentoId;
            set => departamentoId = value;
        }

        // ===========================================================
        // ================ CONSTRUCTORES DE LA CLASE ================
        // ===========================================================

        // Constructor vacío.
        // Permite crear una instancia sin inicializar los valores
        // (por ejemplo, al crear un nuevo municipio desde un formulario).
        public MunicipioRequest() { }

        // Constructor que inicializa una nueva instancia de MunicipioRequest
        // tomando como base un objeto de tipo MunicipioResponse.
        // Esto facilita la conversión entre el modelo de respuesta de la API
        // y el modelo de solicitud, útil para operaciones de actualización.
        public MunicipioRequest(MunicipioResponse municipioRP)
        {
            MunicipioId = municipioRP.MunicipioId;
            NombreMunicipio = municipioRP.NombreMunicipio;
            DepartamentoId = municipioRP.DepartamentoId;
        }
    }
}
