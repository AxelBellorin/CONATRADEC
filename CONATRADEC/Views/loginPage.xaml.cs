using CONATRADEC.ViewModels;
namespace CONATRADEC.Views;

public partial class loginPage : ContentPage
{
    public static string appName = "ConatraCaf� Soil";
    public loginPage()
    {
        InitializeComponent();
        BindingContext = new LoginViewModel();
        Shell.Current.FlyoutBehavior = FlyoutBehavior.Disabled;
    }

    private void UserNameEntryCompleted(object sender, EventArgs e) => PasswordEntry.Focus();

    private void PasswordEntryCompleted(object sender, EventArgs e)
    {
        //Ejecuta el bot�n al presionar Enter
        if(BindingContext is LoginViewModel viewModel)
            viewModel.LoginCommand.Execute(null);
    }  
}