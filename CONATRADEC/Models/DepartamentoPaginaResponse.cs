namespace CONATRADEC.Models
{
    public sealed class DepartamentoPaginaResponse
    {
        public List<DepartamentoResponse> Items { get; set; } =
            new();

        public int PaginaActual { get; set; }

        public int TamanoPagina { get; set; }

        public int TotalRegistros { get; set; }

        public int TotalPaginas { get; set; }

        public int PaisId { get; set; }

        public string NombrePais { get; set; } =
            string.Empty;
    }
}
