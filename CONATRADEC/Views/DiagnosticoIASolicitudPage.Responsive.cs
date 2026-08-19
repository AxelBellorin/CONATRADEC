using CONATRADEC.Controls;
using CONATRADEC.ViewModels;
using Microsoft.Maui;
using Microsoft.Maui.Controls;

namespace CONATRADEC.Views
{
    /// <summary>
    /// Ajustes responsive de Nueva inspección y Mis inspecciones. Las decisiones
    /// se toman por ancho real para que una ventana WinUI reducida se comporte
    /// como una superficie compacta aunque DeviceIdiom continúe siendo Desktop.
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

        private bool paginadorConfigurado;
        private Button? paginaAnteriorButton;
        private Button? paginaSiguienteButton;
        private Label? paginaEstadoLabel;

        protected override void OnSizeAllocated(double width, double height)
        {
            base.OnSizeAllocated(width, height);
            AplicarResponsiveSolicitud();
        }

        /// <summary>
        /// Sustituye únicamente la carga incremental histórica. El EmptyState
        /// existente permanece en el Footer y el listado conserva virtualización.
        /// </summary>
        private void ConfigurarPaginadorListado()
        {
            if (paginadorConfigurado || ListadoGrid == null)
                return;

            paginadorConfigurado = true;
            ListadoGrid.RemainingItemsThreshold = -1;
            ListadoGrid.RemainingItemsThresholdReachedCommand = null;

            Button? cargarMas = BuscarBotonSolicitud(
                "Cargar más inspecciones");
            Grid? bloqueHistorico = cargarMas == null
                ? null
                : ResponsiveLayoutUtility.FindAncestor<Grid>(cargarMas);

            if (bloqueHistorico != null)
                bloqueHistorico.IsVisible = false;

            VerticalStackLayout? footer = bloqueHistorico?.Parent as
                VerticalStackLayout;

            if (footer == null && ListadoGrid.Footer is VerticalStackLayout directo)
                footer = directo;

            if (footer == null)
                return;

            paginaAnteriorButton = new Button
            {
                Text = "Anterior",
                HeightRequest = 44,
                MinimumWidthRequest = 0,
                Padding = new Thickness(12, 6),
                BackgroundColor = Color.FromArgb("#E3EFEA"),
                TextColor = Color.FromArgb("#3B655B"),
                CornerRadius = 10,
                HorizontalOptions = LayoutOptions.Fill,
                Command = listadoViewModel.PaginaAnteriorCommand
            };

            paginaSiguienteButton = new Button
            {
                Text = "Siguiente",
                HeightRequest = 44,
                MinimumWidthRequest = 0,
                Padding = new Thickness(12, 6),
                BackgroundColor = Color.FromArgb("#E3EFEA"),
                TextColor = Color.FromArgb("#3B655B"),
                CornerRadius = 10,
                HorizontalOptions = LayoutOptions.Fill,
                Command = listadoViewModel.PaginaSiguienteCommand
            };

            paginaEstadoLabel = new Label
            {
                FontSize = 13,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#3B655B"),
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center,
                MinimumWidthRequest = 96
            };
            paginaEstadoLabel.SetBinding(
                Label.TextProperty,
                new Binding(
                    nameof(DiagnosticoIASolicitudListadoViewModel.TextoPaginacion),
                    source: listadoViewModel));

            var grid = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition(GridLength.Star),
                    new ColumnDefinition(GridLength.Auto),
                    new ColumnDefinition(GridLength.Star)
                },
                ColumnSpacing = 8,
                HorizontalOptions = LayoutOptions.Fill
            };

            grid.Add(paginaAnteriorButton, 0, 0);
            grid.Add(paginaEstadoLabel, 1, 0);
            grid.Add(paginaSiguienteButton, 2, 0);

            var contenedor = new Border
            {
                Padding = new Thickness(8, 10),
                Margin = new Thickness(0, 4, 0, 0),
                BackgroundColor = Colors.Transparent,
                Stroke = Colors.Transparent,
                Content = grid
            };

            footer.Children.Add(contenedor);
            listadoViewModel.PaginaCargada += OnPaginaListadoCargada;
        }

        private void OnPaginaListadoCargada(object? sender, EventArgs e)
        {
            if (ListadoGrid == null || listadoViewModel.Solicitudes.Count == 0)
                return;

            Dispatcher.Dispatch(() =>
            {
                try
                {
                    ListadoGrid.ScrollTo(
                        0,
                        position: ScrollToPosition.Start,
                        animate: false);
                }
                catch (ArgumentOutOfRangeException)
                {
                    // La colección pudo cambiar entre el evento y el despacho.
                }
            });
        }

        private void AplicarResponsiveSolicitud()
        {
            ConfigurarPaginadorListado();

            double ancho = ObtenerAnchoSolicitud();
            if (ancho <= 0)
                return;

            AjustarSelectorVista(ancho);
            AjustarFilaTerreno(ancho);
            AjustarCabeceraFotografias(ancho);
            AjustarNavegacionFotografias(ancho);
            AjustarMetadatosFotografia(ancho);
            AjustarBarraListado(ancho);
            AjustarBusquedaListado(ancho);
            AjustarFiltrosListado(ancho);
            AjustarPaginador(ancho);
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
            bool compacto = ancho < BreakpointCompacto;

            if (VistaInspeccionesPicker != null)
            {
                VistaInspeccionesPicker.WidthRequest = compacto ? -1 : 315;
                VistaInspeccionesPicker.MinimumWidthRequest = compacto ? 0 : 260;
                VistaInspeccionesPicker.HorizontalOptions =
                    compacto ? LayoutOptions.Fill : LayoutOptions.End;
            }

            if (selectorVistaButton != null)
            {
                selectorVistaButton.WidthRequest = compacto ? -1 : 315;
                selectorVistaButton.MinimumWidthRequest = compacto ? 0 : 260;
                selectorVistaButton.HorizontalOptions =
                    compacto ? LayoutOptions.Fill : LayoutOptions.End;
            }
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

        private void AjustarMetadatosFotografia(double ancho)
        {
            bool compacto = ancho < BreakpointCompacto;

            foreach (string texto in new[]
                     {
                         "Tipo de fotografía *",
                         "Fecha de identificación en campo *"
                     })
            {
                Label? label = ResponsiveLayoutUtility.FindDescendant<Label>(
                    this,
                    item => string.Equals(
                        item.Text?.Trim(),
                        texto,
                        StringComparison.OrdinalIgnoreCase));

                if (label?.Parent is VerticalStackLayout contenido &&
                    contenido.Parent is Border tarjeta)
                {
                    tarjeta.MinimumWidthRequest = compacto ? 0 : 320;
                }
            }
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

        /// <summary>
        /// Los WidthRequest declarados por DeviceIdiom son adecuados en teléfono
        /// y escritorio amplio, pero una ventana WinUI estrecha continúa siendo
        /// Desktop. En compacto se usa el ancho útil real para impedir overflow.
        /// </summary>
        private void AjustarFiltrosListado(double ancho)
        {
            bool compacto = ancho < BreakpointCompacto;
            double anchoCompacto = Math.Max(0, ancho - 54);

            (string Etiqueta, double AnchoNormal)[] filtros =
            [
                ("Técnico responsable", 285),
                ("Propietario", 225),
                ("Departamento", 190),
                ("Tipo de fotografía", 205),
                ("Estado", 215),
                ("Registro desde", 210),
                ("Registro hasta", 210)
            ];

            foreach ((string etiqueta, double anchoNormal) in filtros)
            {
                Label? label = ResponsiveLayoutUtility.FindDescendant<Label>(
                    this,
                    item => string.Equals(
                        item.Text?.Trim(),
                        etiqueta,
                        StringComparison.OrdinalIgnoreCase));

                VerticalStackLayout? contenedor = label == null
                    ? null
                    : ResponsiveLayoutUtility.FindAncestor<VerticalStackLayout>(
                        label);

                if (contenedor == null)
                    continue;

                contenedor.MinimumWidthRequest = 0;
                contenedor.WidthRequest = compacto
                    ? anchoCompacto
                    : anchoNormal;
            }
        }

        private void AjustarPaginador(double ancho)
        {
            if (paginaAnteriorButton == null || paginaSiguienteButton == null)
                return;

            bool muyCompacto = ancho < 430;
            paginaAnteriorButton.FontSize = muyCompacto ? 11 : 13;
            paginaSiguienteButton.FontSize = muyCompacto ? 11 : 13;

            if (paginaEstadoLabel != null)
            {
                paginaEstadoLabel.FontSize = muyCompacto ? 11 : 13;
                paginaEstadoLabel.MinimumWidthRequest = muyCompacto ? 78 : 96;
            }
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
