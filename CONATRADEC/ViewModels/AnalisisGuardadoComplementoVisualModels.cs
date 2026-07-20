using System.Globalization;

namespace CONATRADEC.ViewModels
{
    public sealed class CompraComercialComplementoVisualViewModel
    {
        public string Fuente { get; set; } = string.Empty;

        public string Elemento { get; set; } = string.Empty;

        public decimal RequerimientoOriginal { get; set; }

        public decimal AporteOrganico { get; set; }

        public decimal RequerimientoRestante { get; set; }

        public decimal QuintalesOriginales { get; set; }

        public decimal QuintalesAjustados { get; set; }

        public decimal ReduccionQuintales { get; set; }

        public decimal PrecioPorQq { get; set; }

        public decimal QuintalesComprar { get; set; }

        public decimal CostoCompra { get; set; }

        public string TextoRequerimientoOriginal =>
            RequerimientoOriginal.ToString(
                "N2",
                CultureInfo.InvariantCulture);

        public string TextoAporteOrganico =>
            AporteOrganico.ToString(
                "N2",
                CultureInfo.InvariantCulture);

        public string TextoRequerimientoRestante =>
            RequerimientoRestante.ToString(
                "N2",
                CultureInfo.InvariantCulture);

        public string TextoQuintalesOriginales =>
            QuintalesOriginales.ToString(
                "N2",
                CultureInfo.InvariantCulture);

        public string TextoQuintalesAjustados =>
            QuintalesAjustados.ToString(
                "N2",
                CultureInfo.InvariantCulture);

        public string TextoReduccionQuintales =>
            ReduccionQuintales.ToString(
                "N2",
                CultureInfo.InvariantCulture);

        public string TextoPrecioPorQq =>
            $"C$ {PrecioPorQq.ToString("N2", CultureInfo.InvariantCulture)}";

        public string TextoQuintalesComprar =>
            QuintalesComprar.ToString(
                "N0",
                CultureInfo.InvariantCulture);

        public string TextoCostoCompra =>
            $"C$ {CostoCompra.ToString("N2", CultureInfo.InvariantCulture)}";
    }
}
