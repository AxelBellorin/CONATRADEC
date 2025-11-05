using System;
using System.Windows.Input;
using Microsoft.Maui.Controls;

namespace CONATRADEC.Views
{
    public partial class NuevoAnalisisFormPage : ContentPage
    {
        // Comandos usados por FooterTemplate (nombres deben coincidir exactamente con Template.xaml)
        public ICommand goToMainPageCommand { get; }
        public ICommand goToUserPageButtonCommand { get; }
        public ICommand goToRolPageButtonCommand { get; }
        public ICommand goToCargoPageButtonCommand { get; }
        public ICommand goToMatrizPermisosPageButtonCommad { get; } // nombre con typo para emparejar plantilla

        public NuevoAnalisisFormPage()
        {
            InitializeComponent();

            // Inicializar comandos de navegación
            goToMainPageCommand = new Command(async () =>
            {
                try { await Shell.Current.GoToAsync("//MainPage"); }
                catch (Exception ex) { await DisplayAlert("Error", $"No se pudo navegar: {ex.Message}", "OK"); }
            });

            goToUserPageButtonCommand = new Command(async () =>
            {
                try { await Shell.Current.GoToAsync("//UserPage"); }
                catch (Exception ex) { await DisplayAlert("Error", $"No se pudo navegar: {ex.Message}", "OK"); }
            });

            goToRolPageButtonCommand = new Command(async () =>
            {
                try { await Shell.Current.GoToAsync("//RolPage"); }
                catch (Exception ex) { await DisplayAlert("Error", $"No se pudo navegar: {ex.Message}", "OK"); }
            });

            goToCargoPageButtonCommand = new Command(async () =>
            {
                try { await Shell.Current.GoToAsync("//CargoPage"); }
                catch (Exception ex) { await DisplayAlert("Error", $"No se pudo navegar: {ex.Message}", "OK"); }
            });

            // Usamos el nombre con typo igual que en Template.xaml
            goToMatrizPermisosPageButtonCommad = new Command(async () =>
            {
                try { await Shell.Current.GoToAsync("//MatrizPermisosPage"); }
                catch (Exception ex) { await DisplayAlert("Error", $"No se pudo navegar: {ex.Message}", "OK"); }
            });

            // Exponer los comandos al ControlTemplate: el ContentView con x:Name="UserPage"
            // debe heredar el BindingContext que contiene los comandos.
            // Aseguramos que el ContentView use este objeto como BindingContext.
            UserPage.BindingContext = this;
        }

        private async void OnGuardarClicked(object sender, EventArgs e)
        {
            // Lógica de guardado (puedes reemplazar por la tuya)
            await DisplayAlert("Guardado", "El análisis ha sido guardado.", "OK");

            // Navegar atrás usando Shell. Si la página fue abierta con rutas, ajusta según sea necesario.
            try
            {
                await Shell.Current.GoToAsync("..");
            }
            catch
            {
                // fallback: PopAsync si no se usa Shell
                await Navigation.PopAsync();
            }
        }
    }
}