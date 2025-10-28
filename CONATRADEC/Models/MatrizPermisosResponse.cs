using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CONATRADEC.Models;

namespace CONATRADEC.Models
{
    class MatrizPermisosResponse
    {
        private RolResponse rol;
        private ObservableCollection<InterfazResponse> permisos;

        public RolResponse Rol { get => rol; set => rol = value; }
        public ObservableCollection<InterfazResponse> Permisos { get => permisos; set => permisos = value; }
    }
}
