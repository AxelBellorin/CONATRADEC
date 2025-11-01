using CONATRADEC.ViewModels;
namespace CONATRADEC.Views;

public partial class matrizPermisosPage : ContentPage
{
    public matrizPermisosPage()
    {
        Shell.Current.FlyoutBehavior = FlyoutBehavior.Disabled;
        BindingContext = new MatrizPermisosViewModel();
        InitializeComponent();        
    }
}