namespace CONATRADEC.Models
{
    public sealed class MunicipioPaginaResponse
    {
        public List<MunicipioResponse> Items { get; set; } = new();
        public int PaginaActual { get; set; }
        public int TamanoPagina { get; set; }
        public int TotalRegistros { get; set; }
        public int TotalPaginas { get; set; }
        public int DepartamentoId { get; set; }
        public string NombreDepartamento { get; set; } = string.Empty;
        public int PaisId { get; set; }
        public string NombrePais { get; set; } = string.Empty;
    }
}
