using Microsoft.Maui.Controls;

namespace CONATRADEC.Controls
{
    /// <summary>
    /// Ajusta únicamente la presentación de la barra inferior en móvil.
    /// Mantiene cinco accesos visibles, con etiquetas de una sola línea para
    /// aprovechar mejor el ancho disponible sin modificar la navegación.
    /// </summary>
    public sealed class MobileNavigationBarBehavior :
        Behavior<FlexLayout>
    {
        private const double MobileItemHeight = 66;
        private const double MobileIconSize = 24;
        private const double MobileFontSize = 10.2;
        private const double ConfigurationFontSize = 9.1;
        private const double MobileLabelHeight = 20;

        private FlexLayout? layout;

        protected override void OnAttachedTo(
            FlexLayout bindable)
        {
            base.OnAttachedTo(bindable);

            layout = bindable;
            bindable.Loaded += OnLoaded;

            /*
             * Los controles ya existen cuando se carga el ResourceDictionary,
             * pero se programa un segundo ajuste para ejecutarlo después de que
             * AppNavigationMenuItem aplique EsMovil desde XAML.
             */
            bindable.Dispatcher.Dispatch(
                AplicarAjustes);
        }

        protected override void OnDetachingFrom(
            FlexLayout bindable)
        {
            bindable.Loaded -= OnLoaded;
            layout = null;

            base.OnDetachingFrom(bindable);
        }

        private void OnLoaded(
            object? sender,
            EventArgs e)
        {
            AplicarAjustes();

            layout?.Dispatcher.Dispatch(
                AplicarAjustes);
        }

        private void AplicarAjustes()
        {
            if (layout == null)
                return;

            foreach (IView child in layout.Children)
            {
                if (child is AppNavigationMenuItem item &&
                    item.EsMovil)
                {
                    AjustarMenuItem(item);
                }
            }
        }

        private static void AjustarMenuItem(
            AppNavigationMenuItem item)
        {
            item.HeightRequest =
                MobileItemHeight;

            item.MinimumHeightRequest =
                MobileItemHeight;

            item.VerticalOptions =
                LayoutOptions.Fill;

            if (item.Content is not Grid contentGrid)
                return;

            VerticalStackLayout? mobileLayout =
                contentGrid.Children
                    .OfType<VerticalStackLayout>()
                    .FirstOrDefault();

            if (mobileLayout == null)
                return;

            mobileLayout.HeightRequest =
                MobileItemHeight;

            mobileLayout.MinimumHeightRequest =
                MobileItemHeight;

            mobileLayout.Padding =
                new Thickness(2, 5, 2, 4);

            mobileLayout.Spacing = 2;
            mobileLayout.VerticalOptions =
                LayoutOptions.Center;

            Image? icon =
                mobileLayout.Children
                    .OfType<Image>()
                    .FirstOrDefault();

            if (icon != null)
            {
                icon.HeightRequest =
                    MobileIconSize;

                icon.WidthRequest =
                    MobileIconSize;

                icon.MinimumHeightRequest =
                    MobileIconSize;

                icon.MinimumWidthRequest =
                    MobileIconSize;

                icon.HorizontalOptions =
                    LayoutOptions.Center;

                icon.VerticalOptions =
                    LayoutOptions.Center;
            }

            Label? label =
                mobileLayout.Children
                    .OfType<Label>()
                    .FirstOrDefault();

            if (label == null)
                return;

            /*
             * "Configuración" es la etiqueta más larga de la barra.
             * Se reduce solamente esa opción para conservar el texto completo
             * en una sola línea incluso en teléfonos angostos.
             */
            label.FontSize =
                string.Equals(
                    item.Texto,
                    "Configuración",
                    StringComparison.OrdinalIgnoreCase)
                    ? ConfigurationFontSize
                    : MobileFontSize;

            label.MaxLines = 1;
            label.LineBreakMode =
                LineBreakMode.NoWrap;

            label.LineHeight = 1;
            label.MinimumHeightRequest =
                MobileLabelHeight;

            label.VerticalTextAlignment =
                TextAlignment.Center;

            label.HorizontalTextAlignment =
                TextAlignment.Center;

            label.HorizontalOptions =
                LayoutOptions.Fill;
        }
    }
}
