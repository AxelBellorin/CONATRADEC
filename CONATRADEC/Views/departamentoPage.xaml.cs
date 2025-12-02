using CONATRADEC.Models;
using CONATRADEC.Services;
using CONATRADEC.ViewModels;
namespace CONATRADEC.Views;

[QueryProperty(nameof(Pais), "Pais")]   // Recibe el objeto Cargo (CargoRequest)
[QueryProperty(nameof(TitlePage), "TitlePage")]   // Recibe el objeto Cargo (CargoRequest)
public partial class departamentoPage : ContentPage
{
    private DepartamentoViewModel viewModel = new DepartamentoViewModel();

    public string TitlePage
    {
        set => viewModel.TitlePage = value;
    }
     
    public PaisRequest Pais
    {
        set
        {
            viewModel.PaisRequest = value;
            _ = viewModel.LoadDepartamento(true);
        }
    }

    public departamentoPage()
    {
        Shell.Current.FlyoutBehavior = FlyoutBehavior.Disabled;
        BindingContext = viewModel;
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        if (!PermissionService.Instance.HasRead("departamentoPage"))
        {
            _ = GlobalService.MostrarToastAsync("No tiene permiso para ver departamentos.");
            Shell.Current.GoToAsync("//MainPage");
            return;
        }

        // CARGAR PERMISOS EN EL VM
        viewModel.LoadPagePermissions("departamentoPage");
    }
}