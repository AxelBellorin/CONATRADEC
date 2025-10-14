namespace CONATRADEC.Views;
using CONATRADEC.ViewModels;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();
        BindingContext = new MainPageViewModel();
        Shell.Current.FlyoutBehavior = FlyoutBehavior.Disabled;
    }
}