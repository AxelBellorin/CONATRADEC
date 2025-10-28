using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CONATRADEC.Models;

namespace CONATRADEC.Models
{
    class MatrizPermisosRequest
    {
        private RolRequest rol;
        private List<InterfazRequest> permisos;

        public RolRequest Rol { get => rol; set => rol = value; }
        public List<InterfazRequest> Permisos { get => permisos; set => permisos = value; }

        public MatrizPermisosRequest()
        {

        }
        public MatrizPermisosRequest(MatrizPermisosResponse matrizPermisosResponse)
        {
            Rol = new RolRequest(matrizPermisosResponse.Rol);
            Permisos = matrizPermisosResponse.Permisos
                .Select(p => new InterfazRequest(p))
                .ToList();
        }
    }
}
