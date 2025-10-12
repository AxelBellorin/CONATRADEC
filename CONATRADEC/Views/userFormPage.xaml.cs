namespace CONATRADEC.Views;
using CONATRADEC.ViewModels;
using MiApp.ViewModels;

public partial class userFormPage : ContentPage
{
	public userFormPage()
	{
        Shell.Current.FlyoutBehavior = FlyoutBehavior.Disabled;
		BindingContext = new UserFormViewModel();
        InitializeComponent();
	}
}