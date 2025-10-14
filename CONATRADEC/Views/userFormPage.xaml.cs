namespace CONATRADEC.Views;
using static CONATRADEC.Models.FormMode;
using CONATRADEC.Models;
using CONATRADEC.ViewModels;

[QueryProperty(nameof(Mode), "Mode")]
[QueryProperty(nameof(User), "User")]
public partial class userFormPage : ContentPage
{
    private UserFormViewModel viewModel= new UserFormViewModel();

    public FormModeSelect Mode 
    { 
        set => viewModel.Mode = value; 
    }

    public UserRequest User
    {
        set => viewModel.User = value;
    }
    public userFormPage()
    {
        try
        {
            Shell.Current.FlyoutBehavior = FlyoutBehavior.Disabled;
            //BindingContext = new UserFormViewModel();
            InitializeComponent();
            BindingContext = viewModel;
        }
        catch(Exception ex)
        {
            Console.WriteLine(ex.Message);
        }  
    }
}