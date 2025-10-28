using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CONATRADEC.Models;

namespace CONATRADEC.Models
{
    public class InterfazRequest: Permiso
    {
        private int permisoId;
        private string nombrePermiso=string.Empty;

        public int PermisoId { get => permisoId; set => permisoId = value; }
        public string NombrePermiso { get => nombrePermiso; set => nombrePermiso = value; }

        public InterfazRequest()
        {
        }
        public InterfazRequest(InterfazResponse interfazResponse)
        {
            PermisoId = interfazResponse.PermisoId;
            NombrePermiso = interfazResponse.NombrePermiso;
        }
    }
}
