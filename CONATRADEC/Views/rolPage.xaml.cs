using CONATRADEC.ViewModels;
namespace CONATRADEC.Views;

public partial class rolPage : ContentPage
{
    public rolPage()
    {
        InitializeComponent();
        BindingContext = new RolViewModel();
        Shell.Current.FlyoutBehavior = FlyoutBehavior.Disabled;        
    }
}