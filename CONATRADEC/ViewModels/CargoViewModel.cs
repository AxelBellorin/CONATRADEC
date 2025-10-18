using CONATRADEC.Models;
using CONATRADEC.Services;
using Microsoft.Maui.Networking;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace CONATRADEC.ViewModels
{
    public class CargoViewModel : GlobalService
    {
        private ObservableCollection<CargoRP> list = new ObservableCollection<CargoRP>();
        private readonly CargoApiService cargoApiService;

        public Command AddCommand { get; }
        public Command EditCommand { get; }
        public Command DeleteCommand { get; }
        public Command ViewCommand { get; }
        public ObservableCollection<CargoRP> List { get => list; set { list = value; OnPropertyChanged(); } }

        public CargoViewModel()
        {
            cargoApiService = new CargoApiService();
            AddCommand = new Command(async () => await OnAdd());
            EditCommand = new Command<CargoRP>(OnEdit);
            DeleteCommand = new Command<CargoRP>(OnDelete);
            ViewCommand = new Command<CargoRP>(OnView);
        }
        public async Task LoadCargo(bool isBusy)
        {
            IsBusy = isBusy;

            bool tieneInternet = Connectivity.Current.NetworkAccess == NetworkAccess.Internet;

            if (tieneInternet)
            {
                var response = await cargoApiService.GetCargoAsync();

                if (response.Count() != 0)
                {
                    List.Clear();
                    foreach (var cargo in response.OrderBy(r => r.NombreCargo).ToList())
                        List.Add(cargo);
                }
                else
                {
                    await App.Current.MainPage.DisplayAlert("Información", "No se encontraron cargos.", "OK");
                }
            }
            else
            {
                await App.Current.MainPage.DisplayAlert("Error", "No hay conexión a internet. Por favor, verifica tu conexión e inténtalo de nuevo.", "OK");
            }

            IsBusy = false;
        }
        private async Task OnAdd()
        {
            if (IsBusy) return;

            try
            {
                var parameters = new Dictionary<string, object>
                {
                    { "Mode", FormMode.FormModeSelect.Create},
                    { "Cargo", new CargoRequest(new CargoRP()) }
                };
                await Shell.Current.GoToAsync("//CargoFormPage", parameters);
            }
            catch (Exception ex)
            {
                await App.Current.MainPage.DisplayAlert("Error", $"No se pudo conectar al servidor {ex}", "OK");
            }
        }

        private async void OnEdit(CargoRP cargo)
        {
            if (IsBusy) return;

            try
            {
                if (cargo == null) return;

                var parameters = new Dictionary<string, object>
                {
                    { "Mode", FormMode.FormModeSelect.Edit },
                    { "Cargo", new CargoRequest(cargo) }
                };

                await Shell.Current.GoToAsync("//CargoFormPage", parameters);
            }
            catch (Exception ex)
            {
                await App.Current.MainPage.DisplayAlert("Error", $"{ex}", "OK");
            }

        }

        private async void OnDelete(CargoRP cargo)
        {
            if (IsBusy) return;

            IsBusy = true;
            try
            {
                if (cargo == null) return;

                bool confirm = await App.Current.MainPage.DisplayAlert("Eliminar",
                    $"¿Seguro que deseas eliminar al cargo {cargo.NombreCargo}", "Sí", "No");

                if (confirm)
                {
                    var response = await cargoApiService.DeleteCargoAsyn(new CargoRequest(cargo));
                    if (response)
                    {
                        await App.Current.MainPage.DisplayAlert("Éxito", "Cargo eliminado correctamente", "OK");
                        Task.Run(async () => await LoadCargo(true));
                    }
                    else
                        await App.Current.MainPage.DisplayAlert("Error", "El cargo no se pudo eliminar, intente nuevamente", "OK");

                }
                else
                {
                    IsBusy = false;
                }
            }
            catch (Exception ex)
            {
                await App.Current.MainPage.DisplayAlert("Error", $"{ex}", "OK");
            }
        }

        private async void OnView(CargoRP cargo)
        {
            if (IsBusy) return;

            var parameters = new Dictionary<string, object>
            {
                { "Mode", FormMode.FormModeSelect.View},
                { "Cargo", new CargoRequest(cargo) }
            };
            await Shell.Current.GoToAsync("//CargoFormPage", parameters);
        }
    }
}
