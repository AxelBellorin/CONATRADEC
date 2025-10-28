using CONATRADEC.ViewModels;
namespace CONATRADEC.Views;

public partial class matrizPermisosPage : ContentPage
{
    public matrizPermisosPage()
    {
        InitializeComponent();
        BindingContext = new MatrizPermisosViewModel();
        Shell.Current.FlyoutBehavior = FlyoutBehavior.Disabled;        
    }
}