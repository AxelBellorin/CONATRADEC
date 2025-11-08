using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CONATRADEC.Models
{
    public class PaisResponse
    {
        private int paisId;
        private string nombrePais;
        private string codigoISOPais;

        public int PaisId { get => paisId; set => paisId = value; }
        public string NombrePais { get => nombrePais; set => nombrePais = value; }
        public string CodigoISOPais { get => codigoISOPais; set => codigoISOPais = value; }

        public PaisResponse() { }
    }
}
