namespace CONATRADEC.Models
{
    public sealed class TipoAnalisisSueloResponse
    {
        public int TipoAnalisisSueloId { get; set; }

        public string? CodigoTipoAnalisisSuelo { get; set; }

        public string? NombreTipoAnalisisSuelo { get; set; }

        public string? DescripcionTipoAnalisisSuelo { get; set; }

        public bool Activo { get; set; }

        public int CantidadAnalisis { get; set; }

        public bool EsTipoSistema { get; set; }

        public bool PuedeEliminar { get; set; }

        public string NombreMostrar =>
            NombreTipoAnalisisSuelo?.Trim() ??
            string.Empty;

        public string CodigoMostrar =>
            CodigoTipoAnalisisSuelo?.Trim() ??
            string.Empty;

        public string DescripcionMostrar =>
            string.IsNullOrWhiteSpace(
                DescripcionTipoAnalisisSuelo)
                    ? "Sin descripción registrada."
                    : DescripcionTipoAnalisisSuelo.Trim();

        public string ResumenAnalisis =>
            CantidadAnalisis == 1
                ? "1 análisis asociado"
                : $"{CantidadAnalisis} análisis asociados";

        public string TipoRegistroMostrar =>
            EsTipoSistema
                ? "Sistema"
                : "Personalizado";
    }
}
