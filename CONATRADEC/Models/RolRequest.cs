using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CONATRADEC.Models
{
    public class RolRequest
    {

        private int? rolId;
        private string? nombreRol;
        private string? descripcionRol;
        public int? RolId { get => rolId; set => rolId = value; }
        public string? NombreRol { get => nombreRol; set => nombreRol = value; }
        public string? DescripcionRol { get => descripcionRol; set => descripcionRol = value; }

        public RolRequest(RolRP rolRP)
        {
            RolId= rolRP.RolId;
            NombreRol= rolRP.NombreRol; 
            DescripcionRol= rolRP.DescripcionRol;            
        }
    }
}
