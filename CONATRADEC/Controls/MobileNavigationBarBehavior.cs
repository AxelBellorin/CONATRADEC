using Microsoft.Maui.Controls;

namespace CONATRADEC.Controls
{
    /// <summary>
    /// Mejora la legibilidad de las opciones de la barra inferior.
    ///
    /// AppNavigationMenuItem mantiene su diseño original en Windows.
    /// Este comportamiento solamente ajusta las instancias móviles que están
    /// dentro del FlexLayout del FooterTemplate.
    /// </summary>
    public sealed class MobileNavigationBarBehavior :
        Behavior<FlexLayout>
    {
        private const double MobileItemHeight = 68;
        private const double MobileIconSize = 24;
        private const double MobileFontSize = 11.5;
        private const double MobileLabelHeight = 28;

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
                new Thickness(2, 6, 2, 5);

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

            label.FontSize =
                MobileFontSize;

            label.MaxLines = 2;
            label.LineBreakMode =
                LineBreakMode.WordWrap;

            label.LineHeight = 0.95;
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
