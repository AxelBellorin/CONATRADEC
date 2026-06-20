using CONATRADEC.ViewModels;

namespace CONATRADEC.Views;

public partial class fuenteNutrientePage : ContentPage
{
    private readonly FuenteNutrienteViewModel viewModel = new();

    public fuenteNutrientePage()
    {
        Shell.Current.FlyoutBehavior = FlyoutBehavior.Disabled;
        BindingContext = viewModel;
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        viewModel.LoadPagePermissions("fuenteNutrientePage");

        await viewModel.LoadFuenteNutriente(true);
    }
}