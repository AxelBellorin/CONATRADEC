namespace CONATRADEC.Controls
{
    /// <summary>
    /// Ajusta únicamente la cantidad de columnas de un CollectionView según
    /// el ancho real disponible. No modifica datos, comandos ni navegación.
    ///
    /// El cálculo usa un ancho mínimo útil por tarjeta, igual que Usuarios y
    /// Terrenos, para evitar tarjetas demasiado estrechas en tablet o WinUI.
    /// </summary>
    public sealed class ResponsiveGridItemsLayoutBehavior :
        Behavior<CollectionView>
    {
        private const double AnchoMinimoTarjeta = 430;
        private const double EspaciadoTarjetas = 12;

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

        private void OnLoaded(object? sender, EventArgs e) =>
            ApplyLayout();

        private void OnSizeChanged(object? sender, EventArgs e) =>
            ApplyLayout();

        private void ApplyLayout()
        {
            CollectionView? current = collectionView;

            if (current?.ItemsLayout is not GridItemsLayout gridLayout)
                return;

            double availableWidth = current.Width;
            if (availableWidth <= 0)
                return;

            double requeridoTres =
                (AnchoMinimoTarjeta * 3) +
                (EspaciadoTarjetas * 2);

            double requeridoDos =
                (AnchoMinimoTarjeta * 2) +
                EspaciadoTarjetas;

            int span =
                availableWidth >= requeridoTres
                    ? 3
                    : availableWidth >= requeridoDos
                        ? 2
                        : 1;

            if (gridLayout.Span != span)
                gridLayout.Span = span;
        }
    }
}
