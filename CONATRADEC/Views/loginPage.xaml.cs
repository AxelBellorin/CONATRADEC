using CONATRADEC.ViewModels;
namespace CONATRADEC.Views;

public partial class loginPage : ContentPage
{
    public static string appName = "ConatraCaf� Soil";
    public loginPage()
    {
        Shell.Current.FlyoutBehavior = FlyoutBehavior.Disabled;
        BindingContext = new LoginViewModel();
        InitializeComponent();
    }

    private void UserNameEntryCompleted(object sender, EventArgs e) => PasswordEntry.Focus();

    private void PasswordEntryCompleted(object sender, EventArgs e)
    {
        //Ejecuta el bot�n al presionar Enter
        if(BindingContext is LoginViewModel viewModel)
            viewModel.LoginCommand.Execute(null);
    }  
}