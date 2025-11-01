using CONATRADEC.ViewModels;
namespace CONATRADEC.Views;

public partial class rolPage : ContentPage
{
    public rolPage()
    {
        Shell.Current.FlyoutBehavior = FlyoutBehavior.Disabled;
        BindingContext = new RolViewModel();
        InitializeComponent();      
    }
}