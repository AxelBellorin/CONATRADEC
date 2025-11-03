using CONATRADEC.ViewModels;
namespace CONATRADEC.Views;

public partial class userPage : ContentPage
{
    private UserViewModel viewModel = new UserViewModel();
    public userPage()
    {
        Shell.Current.FlyoutBehavior = FlyoutBehavior.Disabled;
        BindingContext = viewModel;
        InitializeComponent();
    }
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await viewModel.LoadUsers(true);  // Carga los usuarios desde la VM.
    }
}