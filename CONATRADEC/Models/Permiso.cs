using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

// Espacio de nombres que agrupa todos los modelos del proyecto CONATRADEC.
namespace CONATRADEC.Models
{
    // Clase base que representa los permisos de una interfaz o rol.
    // Implementa la interfaz INotifyPropertyChanged para permitir la notificación
    // automática de cambios en las propiedades, lo cual es esencial para el patrón MVVM en MAUI.
    public class Permiso : INotifyPropertyChanged
    {
        // ===========================================================
        // =============== EVENTOS Y MÉTODOS DE NOTIFICACIÓN =========
        // ===========================================================

        // Evento que se dispara cada vez que cambia el valor de una propiedad.
        // Las vistas (Views) que están enlazadas a estas propiedades se actualizan automáticamente.
        public event PropertyChangedEventHandler? PropertyChanged;

        // Método que invoca el evento PropertyChanged.
        // Usa [CallerMemberName] para obtener automáticamente el nombre de la propiedad que lo llamó.
        public void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));


        // ===========================================================
        // =============== CAMPOS PRIVADOS DE LA CLASE ===============
        // ===========================================================

        // Campo que indica si el rol o interfaz tiene permiso de lectura.
        private bool leer;

        // Campo que indica si el rol o interfaz tiene permiso de agregar.
        private bool agregar;

        // Campo que indica si el rol o interfaz tiene permiso de actualización.
        private bool actualizar;

        // Campo que indica si el rol o interfaz tiene permiso de eliminación.
        private bool eliminar;

        // Campo que indica si el objeto ha sido modificado (sucio o "dirty").
        // Es útil para determinar cuándo hay cambios pendientes por guardar.
        private bool isDirty;


        // ===========================================================
        // ============= PROPIEDADES PÚBLICAS CON NOTIFICACIÓN =======
        // ===========================================================

        // Propiedad pública para leer o modificar el permiso de lectura.
        // Si el valor cambia, se notifica al UI y se marca el objeto como modificado.
        public bool Leer
        {
            get => leer;
            set
            {
                if (leer == value) return;         // Evita notificaciones innecesarias si el valor no cambió.
                leer = value;
                OnPropertyChanged();               // Notifica al UI que el valor cambió.
                IsDirty = true;                    // Marca el objeto como modificado.
            }
        }

        // Propiedad pública para leer o modificar el permiso de agregado.
        public bool Agregar
        {
            get => agregar;
            set
            {
                if (agregar == value) return;
                agregar = value;
                OnPropertyChanged();
                IsDirty = true;
            }
        }

        // Propiedad pública para leer o modificar el permiso de actualización.
        public bool Actualizar
        {
            get => actualizar;
            set
            {
                if (actualizar == value) return;
                actualizar = value;
                OnPropertyChanged();
                IsDirty = true;
            }
        }

        // Propiedad pública para leer o modificar el permiso de eliminación.
        public bool Eliminar
        {
            get => eliminar;
            set
            {
                if (eliminar == value) return;
                eliminar = value;
                OnPropertyChanged();
                IsDirty = true;
            }
        }

        // Propiedad que indica si el objeto ha sido modificado desde la última carga o guardado.
        // Se marca con [JsonIgnore] para evitar que se envíe a la API en la serialización JSON.
        [JsonIgnore]
        public bool IsDirty
        {
            get => isDirty;
            set
            {
                isDirty = value;
                OnPropertyChanged(); // Notifica cambios a la vista (por ejemplo, habilitar/deshabilitar botones).
            }
        }
    }
}
