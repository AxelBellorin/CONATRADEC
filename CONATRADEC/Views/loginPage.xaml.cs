using CONATRADEC.ViewModels;
namespace CONATRADEC.Views;

public partial class loginPage : ContentPage
{
	public static string appName = "ConatraCaf� Soil";
	public loginPage()
	{
		InitializeComponent();
		BindingContext = new LoginViewModel();
	}
}