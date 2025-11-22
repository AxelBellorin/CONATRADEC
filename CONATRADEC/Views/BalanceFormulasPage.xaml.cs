using System;
using Microsoft.Maui.Controls;

namespace CONATRADEC.Views
{
	public partial class BalanceFormulasPage : ContentPage
	{
		public BalanceFormulasPage()
		{
			InitializeComponent();
		}

		private async void OnMenuReqTapped(object sender, EventArgs e)
		{
			try
			{
				await Shell.Current.GoToAsync("//NuevoAnalisisFormPage");
			}
			catch (Exception ex)
			{
				await DisplayAlert("Error", $"No se pudo navegar: {ex.Message}", "OK");
			}
		}

		private async void OnMenuBalanceTapped(object sender, EventArgs e)
		{
			try
			{
				await Shell.Current.GoToAsync("//BalanceFormulasPage");
			}
			catch (Exception ex)
			{
				await DisplayAlert("Error", $"No se pudo navegar: {ex.Message}", "OK");
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
