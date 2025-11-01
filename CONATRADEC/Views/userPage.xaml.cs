using CONATRADEC.ViewModels;
namespace CONATRADEC.Views;

public partial class userPage : ContentPage
{
    public userPage()
    {
        Shell.Current.FlyoutBehavior = FlyoutBehavior.Disabled;
        BindingContext = new UserViewModel();
        InitializeComponent();
    }
}