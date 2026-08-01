namespace CONATRADEC.Models
{
    /// <summary>
    /// Página de propietarios devuelta por los endpoints paginados.
    /// Se utiliza tanto en la administración como en los selectores de terreno.
    /// </summary>
    public sealed class PropietarioPaginaResponse
    {
        public List<PropietarioResponse> Items { get; set; } = [];

        public int Pagina { get; set; }

        public int TamanoPagina { get; set; }

        public int TotalRegistros { get; set; }

        public int TotalPaginas { get; set; }

        public bool TienePaginaAnterior { get; set; }

        public bool TienePaginaSiguiente { get; set; }
    }
}
