using CONATRADEC.ViewModels;
namespace CONATRADEC.Views;

public partial class cargoPage : ContentPage
{
    public cargoPage()
    {
        Shell.Current.FlyoutBehavior = FlyoutBehavior.Disabled;
        BindingContext = new CargoViewModel();
        InitializeComponent();   
    }
}