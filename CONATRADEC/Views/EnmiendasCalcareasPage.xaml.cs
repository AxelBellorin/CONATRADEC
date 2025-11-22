using Microsoft.Maui.Controls;

namespace CONATRADEC.Views
{
 public partial class EnmiendasCalcareasPage : ContentPage
 {
 public EnmiendasCalcareasPage()
 {
 // Asegurar que InitializeComponent esté correctamente enlazado
 InitializeComponent();
 }

 private async void OnMenuReqTapped(object sender, EventArgs e)
 {
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
 await Shell.Current.GoToAsync("//BalanceFormulasPage");
 }
 catch
 {
 await DisplayAlert("Navegación", "No se pudo navegar a Balance de fórmulas.", "OK");
 }
 }

 private async void OnMenuEnmiendasTapped(object sender, EventArgs e)
 {
 await DisplayAlert("Navegación", "Ya estás en Enmiendas Calcareas.", "OK");
 }

 private async void OnMenuFertMixtaTapped(object sender, EventArgs e)
 {
 await DisplayAlert("Navegación", "Fertilización Mixta (implementar ruta)", "OK");
 }
 }
}