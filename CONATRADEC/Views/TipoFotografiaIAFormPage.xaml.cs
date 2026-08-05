using CONATRADEC.ViewModels;

namespace CONATRADEC.Views
{
    public partial class TipoFotografiaIAFormPage : ContentPage
    {
        public TipoFotografiaIAFormPage()
        {
            InitializeComponent();
            Shell.Current.FlyoutBehavior = FlyoutBehavior.Disabled;
            BindingContext = new TipoFotografiaIAFormViewModel();
        }
    }
}
