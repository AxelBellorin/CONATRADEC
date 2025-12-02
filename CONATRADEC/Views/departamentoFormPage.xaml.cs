using CONATRADEC.Models;
using CONATRADEC.ViewModels;

namespace CONATRADEC.Views;

[QueryProperty(nameof(Mode), "Mode")]
[QueryProperty(nameof(Pais), "Pais")]
[QueryProperty(nameof(Departamento), "Departamento")]
public partial class departamentoFormPage : ContentPage
{
    private readonly DepartamentoFormViewModel viewModel = new();

    public FormMode.FormModeSelect Mode
    {
        set => viewModel.Mode = value;
    }

    public PaisRequest Pais
    {
        set => viewModel.PaisRequest = value;
    }

    public DepartamentoRequest Departamento
    {
        set => viewModel.Departamento = value;
    }

    public departamentoFormPage()
    {
        Shell.Current.FlyoutBehavior = FlyoutBehavior.Disabled;
        BindingContext = viewModel;
        InitializeComponent();
    }
}
