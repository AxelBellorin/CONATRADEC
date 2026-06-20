using CONATRADEC.Models;
using CONATRADEC.ViewModels;

namespace CONATRADEC.Views;

[QueryProperty(nameof(Mode), "Mode")]
[QueryProperty(nameof(Fuente), "Fuente")]
public partial class fuenteNutrienteFormPage : ContentPage
{
    private readonly FuenteNutrienteFormViewModel viewModel = new();

    public fuenteNutrienteFormPage()
    {
        Shell.Current.FlyoutBehavior = FlyoutBehavior.Disabled;
        BindingContext = viewModel;
        InitializeComponent();
    }

    public FormMode.FormModeSelect Mode
    {
        get => viewModel.Mode;
        set => viewModel.Mode = value;
    }

    public FuenteNutrienteRequest Fuente
    {
        get => viewModel.Fuente;
        set => viewModel.Fuente = value;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await viewModel.InitializeAsync();
    }
}