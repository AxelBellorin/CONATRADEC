namespace CONATRADEC.Controls
{
    /// <summary>
    /// Ajusta únicamente la cantidad de columnas de un CollectionView según
    /// el ancho real disponible. No modifica datos, comandos ni navegación.
    /// </summary>
    public sealed class ResponsiveGridItemsLayoutBehavior : Behavior<CollectionView>
    {
        private const double TwoColumnsBreakpoint = 760;
        private const double ThreeColumnsBreakpoint = 1380;

        private CollectionView? collectionView;

        protected override void OnAttachedTo(CollectionView bindable)
        {
            base.OnAttachedTo(bindable);
            collectionView = bindable;
            bindable.Loaded += OnLoaded;
            bindable.SizeChanged += OnSizeChanged;
            bindable.Dispatcher.Dispatch(ApplyLayout);
        }

        protected override void OnDetachingFrom(CollectionView bindable)
        {
            bindable.Loaded -= OnLoaded;
            bindable.SizeChanged -= OnSizeChanged;
            collectionView = null;
            base.OnDetachingFrom(bindable);
        }

        private void OnLoaded(object? sender, EventArgs e) => ApplyLayout();

        private void OnSizeChanged(object? sender, EventArgs e) => ApplyLayout();

        private void ApplyLayout()
        {
            CollectionView? current = collectionView;
            if (current?.ItemsLayout is not GridItemsLayout gridLayout)
                return;

            double availableWidth = current.Width;
            if (availableWidth <= 0)
                return;

            int span = availableWidth >= ThreeColumnsBreakpoint
                ? 3
                : availableWidth >= TwoColumnsBreakpoint
                    ? 2
                    : 1;

            if (gridLayout.Span != span)
                gridLayout.Span = span;
        }
    }
}
