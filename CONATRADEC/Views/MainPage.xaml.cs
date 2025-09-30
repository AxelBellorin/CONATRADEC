namespace CONATRADEC.Views;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();
        Shell.Current.FlyoutBehavior = FlyoutBehavior.Flyout;
    }
}