using System;

namespace CONATRADEC.Models
{
    public class RangoNutrienteCategoriaItem
    {
        public int TipoCultivoId { get; set; }

        public string NombreCategoria { get; set; } =
            string.Empty;

        public string DescripcionCategoria { get; set; } =
            string.Empty;

        public int CantidadAportes { get; set; }

        public string AportesTexto =>
            CantidadAportes == 1
                ? "1 rango de aporte"
                : $"{CantidadAportes} rangos de aporte";

        public TipoCultivoResponse ToTipoCultivoResponse()
        {
            return new TipoCultivoResponse
            {
                TipoCultivoId = TipoCultivoId,
                NombreTipoCultivo = NombreCategoria,
                TipoCultivo = NombreCategoria,
                DescripcionTipoCultivo =
                    DescripcionCategoria,
                Activo = true
            };
        }
    }
}
