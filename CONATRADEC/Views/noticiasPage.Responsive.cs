using CONATRADEC.Controls;
using Microsoft.Maui;
using Microsoft.Maui.Controls;

namespace CONATRADEC.Views
{
    /// <summary>
    /// Hace que Noticias responda al viewport real en Windows y móvil.
    /// La fuente de datos online/offline no interviene en estas decisiones.
    /// </summary>
    public partial class noticiasPage
    {
        private const double BreakpointUnaColumna = 650d;
        private const double BreakpointTresColumnas = 1080d;
        private const double BreakpointControlesCompactos = 820d;

        private bool? noticiasHeroCompactoAplicado;
        private bool? noticiasFiltrosCompactosAplicado;

        protected override void OnSizeAllocated(double width, double height)
        {
            base.OnSizeAllocated(width, height);
            AplicarResponsiveNoticias();
        }

        private void AplicarResponsiveNoticias()
        {
            double ancho = NoticiasCollectionView?.Width ?? Width;
            if (double.IsNaN(ancho) || ancho <= 0)
                return;

            AjustarColumnasNoticias(ancho);
            AjustarHeroNoticias(ancho);
            AjustarFiltrosNoticias(ancho);
        }

        private void AjustarColumnasNoticias(double ancho)
        {
            if (NoticiasCollectionView.ItemsLayout is not GridItemsLayout layout)
                return;

            int span = ancho switch
            {
                < BreakpointUnaColumna => 1,
                < BreakpointTresColumnas => 2,
                _ => 3
            };

            if (layout.Span != span)
                layout.Span = span;
        }

        private void AjustarHeroNoticias(double ancho)
        {
            bool compacto = ancho < BreakpointControlesCompactos;
            if (noticiasHeroCompactoAplicado == compacto)
                return;

            Button? administrar =
                ResponsiveLayoutUtility.FindDescendant<Button>(
                    this,
                    button => string.Equals(
                        button.Text?.Trim(),
                        "Administrar",
                        StringComparison.OrdinalIgnoreCase));

            if (administrar == null)
                return;

            Grid? grid = ResponsiveLayoutUtility.FindAncestor<Grid>(administrar);
            if (grid == null)
                return;

            View? botonView =
                ResponsiveLayoutUtility.FindDirectChildContaining(
                    grid,
                    administrar);
            View? textoView = grid.Children
                .OfType<View>()
                .FirstOrDefault(view => !ReferenceEquals(view, botonView));

            if (botonView == null || textoView == null)
                return;

            if (compacto)
            {
                ResponsiveLayoutUtility.ConfigureStackedPair(
                    grid,
                    textoView,
                    botonView);
                grid.RowSpacing = 12;
                administrar.MinimumWidthRequest = 0;
                administrar.HorizontalOptions = LayoutOptions.Fill;
            }
            else
            {
                ResponsiveLayoutUtility.ConfigureHorizontalPair(
                    grid,
                    textoView,
                    botonView,
                    GridLength.Star,
                    GridLength.Auto);
                grid.ColumnSpacing = 16;
                administrar.MinimumWidthRequest = 150;
                administrar.HorizontalOptions = LayoutOptions.End;
            }

            noticiasHeroCompactoAplicado = compacto;
        }

        private void AjustarFiltrosNoticias(double ancho)
        {
            bool compacto = ancho < BreakpointControlesCompactos;
            if (noticiasFiltrosCompactosAplicado == compacto)
                return;
            Button? buscar = BuscarBotonNoticias("Buscar");
            Button? limpiar = BuscarBotonNoticias("Limpiar");

            if (buscar == null || limpiar == null)
                return;

            Grid? grid = ResponsiveLayoutUtility.FindAncestor<Grid>(buscar);
            if (grid == null ||
                ResponsiveLayoutUtility.FindAncestor<Grid>(limpiar) != grid)
            {
                return;
            }

            List<View> paneles = grid.Children
                .OfType<Border>()
                .Cast<View>()
                .ToList();

            if (paneles.Count < 2)
                return;

            if (compacto)
            {
                grid.ColumnDefinitions.Clear();
                grid.RowDefinitions.Clear();
                grid.ColumnDefinitions.Add(
                    new ColumnDefinition(GridLength.Star));
                grid.ColumnDefinitions.Add(
                    new ColumnDefinition(GridLength.Star));
                grid.RowDefinitions.Add(
                    new RowDefinition(GridLength.Auto));
                grid.RowDefinitions.Add(
                    new RowDefinition(GridLength.Auto));
                grid.ColumnSpacing = 8;
                grid.RowSpacing = 8;

                PosicionarNoticias(paneles[0], 0, 0);
                PosicionarNoticias(paneles[1], 0, 1);
                PosicionarNoticias(buscar, 1, 0);
                PosicionarNoticias(limpiar, 1, 1);
            }
            else
            {
                grid.ColumnDefinitions.Clear();
                grid.RowDefinitions.Clear();
                grid.ColumnDefinitions.Add(
                    new ColumnDefinition(GridLength.Auto));
                grid.ColumnDefinitions.Add(
                    new ColumnDefinition(GridLength.Auto));
                grid.ColumnDefinitions.Add(
                    new ColumnDefinition(GridLength.Star));
                grid.ColumnDefinitions.Add(
                    new ColumnDefinition(GridLength.Auto));
                grid.ColumnDefinitions.Add(
                    new ColumnDefinition(GridLength.Auto));
                grid.RowDefinitions.Add(
                    new RowDefinition(GridLength.Auto));
                grid.ColumnSpacing = 8;

                PosicionarNoticias(paneles[0], 0, 0);
                PosicionarNoticias(paneles[1], 0, 1);
                PosicionarNoticias(buscar, 0, 3);
                PosicionarNoticias(limpiar, 0, 4);
            }

            noticiasFiltrosCompactosAplicado = compacto;
        }

        private static void PosicionarNoticias(
            View view,
            int fila,
            int columna)
        {
            Grid.SetRow(view, fila);
            Grid.SetColumn(view, columna);
            Grid.SetColumnSpan(view, 1);
        }

        private Button? BuscarBotonNoticias(string texto) =>
            ResponsiveLayoutUtility.FindDescendant<Button>(
                this,
                button => string.Equals(
                    button.Text?.Trim(),
                    texto,
                    StringComparison.OrdinalIgnoreCase));
    }
}
