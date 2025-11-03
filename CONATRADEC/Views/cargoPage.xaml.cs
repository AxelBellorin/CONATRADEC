using CONATRADEC.ViewModels;
namespace CONATRADEC.Views;

public partial class cargoPage : ContentPage
{
    private  CargoViewModel viewModel = new CargoViewModel();
    public cargoPage()
    {
        Shell.Current.FlyoutBehavior = FlyoutBehavior.Disabled;
        BindingContext = viewModel;
        InitializeComponent();   
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await viewModel.LoadCargo(true);  // Carga los cargos desde la VM.
    }
}