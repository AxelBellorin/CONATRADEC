namespace CONATRADEC.Models
{
    public class UnidadMedidaResponse
    {
        public int? UnidadMedidaId { get; set; }

        public string? NombreUnidadMedida { get; set; }

        public string? DescripcionUnidadMedida { get; set; }

        public string? SimboloUnidadMedida { get; set; }

        public string? AbreviaturaUnidadMedida { get; set; }

        public bool? Activo { get; set; }

        public string TextoMostrar
        {
            get
            {
                string simbolo = !string.IsNullOrWhiteSpace(SimboloUnidadMedida)
                    ? SimboloUnidadMedida
                    : AbreviaturaUnidadMedida ?? string.Empty;

                if (!string.IsNullOrWhiteSpace(NombreUnidadMedida) &&
                    !string.IsNullOrWhiteSpace(simbolo))
                    return $"{NombreUnidadMedida} ({simbolo})";

                if (!string.IsNullOrWhiteSpace(NombreUnidadMedida))
                    return NombreUnidadMedida;

                if (!string.IsNullOrWhiteSpace(simbolo))
                    return simbolo;

                return "Unidad sin nombre";
            }
        }

        public string TextoBusqueda
        {
            get
            {
                return $"{NombreUnidadMedida} {DescripcionUnidadMedida} {SimboloUnidadMedida} {AbreviaturaUnidadMedida}"
                    .Trim()
                    .ToUpper();
            }
        }

        public override string ToString()
        {
            return TextoMostrar;
        }
    }
}