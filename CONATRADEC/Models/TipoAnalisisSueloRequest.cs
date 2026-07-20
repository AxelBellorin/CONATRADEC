using System.Text.Json.Serialization;

namespace CONATRADEC.Models
{
    public class TipoAnalisisSueloRequest
    {
        [JsonIgnore]
        public int TipoAnalisisSueloId { get; set; }

        public string NombreTipoAnalisisSuelo { get; set; } = string.Empty;
        public string DescripcionTipoAnalisisSuelo { get; set; } = string.Empty;

        public TipoAnalisisSueloRequest()
        {
        }

        public TipoAnalisisSueloRequest(TipoAnalisisSueloResponse response)
        {
            ArgumentNullException.ThrowIfNull(response);

            TipoAnalisisSueloId = response.TipoAnalisisSueloId;
            NombreTipoAnalisisSuelo = response.NombreTipoAnalisisSuelo ?? string.Empty;
            DescripcionTipoAnalisisSuelo = response.DescripcionTipoAnalisisSuelo ?? string.Empty;
        }
    }
}
