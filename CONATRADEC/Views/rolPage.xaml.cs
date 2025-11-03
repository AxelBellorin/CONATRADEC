using CONATRADEC.ViewModels;
namespace CONATRADEC.Views;

public partial class rolPage : ContentPage
{
    private RolViewModel viewModel = new RolViewModel();
    public rolPage()
    {
        Shell.Current.FlyoutBehavior = FlyoutBehavior.Disabled;
        BindingContext = viewModel;
        InitializeComponent();      
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await viewModel.LoadRol(true);  // Carga los rol desde la VM.
    }
}