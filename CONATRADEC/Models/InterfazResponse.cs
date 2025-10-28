using CONATRADEC.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CONATRADEC.Models
{
    public class InterfazResponse : Permiso
    {
        private int permisoId;
        private string nombrePermiso = string.Empty;

        public int PermisoId { get => permisoId; set { permisoId = value; OnPropertyChanged(); } }
        public string NombrePermiso { get => nombrePermiso; set { nombrePermiso = value; OnPropertyChanged(); } }

        public InterfazResponse(int id, string nombre, bool leer, bool agregar, bool actualizar, bool eliminar)
        {
            PermisoId = id;
            NombrePermiso = nombre;
            Leer = leer;
            Agregar = agregar;
            Actualizar = actualizar;
            Eliminar = eliminar;
            IsDirty = false;
        }

        public InterfazResponse()
        {

        }

        public void SetAll(bool valor)
        {
            Leer = valor;
            Agregar = valor;
            Actualizar = valor;
            Eliminar = valor;
        }

        public void AcceptChanges() => IsDirty = false;
    }
}
