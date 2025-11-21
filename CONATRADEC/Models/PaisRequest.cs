using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace CONATRADEC.Models
{
    public class PaisRequest
    {
        private int paisId;
        private string nombrePais;
        private string codigoISOPais;

        // Se marca con [JsonIgnore] para evitar que se envíe a la API en la serialización JSON.
        [JsonIgnore]
        public int PaisId { get => paisId; set => paisId = value; }
        public string NombrePais { get => nombrePais; set => nombrePais = value; }
        public string CodigoISOPais { get => codigoISOPais; set => codigoISOPais = value; }
        public PaisRequest() { }
        public PaisRequest(PaisResponse paisRP)
        {
            PaisId = paisRP.PaisId;
            NombrePais = paisRP.NombrePais;
            CodigoISOPais = paisRP.CodigoISOPais;
        }
    }
}
