using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CONATRADEC.Models
{
    public class FotoTerrenoResponse
    {
        [JsonPropertyName("fotoTerrenoId")]
        public int? FotoTerrenoId { get; set; }

        [JsonPropertyName("urlFotoTerreno")]
        public string? UrlFotoTerreno { get; set; }

        [JsonPropertyName("terrenoId")]
        public int? TerrenoId { get; set; }
    }

    public class FotoTerrenoSubirResponse
    {
        [JsonPropertyName("mensaje")]
        public string? Mensaje { get; set; }

        [JsonPropertyName("fotos")]
        public List<FotoTerrenoResponse> Fotos { get; set; } = new();
    }
}