using CONATRADEC.Services;
using CONATRADEC.ViewModels;
using Microsoft.Maui.Controls;
using System;
using System.Windows.Input;

namespace CONATRADEC.Views
{
    public partial class NuevoAnalisisFormPage : ContentPage
    {
        private readonly NuevoAnalisisFormViewModel viewModel = new();
        public NuevoAnalisisFormPage()
        {
            Shell.Current.FlyoutBehavior = FlyoutBehavior.Disabled;
            BindingContext = viewModel;
            InitializeComponent();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            // VALIDAR PERMISOS DE LECTURA
            if (!PermissionService.Instance.HasRead("NuevoAnalisisFormPage"))
            {
                await DisplayAlert("Acceso denegado",
                                   "No tiene permisos para ver el formulario de análisis de suelo.",
                                   "Aceptar");

                await Shell.Current.GoToAsync("//MainPage");
                return;
            }

            // CARGAR PERMISOS EN EL VM
            viewModel.LoadPagePermissions("NuevoAnalisisFormPage");


            // CARGAR DATOS
            await viewModel.InicializarAsync(true);

        }
    }
}