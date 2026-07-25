namespace CONATRADEC.Models
{
    public sealed class TipoAnalisisSueloPaginaResponse
    {
        public List<TipoAnalisisSueloResponse> Items { get; set; } =
            new();

        public int PaginaActual { get; set; }

        public int TamanoPagina { get; set; }

        public int TotalRegistros { get; set; }

        public int TotalPaginas { get; set; }
    }
}
