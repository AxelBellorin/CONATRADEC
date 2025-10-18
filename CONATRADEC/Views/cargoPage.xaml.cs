using CONATRADEC.ViewModels;
namespace CONATRADEC.Views;

public partial class cargoPage : ContentPage
{
    public cargoPage()
    {
        InitializeComponent();
        BindingContext = new CargoViewModel();
        Shell.Current.FlyoutBehavior = FlyoutBehavior.Disabled;        
    }
}