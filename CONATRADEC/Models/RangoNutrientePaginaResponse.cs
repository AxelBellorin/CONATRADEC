namespace CONATRADEC.Models
{
    public sealed class RangoNutrienteCategoriaPaginaResponse
    {
        public List<RangoNutrienteCategoriaItem> Items { get; set; } =
            new();

        public int PaginaActual { get; set; }

        public int TamanoPagina { get; set; }

        public int TotalRegistros { get; set; }

        public int TotalPaginas { get; set; }
    }

    public sealed class RangoNutrientePaginaResponse
    {
        public List<RangoNutrienteResponse> Items { get; set; } =
            new();

        public int PaginaActual { get; set; }

        public int TamanoPagina { get; set; }

        public int TotalRegistros { get; set; }

        public int TotalPaginas { get; set; }
    }
}
