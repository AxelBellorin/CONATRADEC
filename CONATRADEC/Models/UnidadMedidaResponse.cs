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
                /*
                 * La descripción puede explicar que una unidad se convierte
                 * internamente a porcentaje. No debe hacer que esa unidad sea
                 * identificada como "%" por las validaciones del formulario.
                 *
                 * Se conservan los demás términos descriptivos para no perder
                 * utilidad en búsquedas, pero los indicadores exclusivos de
                 * porcentaje se toman solamente del nombre, símbolo o
                 * abreviatura reales de la unidad.
                 */
                string descripcion =
                    (DescripcionUnidadMedida ?? string.Empty)
                        .ToUpperInvariant()
                        .Replace("%", string.Empty)
                        .Replace("PORCENTAJE", string.Empty);

                return $"{NombreUnidadMedida} {descripcion} {SimboloUnidadMedida} {AbreviaturaUnidadMedida}"
                    .Trim()
                    .ToUpperInvariant();
            }
        }

        public override string ToString()
        {
            return TextoMostrar;
        }
    }
}
