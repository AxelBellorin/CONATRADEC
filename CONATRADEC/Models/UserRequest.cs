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
    // utilizada para enviar información de un usuario hacia la API.
    public class UserRequest
    {
        // ===========================================================
        // =============== CAMPOS PRIVADOS DE LA CLASE ===============
        // ===========================================================

        // Campo que almacena el identificador único del usuario.
        // Nullable (int?) para permitir la creación de nuevos usuarios sin ID asignado aún.
        private int? id;

        // Campo que almacena el primer nombre del usuario.
        private string? firstName;

        // Campo que almacena el apellido del usuario.
        private string? lastName;

        // Campo que almacena la edad del usuario.
        private int? age;

        // Campo que almacena el correo electrónico del usuario.
        private string? email;

        // Campo que almacena la ruta o URL de la imagen de perfil del usuario.
        private string? image;


        // ===========================================================
        // ============= PROPIEDADES PÚBLICAS CON ENCAPSULAMIENTO ====
        // ===========================================================

        // Propiedad pública para acceder o modificar el ID del usuario.
        public int? Id { get => id; set => id = value; }

        // Propiedad pública para acceder o modificar el primer nombre.
        public string? FirstName { get => firstName; set => firstName = value; }

        // Propiedad pública para acceder o modificar el apellido.
        public string? LastName { get => lastName; set => lastName = value; }

        // Propiedad pública para acceder o modificar la edad.
        public int? Age { get => age; set => age = value; }

        // Propiedad pública para acceder o modificar el correo electrónico.
        public string? Email { get => email; set => email = value; }

        // Propiedad pública para acceder o modificar la imagen de perfil.
        public string? Image { get => image; set => image = value; }


        // ===========================================================
        // ==================== CONSTRUCTORES ========================
        // ===========================================================

        // Constructor vacío: permite crear instancias vacías del modelo.
        // Útil cuando se desea inicializar manualmente los valores después.
        public UserRequest() { }

        // Constructor que inicializa una nueva instancia de UserRequest
        // a partir de un objeto de tipo UserRP (respuesta del servidor).
        // Facilita la conversión entre los modelos de respuesta y solicitud.
        public UserRequest(UserRP user)
        {
            // Copia el ID del usuario.
            Id = user.Id;

            // Copia el nombre del usuario.
            FirstName = user.FirstName;

            // Copia el apellido del usuario.
            LastName = user.LastName;

            // Copia la edad del usuario.
            Age = user.Age;

            // Copia el correo electrónico del usuario.
            Email = user.Email;

            // Copia la imagen del usuario correctamente.
            Image = user.Image;
        }
    }
}
