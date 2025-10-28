using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CONATRADEC.Models
{
    public class CargoResponse
    {

        private int? cargoId;
        private string? nombreCargo;
        private string? descripcionCargo;
        public int? CargoId { get => cargoId; set => cargoId = value; }
        public string? NombreCargo { get => nombreCargo; set => nombreCargo = value; }
        public string? DescripcionCargo { get => descripcionCargo; set => descripcionCargo = value; }

    }
}
