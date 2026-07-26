using CONATRADEC.Services;

namespace CONATRADEC.Views
{
    public partial class sinPermisosPage : ContentPage
    {
        public sinPermisosPage()
        {
            InitializeComponent();

            Shell.Current.FlyoutBehavior =
                FlyoutBehavior.Disabled;

            // FooterTemplate necesita este comando para permitir cerrar sesión.
            BindingContext = new GlobalService();
        }
    }
}
