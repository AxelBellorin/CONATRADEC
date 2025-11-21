using CONATRADEC.ViewModels;
namespace CONATRADEC.Views;

public partial class paisPage : ContentPage
{
    PaisViewModel viewModel = new PaisViewModel();
    public paisPage()
    {
        Shell.Current.FlyoutBehavior = FlyoutBehavior.Disabled;
        BindingContext = viewModel;
        InitializeComponent();   
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await viewModel.LoadPais(true);  // Carga los paises desde la VM.
    }
}