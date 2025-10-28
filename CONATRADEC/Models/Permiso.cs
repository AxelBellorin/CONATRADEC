using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace CONATRADEC.Models
{
    public class Permiso: INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] string name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private bool leer;
        private bool agregar;
        private bool actualizar;
        private bool eliminar;
        private bool isDirty;
        public bool Leer { get => leer; set { leer = value; OnPropertyChanged(); IsDirty = true; } }
        public bool Agregar { get => agregar; set { agregar = value; OnPropertyChanged(); IsDirty = true; } }
        public bool Actualizar { get => actualizar; set { actualizar = value; OnPropertyChanged(); IsDirty = true; } }
        public bool Eliminar { get => eliminar; set { eliminar = value; OnPropertyChanged(); IsDirty = true; } }
        public bool IsDirty { get => isDirty; set { isDirty = value; OnPropertyChanged();} }
    }
}
