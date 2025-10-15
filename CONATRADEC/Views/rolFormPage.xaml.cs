namespace CONATRADEC.Views;
using static CONATRADEC.Models.FormMode;
using CONATRADEC.Models;
using CONATRADEC.ViewModels;

[QueryProperty(nameof(Mode), "Mode")]
[QueryProperty(nameof(User), "Rol")]
public partial class rolFormPage : ContentPage
{
    private RolFormViewModel viewModel= new RolFormViewModel();

    public FormModeSelect Mode 
    { 
        set => viewModel.Mode = value; 
    }

    public RolRequest User
    {
        set => viewModel.Rol = value;
    }
    public rolFormPage()
    {
        try
        {
            InitializeComponent();
            Shell.Current.FlyoutBehavior = FlyoutBehavior.Disabled;
            BindingContext = viewModel;                      
        }
        catch(Exception ex)
        {
            Console.WriteLine(ex.Message);
        }  
    }
}