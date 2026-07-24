using System.Text.Json.Serialization;

namespace CONATRADEC.Models
{
    public sealed class AnalisisListadoPaginadoResponse
    {
        [JsonPropertyName("items")]
        public List<AnalisisGuardadoResumen> Items { get; set; } = new();

        [JsonPropertyName("pagina")]
        public int Pagina { get; set; }

        [JsonPropertyName("tamanoPagina")]
        public int TamanoPagina { get; set; }

        [JsonPropertyName("totalRegistros")]
        public int TotalRegistros { get; set; }

        [JsonPropertyName("totalPaginas")]
        public int TotalPaginas { get; set; }

        [JsonPropertyName("tieneMas")]
        public bool TieneMas { get; set; }

        [JsonPropertyName("esAdministrador")]
        public bool EsAdministrador { get; set; }

        [JsonPropertyName("usuarios")]
        public List<UsuarioFiltroAnalisis> Usuarios { get; set; } = new();
    }
}
