namespace CONATRADEC.Models
{
    public class TipoCultivoResponse
    {
        public int? TipoCultivoId { get; set; }

        public string? NombreTipoCultivo { get; set; }

        public string? TipoCultivo { get; set; }

        public string? DescripcionTipoCultivo { get; set; }

        public bool? Activo { get; set; }

        public string NombreMostrar
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(TipoCultivo))
                    return TipoCultivo;

                if (!string.IsNullOrWhiteSpace(NombreTipoCultivo))
                    return NombreTipoCultivo;

                return "Sin nombre";
            }
        }
    }
}