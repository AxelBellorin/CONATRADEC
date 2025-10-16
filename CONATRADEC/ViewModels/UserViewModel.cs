using CONATRADEC.Models;
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
    public class UserViewModel : GlobalService
    {
        private ObservableCollection<UserRP> usersList = new ObservableCollection<UserRP>();
        private readonly UserApiService userApiService;

        public Command AddUserCommand { get; }
        public Command EditUserCommand { get; }
        public Command DeleteUserCommand { get; }
        public Command ViewUserCommand { get; }
        public ObservableCollection<UserRP> UsersList { get => usersList; set { usersList = value; OnPropertyChanged(); } }

        public UserViewModel()
        {
            userApiService = new UserApiService();
            AddUserCommand = new Command(async () => await OnAddUser());
            EditUserCommand = new Command<UserRP>(OnEditUser);
            DeleteUserCommand = new Command<UserRP>(OnDeleteUser);
            ViewUserCommand = new Command<UserRP>(OnViewUser);
        }
        public async Task LoadUsers(bool isBusy)
        {
            IsBusy = isBusy;

            var usersresponse = await userApiService.GetUsersAsync();

            if (usersresponse.Users.Count() != 0)
            {
                UsersList.Clear();
                foreach (var user in usersresponse.Users.OrderBy(r => r.FirstName).ToList())
                {
                    UsersList.Add(user);
                }
            }
            else
            {
                await App.Current.MainPage.DisplayAlert("Información", "No se encontraron usuarios.", "OK");
            }
            IsBusy = false;
        }
        private async Task OnAddUser()
        {
            try
            {
                var parameters = new Dictionary<string, object>
                {
                    { "Mode", FormMode.FormModeSelect.Create},
                    { "User", new UserRequest(new UserRP()) }
                };
                await Shell.Current.GoToAsync("//UserFormPage", parameters);
            }
            catch (Exception ex)
            {
                await App.Current.MainPage.DisplayAlert("Error", $"No se pudo conectar al servidor {ex}", "OK");
            }
        }

        private async void OnEditUser(UserRP user)
        {
            try
            {
                if (user == null) return;

                var parameters = new Dictionary<string, object>
                {
                    { "Mode", FormMode.FormModeSelect.Edit },
                    { "User", new UserRequest(user) }
                };

                //await App.Current.MainPage.DisplayAlert("Editar", $"Editar usuario: {user.FirstName}", "OK");
                await Shell.Current.GoToAsync("//UserFormPage", parameters);
            }
            catch (Exception ex)
            {
                await App.Current.MainPage.DisplayAlert("Error", $"{ex}", "OK");
            }

        }

        private async void OnDeleteUser(UserRP user)
        {
            try
            {
                if (user == null) return;

                bool confirm = await App.Current.MainPage.DisplayAlert("Eliminar",
                    $"¿Seguro que deseas eliminar a {user.FirstName + " " + user.LastName}?", "Sí", "No");

                if (confirm)
                    UsersList.Remove(user);

            }
            catch (Exception ex)
            {
                await App.Current.MainPage.DisplayAlert("Error", $"{ex}", "OK");
            }
        }

        private async void OnViewUser(UserRP user)
        {
            var parameters = new Dictionary<string, object>
            {
                { "Mode", FormMode.FormModeSelect.View},
                { "User", new UserRequest(user) }
            };
            await Shell.Current.GoToAsync("//UserFormPage", parameters);
        }
    }
}
