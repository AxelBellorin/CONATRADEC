using System;
using Microsoft.Maui.Controls;

namespace CONATRADEC.Views
{
    public partial class ResultadosAnalisisPage : ContentPage
    {
        public ResultadosAnalisisPage()
        {
            InitializeComponent();
            Shell.Current.FlyoutBehavior = FlyoutBehavior.Disabled;
        }

        private async void OnVolverClicked(object sender, EventArgs e)
        {
            try
            {
                // Intentamos navegación por Shell a la página del análisis
                await Shell.Current.GoToAsync("//NuevoAnalisisFormPage");
            }
            catch
            {
                // Fallback: push directo si la ruta no está registrada
                await Navigation.PushAsync(new NuevoAnalisisFormPage());
            }
        }

        // Menú: handlers copiados al code-behind. Sustituye navegación por rutas reales si existen.
        private async void OnMenuReqTapped(object sender, EventArgs e)
        {
            // Ejemplo: navegar a la página de Requerimiento Anual (usa la ruta registrada si la tienes)
            try
            {
                await Shell.Current.GoToAsync("//NuevoAnalisisFormPage");
            }
            catch
            {
                await DisplayAlert("Navegación", "Ir a Requerimiento Anual (implementar ruta)", "OK");
            }
        }

        private async void OnMenuBalanceTapped(object sender, EventArgs e)
        {
            try
            {
                // Intentamos navegar por Shell si se registró la ruta
                await Shell.Current.GoToAsync("//BalanceFormulasPage");
            }
            catch
            {
                // Fallback: instanciar directamente la nueva página
                try
                {
                    await Navigation.PushAsync(new BalanceFormulasPage());
                }
                catch
                {
                    await DisplayAlert("Navegación", "No se pudo navegar a Balance de fórmulas.", "OK");
                }
            }
        }

        private async void OnMenuEnmiendasTapped(object sender, EventArgs e)
        {
            await DisplayAlert("Navegación", "Enmiendas Calcareas (implementar ruta)", "OK");
        }

        private async void OnMenuFertMixtaTapped(object sender, EventArgs e)
        {
            await DisplayAlert("Navegación", "Fertilización Mixta (implementar ruta)", "OK");
        }
    }
}