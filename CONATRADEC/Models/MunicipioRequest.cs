using System.Text.Json.Serialization;

namespace CONATRADEC.Models
{
    public sealed class MunicipioRequest
    {
        [JsonIgnore]
        public int? MunicipioId { get; set; }

        public string NombreMunicipio { get; set; } = string.Empty;
        public int? DepartamentoId { get; set; }

        public MunicipioRequest()
        {
        }

        public MunicipioRequest(MunicipioResponse municipio)
        {
            ArgumentNullException.ThrowIfNull(municipio);

            MunicipioId = municipio.MunicipioId;
            NombreMunicipio = municipio.NombreMunicipio ?? string.Empty;
            DepartamentoId = municipio.DepartamentoId;
        }
    }
}
