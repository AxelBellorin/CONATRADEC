using CONATRADEC.Controls;
using Microsoft.Maui;
using Microsoft.Maui.Controls;

namespace CONATRADEC.Views
{
    /// <summary>
    /// Ajustes de containment para Nueva inspección y Mis inspecciones.
    /// No cambia datos ni comandos; únicamente reorganiza controles existentes.
    /// </summary>
    public partial class DiagnosticoIASolicitudPage
    {
        private const double BreakpointCompacto = 740d;
        private const double BreakpointMuyCompacto = 560d;

        private static readonly BindableProperty SolicitudResponsiveStateProperty =
            BindableProperty.CreateAttached(
                "SolicitudResponsiveState",
                typeof(bool?),
                typeof(DiagnosticoIASolicitudPage),
                null);

        protected override void OnSizeAllocated(double width, double height)
        {
            base.OnSizeAllocated(width, height);
            AplicarResponsiveSolicitud();
        }

        private void AplicarResponsiveSolicitud()
        {
            double ancho = ObtenerAnchoSolicitud();
            if (ancho <= 0)
                return;

            AjustarSelectorVista(ancho);
            AjustarFilaTerreno(ancho);
            AjustarCabeceraFotografias(ancho);
            AjustarNavegacionFotografias(ancho);
            AjustarBarraListado(ancho);
            AjustarBusquedaListado(ancho);
        }

        private double ObtenerAnchoSolicitud()
        {
            double ancho = ContenidoScroll?.Width ?? Width;
            if (double.IsNaN(ancho) || ancho <= 0)
                ancho = Width;

            return double.IsNaN(ancho) ? 0 : ancho;
        }

        private void AjustarSelectorVista(double ancho)
        {
            if (VistaInspeccionesPicker == null)
                return;

            bool compacto = ancho < BreakpointCompacto;
            VistaInspeccionesPicker.WidthRequest = compacto ? -1 : 315;
            VistaInspeccionesPicker.MinimumWidthRequest = compacto ? 0 : 260;
            VistaInspeccionesPicker.HorizontalOptions =
                compacto ? LayoutOptions.Fill : LayoutOptions.End;
        }

        private void AjustarFilaTerreno(double ancho)
        {
            Entry? entry = BuscarEntrySolicitud("Código de terreno opcional");
            Button? boton = BuscarBotonSolicitud("Buscar terreno");

            if (entry == null || boton == null)
                return;

            Grid? grid = ResponsiveLayoutUtility.FindAncestor<Grid>(entry);
            if (grid == null ||
                ResponsiveLayoutUtility.FindAncestor<Grid>(boton) != grid)
            {
                return;
            }

            bool compacto = ancho < BreakpointCompacto;
            if (EstadoSolicitudAplicado(grid) == compacto)
                return;

            View? entryView =
                ResponsiveLayoutUtility.FindDirectChildContaining(grid, entry);
            View? botonView =
                ResponsiveLayoutUtility.FindDirectChildContaining(grid, boton);

            if (entryView == null || botonView == null)
                return;

            if (compacto)
            {
                ResponsiveLayoutUtility.ConfigureStackedPair(
                    grid,
                    entryView,
                    botonView);
                grid.RowSpacing = 8;
                boton.HorizontalOptions = LayoutOptions.Fill;
            }
            else
            {
                ResponsiveLayoutUtility.ConfigureHorizontalPair(
                    grid,
                    entryView,
                    botonView,
                    GridLength.Star,
                    GridLength.Auto);
                grid.ColumnSpacing = 10;
                boton.HorizontalOptions = LayoutOptions.End;
            }

            MarcarEstadoSolicitud(grid, compacto);
        }

        private void AjustarCabeceraFotografias(double ancho)
        {
            Button? galeria = BuscarBotonSolicitud("Galería");
            Button? camara = BuscarBotonSolicitud("Cámara");

            if (galeria == null || camara == null)
                return;

            Grid? botonesGrid = ResponsiveLayoutUtility.FindAncestor<Grid>(galeria);
            if (botonesGrid == null ||
                ResponsiveLayoutUtility.FindAncestor<Grid>(camara) != botonesGrid)
            {
                return;
            }

            Grid? cabeceraGrid = ResponsiveLayoutUtility.FindAncestor<Grid>(botonesGrid);
            if (cabeceraGrid == null)
                return;

            bool compacto = ancho < BreakpointCompacto;
            if (EstadoSolicitudAplicado(cabeceraGrid) == compacto)
                return;

            View? botonesView =
                ResponsiveLayoutUtility.FindDirectChildContaining(
                    cabeceraGrid,
                    botonesGrid);
            View? tituloView = cabeceraGrid.Children
                .OfType<View>()
                .FirstOrDefault(view => !ReferenceEquals(view, botonesView));

            if (botonesView == null || tituloView == null)
                return;

            if (compacto)
            {
                ResponsiveLayoutUtility.ConfigureStackedPair(
                    cabeceraGrid,
                    tituloView,
                    botonesView);
                cabeceraGrid.RowSpacing = 8;
            }
            else
            {
                cabeceraGrid.ColumnDefinitions.Clear();
                cabeceraGrid.RowDefinitions.Clear();
                cabeceraGrid.ColumnDefinitions.Add(
                    new ColumnDefinition(GridLength.Star));
                cabeceraGrid.ColumnDefinitions.Add(
                    new ColumnDefinition(GridLength.Auto));
                cabeceraGrid.ColumnDefinitions.Add(
                    new ColumnDefinition(GridLength.Auto));
                cabeceraGrid.RowDefinitions.Add(
                    new RowDefinition(GridLength.Auto));

                Grid.SetRow(tituloView, 0);
                Grid.SetColumn(tituloView, 0);
                Grid.SetColumnSpan(tituloView, 1);

                Grid.SetRow(botonesView, 0);
                Grid.SetColumn(botonesView, 1);
                Grid.SetColumnSpan(botonesView, 2);
            }

            MarcarEstadoSolicitud(cabeceraGrid, compacto);
        }

        private void AjustarNavegacionFotografias(double ancho)
        {
            Button? anterior = BuscarBotonSolicitud("← Anterior");
            Button? siguiente = BuscarBotonSolicitud("Siguiente →");

            if (anterior == null || siguiente == null)
                return;

            Grid? grid = ResponsiveLayoutUtility.FindAncestor<Grid>(anterior);
            if (grid == null ||
                ResponsiveLayoutUtility.FindAncestor<Grid>(siguiente) != grid)
            {
                return;
            }

            bool compacto = ancho < BreakpointCompacto;
            if (EstadoSolicitudAplicado(grid) == compacto)
                return;

            View? anteriorView =
                ResponsiveLayoutUtility.FindDirectChildContaining(grid, anterior);
            View? siguienteView =
                ResponsiveLayoutUtility.FindDirectChildContaining(grid, siguiente);
            View? estadoView = grid.Children
                .OfType<View>()
                .FirstOrDefault(view =>
                    !ReferenceEquals(view, anteriorView) &&
                    !ReferenceEquals(view, siguienteView));

            if (anteriorView == null ||
                siguienteView == null ||
                estadoView == null)
            {
                return;
            }

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
                grid.RowSpacing = 7;

                Grid.SetRow(anteriorView, 0);
                Grid.SetColumn(anteriorView, 0);
                Grid.SetColumnSpan(anteriorView, 1);

                Grid.SetRow(siguienteView, 0);
                Grid.SetColumn(siguienteView, 1);
                Grid.SetColumnSpan(siguienteView, 1);

                Grid.SetRow(estadoView, 1);
                Grid.SetColumn(estadoView, 0);
                Grid.SetColumnSpan(estadoView, 2);

                anterior.MinimumWidthRequest = 0;
                siguiente.MinimumWidthRequest = 0;
                anterior.HorizontalOptions = LayoutOptions.Fill;
                siguiente.HorizontalOptions = LayoutOptions.Fill;
            }
            else
            {
                grid.ColumnDefinitions.Clear();
                grid.RowDefinitions.Clear();
                grid.ColumnDefinitions.Add(
                    new ColumnDefinition(GridLength.Auto));
                grid.ColumnDefinitions.Add(
                    new ColumnDefinition(GridLength.Star));
                grid.ColumnDefinitions.Add(
                    new ColumnDefinition(GridLength.Auto));
                grid.RowDefinitions.Add(
                    new RowDefinition(GridLength.Auto));
                grid.ColumnSpacing = 12;

                Grid.SetRow(anteriorView, 0);
                Grid.SetColumn(anteriorView, 0);
                Grid.SetColumnSpan(anteriorView, 1);

                Grid.SetRow(estadoView, 0);
                Grid.SetColumn(estadoView, 1);
                Grid.SetColumnSpan(estadoView, 1);

                Grid.SetRow(siguienteView, 0);
                Grid.SetColumn(siguienteView, 2);
                Grid.SetColumnSpan(siguienteView, 1);

                anterior.MinimumWidthRequest = 130;
                siguiente.MinimumWidthRequest = 130;
            }

            MarcarEstadoSolicitud(grid, compacto);
        }

        private void AjustarBarraListado(double ancho)
        {
            Button? actualizar = BuscarBotonSolicitud("Actualizar");
            if (actualizar == null)
                return;

            Grid? grid = ResponsiveLayoutUtility.FindAncestor<Grid>(actualizar);
            if (grid == null)
                return;

            bool compacto = ancho < BreakpointCompacto;
            if (EstadoSolicitudAplicado(grid) == compacto)
                return;

            Button? filtros = grid.Children
                .OfType<Button>()
                .FirstOrDefault(button => !ReferenceEquals(button, actualizar));
            View? resumen = grid.Children
                .OfType<View>()
                .FirstOrDefault(view => view is not Button);

            if (filtros == null || resumen == null)
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
                grid.RowSpacing = 7;
                grid.ColumnSpacing = 6;

                Grid.SetRow(resumen, 0);
                Grid.SetColumn(resumen, 0);
                Grid.SetColumnSpan(resumen, 2);

                Grid.SetRow(filtros, 1);
                Grid.SetColumn(filtros, 0);
                Grid.SetColumnSpan(filtros, 1);

                Grid.SetRow(actualizar, 1);
                Grid.SetColumn(actualizar, 1);
                Grid.SetColumnSpan(actualizar, 1);
            }
            else
            {
                grid.ColumnDefinitions.Clear();
                grid.RowDefinitions.Clear();
                grid.ColumnDefinitions.Add(
                    new ColumnDefinition(GridLength.Star));
                grid.ColumnDefinitions.Add(
                    new ColumnDefinition(GridLength.Auto));
                grid.ColumnDefinitions.Add(
                    new ColumnDefinition(GridLength.Auto));
                grid.RowDefinitions.Add(
                    new RowDefinition(GridLength.Auto));
                grid.ColumnSpacing = 6;

                Grid.SetRow(resumen, 0);
                Grid.SetColumn(resumen, 0);
                Grid.SetColumnSpan(resumen, 1);

                Grid.SetRow(filtros, 0);
                Grid.SetColumn(filtros, 1);
                Grid.SetColumnSpan(filtros, 1);

                Grid.SetRow(actualizar, 0);
                Grid.SetColumn(actualizar, 2);
                Grid.SetColumnSpan(actualizar, 1);
            }

            MarcarEstadoSolicitud(grid, compacto);
        }

        private void AjustarBusquedaListado(double ancho)
        {
            Entry? entry = BuscarEntrySolicitud(
                "Nombre, terreno, propietario, técnico, ubicación o archivo");
            Button? buscar = BuscarBotonSolicitud("Buscar");

            if (entry == null || buscar == null)
                return;

            Grid? grid = ResponsiveLayoutUtility.FindAncestor<Grid>(entry);
            if (grid == null ||
                ResponsiveLayoutUtility.FindAncestor<Grid>(buscar) != grid)
            {
                return;
            }

            bool compacto = ancho < BreakpointMuyCompacto;
            if (EstadoSolicitudAplicado(grid) == compacto)
                return;

            View? entryView =
                ResponsiveLayoutUtility.FindDirectChildContaining(grid, entry);
            View? botonView =
                ResponsiveLayoutUtility.FindDirectChildContaining(grid, buscar);

            if (entryView == null || botonView == null)
                return;

            if (compacto)
            {
                ResponsiveLayoutUtility.ConfigureStackedPair(
                    grid,
                    entryView,
                    botonView);
                grid.RowSpacing = 7;
                buscar.MinimumWidthRequest = 0;
                buscar.HorizontalOptions = LayoutOptions.Fill;
            }
            else
            {
                ResponsiveLayoutUtility.ConfigureHorizontalPair(
                    grid,
                    entryView,
                    botonView,
                    GridLength.Star,
                    GridLength.Auto);
                grid.ColumnSpacing = 7;
                buscar.MinimumWidthRequest = 120;
                buscar.HorizontalOptions = LayoutOptions.End;
            }

            MarcarEstadoSolicitud(grid, compacto);
        }

        private static bool? EstadoSolicitudAplicado(Grid grid) =>
            grid.GetValue(SolicitudResponsiveStateProperty) is bool estado
                ? estado
                : null;

        private static void MarcarEstadoSolicitud(Grid grid, bool estado) =>
            grid.SetValue(SolicitudResponsiveStateProperty, estado);

        private Entry? BuscarEntrySolicitud(string placeholder) =>
            ResponsiveLayoutUtility.FindDescendant<Entry>(
                this,
                entry => string.Equals(
                    entry.Placeholder?.Trim(),
                    placeholder,
                    StringComparison.OrdinalIgnoreCase));

        private Button? BuscarBotonSolicitud(string texto) =>
            ResponsiveLayoutUtility.FindDescendant<Button>(
                this,
                button => string.Equals(
                    button.Text?.Trim(),
                    texto,
                    StringComparison.OrdinalIgnoreCase));
    }
}
