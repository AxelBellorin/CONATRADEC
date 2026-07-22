using CONATRADEC.Models;
using CONATRADEC.Services;
using CONATRADEC.ViewModels;

namespace CONATRADEC.Views;

[QueryProperty(nameof(Pais), "Pais")]
[QueryProperty(nameof(TitlePage), "TitlePage")]
public partial class departamentoPage : ContentPage
{
    private readonly DepartamentoViewModel viewModel = new();

    private bool paginaVisible;
    private bool permisosCargados;

    public string TitlePage
    {
        set => viewModel.TitlePage = value;
    }

    public PaisRequest Pais
    {
        set
        {
            viewModel.PaisRequest =
                value ?? new PaisRequest();

            // Shell puede aplicar QueryProperty después de OnAppearing.
            // Si la página ya está visible, cargamos apenas llegue el PaisId.
            if (paginaVisible && permisosCargados)
                _ = IntentarCargarDepartamentosAsync(true);
        }
    }

    public departamentoPage()
    {
        Shell.Current.FlyoutBehavior =
            FlyoutBehavior.Disabled;

        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        paginaVisible = true;

        if (!PermissionService.Instance.HasRead(
                "departamentoPage"))
        {
            paginaVisible = false;

            await GlobalService.MostrarToastAsync(
                "No tiene permiso para ver departamentos.");

            await Shell.Current.GoToAsync("//MainPage");
            return;
        }

        viewModel.LoadPagePermissions(
            "departamentoPage");

        permisosCargados = true;

        // Si el parámetro ya fue aplicado, carga ahora.
        // Si todavía no llegó, el setter Pais realizará la carga después.
        await IntentarCargarDepartamentosAsync(true);
    }

    protected override void OnDisappearing()
    {
        paginaVisible = false;
        base.OnDisappearing();
    }

    private async Task IntentarCargarDepartamentosAsync(
        bool mostrarIndicadorCarga)
    {
        if (!paginaVisible ||
            !permisosCargados ||
            viewModel.PaisRequest.PaisId <= 0)
        {
            return;
        }

        await viewModel.LoadDepartamento(
            mostrarIndicadorCarga);
    }
}
