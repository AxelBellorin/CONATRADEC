using Microsoft.Maui.Devices;

namespace CONATRADEC.Controls
{
    /// <summary>
    /// Cambia únicamente la presentación del menú según el ancho disponible.
    ///
    /// En Windows amplio muestra la barra lateral. En una ventana estrecha,
    /// Android o iOS oculta la barra lateral y utiliza la navegación inferior.
    /// No modifica rutas, páginas ni el estado de navegación de Shell.
    /// </summary>
    public sealed class ResponsiveNavigationLayoutBehavior : Behavior<Grid>
    {
        private const double DesktopBreakpoint = 900;
        private const double DesktopMenuWidth = 230;

        private Grid? layout;
        private Border? desktopNavigation;
        private Border? compactNavigation;
        private ColumnDefinition? navigationColumn;

        protected override void OnAttachedTo(Grid bindable)
        {
            base.OnAttachedTo(bindable);

            layout = bindable;
            bindable.Loaded += OnLoaded;
            bindable.SizeChanged += OnSizeChanged;

            bindable.Dispatcher.Dispatch(ApplyLayout);
        }

        protected override void OnDetachingFrom(Grid bindable)
        {
            bindable.Loaded -= OnLoaded;
            bindable.SizeChanged -= OnSizeChanged;

            layout = null;
            desktopNavigation = null;
            compactNavigation = null;
            navigationColumn = null;

            base.OnDetachingFrom(bindable);
        }

        private void OnLoaded(object? sender, EventArgs e)
        {
            ResolveElements();
            ApplyLayout();
        }

        private void OnSizeChanged(object? sender, EventArgs e)
        {
            ApplyLayout();
        }

        private void ResolveElements()
        {
            Grid? current = layout;
            if (current == null)
                return;

            navigationColumn = current.ColumnDefinitions.Count > 0
                ? current.ColumnDefinitions[0]
                : null;

            desktopNavigation = null;
            compactNavigation = null;

            foreach (IView child in current.Children)
            {
                if (child is not Border border)
                    continue;

                if (string.Equals(
                        border.AutomationId,
                        "DesktopNavigation",
                        StringComparison.Ordinal))
                {
                    desktopNavigation = border;
                }
                else if (string.Equals(
                             border.AutomationId,
                             "CompactNavigation",
                             StringComparison.Ordinal))
                {
                    compactNavigation = border;
                }
            }
        }

        private void ApplyLayout()
        {
            Grid? current = layout;
            if (current == null)
                return;

            if (desktopNavigation == null ||
                compactNavigation == null ||
                navigationColumn == null)
            {
                ResolveElements();
            }

            double availableWidth = current.Width;
            if (availableWidth <= 0 &&
                Application.Current?.Windows.Count > 0)
            {
                availableWidth = Application.Current.Windows[0].Width;
            }

            bool isWindows =
                DeviceInfo.Current.Platform == DevicePlatform.WinUI;

            bool showDesktopNavigation =
                isWindows && availableWidth >= DesktopBreakpoint;

            if (navigationColumn != null)
            {
                navigationColumn.Width = showDesktopNavigation
                    ? new GridLength(DesktopMenuWidth)
                    : new GridLength(0);
            }

            if (desktopNavigation != null)
            {
                desktopNavigation.IsVisible = showDesktopNavigation;
                desktopNavigation.InputTransparent =
                    !showDesktopNavigation;
            }

            if (compactNavigation != null)
            {
                compactNavigation.IsVisible = !showDesktopNavigation;
                compactNavigation.InputTransparent =
                    showDesktopNavigation;
            }
        }
    }
}
