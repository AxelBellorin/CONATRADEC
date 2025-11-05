using System;
using Microsoft.Maui.Controls;

namespace CONATRADEC.Views
{
    public partial class NuevoAnalisisFormPage : ContentPage
    {
        public NuevoAnalisisFormPage()
        {
            InitializeComponent();
        }

        private async void OnGuardarClicked(object sender, EventArgs e)
        {
            // Aquí puedes agregar la lógica de guardar el análisis
            await DisplayAlert("Guardado", "El análisis ha sido guardado.", "OK");
            await Navigation.PopAsync(); // Vuelve atrás después de guardar
        }
    }
}