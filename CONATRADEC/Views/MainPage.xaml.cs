namespace CONATRADEC.Views;
using CONATRADEC.ViewModels;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        Shell.Current.FlyoutBehavior = FlyoutBehavior.Disabled;
        BindingContext = new MainPageViewModel(); 
        InitializeComponent();
    }
}