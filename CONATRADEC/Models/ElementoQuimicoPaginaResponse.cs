namespace CONATRADEC.Models
{
    public sealed class ElementoQuimicoPaginaResponse
    {
        public List<ElementoQuimicoResponse> Items { get; set; } =
            new();

        public int PaginaActual { get; set; }

        public int TamanoPagina { get; set; }

        public int TotalRegistros { get; set; }

        public int TotalPaginas { get; set; }
    }
}
