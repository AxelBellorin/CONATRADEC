namespace CONATRADEC.Models
{
    public sealed class RangoNutrienteCategoriaItem
    {
        public int TipoCultivoId { get; set; }

        public string NombreCategoria { get; set; } =
            string.Empty;

        public string DescripcionCategoria { get; set; } =
            string.Empty;

        public int CantidadAportes { get; set; }

        public string AportesTexto =>
            CantidadAportes == 1
                ? "1 rango nutricional"
                : $"{CantidadAportes} rangos nutricionales";

        public string DescripcionMostrar =>
            string.IsNullOrWhiteSpace(DescripcionCategoria)
                ? "Sin descripción registrada."
                : DescripcionCategoria.Trim();

        public TipoCultivoResponse ToTipoCultivoResponse() =>
            new()
            {
                TipoCultivoId = TipoCultivoId,
                NombreTipoCultivo = NombreCategoria,
                TipoCultivo = NombreCategoria,
                DescripcionTipoCultivo = DescripcionCategoria,
                Activo = true
            };
    }
}
