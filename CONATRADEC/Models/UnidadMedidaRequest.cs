namespace CONATRADEC.Models
{
    public class UnidadMedidaRequest
    {
        public int? UnidadMedidaId { get; set; }

        public string? NombreUnidadMedida { get; set; }

        public string? DescripcionUnidadMedida { get; set; }

        public string? SimboloUnidadMedida { get; set; }

        public string? AbreviaturaUnidadMedida { get; set; }

        public bool Activo { get; set; } = true;
    }
}