using CONATRADEC.Services;
using CONATRADEC.ViewModels;

namespace CONATRADEC.Views;

public partial class paisPage : ContentPage
{
    private readonly PaisViewModel viewModel = new();

    public paisPage()
    {
        Shell.Current.FlyoutBehavior = FlyoutBehavior.Disabled;
        BindingContext = viewModel;
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // VALIDAR PERMISOS DE LECTURA
        if (!PermissionService.Instance.HasRead("paisPage"))
        {
            await DisplayAlert("Acceso denegado",
                               "No tiene permisos para ver países.",
                               "Aceptar");

            await Shell.Current.GoToAsync("//MainPage");
            return;
        }

        // CARGAR PERMISOS EN EL VM
        viewModel.LoadPagePermissions("paisPage");

        // CARGAR DATOS
        await viewModel.LoadPais(true);
    }
}
