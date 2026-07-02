using CONATRADEC.Views;

namespace CONATRADEC
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            // Fuerza la app a usar tema claro aunque el sistema esté en modo oscuro
            UserAppTheme = AppTheme.Light;
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
#if WINDOWS
            var page = new AppShell();
            var window = new Window(page);
            window.Title = "ConatraCafé Soil";
            return window;
#else
            return new Window(new AppShell());
#endif
        }
    }
}