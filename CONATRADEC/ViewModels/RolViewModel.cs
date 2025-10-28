using CONATRADEC.Models;
using CONATRADEC.Services;
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
    public class RolViewModel : GlobalService
    {
        private ObservableCollection<RolResponse> list = new ObservableCollection<RolResponse>();
        private readonly RolApiService rolApiService;

        public Command AddCommand { get; }
        public Command EditCommand { get; }
        public Command DeleteCommand { get; }
        public Command ViewCommand { get; }
        public ObservableCollection<RolResponse> List { get => list; set { list = value; OnPropertyChanged(); } }

        public RolViewModel()
        {
            rolApiService = new RolApiService();
            AddCommand = new Command(async () => await OnAdd());
            EditCommand = new Command<RolResponse>(OnEdit);
            DeleteCommand = new Command<RolResponse>(OnDelete);
            ViewCommand = new Command<RolResponse>(OnView);
        }
        public async Task LoadRol(bool isBusy)
        {
            IsBusy = isBusy;

            var response = await rolApiService.GetRolAsync();

            if (response.Count() != 0)
            {
                List.Clear();
                List = response;
            }
            else
            {
                await App.Current.MainPage.DisplayAlert("Información", "No se encontraron cargos.", "OK");
            }
            IsBusy = false;
        }
        private async Task OnAdd()
        {
            if(IsBusy) return;

            try
            {
                var parameters = new Dictionary<string, object>
                {
                    { "Mode", FormMode.FormModeSelect.Create},
                    { "Rol", new RolRequest(new RolResponse()) }
                };
                await Shell.Current.GoToAsync("//RolFormPage", parameters);
            }
            catch (Exception ex)
            {
                await App.Current.MainPage.DisplayAlert("Error", $"No se pudo conectar al servidor {ex}", "OK");
            }
        }

        private async void OnEdit(RolResponse rol)
        {
            if (IsBusy) return;

            try
            {
                if (rol == null) return;

                var parameters = new Dictionary<string, object>
                {
                    { "Mode", FormMode.FormModeSelect.Edit },
                    { "Rol", new RolRequest(rol) }
                };

                //await App.Current.MainPage.DisplayAlert("Editar", $"Editar usuario: {user.FirstName}", "OK");
                await Shell.Current.GoToAsync("//RolFormPage", parameters);
            }
            catch (Exception ex)
            {
                await App.Current.MainPage.DisplayAlert("Error", $"{ex}", "OK");
            }

        }

        private async void OnDelete(RolResponse rol)
        {
            if (IsBusy) return;

            IsBusy = true;
            try
            {
                if (rol == null) return;

                bool confirm = await App.Current.MainPage.DisplayAlert("Eliminar",
                    $"¿Seguro que deseas eliminar al rol {rol.NombreRol}", "Sí", "No");

                if (confirm)
                {
                    var response = await rolApiService.DeleteRolAsyn(new RolRequest(rol));
                    if (response)
                    {
                        await App.Current.MainPage.DisplayAlert("Éxito", "Rol eliminado correctamente", "OK");
                        Task.Run(async () => await LoadRol(IsBusy));
                    }else
                        await App.Current.MainPage.DisplayAlert("Error", "El rol no se pudo eliminar, intente nuevamente", "OK");
                    
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

        private async void OnView(RolResponse rol)
        {
            if (IsBusy) return;

            var parameters = new Dictionary<string, object>
            {
                { "Mode", FormMode.FormModeSelect.View},
                { "Rol", new RolRequest(rol) }
            };
            await Shell.Current.GoToAsync("//RolFormPage", parameters);
        }
    }
}
