using CONATRADEC.ViewModels;
namespace CONATRADEC.Views;

public partial class loginPage : ContentPage
{
	public static string appName = "ConatraCafé Soil";
	public loginPage()
	{
		InitializeComponent();
		BindingContext = new LoginViewModel();
	}
}