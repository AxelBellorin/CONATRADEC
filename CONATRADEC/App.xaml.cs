using CONATRADEC.Views;

namespace CONATRADEC
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
        }
        protected override Window CreateWindow(IActivationState? activationState)
        {

#if WINDOWS
            // Devuelves la ventana con su contenido raíz
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