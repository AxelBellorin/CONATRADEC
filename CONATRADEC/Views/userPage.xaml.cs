using CONATRADEC.ViewModels;
namespace CONATRADEC.Views;

public partial class userPage : ContentPage
{
	public userPage()
	{		       
            BindingContext = new UserViewModel();
            Shell.Current.FlyoutBehavior = FlyoutBehavior.Disabled;
            InitializeComponent();
    }
}