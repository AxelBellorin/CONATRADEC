namespace CONATRADEC.Views;
using static CONATRADEC.Models.FormMode;
using CONATRADEC.Models;
using CONATRADEC.ViewModels;
using CONATRADEC.Services;  

[QueryProperty(nameof(Mode), "Mode")]
[QueryProperty(nameof(User), "User")]
public partial class userFormPage : ContentPage
{
    private UserFormViewModel viewModel = new UserFormViewModel();

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
            BindingContext = viewModel;
            InitializeComponent();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }

    protected override async void OnAppearing()
    {
        try
        {
            base.OnAppearing();
            if (BindingContext is UserFormViewModel vm)
                await vm.InicializarAsync();
        }
        catch (Exception ex)
        {
            _ = GlobalService.MostrarToastAsync("Error" + ex.Message);
        }
    }
}