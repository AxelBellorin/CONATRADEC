using System.Text.Json.Serialization;

namespace CONATRADEC.Models
{
    /// <summary>
    /// Página de usuarios inactivos devuelta por el endpoint administrativo.
    /// Mantiene en memoria únicamente los registros de la página actual.
    /// </summary>
    public sealed class UsuarioInactivoPaginaResponse
    {
        [JsonPropertyName("items")]
        public List<CatalogoEliminadoItem> Items { get; set; } = new();

        [JsonPropertyName("paginaActual")]
        public int PaginaActual { get; set; }

        [JsonPropertyName("tamanoPagina")]
        public int TamanoPagina { get; set; }

        [JsonPropertyName("totalRegistros")]
        public int TotalRegistros { get; set; }

        [JsonPropertyName("totalPaginas")]
        public int TotalPaginas { get; set; }
    }

    public sealed class UsuarioInactivoPaginaEnvelope
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("data")]
        public UsuarioInactivoPaginaResponse? Data { get; set; }
    }
}
