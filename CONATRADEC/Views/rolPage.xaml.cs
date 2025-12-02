using CONATRADEC.ViewModels;

namespace CONATRADEC.Views;

public partial class rolPage : ContentPage
{
    private readonly RolViewModel viewModel = new();

    public rolPage()
    {
        Shell.Current.FlyoutBehavior = FlyoutBehavior.Disabled;
        BindingContext = viewModel;
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // CARGAR PERMISOS DE ESTA PAGE
        viewModel.LoadPagePermissions("rolPage");

        await viewModel.LoadRol(true);
    }
}
