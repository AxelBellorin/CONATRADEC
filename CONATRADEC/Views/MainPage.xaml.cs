namespace CONATRADEC.Views;
using System;
using Microsoft.Maui.Controls;
using CONATRADEC.ViewModels;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        Shell.Current.FlyoutBehavior = FlyoutBehavior.Disabled;
        BindingContext = new MainPageViewModel(); 
        InitializeComponent();
    }

    private async void OnNuevoAnalisisClicked(object sender, EventArgs e)
    {
        // Navega a la página registrada en AppShell.
        // Uso de ruta absoluta para asegurar que Shell resuelve la ShellContent raíz.
        await Shell.Current.GoToAsync("//NuevoAnalisisFormPage");
    }
}