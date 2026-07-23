using Microsoft.Maui.Controls;

namespace CONATRADEC.Controls
{
    /// <summary>
    /// Aplica una transición breve únicamente al contenido derecho de la página.
    ///
    /// La navegación de Shell permanece sin animación para que el menú lateral
    /// de Windows y la barra inferior móvil no se desplacen ni parpadeen.
    /// </summary>
    public sealed class NavigationContentTransitionBehavior
        : Behavior<ContentPresenter>
    {
        private ContentPresenter? presenter;
        private bool animationStarted;

        protected override void OnAttachedTo(ContentPresenter bindable)
        {
            base.OnAttachedTo(bindable);

            presenter = bindable;
            presenter.Opacity = 0;
            presenter.Loaded += OnPresenterLoaded;
            presenter.Unloaded += OnPresenterUnloaded;
        }

        protected override void OnDetachingFrom(ContentPresenter bindable)
        {
            bindable.Loaded -= OnPresenterLoaded;
            bindable.Unloaded -= OnPresenterUnloaded;
            bindable.Opacity = 1;

            presenter = null;
            animationStarted = false;

            base.OnDetachingFrom(bindable);
        }

        private async void OnPresenterLoaded(object? sender, EventArgs e)
        {
            ContentPresenter? current = presenter;

            if (current == null || animationStarted)
                return;

            animationStarted = true;
            current.Opacity = 0;

            try
            {
                // En .NET MAUI 9 el método compatible es FadeTo(...).
                // La animación se aplica únicamente al contenido derecho.
#pragma warning disable CS0618
                await current.FadeTo(
                    1,
                    120,
                    Easing.CubicOut);
#pragma warning restore CS0618
            }
            catch (OperationCanceledException)
            {
                current.Opacity = 1;
            }
            finally
            {
                current.Opacity = 1;
            }
        }

        private void OnPresenterUnloaded(object? sender, EventArgs e)
        {
            animationStarted = false;

            if (presenter != null)
                presenter.Opacity = 0;
        }
    }
}
