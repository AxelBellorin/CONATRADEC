namespace CONATRADEC.Views;
using static CONATRADEC.Models.FormMode;
using CONATRADEC.Models;
using CONATRADEC.ViewModels;

[QueryProperty(nameof(Mode), "Mode")]
[QueryProperty(nameof(Rol), "Rol")]
public partial class rolFormPage : ContentPage
{
    private RolFormViewModel viewModel= new RolFormViewModel();
    private RolRequest rolr;

    public FormModeSelect Mode 
    { 
        set => viewModel.Mode = value; 
    }

    public RolRequest Rol
    {
        get => rolr;
        set { viewModel.Rol = value; }

    }
    public rolFormPage()
    {
        try
        {
            Shell.Current.FlyoutBehavior = FlyoutBehavior.Disabled;
            BindingContext = viewModel;
            InitializeComponent();            
        }
        catch(Exception ex)
        {
            Console.WriteLine(ex.Message);
        }  
    }
}