using CONATRADEC.ViewModels;

namespace CONATRADEC.Views;

public partial class loginPage : ContentPage
{
    public static string appName = "ConatraCafé Soil";

    public loginPage()
    {
        Shell.Current.FlyoutBehavior = FlyoutBehavior.Disabled;

        BindingContext = new LoginViewModel();
        InitializeComponent();
    }

    private void UserNameEntryCompleted(object sender, EventArgs e)
        => PasswordEntry.Focus();

    private void PasswordEntryCompleted(object sender, EventArgs e)
    {
        if (BindingContext is LoginViewModel viewModel)
            viewModel.LoginCommand.Execute(null);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await Task.Delay(100);

        if (BindingContext is LoginViewModel vm)
            await vm.LoadSavedAsync();
    }
}
