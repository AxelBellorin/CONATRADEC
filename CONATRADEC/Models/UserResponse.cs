using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// Espacio de nombres que agrupa todos los modelos del proyecto CONATRADEC.
namespace CONATRADEC.Models
{
    // ===========================================================
    // =============== MODELO DE USUARIO INDIVIDUAL ==============
    // ===========================================================

    // Clase que representa la estructura básica de un usuario (UserRP)
    // tal como se recibe desde la API o base de datos.
    // Este modelo se utiliza para mapear información individual del usuario.
    public class UserRP
    {
        // Campo que almacena el identificador único del usuario.
        private int id;

        // Campo que almacena el primer nombre del usuario.
        private string firstName;

        // Campo que almacena el apellido del usuario.
        private string lastName;

        // Campo que almacena la edad del usuario.
        private int age;

        // Campo que almacena el correo electrónico del usuario.
        private string email;

        // Campo que almacena la ruta o URL de la imagen del usuario.
        private string image;


        // ===========================================================
        // ============= PROPIEDADES PÚBLICAS CON ENCAPSULAMIENTO ====
        // ===========================================================

        // Propiedad pública para acceder o modificar el ID del usuario.
        public int Id { get => id; set => id = value; }

        // Propiedad pública para acceder o modificar el primer nombre del usuario.
        public string FirstName { get => firstName; set => firstName = value; }

        // Propiedad pública para acceder o modificar el apellido del usuario.
        public string LastName { get => lastName; set => lastName = value; }

        // Propiedad pública para acceder o modificar la edad del usuario.
        public int Age { get => age; set => age = value; }

        // Propiedad pública para acceder o modificar el correo electrónico del usuario.
        public string Email { get => email; set => email = value; }

        // Propiedad pública para acceder o modificar la imagen del usuario.
        public string Image { get => image; set => image = value; }
    }


    // ===========================================================
    // ============ MODELO DE RESPUESTA CON COLECCIÓN ============
    // ===========================================================

    // Clase que representa la respuesta completa de usuarios devuelta por la API.
    // Contiene una colección (lista) de objetos UserRP.
    // El uso del constructor con inicialización evita nulos en la lista.
    public class UserResponse()
    {
        // Propiedad pública que almacena una lista de usuarios.
        // Se inicializa automáticamente para evitar referencias nulas.
        public List<UserRP> Users { get; set; } = new();
    }
}
