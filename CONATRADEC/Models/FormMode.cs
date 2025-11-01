// Espacio de nombres que agrupa todos los modelos del proyecto CONATRADEC.
namespace CONATRADEC.Models
{
    // Clase contenedora para definir modos de funcionamiento de formularios dentro del sistema.
    // Se utiliza principalmente para indicar si un formulario está en modo creación, edición o solo lectura.
    public class FormMode
    {
        // Enumeración interna que define los distintos modos de un formulario.
        // Facilita la gestión de la lógica en la capa ViewModel o View (por ejemplo, habilitar/deshabilitar controles).
        public enum FormModeSelect
        {
            // Indica que el formulario se utiliza para crear un nuevo registro.
            Create,

            // Indica que el formulario se utiliza para editar un registro existente.
            Edit,

            // Indica que el formulario solo muestra información (sin permitir edición).
            View
        }
    }
}
