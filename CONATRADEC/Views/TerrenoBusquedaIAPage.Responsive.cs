using CONATRADEC.Controls;
using Microsoft.Maui;
using Microsoft.Maui.Controls;

namespace CONATRADEC.Views
{
    /// <summary>
    /// Responsive por ancho real para la búsqueda de terrenos del flujo IA.
    /// Funciona igual con datos del servidor o con el catálogo local offline.
    /// </summary>
    public partial class TerrenoBusquedaIAPage
    {
        private const double BreakpointFiltrosUnaColumna = 820d;
        private const double BreakpointResultadoVertical = 720d;
        private const double BreakpointExtensionVertical = 480d;

        private CollectionView? terrenoResponsiveCollection;
        private bool terrenoResponsiveEventosConectados;
        private bool? terrenoFiltrosUnaColumnaAplicado;
        private bool? terrenoExtensionVerticalAplicado;

        private static readonly BindableProperty TerrenoResultadoVerticalProperty =
            BindableProperty.CreateAttached(
                "TerrenoResultadoVertical",
                typeof(bool?),
                typeof(TerrenoBusquedaIAPage),
                null);

        protected override void OnSizeAllocated(double width, double height)
        {
            base.OnSizeAllocated(width, height);
            AplicarResponsiveTerrenoBusqueda();
        }

        private void AplicarResponsiveTerrenoBusqueda()
        {
            AsegurarEventosTerrenoResponsive();

            double ancho = terrenoResponsiveCollection?.Width ?? Width;
            if (double.IsNaN(ancho) || ancho <= 0)
                return;

            ConfigurarFiltrosTerreno(ancho);
            ConfigurarResultadosTerreno(ancho);
        }

        private void AsegurarEventosTerrenoResponsive()
        {
            terrenoResponsiveCollection ??=
                ResponsiveLayoutUtility.FindDescendant<CollectionView>(
                    this,
                    _ => true);

            if (terrenoResponsiveCollection == null ||
                terrenoResponsiveEventosConectados)
            {
                return;
            }

            terrenoResponsiveCollection.SizeChanged +=
                TerrenoResponsiveCollection_SizeChanged;
            terrenoResponsiveCollection.Scrolled +=
                TerrenoResponsiveCollection_Scrolled;

            terrenoResponsiveEventosConectados = true;
        }

        private void TerrenoResponsiveCollection_SizeChanged(
            object? sender,
            EventArgs e) =>
            AplicarResponsiveTerrenoBusqueda();

        private void TerrenoResponsiveCollection_Scrolled(
            object? sender,
            ItemsViewScrolledEventArgs e)
        {
            Dispatcher.Dispatch(() =>
            {
                double ancho = terrenoResponsiveCollection?.Width ?? Width;
                ConfigurarResultadosTerreno(ancho);
            });
        }

        private void ConfigurarFiltrosTerreno(double ancho)
        {
            Entry? codigo = BuscarEntryTerreno("Código del terreno");
            Entry? propietario = BuscarEntryTerreno("Nombre del propietario");
            Entry? identificacion = BuscarEntryTerreno("Identificación del propietario");
            Entry? ubicacion = BuscarEntryTerreno("País, departamento o municipio");
            Entry? direccion = BuscarEntryTerreno("Dirección o referencia");
            Entry? extensionMin = BuscarEntryTerreno("Mz mín.");
            Entry? extensionMax = BuscarEntryTerreno("Mz máx.");

            if (codigo == null || propietario == null ||
                identificacion == null || ubicacion == null ||
                direccion == null || extensionMin == null ||
                extensionMax == null)
            {
                return;
            }

            Grid? gridFiltros =
                ResponsiveLayoutUtility.FindAncestor<Grid>(codigo);
            Grid? gridExtension =
                ResponsiveLayoutUtility.FindAncestor<Grid>(extensionMin);

            if (gridFiltros == null || gridExtension == null)
                return;

            bool unaColumna = ancho < BreakpointFiltrosUnaColumna;
            bool extensionVertical = ancho < BreakpointExtensionVertical;

            View? codigoView =
                ResponsiveLayoutUtility.FindDirectChildContaining(
                    gridFiltros,
                    codigo);
            View? propietarioView =
                ResponsiveLayoutUtility.FindDirectChildContaining(
                    gridFiltros,
                    propietario);
            View? identificacionView =
                ResponsiveLayoutUtility.FindDirectChildContaining(
                    gridFiltros,
                    identificacion);
            View? ubicacionView =
                ResponsiveLayoutUtility.FindDirectChildContaining(
                    gridFiltros,
                    ubicacion);
            View? direccionView =
                ResponsiveLayoutUtility.FindDirectChildContaining(
                    gridFiltros,
                    direccion);
            View? extensionView =
                ResponsiveLayoutUtility.FindDirectChildContaining(
                    gridFiltros,
                    gridExtension);

            View[] campos =
            [
                codigoView!,
                propietarioView!,
                identificacionView!,
                ubicacionView!,
                direccionView!,
                extensionView!
            ];

            if (campos.Any(item => item == null))
                return;

            int columnas = unaColumna ? 1 : 2;

            if (terrenoFiltrosUnaColumnaAplicado != unaColumna)
            {
                gridFiltros.ColumnDefinitions.Clear();
                gridFiltros.RowDefinitions.Clear();

                for (int columna = 0; columna < columnas; columna++)
                {
                    gridFiltros.ColumnDefinitions.Add(
                        new ColumnDefinition(GridLength.Star));
                }

                int filas =
                    (int)Math.Ceiling(campos.Length / (double)columnas);

                for (int fila = 0; fila < filas; fila++)
                {
                    gridFiltros.RowDefinitions.Add(
                        new RowDefinition(GridLength.Auto));
                }

                for (int indice = 0; indice < campos.Length; indice++)
                {
                    Grid.SetRow(campos[indice], indice / columnas);
                    Grid.SetColumn(campos[indice], indice % columnas);
                    Grid.SetColumnSpan(campos[indice], 1);
                }

                terrenoFiltrosUnaColumnaAplicado = unaColumna;
            }

            View? extensionMinView =
                ResponsiveLayoutUtility.FindDirectChildContaining(
                    gridExtension,
                    extensionMin);
            View? extensionMaxView =
                ResponsiveLayoutUtility.FindDirectChildContaining(
                    gridExtension,
                    extensionMax);

            if (extensionMinView == null || extensionMaxView == null)
                return;

            if (terrenoExtensionVerticalAplicado != extensionVertical)
            {
                if (extensionVertical)
                {
                    ResponsiveLayoutUtility.ConfigureStackedPair(
                        gridExtension,
                        extensionMinView,
                        extensionMaxView);
                    gridExtension.RowSpacing = 8;
                }
                else
                {
                    ResponsiveLayoutUtility.ConfigureHorizontalPair(
                        gridExtension,
                        extensionMinView,
                        extensionMaxView,
                        GridLength.Star,
                        GridLength.Star);
                    gridExtension.ColumnSpacing = 8;
                }

                terrenoExtensionVerticalAplicado = extensionVertical;
            }
        }

        private void ConfigurarResultadosTerreno(double ancho)
        {
            bool vertical = ancho < BreakpointResultadoVertical;

            IEnumerable<Button> botones =
                ResponsiveLayoutUtility.FindDescendants<Button>(this)
                    .Where(button => string.Equals(
                        button.Text?.Trim(),
                        "Seleccionar terreno",
                        StringComparison.OrdinalIgnoreCase));

            foreach (Button boton in botones)
            {
                Grid? grid = ResponsiveLayoutUtility.FindAncestor<Grid>(boton);
                if (grid == null)
                    continue;

                bool? estadoAplicado =
                    grid.GetValue(TerrenoResultadoVerticalProperty) is bool estado
                        ? estado
                        : null;

                if (estadoAplicado == vertical)
                    continue;

                View? botonView =
                    ResponsiveLayoutUtility.FindDirectChildContaining(
                        grid,
                        boton);

                View? contenido = grid.Children
                    .OfType<View>()
                    .FirstOrDefault(view => !ReferenceEquals(view, botonView));

                if (botonView == null || contenido == null)
                    continue;

                if (vertical)
                {
                    ResponsiveLayoutUtility.ConfigureStackedPair(
                        grid,
                        contenido,
                        botonView);
                    grid.RowSpacing = 10;
                    boton.MinimumWidthRequest = 0;
                    boton.HorizontalOptions = LayoutOptions.Fill;
                }
                else
                {
                    ResponsiveLayoutUtility.ConfigureHorizontalPair(
                        grid,
                        contenido,
                        botonView,
                        GridLength.Star,
                        GridLength.Auto);
                    grid.ColumnSpacing = 14;
                    boton.MinimumWidthRequest = 170;
                    boton.HorizontalOptions = LayoutOptions.End;
                }

                grid.SetValue(TerrenoResultadoVerticalProperty, vertical);
            }
        }

        private Entry? BuscarEntryTerreno(string placeholder) =>
            ResponsiveLayoutUtility.FindDescendant<Entry>(
                this,
                entry => string.Equals(
                    entry.Placeholder?.Trim(),
                    placeholder,
                    StringComparison.OrdinalIgnoreCase));
    }
}
