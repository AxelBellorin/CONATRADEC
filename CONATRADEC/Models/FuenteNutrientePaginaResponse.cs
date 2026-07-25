namespace CONATRADEC.Models
{
    public sealed class FuenteNutrientePaginaResponse
    {
        public List<FuenteNutrienteResponse> Items { get; set; } =
            new();

        public int PaginaActual { get; set; }

        public int TamanoPagina { get; set; }

        public int TotalRegistros { get; set; }

        public int TotalPaginas { get; set; }
    }
}
