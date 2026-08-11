using CONATRADEC.Controls;
using Microsoft.Maui;
using Microsoft.Maui.Controls;

namespace CONATRADEC.Views
{
    /// <summary>
    /// Ajusta el modal de fotografía vinculada a inspección al ancho real.
    /// </summary>
    public partial class FotografiaVinculadaInspeccionPage
    {
        private const double AnchoMaximoModal = 620d;
        private const double BreakpointBotonesVerticales = 520d;
        private const double BreakpointDetalleVertical = 430d;

        private bool? fotoBotonesVerticalesAplicado;
        private bool? fotoDetallesVerticalesAplicado;

        protected override void OnSizeAllocated(double width, double height)
        {
            base.OnSizeAllocated(width, height);
            AplicarResponsiveFotografiaVinculada();
        }

        private void AplicarResponsiveFotografiaVinculada()
        {
            double ancho = Width;
            if (double.IsNaN(ancho) || ancho <= 0)
                return;

            AjustarAnchoModal(ancho);
            AjustarBotonesModal(ancho);
            AjustarDetallesModal(ancho);
        }

        private void AjustarAnchoModal(double anchoPagina)
        {
            Border? modal = ResponsiveLayoutUtility.FindDescendants<Border>(this)
                .Where(border =>
                    Math.Abs(border.MaximumWidthRequest - AnchoMaximoModal) < 0.1)
                .FirstOrDefault();

            if (modal == null)
                return;

            double paddingHorizontal =
                (Content as Grid)?.Padding.HorizontalThickness ?? 28d;
            double disponible = Math.Max(0d, anchoPagina - paddingHorizontal);

            if (disponible <= 0)
                return;

            double anchoObjetivo = Math.Min(AnchoMaximoModal, disponible);
            if (Math.Abs(modal.WidthRequest - anchoObjetivo) > 0.5)
                modal.WidthRequest = anchoObjetivo;

            modal.MaximumWidthRequest = AnchoMaximoModal;
        }

        private void AjustarBotonesModal(double ancho)
        {
            bool vertical = ancho < BreakpointBotonesVerticales;
            if (fotoBotonesVerticalesAplicado == vertical)
                return;

            Button? cerrar = BuscarBotonModal("Cerrar");
            Button? ir = BuscarBotonModal("Ir a la inspección");

            if (cerrar == null || ir == null)
                return;

            Grid? grid = ResponsiveLayoutUtility.FindAncestor<Grid>(cerrar);
            if (grid == null ||
                ResponsiveLayoutUtility.FindAncestor<Grid>(ir) != grid)
            {
                return;
            }

            View? cerrarView =
                ResponsiveLayoutUtility.FindDirectChildContaining(grid, cerrar);
            View? irView =
                ResponsiveLayoutUtility.FindDirectChildContaining(grid, ir);

            if (cerrarView == null || irView == null)
                return;

            if (vertical)
            {
                ResponsiveLayoutUtility.ConfigureStackedPair(
                    grid,
                    cerrarView,
                    irView);
                grid.RowSpacing = 9;
            }
            else
            {
                ResponsiveLayoutUtility.ConfigureHorizontalPair(
                    grid,
                    cerrarView,
                    irView,
                    GridLength.Star,
                    GridLength.Star);
                grid.ColumnSpacing = 10;
            }

            fotoBotonesVerticalesAplicado = vertical;
        }

        private void AjustarDetallesModal(double ancho)
        {
            bool vertical = ancho < BreakpointDetalleVertical;
            if (fotoDetallesVerticalesAplicado == vertical)
                return;

            bool aplicadoTecnico = AjustarFilaDetalle("Técnico", vertical);
            bool aplicadoPublicada = AjustarFilaDetalle("Publicada por", vertical);
            bool aplicadoPublicacion = AjustarFilaDetalle("Publicación", vertical);

            if (aplicadoTecnico && aplicadoPublicada && aplicadoPublicacion)
                fotoDetallesVerticalesAplicado = vertical;
        }

        private bool AjustarFilaDetalle(string etiqueta, bool vertical)
        {
            Label? titulo = ResponsiveLayoutUtility.FindDescendant<Label>(
                this,
                label => string.Equals(
                    label.Text?.Trim(),
                    etiqueta,
                    StringComparison.OrdinalIgnoreCase));

            if (titulo == null)
                return false;

            Grid? grid = ResponsiveLayoutUtility.FindAncestor<Grid>(titulo);
            if (grid == null)
                return false;

            List<View> vistas = grid.Children.OfType<View>().ToList();
            if (vistas.Count < 2)
                return false;

            View primero = vistas[0];
            View segundo = vistas[1];

            if (vertical)
            {
                ResponsiveLayoutUtility.ConfigureStackedPair(
                    grid,
                    primero,
                    segundo);
                grid.RowSpacing = 4;
            }
            else
            {
                ResponsiveLayoutUtility.ConfigureHorizontalPair(
                    grid,
                    primero,
                    segundo,
                    new GridLength(110),
                    GridLength.Star);
                grid.ColumnSpacing = 10;
            }

            return true;
        }

        private Button? BuscarBotonModal(string texto) =>
            ResponsiveLayoutUtility.FindDescendant<Button>(
                this,
                button => string.Equals(
                    button.Text?.Trim(),
                    texto,
                    StringComparison.OrdinalIgnoreCase));
    }
}
