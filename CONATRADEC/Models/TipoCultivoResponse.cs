namespace CONATRADEC.Models
{
    public sealed class TipoCultivoResponse
    {
        public int TipoCultivoId { get; set; }

        public string? NombreTipoCultivo { get; set; }

        /*
         * Se conserva por compatibilidad con las pantallas de análisis
         * que anteriormente recibían el nombre en esta propiedad.
         */
        public string? TipoCultivo { get; set; }

        public string? DescripcionTipoCultivo { get; set; }

        public bool Activo { get; set; }

        public int CantidadRangosActivos { get; set; }

        public int CantidadAnalisis { get; set; }

        public string NombreMostrar =>
            !string.IsNullOrWhiteSpace(TipoCultivo)
                ? TipoCultivo.Trim()
                : NombreTipoCultivo?.Trim() ?? string.Empty;

        public string DescripcionMostrar =>
            string.IsNullOrWhiteSpace(DescripcionTipoCultivo)
                ? "Sin descripción registrada."
                : DescripcionTipoCultivo.Trim();

        public string ResumenRangos =>
            CantidadRangosActivos == 1
                ? "1 rango nutricional"
                : $"{CantidadRangosActivos} rangos nutricionales";

        public string ResumenAnalisis =>
            CantidadAnalisis == 1
                ? "1 análisis asociado"
                : $"{CantidadAnalisis} análisis asociados";
    }
}
