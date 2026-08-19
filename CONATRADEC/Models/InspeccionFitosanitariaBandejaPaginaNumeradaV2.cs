namespace CONATRADEC.Models
{
    /// <summary>
    /// Página numerada usada exclusivamente por Solicitudes e Historial.
    /// El contrato por cursor existente se conserva para las otras bandejas.
    /// </summary>
    public sealed class InspeccionFitosanitariaBandejaPaginaNumeradaV2
    {
        public List<InspeccionFitosanitariaBandejaItemV2> Items { get; set; } = [];
        public int Pagina { get; set; } = 1;
        public int TamanoPagina { get; set; } = 20;
        public int Total { get; set; }
        public int TotalPaginas { get; set; }
    }
}
