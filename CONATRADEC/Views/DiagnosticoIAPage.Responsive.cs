using CONATRADEC.Controls;
using CONATRADEC.ViewModels;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using System.ComponentModel;

namespace CONATRADEC.Views
{
    /// <summary>
    /// Adaptación por ancho real del panel principal de inspección.
    /// Evita asumir que WinUI siempre dispone de espacio para dos columnas.
    /// </summary>
    public partial class DiagnosticoIAPage
    {
        private const double BreakpointDosColumnas = 940d;
        private bool responsiveViewModelSuscrito;
        private ScrollView? responsiveScroll;
        private VerticalStackLayout? responsiveContenido;
        private Grid? responsiveOpcionesGrid;
        private readonly List<Border> responsiveTarjetas = [];
        private int responsiveColumnasAplicadas = -1;
        private string responsiveFirmaVisibilidad = string.Empty;

        protected override void OnBindingContextChanged()
        {
            base.OnBindingContextChanged();

            if (responsiveViewModelSuscrito ||
                BindingContext is not DiagnosticoIAViewModel vm)
            {
                return;
            }

            vm.PropertyChanged += ResponsiveViewModel_PropertyChanged;
            responsiveViewModelSuscrito = true;
        }

        protected override void OnSizeAllocated(double width, double height)
        {
            base.OnSizeAllocated(width, height);
            AplicarResponsiveDiagnostico();
        }

        private void ResponsiveViewModel_PropertyChanged(
            object? sender,
            PropertyChangedEventArgs e)
        {
            if (e.PropertyName is not (
                nameof(DiagnosticoIAViewModel.PuedeNuevaInspeccion) or
                nameof(DiagnosticoIAViewModel.PuedeMisInspecciones) or
                nameof(DiagnosticoIAViewModel.PuedeAnalizador) or
                nameof(DiagnosticoIAViewModel.PuedeAprobador) or
                nameof(DiagnosticoIAViewModel.PuedeHistorial)))
            {
                return;
            }

            Dispatcher.Dispatch(AplicarResponsiveDiagnostico);
        }

        private void AplicarResponsiveDiagnostico()
        {
            AsegurarReferenciasResponsiveDiagnostico();

            if (responsiveScroll == null ||
                responsiveContenido == null ||
                responsiveOpcionesGrid == null)
            {
                return;
            }

            double anchoDisponible = responsiveScroll.Width;
            if (double.IsNaN(anchoDisponible) || anchoDisponible <= 0)
                anchoDisponible = Width;

            if (double.IsNaN(anchoDisponible) || anchoDisponible <= 0)
                return;

            /*
             * ScrollView mide su contenido con bastante libertad horizontal.
             * Limitar el contenido al viewport evita que el MaximumWidthRequest
             * de 1160 termine creando una superficie más ancha que la ventana.
             */
            double anchoContenido = Math.Min(1160d, anchoDisponible);
            if (Math.Abs(responsiveContenido.WidthRequest - anchoContenido) > 0.5)
                responsiveContenido.WidthRequest = anchoContenido;

            double anchoInterior = Math.Max(
                0d,
                anchoContenido -
                responsiveContenido.Padding.HorizontalThickness);

            int columnas = anchoInterior >= BreakpointDosColumnas
                ? 2
                : 1;

            string firma = string.Join(
                ",",
                responsiveTarjetas.Select(item => item.IsVisible ? "1" : "0"));

            if (responsiveColumnasAplicadas == columnas &&
                string.Equals(
                    responsiveFirmaVisibilidad,
                    firma,
                    StringComparison.Ordinal))
            {
                return;
            }

            ReconfigurarOpcionesDiagnostico(columnas);
            responsiveColumnasAplicadas = columnas;
            responsiveFirmaVisibilidad = firma;
        }

        private void AsegurarReferenciasResponsiveDiagnostico()
        {
            if (responsiveTarjetas.Count == 0)
            {
                AgregarTarjetaResponsive("Nueva inspección");
                AgregarTarjetaResponsive("Mis inspecciones");
                AgregarTarjetaResponsive("Bandeja del analizador");
                AgregarTarjetaResponsive("Bandeja del aprobador");
                AgregarTarjetaResponsive("Historial");
            }

            responsiveOpcionesGrid ??=
                responsiveTarjetas
                    .Select(item => item.Parent)
                    .OfType<Grid>()
                    .GroupBy(item => item)
                    .OrderByDescending(group => group.Count())
                    .Select(group => group.Key)
                    .FirstOrDefault();

            /*
             * Se busca el ScrollView a partir de una tarjeta del módulo para no
             * confundirlo con superficies desplazables del FooterTemplate.
             */
            responsiveScroll ??= responsiveTarjetas.Count > 0
                ? ResponsiveLayoutUtility.FindAncestor<ScrollView>(
                    responsiveTarjetas[0])
                : null;

            responsiveContenido ??=
                responsiveScroll?.Content as VerticalStackLayout;
        }

        private void AgregarTarjetaResponsive(string titulo)
        {
            Border? tarjeta =
                ResponsiveLayoutUtility.FindSectionCard(this, titulo);

            if (tarjeta != null && !responsiveTarjetas.Contains(tarjeta))
                responsiveTarjetas.Add(tarjeta);
        }

        private void ReconfigurarOpcionesDiagnostico(int columnas)
        {
            if (responsiveOpcionesGrid == null ||
                responsiveTarjetas.Count == 0)
            {
                return;
            }

            responsiveOpcionesGrid.ColumnDefinitions.Clear();
            responsiveOpcionesGrid.RowDefinitions.Clear();

            for (int columna = 0; columna < columnas; columna++)
            {
                responsiveOpcionesGrid.ColumnDefinitions.Add(
                    new ColumnDefinition(GridLength.Star));
            }

            List<Border> visibles = responsiveTarjetas
                .Where(item => item.IsVisible)
                .ToList();

            int totalFilas = Math.Max(
                1,
                (int)Math.Ceiling(visibles.Count / (double)columnas));

            for (int fila = 0; fila < totalFilas; fila++)
            {
                responsiveOpcionesGrid.RowDefinitions.Add(
                    new RowDefinition(GridLength.Auto));
            }

            foreach (Border tarjeta in responsiveTarjetas)
            {
                /*
                 * Desde aquí la posición depende del viewport real. Se retiran
                 * únicamente los bindings visuales de fila/columna; los demás
                 * bindings y comandos permanecen intactos.
                 */
                tarjeta.RemoveBinding(Grid.RowProperty);
                tarjeta.RemoveBinding(Grid.ColumnProperty);
                tarjeta.RemoveBinding(Grid.ColumnSpanProperty);

                Grid.SetRow(tarjeta, 0);
                Grid.SetColumn(tarjeta, 0);
                Grid.SetColumnSpan(tarjeta, 1);
            }

            for (int indice = 0; indice < visibles.Count; indice++)
            {
                Border tarjeta = visibles[indice];
                Grid.SetRow(tarjeta, indice / columnas);
                Grid.SetColumn(tarjeta, indice % columnas);
            }
        }
    }
}
