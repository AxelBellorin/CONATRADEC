namespace CONATRADEC.Views;
using static CONATRADEC.Models.FormMode;
using CONATRADEC.Models;
using CONATRADEC.ViewModels;

[QueryProperty(nameof(Mode), "Mode")]
[QueryProperty(nameof(Cargo), "Cargo")]
public partial class cargoFormPage : ContentPage
{
    private CargoFormViewModel viewModel= new CargoFormViewModel();

    public FormModeSelect Mode 
    { 
        set => viewModel.Mode = value; 
    }

    public CargoRequest Cargo
    {
        set { viewModel.Cargo = value;}
    }
    public cargoFormPage()
    {
        try
        {
            Shell.Current.FlyoutBehavior = FlyoutBehavior.Disabled;
            InitializeComponent();
            BindingContext = viewModel;
        }
        catch(Exception ex)
        {
            Console.WriteLine(ex.Message);
        }  
    }
}