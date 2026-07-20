using System.Text.Json.Serialization;

namespace CONATRADEC.Models
{
    public class TipoCultivoRequest
    {
        [JsonIgnore]
        public int TipoCultivoId { get; set; }

        public string NombreTipoCultivo { get; set; } = string.Empty;
        public string DescripcionTipoCultivo { get; set; } = string.Empty;

        public TipoCultivoRequest()
        {
        }

        public TipoCultivoRequest(TipoCultivoResponse response)
        {
            ArgumentNullException.ThrowIfNull(response);

            TipoCultivoId = response.TipoCultivoId;
            NombreTipoCultivo = response.NombreTipoCultivo ?? string.Empty;
            DescripcionTipoCultivo = response.DescripcionTipoCultivo ?? string.Empty;
        }
    }
}
